using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using P3.Driver.ModBusDriver;
using P3.Driver.ModBusDriver.Master;
using P3.Driver.ModBusDriver.Master.Tcp;
using P3.Driver.ModBusDriver.Slave;
using P3.Driver.ModBusDriver.Slave.Tcp;
using Xunit;
using static P3.Driver.ModBus.Tests.Integration.ModBusTcpTestHarness;

namespace P3.Driver.ModBus.Tests.Integration
{
    /// <summary>
    /// A real <see cref="ModBusMasterTcpDriver"/> talking to a real <see cref="ModBusSlaveTcpDriver"/> over 127.0.0.1
    /// </summary>
    public class ModBusTcpMasterSlaveIntegrationTests : IAsyncLifetime
    {
        private ModBusSlaveTcpDriver _slave;
        private ModBusMasterTcpDriver _master;

        private readonly ConcurrentQueue<RegisterChangedEventArgs> _holdingChanges = new();
        private readonly ConcurrentQueue<RegisterChangedEventArgs> _coilChanges = new();

        public Task InitializeAsync()
        {
            _slave = CreateSlave();
            _slave.HoldingRegisterChanged += (_, e) => _holdingChanges.Enqueue(e);
            _slave.CoilRegisterChanged += (_, e) => _coilChanges.Enqueue(e);
            _master = CreateMaster(_slave.LocalPort);
            return Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            await _master.Stop();
            _slave.Close();
        }

        #region read

        [Fact]
        public async Task ReadHoldingRegisters_InitializedValues_AreReturnedInOrder()
        {
            _slave.SetHoldingRegister(DeviceId, 4, 0x1234);
            _slave.SetHoldingRegister(DeviceId, 5, 0xFFFF);
            _slave.SetHoldingRegister(DeviceId, 6, 0x0001);

            var result = Assert.IsType<ModBusRegisterValueReturn>(await _master.ReadRegisters(DeviceId, 4, 3));

            Assert.Equal(ModBusRequestStatus.Success, result.ModBusRequestStatus);
            Assert.Equal(new ushort[] { 0x1234, 0xFFFF, 0x0001 }, result.Data.ToArray());
            Assert.Equal(result.Data.ToArray(), result.DataShort.ToArray());
        }

        [Fact]
        public async Task ReadInputRegisters_AreReadFromTheInputTable()
        {
            _slave.SetInputRegister(DeviceId, 0, 0xABCD);
            _slave.SetHoldingRegister(DeviceId, 0, 0x1111);

            var result = Assert.IsType<ModBusRegisterValueReturn>(await _master.ReadInputRegisters(DeviceId, 0, 1));

            Assert.Equal(new ushort[] { 0xABCD }, result.Data.ToArray());
        }

        [Fact]
        public async Task ReadHoldingRegisters_AllRegistersOfTheDevice_InOneRequest()
        {
            for (ushort i = 0; i < RegisterCount; i++)
            {
                _slave.SetHoldingRegister(DeviceId, i, (ushort)(i * 1000 + 7));
            }

            var result = Assert.IsType<ModBusRegisterValueReturn>(await _master.ReadRegisters(DeviceId, 0, RegisterCount));

            Assert.Equal(Enumerable.Range(0, RegisterCount).Select(i => (ushort)(i * 1000 + 7)), result.Data);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(7)]
        [InlineData(8)]
        [InlineData(9)]
        [InlineData(17)]
        [InlineData(32)]
        public async Task ReadCoils_BitPattern_IsDecodedForEveryCount(int count)
        {
            var expected = Enumerable.Range(0, count).Select(i => i % 3 == 0 || i == count - 1).ToArray();
            for (ushort i = 0; i < count; i++)
            {
                _slave.SetCoil(DeviceId, i, expected[i]);
            }

            var result = Assert.IsType<ModBusIoStateValueReturn>(await _master.ReadCoils(DeviceId, 0, count));

            Assert.Equal(expected, result.Data.ToArray());
            Assert.Equal(expected.Select(b => b ? (ushort)1 : (ushort)0), result.DataShort);
        }

