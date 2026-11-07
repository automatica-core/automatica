using System;
using System.Linq;
using System.Threading.Tasks;
using P3.Driver.ModBusDriver.Master;
using P3.Driver.ModBusDriver.Slave.Tcp;
using Xunit;
using static P3.Driver.ModBus.Tests.Integration.ModBusTcpTestHarness;

namespace P3.Driver.ModBus.Tests.Integration
{
    /// <summary>
    /// Raw MBAP frames against a real <see cref="ModBusSlaveTcpDriver"/>: transaction ids, frames split over several TCP writes,
    /// frame length limits and timeouts
    /// </summary>
    public class ModBusSlaveTcpWireTests
    {
        #region transaction id

        [Theory]
        [InlineData((ushort)0x0000)]
        [InlineData((ushort)0x0001)]
        [InlineData((ushort)0xBEEF)]
        [InlineData((ushort)0xFFFF)]
        public async Task Response_EchoesTheTransactionIdAndProtocolId(ushort transactionId)
        {
            var slave = CreateSlave();
            try
            {
                slave.SetHoldingRegister(DeviceId, 0, 0x0102);
                using var client = await ConnectRaw(slave.LocalPort);

                var response = await SendAndReceive(client, ReadRequest(transactionId, 0x03, 0, 1));

                Assert.Equal(new byte[]
                {
                    (byte)(transactionId >> 8), (byte)transactionId, 0x00, 0x00, 0x00, 0x05, // MBAP, length = unit + fc + count + 2 bytes
                    DeviceId, 0x03, 0x02, 0x01, 0x02
                }, response);
            }
            finally
            {
                slave.Close();
            }
        }

        [Fact]
        public async Task ExceptionResponse_EchoesTheTransactionId()
        {
            var slave = CreateSlave();
            try
            {
                using var client = await ConnectRaw(slave.LocalPort);

                var response = await SendAndReceive(client, ReadRequest(0x4242, 0x03, 1000, 1));

                Assert.Equal(new byte[] { 0x42, 0x42, 0x00, 0x00, 0x00, 0x03, DeviceId, 0x83, 0x02 }, response);
            }
            finally
            {
                slave.Close();
            }
        }

        [Fact]
        public async Task UnsupportedFunction_IsAnsweredWithIllegalFunction()
        {
            var slave = CreateSlave();
            try
            {
                using var client = await ConnectRaw(slave.LocalPort);

                // FC 0x2B (encapsulated interface) is not implemented
                var response = await SendAndReceive(client, ReadRequest(7, 0x2B, 0, 0));

                Assert.Equal(new byte[] { 0x00, 0x07, 0x00, 0x00, 0x00, 0x03, DeviceId, 0x2B | 0x80, 0x01 }, response);

                // still connected
                Assert.Equal(0x03, (await SendAndReceive(client, ReadRequest(8, 0x03, 0, 1)))[7]);
            }
            finally
            {
                slave.Close();
            }
        }

        [Fact]
        public async Task WriteSingleCoil_InvalidValue_IsAnsweredWithIllegalDataValue()
        {
            var slave = CreateSlave();
            try
            {
                using var client = await ConnectRaw(slave.LocalPort);

                // FC05 only allows 0x0000 and 0xFF00
                var response = await SendAndReceive(client, Mbap(3, DeviceId, 0x05, 0x00, 0x01, 0x12, 0x34));

                Assert.Equal(new byte[] { 0x00, 0x03, 0x00, 0x00, 0x00, 0x03, DeviceId, 0x85, 0x03 }, response);
                Assert.False(slave.GetCoil(DeviceId, 1));
            }
            finally
            {
                slave.Close();
            }
        }

        [Fact]
        public async Task WriteSingleCoil_Response_EchoesTheRequest()
        {
            var slave = CreateSlave();
            try
            {
                using var client = await ConnectRaw(slave.LocalPort);
                var request = Mbap(0x0A0B, DeviceId, 0x05, 0x00, 0x04, 0xFF, 0x00);

                var response = await SendAndReceive(client, request);

                Assert.Equal(request, response);
                Assert.True(slave.GetCoil(DeviceId, 4));
            }
            finally
            {
                slave.Close();
            }
        }

        [Fact]
        public async Task WriteSingleRegister_Response_EchoesTheRequest()
        {
            var slave = CreateSlave();
            try
            {
                using var client = await ConnectRaw(slave.LocalPort);
                var request = Mbap(0x0C0D, DeviceId, 0x06, 0x00, 0x02, 0xAB, 0xCD);

                var response = await SendAndReceive(client, request);

                Assert.Equal(request, response);
                Assert.Equal(0xABCD, slave.GetHoldingRegister(DeviceId, 2));
            }
            finally
            {
                slave.Close();
            }
        }

