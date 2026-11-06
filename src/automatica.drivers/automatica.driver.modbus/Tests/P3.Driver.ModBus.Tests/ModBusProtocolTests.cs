using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.TelegramMonitor;
using Microsoft.Extensions.Logging.Abstractions;
using P3.Driver.ModBusDriver;
using P3.Driver.ModBusDriver.Master;
using P3.Driver.ModBusDriver.Master.Rtu;
using P3.Driver.ModBusDriver.Master.Tcp;
using P3.Driver.ModBusDriver.Slave;
using P3.Driver.ModBusDriver.Slave.Tcp;
using RJCP.IO.Ports;
using Xunit;

namespace P3.Driver.ModBus.Tests
{
    public class ModBusProtocolTests
    {
        #region RTU

        [Theory]
        [InlineData("n", Parity.None)]
        [InlineData("NoParity", Parity.None)]
        [InlineData("none", Parity.None)]
        [InlineData("e", Parity.Even)]
        [InlineData("ep", Parity.Even)]
        [InlineData("even", Parity.Even)]
        [InlineData("EventParity", Parity.Even)]
        [InlineData("EvenParity", Parity.Even)]
        [InlineData("o", Parity.Odd)]
        [InlineData("ODD", Parity.Odd)]
        [InlineData("OddParity", Parity.Odd)]
        [InlineData("m", Parity.Mark)]
        [InlineData("space", Parity.Space)]
        public void Rtu_Parity_IsMapped(string value, Parity expected)
        {
            Assert.Equal(expected, ModBusMasterRtuDriver.FromString(value));
        }

        [Theory]
        [InlineData(1d, StopBits.One)]
        [InlineData(1.5d, StopBits.One5)]
        [InlineData(2d, StopBits.Two)]
        public void Rtu_StopBits_IsMapped(double value, StopBits expected)
        {
            Assert.Equal(expected, ModBusMasterRtuDriver.FromString(value));
        }

        private static byte[] WithCrc(params byte[] frame)
        {
            return frame.Concat(ModbusUtility.CalculateCrc(frame)).ToArray();
        }

        /// <summary>
        /// Returns at most <c>chunkSize</c> bytes per read and 0 (like a serial read timeout) when nothing is left
        /// </summary>
        private class ChunkedStream : Stream
        {
            private readonly Queue<byte> _data;
            private readonly int _chunkSize;

            public ChunkedStream(byte[] data, int chunkSize)
            {
                _data = new Queue<byte>(data);
                _chunkSize = chunkSize;
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                var n = 0;
                while (n < count && n < _chunkSize && _data.Count > 0)
                {
                    buffer[offset + n++] = _data.Dequeue();
                }
                return n;
            }

            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => throw new NotSupportedException();
            public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
            public override void Flush() { }
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        }

        [Fact]
        public async Task Rtu_ReadsFragmentedFrameCompletely()
        {
            var response = WithCrc(0x01, 0x03, 0x04, 0x00, 0x7B, 0xFF, 0xFB);
            var stream = new ChunkedStream(response, 1);

            using var cts = new CancellationTokenSource(2000);
            var frame = await ModBusFrameReader.ReadRtuFrameAsync(stream, ModBusFunction.ReadHoldingRegisters, 2, cts.Token);

            Assert.Equal(response, frame);
        }

        [Fact]
        public async Task Rtu_ReturnsExceptionReply()
        {
            // illegal data address for a 2 register read, shorter than the expected 9 bytes
            var response = WithCrc(0x01, 0x83, 0x02);
            var stream = new ChunkedStream(response, 2);

            using var cts = new CancellationTokenSource(2000);
            var frame = await ModBusFrameReader.ReadRtuFrameAsync(stream, ModBusFunction.ReadHoldingRegisters, 2, cts.Token);

            Assert.Equal(5, frame.Length);
            Assert.Equal(0x83, frame[1]);
            Assert.Equal((byte)ModBusExceptionCode.IllegalDataAddress, frame[2]);
        }

        [Fact]
        public async Task Rtu_IncompleteFrame_TimesOut()
        {
            var response = WithCrc(0x01, 0x03, 0x04, 0x00, 0x7B, 0xFF, 0xFB).Take(5).ToArray();
            var stream = new ChunkedStream(response, 3);

            using var cts = new CancellationTokenSource(300);
            await Assert.ThrowsAsync<TimeoutException>(() =>
                ModBusFrameReader.ReadRtuFrameAsync(stream, ModBusFunction.ReadHoldingRegisters, 2, cts.Token));
        }