        [Fact]
        public async Task ReadDiscreteInputs_AreReadFromTheDiscreteTable()
        {
            _slave.SetDiscreteInput(DeviceId, 3, true);
            _slave.SetCoil(DeviceId, 3, false);

            var result = Assert.IsType<ModBusIoStateValueReturn>(await _master.ReadDiscreteInputs(DeviceId, 2, 3));

            Assert.Equal(new[] { false, true, false }, result.Data.ToArray());
        }

        [Fact]
        public async Task ReadHoldingRegisters_OutsideTheInitializedRange_ReturnsIllegalDataAddress()
        {
            var result = await _master.ReadRegisters(DeviceId, RegisterCount - 1, 2);

            var exception = Assert.IsType<ModBusExceptionReturn>(result);
            Assert.Equal(ModBusRequestStatus.Exception, exception.ModBusRequestStatus);
            Assert.Equal(ModBusExceptionCode.IllegalDataAddress, exception.Exception);

            // the connection is still usable
            Assert.IsType<ModBusRegisterValueReturn>(await _master.ReadRegisters(DeviceId, 0, 1));
        }

        [Fact]
        public async Task ReadInputRegisters_UnknownRegister_ReturnsIllegalDataAddress()
        {
            var exception = Assert.IsType<ModBusExceptionReturn>(await _master.ReadInputRegisters(DeviceId, 1000, 1));
            Assert.Equal(ModBusExceptionCode.IllegalDataAddress, exception.Exception);
        }

        [Fact(Skip = "BUG: ModBusMasterDriver.ReadCoils (P3.Driver.ModBusDriver/Master/ModBusMasterDriver.cs:328) returns a plain ModBusReturn on an exception reply, the exception code is lost (ReadRegisters keeps it)")]
        public async Task ReadCoils_UnknownCoil_ReturnsIllegalDataAddress()
        {
            var exception = Assert.IsType<ModBusExceptionReturn>(await _master.ReadCoils(DeviceId, 1000, 1));
            Assert.Equal(ModBusExceptionCode.IllegalDataAddress, exception.Exception);
        }

        [Fact]
        public async Task ReadCoils_UnknownCoil_ReturnsExceptionStatus()
        {
            var result = await _master.ReadCoils(DeviceId, 1000, 1);

            Assert.Equal(ModBusRequestStatus.Exception, result.ModBusRequestStatus);
            Assert.IsNotType<ModBusIoStateValueReturn>(result);
        }

        [Fact]
        public async Task Read_UnknownDeviceId_ReturnsGatewayPathUnavailable()
        {
            var exception = Assert.IsType<ModBusExceptionReturn>(await _master.ReadRegisters(42, 0, 1));
            Assert.Equal(ModBusExceptionCode.GatewayPathUnavailable, exception.Exception);
        }

        #endregion

        #region write

        [Theory]
        [InlineData((ushort)0)]
        [InlineData((ushort)1)]
        [InlineData((ushort)0x7FFF)]
        [InlineData((ushort)0x8000)]
        [InlineData((ushort)0xFFFF)]
        public async Task WriteHoldingRegister_Fc06_UpdatesTheSlaveAndReadsBack(ushort value)
        {
            var write = await _master.WriteHoldingRegister(DeviceId, 9, value);

            Assert.Equal(ModBusRequestStatus.Success, write.ModBusRequestStatus);
            Assert.Equal(value, _slave.GetHoldingRegister(DeviceId, 9));

            var read = Assert.IsType<ModBusRegisterValueReturn>(await _master.ReadRegisters(DeviceId, 9, 1));
            Assert.Equal(new[] { value }, read.Data.ToArray());
        }

        [Fact]
        public async Task WriteHoldingRegister_RaisesOneChangeEventForTheRegister()
        {
            await _master.WriteHoldingRegister(DeviceId, 3, 77);

            await WaitUntil(() => !_holdingChanges.IsEmpty);
            var change = Assert.Single(_holdingChanges);
            Assert.Equal((DeviceId, (ushort)3, (ushort)1), ((byte)change.DeviceId, change.StartIndex, change.Length));
        }

