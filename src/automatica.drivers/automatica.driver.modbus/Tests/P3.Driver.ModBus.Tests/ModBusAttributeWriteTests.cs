using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.Driver;
using P3.Driver.ModBusDriver;
using P3.Driver.ModBusDriver.Master;
using P3.Driver.ModBusDriver.Slave;
using P3.Driver.ModBusDriver.Slave.Tcp;
using P3.Driver.ModBusDriverFactory;
using P3.Driver.ModBusDriverFactory.Attributes;
using P3.Driver.ModBusDriverFactory.Master;
using P3.Driver.ModBusDriverFactory.Slave;
using Xunit;

namespace P3.Driver.ModBus.Tests
{
    public class ModBusAttributeWriteTests : ModBusBaseTest
    {
        private class FakeMasterDriver : IModBusMasterDriver
        {
            public ModBusReturn Result { get; set; } = new ModBusReturn(ModBusRequestStatus.Success);
            public List<string> Calls { get; } = new();
            public ushort[] MultipleValues { get; private set; }

            public bool Connected => true;
            public bool Open() => true;
            public Task<bool> Stop() => Task.FromResult(true);
            public Task Reconnect() => Task.CompletedTask;

            public Task<ModBusReturn> ReadInputRegisters(byte slaveId, ushort address, int numberOfRegisters, CancellationToken cts = default) => throw new NotSupportedException();
            public Task<ModBusReturn> ReadRegisters(byte slaveId, ushort address, int numberOfRegisters, CancellationToken cts = default) => throw new NotSupportedException();
            public Task<ModBusReturn> ReadCoils(byte slaveId, ushort address, int numberOfRegisters, CancellationToken cts = default) => throw new NotSupportedException();
            public Task<ModBusReturn> ReadDiscreteInputs(byte slaveId, ushort address, int numberOfRegisters, CancellationToken cts = default) => throw new NotSupportedException();

            public Task<ModBusReturn> WriteHoldingRegister(int deviceId, ushort register, ushort value, CancellationToken cts = default)
            {
                Calls.Add($"FC06 {register}={value}");
                return Task.FromResult(Result);
            }

            public Task<ModBusReturn> WriteCoil(int deviceId, ushort register, bool value, CancellationToken cts = default)
            {
                Calls.Add($"FC05 {register}={value}");
                return Task.FromResult(Result);
            }

            public Task<ModBusReturn> WriteMultipleHoldingRegisters(int deviceId, ushort startRegister, ushort[] values, CancellationToken cts = default)
            {
                Calls.Add($"FC16 {startRegister}x{values.Length}");
                MultipleValues = values;
                return Task.FromResult(Result);
            }

            public Task<ModBusReturn> WriteMultipleCoils(int deviceId, ushort startRegister, bool[] values, CancellationToken cts = default) => throw new NotSupportedException();
        }

        private class RecordingWriteContext : IWriteContext
        {
            public List<object> Values { get; } = new();

            public Task DispatchValue(object value, CancellationToken token = default)
            {
                Values.Add(value);
                return Task.CompletedTask;
            }
        }

        private class TestableMasterAttribute : ModBusMasterAttribute
        {
            public TestableMasterAttribute(IDriverContext ctx, ModBusMasterDevice parent, IModBusMasterDriver driver, ModBusAttribute attribute)
                : base(ctx, parent, driver, attribute)
            {
            }

            public Task InvokeWrite(object value, IWriteContext context) => Write(value, context);
        }

        private async Task<(TestableMasterAttribute attribute, FakeMasterDriver driver)> CreateMasterAttribute(Guid template)
        {
            // the default table of the register templates is the holding register table
            var real = await InitAttribute(template);
            Assert.Equal(ModBusTable.HoldingRegister, real.Attribute.Table);

            var driver = new FakeMasterDriver();
            var device = new ModBusMasterDevice(real.DriverContext, driver, new SemaphoreSlim(1));
            return (new TestableMasterAttribute(real.DriverContext, device, driver, real.Attribute), driver);
        }

        [Fact]
        public async Task MasterWrite_4Byte_UsesSingleMultipleRegisterWrite()
        {
            var (attribute, driver) = await CreateMasterAttribute(ModBusDriverFactory.ModBusDriverFactory.Register4ByteUnsignedGuid);
            var context = new RecordingWriteContext();

            await attribute.InvokeWrite(-5, context);

            Assert.Equal(new[] { "FC16 0x2" }, driver.Calls);
            Assert.Equal(new ushort[] { 0xFFFF, 0xFFFB }, driver.MultipleValues);
            Assert.Equal(new object[] { -5 }, context.Values);
        }