        [Fact]
        public async Task WriteMultipleRegisters_Response_HasStartAndQuantity()
        {
            var slave = CreateSlave();
            try
            {
                using var client = await ConnectRaw(slave.LocalPort);

                var response = await SendAndReceive(client, Mbap(0x0101, DeviceId, 0x10, 0x00, 0x03, 0x00, 0x02, 0x04, 0x11, 0x22, 0x33, 0x44));

                Assert.Equal(new byte[] { 0x01, 0x01, 0x00, 0x00, 0x00, 0x06, DeviceId, 0x10, 0x00, 0x03, 0x00, 0x02 }, response);
                Assert.Equal(new ushort[] { 0x1122, 0x3344 }, slave.GetHoldingRegisters(DeviceId, 3, 2).ToArray());
            }
            finally
            {
                slave.Close();
            }
        }

        [Fact]
        public async Task WriteMultipleRegisters_ByteCountBeyondTheFrame_ClosesTheConnectionWithoutWriting()
        {
            var slave = CreateSlave();
            try
            {
                using var client = await ConnectRaw(slave.LocalPort);

                // byte count 0x40 but only 4 data bytes in the frame
                await client.GetStream().WriteAsync(Mbap(1, DeviceId, 0x10, 0x00, 0x00, 0x00, 0x02, 0x40, 0x11, 0x22, 0x33, 0x44));

                Assert.True(await IsClosedByServer(client));
                Assert.Equal(0, slave.GetHoldingRegister(DeviceId, 0));

                // other masters are still served
                using var other = await ConnectRaw(slave.LocalPort);
                Assert.Equal(0x03, (await SendAndReceive(other, ReadRequest(2, 0x03, 0, 1)))[7]);
            }
            finally
            {
                slave.Close();
            }
        }

        [Fact]
        public async Task Master_UsesANewTransactionIdForEveryRequest()
        {
            var slave = CreateSlave();
            using var proxy = new ModBusTcpProxy(slave.LocalPort);
            var master = CreateMaster(proxy.Port);
            try
            {
                for (var i = 0; i < 5; i++)
                {
                    Assert.IsType<ModBusRegisterValueReturn>(await master.ReadRegisters(DeviceId, 0, 1));
                }
                Assert.Equal(ModBusRequestStatus.Success, (await master.WriteHoldingRegister(DeviceId, 0, 1)).ModBusRequestStatus);
                Assert.Equal(ModBusRequestStatus.Success, (await master.WriteMultipleHoldingRegisters(DeviceId, 0, new ushort[] { 1, 2 })).ModBusRequestStatus);

                var ids = proxy.RequestTransactionIds;
                Assert.Equal(7, ids.Count);
                Assert.Equal(ids.Count, ids.Distinct().Count());
            }
            finally
            {
                await master.Stop();
                slave.Close();
            }
        }

        [Fact]
        public async Task Master_LateDuplicateResponseOfThePreviousRequest_IsDropped()
        {
            var slave = CreateSlave();
            using var proxy = new ModBusTcpProxy(slave.LocalPort) { DuplicateResponses = true };
            var master = CreateMaster(proxy.Port);
            try
            {
                slave.SetHoldingRegister(DeviceId, 0, 1000);
                slave.SetHoldingRegister(DeviceId, 1, 2000);
                slave.SetHoldingRegister(DeviceId, 2, 3000);

                // every answer arrives twice, the copy has the transaction id of the previous request and must not be taken as the answer
                var first = Assert.IsType<ModBusRegisterValueReturn>(await master.ReadRegisters(DeviceId, 0, 1));
                var second = Assert.IsType<ModBusRegisterValueReturn>(await master.ReadRegisters(DeviceId, 1, 1));
                var third = Assert.IsType<ModBusRegisterValueReturn>(await master.ReadRegisters(DeviceId, 2, 1));

                Assert.Equal((ushort)1000, first.Data[0]);
                Assert.Equal((ushort)2000, second.Data[0]);
                Assert.Equal((ushort)3000, third.Data[0]);
            }
            finally
            {
                await master.Stop();
                slave.Close();
            }
        }

        #endregion

        #region partial and pipelined frames

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(5)]
        [InlineData(7)]
        public async Task SlowFrame_SplitOverSeveralTcpWrites_IsAnswered(int chunkSize)
        {
            var slave = CreateSlave();
            try
            {
                slave.SetHoldingRegister(DeviceId, 1, 0x5A5A);
                using var client = await ConnectRaw(slave.LocalPort);
                var request = ReadRequest(0x0102, 0x03, 1, 1);

                foreach (var chunk in request.Chunk(chunkSize))
                {
                    await client.GetStream().WriteAsync(chunk);
                    await client.GetStream().FlushAsync();
                    await Task.Delay(15);
                }

                var response = await ReadResponse(client);
                Assert.Equal(new byte[] { 0x01, 0x02, 0x00, 0x00, 0x00, 0x05, DeviceId, 0x03, 0x02, 0x5A, 0x5A }, response);
            }
            finally
            {
                slave.Close();
            }
        }