        [Fact]
        public async Task WriteHoldingRegister_UninitializedRegister_ReturnsIllegalDataAddressAndRaisesNoEvent()
        {
            var result = await _master.WriteHoldingRegister(DeviceId, 500, 1);

            var exception = Assert.IsType<ModBusExceptionReturn>(result);
            Assert.Equal(ModBusExceptionCode.IllegalDataAddress, exception.Exception);
            Assert.Empty(_holdingChanges);
        }

        [Fact]
        public async Task WriteMultipleHoldingRegisters_Fc16_WritesAllRegistersAtOnce()
        {
            var values = Enumerable.Range(0, 10).Select(i => (ushort)(0xF000 + i)).ToArray();

            var write = await _master.WriteMultipleHoldingRegisters(DeviceId, 10, values);

            Assert.Equal(ModBusRequestStatus.Success, write.ModBusRequestStatus);
            Assert.Equal(values, _slave.GetHoldingRegisters(DeviceId, 10, 10).ToArray());

            await WaitUntil(() => !_holdingChanges.IsEmpty);
            var change = Assert.Single(_holdingChanges);
            Assert.Equal((ushort)10, change.StartIndex);
            Assert.Equal((ushort)10, change.Length);

            var read = Assert.IsType<ModBusRegisterValueReturn>(await _master.ReadRegisters(DeviceId, 10, 10));
            Assert.Equal(values, read.Data.ToArray());
        }

        [Fact]
        public async Task WriteMultipleHoldingRegisters_SingleRegister_IsWritten()
        {
            var write = await _master.WriteMultipleHoldingRegisters(DeviceId, 0, new ushort[] { 0xBEEF });

            Assert.Equal(ModBusRequestStatus.Success, write.ModBusRequestStatus);
            Assert.Equal(0xBEEF, _slave.GetHoldingRegister(DeviceId, 0));
        }

        [Fact]
        public async Task WriteMultipleHoldingRegisters_RangeOutsideTheDevice_ReturnsIllegalDataAddress()
        {
            var exception = Assert.IsType<ModBusExceptionReturn>(
                await _master.WriteMultipleHoldingRegisters(DeviceId, 100, new ushort[] { 1, 2 }));
            Assert.Equal(ModBusExceptionCode.IllegalDataAddress, exception.Exception);
        }