        [Fact]
        public async Task MasterWrite_2Byte_UsesSingleRegisterWrite()
        {
            var (attribute, driver) = await CreateMasterAttribute(ModBusDriverFactory.ModBusDriverFactory.Register2ByteGuid);
            var context = new RecordingWriteContext();

            await attribute.InvokeWrite(123, context);

            Assert.Equal(new[] { "FC06 0=123" }, driver.Calls);
            Assert.Single(context.Values);
        }

        [Fact]
        public async Task MasterWrite_ExceptionReply_ThrowsAndDoesNotDispatch()
        {
            var (attribute, driver) = await CreateMasterAttribute(ModBusDriverFactory.ModBusDriverFactory.Register2ByteGuid);
            driver.Result = new ModBusExceptionReturn(ModBusExceptionCode.IllegalDataAddress);
            var context = new RecordingWriteContext();

            var e = await Assert.ThrowsAsync<P3.Driver.ModBusDriverFactory.Common.ModBusException>(() => attribute.InvokeWrite(1, context));
            Assert.Equal(ModBusExceptionCode.IllegalDataAddress, e.ExceptionCode);
            Assert.Empty(context.Values);
        }

        [Fact]
        public async Task MasterWrite_Timeout_ThrowsAndDoesNotDispatch()
        {
            var (attribute, driver) = await CreateMasterAttribute(ModBusDriverFactory.ModBusDriverFactory.Register2ByteGuid);
            driver.Result = new ModBusFrameReturn(ModBusRequestStatus.InvalidDataRead, null);
            var context = new RecordingWriteContext();

            await Assert.ThrowsAsync<System.IO.IOException>(() => attribute.InvokeWrite(1, context));
            Assert.Empty(context.Values);
        }

        [Fact]
        public async Task MasterWrite_InvalidValue_IsNotSent()
        {
            var (attribute, driver) = await CreateMasterAttribute(ModBusDriverFactory.ModBusDriverFactory.Register2ByteGuid);
            var context = new RecordingWriteContext();

            await attribute.InvokeWrite(-1, context); // out of range for "+2 Byte", a retry cannot help

            Assert.Empty(driver.Calls);
            Assert.Empty(context.Values);
        }

        [Fact]
        public async Task Slave_MasterWrite_DispatchesAttributeValue()
        {
            var attribute = await InitSlaveAttribute(ModBusDriverFactory.ModBusDriverFactory.Register4ByteUnsignedGuid, instance =>
            {
                instance.ObjId = Guid.NewGuid();
                instance.GetProperty("modbus-register").Value = 2;
                instance.GetProperty("modbus-table").Value = ModBusTable.HoldingRegister;
                instance.GetProperty("modbus-byte-order").Value = ModBus4ByteOrder.AB_CD;
                return instance;
            });

            var slave = (ModBusSlaveDriver<ModBusSlaveTcpConfig>)attribute.Driver;
            var header = new byte[] { 0x00, 0x01, 0x00, 0x00, 0x00, 0x0B };

            // a master writes -5 (FC16) to register 2..3 of device 0
            slave.GetResponseData(0, 0x10, 2, 2, new byte[] { 0x00, 0x10, 0x00, 0x02, 0x00, 0x02, 0x04, 0xFF, 0xFF, 0xFF, 0xFB }, header);

            object value = null;
            for (var i = 0; i < 50 && value == null; i++)
            {
                value = Dispatcher.GetValue(DispatchableType.NodeInstance, attribute.Id)?.Value;
                if (value == null)
                {
                    await Task.Delay(20);
                }
            }

            Assert.Equal(-5, value);

            // a single register write (FC06) to the low word of the attribute dispatches the new 32 bit value
            slave.GetResponseData(0, 0x06, 3, 7, new byte[] { 0x00, 0x06, 0x00, 0x03, 0x00, 0x07 }, header);
            for (var i = 0; i < 50 && !Equals(value, unchecked((int)0xFFFF0007)); i++)
            {
                await Task.Delay(20);
                value = Dispatcher.GetValue(DispatchableType.NodeInstance, attribute.Id)?.Value;
            }
            Assert.Equal(unchecked((int)0xFFFF0007), value);

            await attribute.Stop();
        }
    }
}