        [Fact]
        public async Task SlowFrame_SplitInsideTheLengthField_IsAnswered()
        {
            var slave = CreateSlave();
            try
            {
                using var client = await ConnectRaw(slave.LocalPort);
                var request = Mbap(9, DeviceId, 0x06, 0x00, 0x00, 0x00, 0x2A);

                // first part ends between the two length bytes, the rest follows in two more writes
                await client.GetStream().WriteAsync(request.AsMemory(0, 5));
                await Task.Delay(50);
                await client.GetStream().WriteAsync(request.AsMemory(5, 3));
                await Task.Delay(50);
                await client.GetStream().WriteAsync(request.AsMemory(8));

                Assert.Equal(request, await ReadResponse(client));
                Assert.Equal(42, slave.GetHoldingRegister(DeviceId, 0));
            }
            finally
            {
                slave.Close();
            }
        }

        [Fact]
        public async Task PipelinedRequests_InOneTcpWrite_AreAllAnsweredInOrder()
        {
            var slave = CreateSlave();
            try
            {
                slave.SetHoldingRegister(DeviceId, 0, 10);
                slave.SetHoldingRegister(DeviceId, 1, 20);
                using var client = await ConnectRaw(slave.LocalPort);

                var requests = ReadRequest(1, 0x03, 0, 1)
                    .Concat(Mbap(2, DeviceId, 0x06, 0x00, 0x02, 0x00, 0x1E))
                    .Concat(ReadRequest(3, 0x03, 1, 2))
                    .ToArray();
                await client.GetStream().WriteAsync(requests);

                var first = await ReadResponse(client);
                var second = await ReadResponse(client);
                var third = await ReadResponse(client);

                Assert.Equal(new byte[] { 0x00, 0x01 }, first.Take(2));
                Assert.Equal(new byte[] { 0x00, 0x0A }, first.Skip(9));
                Assert.Equal(new byte[] { 0x00, 0x02 }, second.Take(2));
                Assert.Equal(0x06, second[7]);
                Assert.Equal(new byte[] { 0x00, 0x03 }, third.Take(2));
                Assert.Equal(new byte[] { 0x00, 0x14, 0x00, 0x1E }, third.Skip(9));
            }
            finally
            {
                slave.Close();
            }
        }

        [Fact]
        public async Task StartedFrame_NotCompletedWithinTheFrameTimeout_ClosesTheConnection()
        {
            var slave = CreateSlave(c => c.FrameTimeout = TimeSpan.FromMilliseconds(200));
            try
            {
                using var client = await ConnectRaw(slave.LocalPort);
                var request = ReadRequest(1, 0x03, 0, 1);

                // header and a part of the pdu, the rest never comes
                await client.GetStream().WriteAsync(request.AsMemory(0, 8));

                Assert.True(await IsClosedByServer(client));
                await WaitUntil(() => slave.ConnectionCount == 0);
            }
            finally
            {
                slave.Close();
            }
        }

        [Fact]
        public async Task StartedHeader_NotCompletedWithinTheFrameTimeout_ClosesTheConnection()
        {
            var slave = CreateSlave(c => c.FrameTimeout = TimeSpan.FromMilliseconds(200));
            try
            {
                using var client = await ConnectRaw(slave.LocalPort);

                await client.GetStream().WriteAsync(new byte[] { 0x00, 0x01, 0x00 });

                Assert.True(await IsClosedByServer(client));
            }
            finally
            {
                slave.Close();
            }
        }

        [Fact]
        public async Task SlowFrame_WithinTheFrameTimeout_IsNotClosed()
        {
            var slave = CreateSlave(c => c.FrameTimeout = TimeSpan.FromMilliseconds(1500));
            try
            {
                using var client = await ConnectRaw(slave.LocalPort);
                var request = ReadRequest(5, 0x03, 0, 1);

                await client.GetStream().WriteAsync(request.AsMemory(0, 6));
                await Task.Delay(150);
                await client.GetStream().WriteAsync(request.AsMemory(6));

                var response = await ReadResponse(client);
                Assert.Equal(0x03, response[7]);
            }
            finally
            {
                slave.Close();
            }
        }

        #endregion