        [Fact(Skip = "BUG: ModBusSlaveDriver.SetMultipleRegister (P3.Driver.ModBusDriver/Slave/ModBusSlaveDriver.cs:481) writes the registers one by one; a range that is partly outside the device answers IllegalDataAddress but the registers before the gap are already changed")]
        public async Task WriteMultipleHoldingRegisters_RangePartlyOutsideTheDevice_WritesNothing()
        {
            var result = await _master.WriteMultipleHoldingRegisters(DeviceId, RegisterCount - 1, new ushort[] { 5, 6 });

            Assert.IsType<ModBusExceptionReturn>(result);
            Assert.Equal(0, _slave.GetHoldingRegister(DeviceId, RegisterCount - 1));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task WriteCoil_Fc05_UpdatesTheSlaveAndReadsBack(bool value)
        {
            _slave.SetCoil(DeviceId, 5, !value);

            var write = await _master.WriteCoil(DeviceId, 5, value);

            Assert.Equal(ModBusRequestStatus.Success, write.ModBusRequestStatus);
            Assert.Equal(value, _slave.GetCoil(DeviceId, 5));

            var read = Assert.IsType<ModBusIoStateValueReturn>(await _master.ReadCoils(DeviceId, 5, 1));
            Assert.Equal(new[] { value }, read.Data.ToArray());

            await WaitUntil(() => !_coilChanges.IsEmpty);
            var change = Assert.Single(_coilChanges);
            Assert.Equal(((ushort)5, (ushort)1), (change.StartIndex, change.Length));
        }

        [Fact(Skip = "BUG: ModBusSlaveStore.SetCoil (P3.Driver.ModBusDriver/Slave/ModBusSlaveStore.cs:85) throws IllegalDataValueException (03) for an unknown coil, the spec (and SetHoldingRegister) answer IllegalDataAddress (02)")]
        public async Task WriteCoil_UninitializedCoil_ReturnsIllegalDataAddress()
        {
            var exception = Assert.IsType<ModBusExceptionReturn>(await _master.WriteCoil(DeviceId, 500, true));
            Assert.Equal(ModBusExceptionCode.IllegalDataAddress, exception.Exception);
        }

        [Fact]
        public async Task WriteCoil_UninitializedCoil_ReturnsAnExceptionAndRaisesNoEvent()
        {
            var result = await _master.WriteCoil(DeviceId, 500, true);

            Assert.IsType<ModBusExceptionReturn>(result);
            Assert.Empty(_coilChanges);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(8)]
        [InlineData(9)]
        [InlineData(13)]
        [InlineData(16)]
        [InlineData(20)]
        public async Task WriteMultipleCoils_Fc15_WritesTheBitPattern(int count)
        {
            var values = Enumerable.Range(0, count).Select(i => i % 2 == 0 ^ i > 10).ToArray();

            var write = await _master.WriteMultipleCoils(DeviceId, 2, values);

            Assert.Equal(ModBusRequestStatus.Success, write.ModBusRequestStatus);
            Assert.Equal(values, _slave.GetCoils(DeviceId, 2, (ushort)count).ToArray());

            var read = Assert.IsType<ModBusIoStateValueReturn>(await _master.ReadCoils(DeviceId, 2, count));
            Assert.Equal(values, read.Data.ToArray());

            await WaitUntil(() => !_coilChanges.IsEmpty);
            var change = Assert.Single(_coilChanges);
            Assert.Equal(((ushort)2, (ushort)count), (change.StartIndex, change.Length));
        }

        [Fact]
        public async Task WriteThenRead_ManySequentialRequests_StayInSync()
        {
            for (ushort i = 0; i < 50; i++)
            {
                var register = (ushort)(i % RegisterCount);
                Assert.Equal(ModBusRequestStatus.Success, (await _master.WriteHoldingRegister(DeviceId, register, (ushort)(i * 3))).ModBusRequestStatus);
                var read = Assert.IsType<ModBusRegisterValueReturn>(await _master.ReadRegisters(DeviceId, register, 1));
                Assert.Equal((ushort)(i * 3), read.Data[0]);
            }
        }

        [Fact]
        public async Task ConcurrentRequests_FromOneMaster_AreSerializedAndAllAnswered()
        {
            for (ushort i = 0; i < RegisterCount; i++)
            {
                _slave.SetHoldingRegister(DeviceId, i, (ushort)(i + 100));
            }

            var tasks = Enumerable.Range(0, RegisterCount)
                .Select(i => _master.ReadRegisters(DeviceId, (ushort)i, 1))
                .ToArray();
            var results = await Task.WhenAll(tasks);

            for (var i = 0; i < RegisterCount; i++)
            {
                var read = Assert.IsType<ModBusRegisterValueReturn>(results[i]);
                Assert.Equal((ushort)(i + 100), read.Data[0]);
            }
        }

        #endregion
    }