        [Fact]
        public async Task Rtu_InvalidCrc_Throws()
        {
            var response = WithCrc(0x01, 0x06, 0x00, 0x01, 0x00, 0x03);
            response[^1] ^= 0xFF;

            using var cts = new CancellationTokenSource(2000);
            await Assert.ThrowsAsync<InvalidDataException>(() =>
                ModBusFrameReader.ReadRtuFrameAsync(new ChunkedStream(response, 8), ModBusFunction.WriteSingleRegister, 1, cts.Token));
        }

        #endregion

        #region TCP master against a fake slave

        private static byte[] Mbap(ushort transactionId, params byte[] unitAndPdu)
        {
            var frame = new List<byte>
            {
                (byte)(transactionId >> 8), (byte)transactionId, 0, 0,
                (byte)(unitAndPdu.Length >> 8), (byte)unitAndPdu.Length
            };
            frame.AddRange(unitAndPdu);
            return frame.ToArray();
        }

        /// <summary>
        /// Accepts one connection, for each request calls <paramref name="answer"/> and sends the returned chunks with a small delay in between
        /// </summary>
        private static async Task<(TcpListener listener, Task serverTask, List<byte[]> requests)> StartFakeSlave(
            Func<byte[], ushort, IEnumerable<byte[]>> answer, int requestCount)
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var requests = new List<byte[]>();

            var serverTask = Task.Run(async () =>
            {
                using var client = await listener.AcceptTcpClientAsync();
                var stream = client.GetStream();
                for (var i = 0; i < requestCount; i++)
                {
                    var header = new byte[6];
                    await stream.ReadExactlyAsync(header);
                    var body = new byte[(header[4] << 8) | header[5]];
                    await stream.ReadExactlyAsync(body);
                    var request = header.Concat(body).ToArray();
                    lock (requests)
                    {
                        requests.Add(request);
                    }

                    foreach (var chunk in answer(request, (ushort)((header[0] << 8) | header[1])))
                    {
                        await stream.WriteAsync(chunk);
                        await stream.FlushAsync();
                        await Task.Delay(20);
                    }
                }
                await Task.Delay(200);
            });

            await Task.Yield();
            return (listener, serverTask, requests);
        }

        private static ModBusMasterTcpDriver CreateMaster(TcpListener listener)
        {
            var master = new ModBusMasterTcpDriver(new ModBusMasterTcpConfig
            {
                IpAddress = IPAddress.Loopback,
                Port = (short)((IPEndPoint)listener.LocalEndpoint).Port,
                Timeout = 2000
            }, new EmptyTelegramMonitorInstance());
            Assert.True(master.Open());
            return master;
        }

        [Fact]
        public async Task Tcp_DropsStaleResponse_AndReadsFragmentedFrame()
        {
            var (listener, serverTask, _) = await StartFakeSlave((request, tid) =>
            {
                var stale = Mbap((ushort)(tid - 1), 0x01, 0x03, 0x04, 0xDE, 0xAD, 0xBE, 0xEF);
                var match = Mbap(tid, 0x01, 0x03, 0x04, 0x00, 0x7B, 0xFF, 0xFB);
                // stale answer of an earlier request first, then the matching one in 3 fragments
                return new[] { stale, match.Take(3).ToArray(), match.Skip(3).Take(5).ToArray(), match.Skip(8).ToArray() };
            }, 1);

            try
            {
                var master = CreateMaster(listener);
                var result = await master.ReadRegisters(1, 0, 2);

                var registers = Assert.IsType<ModBusRegisterValueReturn>(result);
                Assert.Equal(new ushort[] { 0x007B, 0xFFFB }, registers.Data.ToArray());
                await master.Stop();
            }
            finally
            {
                listener.Stop();
            }
            await serverTask;
        }

        [Fact]
        public async Task Tcp_ExceptionReply_IsReturned()
        {
            var (listener, serverTask, _) = await StartFakeSlave((request, tid) =>
                new[] { Mbap(tid, 0x01, 0x90, (byte)ModBusExceptionCode.IllegalDataValue) }, 1);

            try
            {
                var master = CreateMaster(listener);
                var result = await master.WriteMultipleHoldingRegisters(1, 10, new ushort[] { 1, 2 });

                var exception = Assert.IsType<ModBusExceptionReturn>(result);
                Assert.Equal(ModBusExceptionCode.IllegalDataValue, exception.Exception);
                await master.Stop();
            }
            finally
            {
                listener.Stop();
            }
            await serverTask;
        }