        #region frame length

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(ModBusSlaveTcpDriver.MaxFrameLength + 1)]
        [InlineData(300)]
        [InlineData(0x7FFF)]
        [InlineData(0xFFFF)]
        public async Task InvalidFrameLength_ClosesTheConnectionWithoutReadingThePayload(int length)
        {
            var slave = CreateSlave();
            try
            {
                using var client = await ConnectRaw(slave.LocalPort);

                await client.GetStream().WriteAsync(new byte[] { 0x00, 0x01, 0x00, 0x00, (byte)(length >> 8), (byte)length });

                Assert.True(await IsClosedByServer(client));
                await WaitUntil(() => slave.ConnectionCount == 0);
            }
            finally
            {
                slave.Close();
            }
        }

        [Theory]
        [InlineData(2)]
        [InlineData(5)]
        public async Task FrameShorterThanARequest_ClosesTheConnection(int length)
        {
            var slave = CreateSlave();
            try
            {
                using var client = await ConnectRaw(slave.LocalPort);

                var frame = new byte[] { 0x00, 0x01, 0x00, 0x00, 0x00, (byte)length }
                    .Concat(new byte[] { DeviceId, 0x03, 0x00, 0x00, 0x00 }.Take(length))
                    .ToArray();
                await client.GetStream().WriteAsync(frame);

                Assert.True(await IsClosedByServer(client));
            }
            finally
            {
                slave.Close();
            }
        }

        [Fact]
        public async Task MaximumFrameLength_IsAccepted()
        {
            var slave = CreateSlave();
            try
            {
                slave.SetHoldingRegister(DeviceId, 0, 0x0F0F);
                using var client = await ConnectRaw(slave.LocalPort);

                // unit + read request + padding up to the 254 bytes allowed by the MBAP length field
                var pdu = new byte[ModBusSlaveTcpDriver.MaxFrameLength];
                pdu[0] = DeviceId;
                pdu[1] = 0x03;
                pdu[5] = 0x01;
                var response = await SendAndReceive(client, Mbap(0x0FF0, pdu));

                Assert.Equal(new byte[] { 0x0F, 0xF0, 0x00, 0x00, 0x00, 0x05, DeviceId, 0x03, 0x02, 0x0F, 0x0F }, response);
            }
            finally
            {
                slave.Close();
            }
        }

        [Fact]
        public async Task InvalidFrameLength_OnOneConnection_DoesNotAffectOtherConnections()
        {
            var slave = CreateSlave();
            try
            {
                using var good = await ConnectRaw(slave.LocalPort);
                using var bad = await ConnectRaw(slave.LocalPort);
                await SendAndReceive(good, ReadRequest(1, 0x03, 0, 1));

                await bad.GetStream().WriteAsync(new byte[] { 0x00, 0x01, 0x00, 0x00, 0x10, 0x00 });
                Assert.True(await IsClosedByServer(bad));

                Assert.Equal(0x03, (await SendAndReceive(good, ReadRequest(2, 0x03, 0, 1)))[7]);
            }
            finally
            {
                slave.Close();
            }
        }

        #endregion

        #region idle timeout

        [Fact]
        public async Task IdleTimeout_ConnectionWithoutRequests_IsClosed()
        {
            var slave = CreateSlave(c => c.IdleTimeout = TimeSpan.FromMilliseconds(150));
            try
            {
                using var client = await ConnectRaw(slave.LocalPort);
                await WaitUntil(() => slave.ConnectionCount == 1);

                Assert.True(await IsClosedByServer(client));
                await WaitUntil(() => slave.ConnectionCount == 0);
            }
            finally
            {
                slave.Close();
            }
        }

        [Fact]
        public async Task IdleTimeout_AfterTheLastRequest_ClosesTheConnection()
        {
            var slave = CreateSlave(c => c.IdleTimeout = TimeSpan.FromMilliseconds(150));
            try
            {
                using var client = await ConnectRaw(slave.LocalPort);
                Assert.Equal(0x03, (await SendAndReceive(client, ReadRequest(1, 0x03, 0, 1)))[7]);

                Assert.True(await IsClosedByServer(client));
            }
            finally
            {
                slave.Close();
            }
        }

        [Fact]
        public async Task IdleTimeout_IsRestartedByEveryRequest()
        {
            var slave = CreateSlave(c => c.IdleTimeout = TimeSpan.FromMilliseconds(600));
            try
            {
                using var client = await ConnectRaw(slave.LocalPort);

                // 8 requests 150 ms apart: the connection lives longer than the idle timeout in total
                for (ushort i = 0; i < 8; i++)
                {
                    Assert.Equal(0x03, (await SendAndReceive(client, ReadRequest(i, 0x03, 0, 1)))[7]);
                    await Task.Delay(150);
                }
            }
            finally
            {
                slave.Close();
            }
        }

        [Fact]
        public void Config_Defaults_AreLongEnoughForSlowPollIntervals()
        {
            var config = new ModBusSlaveTcpConfig();

            Assert.Equal(TimeSpan.FromMinutes(2), config.IdleTimeout);
            Assert.Equal(TimeSpan.FromSeconds(10), config.FrameTimeout);
            Assert.Equal(16, config.MaxConnections);
            Assert.False(config.ReadOnly);
        }

        #endregion
    }
}