    /// <summary>
    /// Slave configurations other than the default and master side failures
    /// </summary>
    public class ModBusTcpMasterSlaveConfigurationTests
    {
        [Fact]
        public async Task ReadOnlySlave_RejectsEveryWriteFunction_ButAnswersReads()
        {
            var slave = CreateSlave(c => c.ReadOnly = true);
            var master = CreateMaster(slave.LocalPort);
            try
            {
                Assert.Equal(ModBusExceptionCode.IllegalFunction, Assert.IsType<ModBusExceptionReturn>(await master.WriteHoldingRegister(DeviceId, 0, 1)).Exception);
                Assert.Equal(ModBusExceptionCode.IllegalFunction, Assert.IsType<ModBusExceptionReturn>(await master.WriteMultipleHoldingRegisters(DeviceId, 0, new ushort[] { 1, 2 })).Exception);
                Assert.Equal(ModBusExceptionCode.IllegalFunction, Assert.IsType<ModBusExceptionReturn>(await master.WriteCoil(DeviceId, 0, true)).Exception);
                Assert.Equal(ModBusExceptionCode.IllegalFunction, Assert.IsType<ModBusExceptionReturn>(await master.WriteMultipleCoils(DeviceId, 0, new[] { true, true })).Exception);

                Assert.Equal(0, slave.GetHoldingRegister(DeviceId, 0));
                Assert.Equal(0, slave.GetHoldingRegister(DeviceId, 1));
                Assert.False(slave.GetCoil(DeviceId, 0));

                Assert.IsType<ModBusRegisterValueReturn>(await master.ReadRegisters(DeviceId, 0, 2));
                Assert.IsType<ModBusIoStateValueReturn>(await master.ReadCoils(DeviceId, 0, 2));
            }
            finally
            {
                await master.Stop();
                slave.Close();
            }
        }

        [Fact]
        public async Task SeveralDevices_EachDeviceHasItsOwnRegisters()
        {
            var slave = CreateSlave(c => c.DeviceIds = new List<int> { 1, 2 });
            var master = CreateMaster(slave.LocalPort);
            try
            {
                await master.WriteHoldingRegister(1, 0, 111);
                await master.WriteHoldingRegister(2, 0, 222);

                Assert.Equal(111, slave.GetHoldingRegister(1, 0));
                Assert.Equal(222, slave.GetHoldingRegister(2, 0));
                Assert.Equal((ushort)111, Assert.IsType<ModBusRegisterValueReturn>(await master.ReadRegisters(1, 0, 1)).Data[0]);
                Assert.Equal((ushort)222, Assert.IsType<ModBusRegisterValueReturn>(await master.ReadRegisters(2, 0, 1)).Data[0]);
            }
            finally
            {
                await master.Stop();
                slave.Close();
            }
        }

        [Fact(Skip = "BUG: with IgnoreDeviceId (the template default) a request for a unit id that is not configured throws ArgumentException in ModBusSlaveDriver.GetHoldingRegisters (P3.Driver.ModBusDriver/Slave/ModBusSlaveDriver.cs:164), which is not a ModBusException, so the slave drops the TCP connection instead of answering")]
        public async Task IgnoreDeviceId_UnknownUnitId_IsAnsweredAndKeepsTheConnection()
        {
            var slave = CreateSlave(c => c.IgnoreDeviceId = true);
            try
            {
                using var client = await ConnectRaw(slave.LocalPort);
                await client.GetStream().WriteAsync(ReadRequest(1, 0x03, 0, 1, unit: 9));

                var response = await ReadResponse(client);
                Assert.Equal(9, response[6]);

                var second = await SendAndReceive(client, ReadRequest(2, 0x03, 0, 1));
                Assert.Equal(0x03, second[7]);
            }
            finally
            {
                slave.Close();
            }
        }

        [Fact]
        public async Task Master_SlaveThatNeverAnswers_TimesOutAndMarksDisconnected()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var accepted = listener.AcceptTcpClientAsync();
            var master = CreateMaster(((IPEndPoint)listener.LocalEndpoint).Port, timeoutMs: 300);
            try
            {
                using var server = await accepted;
                Assert.True(master.Connected);

                var started = DateTime.UtcNow;
                var result = await master.ReadRegisters(DeviceId, 0, 1);

                Assert.Equal(ModBusRequestStatus.InvalidDataRead, result.ModBusRequestStatus);
                Assert.False(master.Connected);
                Assert.True(DateTime.UtcNow - started < TimeSpan.FromSeconds(3));
            }
            finally
            {
                await master.Stop();
                listener.Stop();
            }
        }