        [Fact]
        public async Task Tcp_ClosedConnection_MarksDisconnected()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var serverTask = Task.Run(async () =>
            {
                using var client = await listener.AcceptTcpClientAsync();
                var buffer = new byte[12];
                await client.GetStream().ReadExactlyAsync(buffer);
                // close without answering
            });

            try
            {
                var master = CreateMaster(listener);
                await Assert.ThrowsAsync<IOException>(() => master.ReadRegisters(1, 0, 1));
                Assert.False(master.Connected);
                await master.Stop();
            }
            finally
            {
                listener.Stop();
            }
            await serverTask;
        }

        [Fact]
        public async Task Tcp_ReadCoils_MoreThan16()
        {
            var (listener, serverTask, _) = await StartFakeSlave((request, tid) =>
                new[] { Mbap(tid, 0x01, 0x01, 0x03, 0xFF, 0x00, 0x0A) }, 1);

            try
            {
                var master = CreateMaster(listener);
                var result = await master.ReadCoils(1, 0, 20);

                var coils = Assert.IsType<ModBusIoStateValueReturn>(result);
                var expected = Enumerable.Repeat(true, 8).Concat(Enumerable.Repeat(false, 8))
                    .Concat(new[] { false, true, false, true }).ToArray();
                Assert.Equal(expected, coils.Data.ToArray());
                await master.Stop();
            }
            finally
            {
                listener.Stop();
            }
            await serverTask;
        }

        [Fact]
        public async Task Tcp_WriteMultipleCoils_EncodesBytes()
        {
            var (listener, serverTask, requests) = await StartFakeSlave((request, tid) =>
                new[] { Mbap(tid, 0x01, 0x0F, request[8], request[9], request[10], request[11]) }, 2);

            try
            {
                var master = CreateMaster(listener);

                // 8 coils -> exactly 1 byte
                var values = new[] { true, false, true, false, true, false, true, false };
                Assert.Equal(ModBusRequestStatus.Success, (await master.WriteMultipleCoils(1, 0, values)).ModBusRequestStatus);

                // 17 coils -> 3 bytes, coil 8 is bit 0 of byte 2, coil 16 is bit 0 of byte 3
                var values17 = new bool[17];
                values17[8] = true;
                values17[16] = true;
                Assert.Equal(ModBusRequestStatus.Success, (await master.WriteMultipleCoils(1, 0, values17)).ModBusRequestStatus);

                await master.Stop();
            }
            finally
            {
                listener.Stop();
            }
            await serverTask;

            // mbap(6) unit(1) fc(1) addr(2) qty(2) byteCount(1) data
            Assert.Equal(1, requests[0][12]);
            Assert.Equal(14, requests[0].Length);
            Assert.Equal(0x55, requests[0][13]);

            Assert.Equal(3, requests[1][12]);
            Assert.Equal(16, requests[1].Length);
            Assert.Equal(new byte[] { 0x00, 0x01, 0x01 }, requests[1].Skip(13).ToArray());
        }

        #endregion

        #region TCP slave

        private static int GetFreePort()
        {
            var l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();
            var port = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();
            return port;
        }

        private static ModBusSlaveTcpDriver CreateSlave(int port)
        {
            var slave = new ModBusSlaveTcpDriver(new ModBusSlaveTcpConfig
            {
                Port = (ushort)port,
                IgnoreDeviceId = false,
                DeviceIds = new List<int> { 1 }
            }, new EmptyTelegramMonitorInstance(), NullLogger.Instance);

            for (ushort i = 0; i < 4; i++)
            {
                slave.InitHoldingRegister(1, i, 0);
                slave.InitCoil(1, i, false);
            }

            return slave;
        }

        [Fact]
        public void Slave_ReadResponse_HasNoTrailingBytes()
        {
            var slave = CreateSlave(GetFreePort());
            slave.SetHoldingRegister(1, 0, 0x1234);

            var header = new byte[] { 0x00, 0x07, 0x00, 0x00, 0x00, 0x06 };
            var request = new byte[] { 0x01, 0x03, 0x00, 0x00, 0x00, 0x01 };
            var response = slave.GetResponseData(1, 0x03, 0, 1, request, header).ToArray();

            // mbap(6) + unit, fc, byte count, 2 data bytes
            Assert.Equal(new byte[] { 0x00, 0x07, 0x00, 0x00, 0x00, 0x05, 0x01, 0x03, 0x02, 0x12, 0x34 }, response);
        }

        [Fact]
        public void Slave_RaisesChangedEvents_ForAllWriteFunctions()
        {
            var slave = CreateSlave(GetFreePort());
            var holding = new List<RegisterChangedEventArgs>();
            var coils = new List<RegisterChangedEventArgs>();
            slave.HoldingRegisterChanged += (_, e) => holding.Add(e);
            slave.CoilRegisterChanged += (_, e) => coils.Add(e);

            var header = new byte[] { 0x00, 0x01, 0x00, 0x00, 0x00, 0x06 };

            // FC06 register 1 = 5
            slave.GetResponseData(1, 0x06, 1, 5, new byte[] { 0x01, 0x06, 0x00, 0x01, 0x00, 0x05 }, header);
            // FC16 register 2..3 = 7, 8
            slave.GetResponseData(1, 0x10, 2, 2, new byte[] { 0x01, 0x10, 0x00, 0x02, 0x00, 0x02, 0x04, 0x00, 0x07, 0x00, 0x08 }, header);
            // FC05 coil 0 = on
            slave.GetResponseData(1, 0x05, 0, 0xFF00, new byte[] { 0x01, 0x05, 0x00, 0x00, 0xFF, 0x00 }, header);
            // FC15 coils 1..2 = on, off
            slave.GetResponseData(1, 0x0F, 1, 2, new byte[] { 0x01, 0x0F, 0x00, 0x01, 0x00, 0x02, 0x01, 0x01 }, header);

            Assert.Equal(5, slave.GetHoldingRegister(1, 1));
            Assert.Equal(new ushort[] { 7, 8 }, slave.GetHoldingRegisters(1, 2, 2).ToArray());
            Assert.True(slave.GetCoil(1, 0));
            Assert.True(slave.GetCoil(1, 1));
            Assert.False(slave.GetCoil(1, 2));

            Assert.Equal(2, holding.Count);
            Assert.Equal((1, (ushort)1, (ushort)1), (holding[0].DeviceId, holding[0].StartIndex, holding[0].Length));
            Assert.Equal((1, (ushort)2, (ushort)2), (holding[1].DeviceId, holding[1].StartIndex, holding[1].Length));

            Assert.Equal(2, coils.Count);
            Assert.Equal((1, (ushort)0, (ushort)1), (coils[0].DeviceId, coils[0].StartIndex, coils[0].Length));
            Assert.Equal((1, (ushort)1, (ushort)2), (coils[1].DeviceId, coils[1].StartIndex, coils[1].Length));
        }

        [Fact]
        public async Task MasterAndSlave_WriteMultipleRegisters_RoundTrip()
        {
            var port = GetFreePort();
            var slave = CreateSlave(port);
            var changed = new TaskCompletionSource<RegisterChangedEventArgs>(TaskCreationOptions.RunContinuationsAsynchronously);
            slave.HoldingRegisterChanged += (_, e) => changed.TrySetResult(e);
            slave.Open();

            var master = new ModBusMasterTcpDriver(new ModBusMasterTcpConfig
            {
                IpAddress = IPAddress.Loopback,
                Port = (short)port,
                Timeout = 2000
            }, new EmptyTelegramMonitorInstance());

            try
            {
                Assert.True(master.Open());

                var write = await master.WriteMultipleHoldingRegisters(1, 0, new ushort[] { 0xFFFF, 0xFFFB });
                Assert.Equal(ModBusRequestStatus.Success, write.ModBusRequestStatus);

                var args = await changed.Task.WaitAsync(TimeSpan.FromSeconds(2));
                Assert.Equal((ushort)0, args.StartIndex);
                Assert.Equal((ushort)2, args.Length);

                var read = Assert.IsType<ModBusRegisterValueReturn>(await master.ReadRegisters(1, 0, 2));
                Assert.Equal(new ushort[] { 0xFFFF, 0xFFFB }, read.Data.ToArray());

                // a read returns exactly the requested registers, no trailing bytes in the frame
                var read1 = Assert.IsType<ModBusRegisterValueReturn>(await master.ReadRegisters(1, 1, 1));
                Assert.Equal(new ushort[] { 0xFFFB }, read1.Data.ToArray());
            }
            finally
            {
                await master.Stop();
                slave.Close();
            }
        }

        #endregion
    }
}