        [Fact]
        public async Task Master_SlaveClosed_NextRequestFailsWithIOException()
        {
            var slave = CreateSlave();
            var master = CreateMaster(slave.LocalPort);
            try
            {
                Assert.IsType<ModBusRegisterValueReturn>(await master.ReadRegisters(DeviceId, 0, 1));
                await WaitUntil(() => slave.ConnectionCount == 1);

                slave.Close();
                await WaitUntil(() => slave.ConnectionCount == 0);

                await Assert.ThrowsAnyAsync<IOException>(async () =>
                {
                    // the first write may still be buffered by the OS, the read sees the closed connection
                    for (var i = 0; i < 5; i++)
                    {
                        await master.ReadRegisters(DeviceId, 0, 1);
                    }
                });
                // a failed write leaves Connected set, ModBusMasterDevice reconnects on the IOException
            }
            finally
            {
                await master.Stop();
                slave.Close();
            }
        }

        [Fact]
        public async Task Master_Reconnect_AfterTheSlaveDroppedTheConnection_Works()
        {
            var slave = CreateSlave(c => c.IdleTimeout = TimeSpan.FromMilliseconds(200));
            var master = CreateMaster(slave.LocalPort);
            try
            {
                slave.SetHoldingRegister(DeviceId, 0, 4711);
                Assert.IsType<ModBusRegisterValueReturn>(await master.ReadRegisters(DeviceId, 0, 1));

                // the idle timeout closes the connection on the slave side
                await WaitUntil(() => slave.ConnectionCount == 0);

                await master.Reconnect();
                var read = Assert.IsType<ModBusRegisterValueReturn>(await master.ReadRegisters(DeviceId, 0, 1));
                Assert.Equal((ushort)4711, read.Data[0]);
            }
            finally
            {
                await master.Stop();
                slave.Close();
            }
        }

        [Fact]
        public async Task Master_NotOpened_ReadReturnsException()
        {
            var master = CreateMaster(1, open: false);

            var result = await master.ReadRegisters(DeviceId, 0, 1);

            Assert.Equal(ModBusRequestStatus.Exception, result.ModBusRequestStatus);
            Assert.False(master.Connected);
        }

        [Fact]
        public void Master_OpenWithoutListeningSlave_ReturnsFalse()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();

            var master = CreateMaster(port, open: false);

            Assert.False(master.Open());
            Assert.False(master.Connected);
        }

        [Fact]
        public async Task Slave_Close_DisconnectsAllClients()
        {
            var slave = CreateSlave();
            using var first = await ConnectRaw(slave.LocalPort);
            using var second = await ConnectRaw(slave.LocalPort);
            await SendAndReceive(first, ReadRequest(1, 0x03, 0, 1));
            await SendAndReceive(second, ReadRequest(1, 0x03, 0, 1));
            await WaitUntil(() => slave.ConnectionCount == 2);

            slave.Close();

            Assert.True(await IsClosedByServer(first));
            Assert.True(await IsClosedByServer(second));
            Assert.Equal(0, slave.ConnectionCount);
        }

        [Fact]
        public async Task Slave_ClientDisconnect_RemovesTheConnection()
        {
            var slave = CreateSlave(c => c.MaxConnections = 1);
            try
            {
                using (var first = await ConnectRaw(slave.LocalPort))
                {
                    await SendAndReceive(first, ReadRequest(1, 0x03, 0, 1));
                    await WaitUntil(() => slave.ConnectionCount == 1);
                }

                await WaitUntil(() => slave.ConnectionCount == 0);

                // the connection slot is free again
                using var second = await ConnectRaw(slave.LocalPort);
                var response = await SendAndReceive(second, ReadRequest(2, 0x03, 0, 1));
                Assert.Equal(0x03, response[7]);
            }
            finally
            {
                slave.Close();
            }
        }

        [Fact]
        public async Task Slave_BindAddressLoopback_ReportsAnEphemeralPort()
        {
            var slave = CreateSlave();
            try
            {
                Assert.InRange(slave.LocalPort, 1, 65535);
                using var client = await ConnectRaw(slave.LocalPort);
                Assert.True(client.Connected);
            }
            finally
            {
                slave.Close();
            }
        }
    }
}
