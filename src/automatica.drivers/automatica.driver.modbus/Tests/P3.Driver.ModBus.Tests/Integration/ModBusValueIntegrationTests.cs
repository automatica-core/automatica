using System;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.UnitTests.Base.Common;
using Automatica.Core.UnitTests.Drivers;
using Automatica.Core.UnitTests.Base.Drivers;
using Microsoft.Extensions.Logging.Abstractions;
using P3.Driver.ModBusDriver;
using P3.Driver.ModBusDriver.Master;
using P3.Driver.ModBusDriver.Master.Tcp;
using P3.Driver.ModBusDriver.Slave.Tcp;
using P3.Driver.ModBusDriverFactory;
using P3.Driver.ModBusDriverFactory.Attributes;
using P3.Driver.ModBusDriverFactory.Master;
using Xunit;
using static P3.Driver.ModBus.Tests.Integration.ModBusTcpTestHarness;
using MbFactory = P3.Driver.ModBusDriverFactory.ModBusDriverFactory;

namespace P3.Driver.ModBus.Tests.Integration
{
    /// <summary>
    /// Signed, float and scaled values converted by the attribute types and sent over a real master/slave TCP connection
    /// </summary>
    public class ModBusValueIntegrationTests : ModBusBaseTest, IAsyncLifetime
    {
        private ModBusSlaveTcpDriver _slave;
        private ModBusMasterTcpDriver _master;

        public Task InitializeAsync()
        {
            _slave = CreateSlave();
            _master = CreateMaster(_slave.LocalPort);
            return Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            await _master.Stop();
            _slave.Close();
        }

        private async Task<ModBusAttribute> CreateAttribute(Guid template, double factor = 1, double offset = 0, object byteOrder = null)
        {
            var attribute = await InitAttribute(template, instance =>
            {
                instance.GetProperty("factor").Value = factor;
                instance.GetProperty("offset").Value = offset;
                if (byteOrder != null)
                {
                    instance.GetProperty("modbus-byte-order").Value = byteOrder;
                }
                return instance;
            });
            return attribute.Attribute;
        }

        /// <summary>
        /// Converts <paramref name="value"/>, writes it like ModBusMasterAttribute does (FC06 for one register, FC16 otherwise),
        /// reads it back over the wire and converts it again
        /// </summary>
        private async Task<(ushort[] slaveRegisters, object readBack)> WriteAndReadBack(ModBusAttribute attribute, object value, ushort register = 4)
        {
            var bus = attribute.ConvertValueToBus(value, out _);
            Assert.Equal(attribute.RegisterLength, bus.Length);

            var write = bus.Length == 1
                ? await _master.WriteHoldingRegister(DeviceId, register, bus[0])
                : await _master.WriteMultipleHoldingRegisters(DeviceId, register, bus);
            Assert.Equal(ModBusRequestStatus.Success, write.ModBusRequestStatus);

            var slaveRegisters = _slave.GetHoldingRegisters(DeviceId, register, (ushort)bus.Length).ToArray();

            var read = Assert.IsType<ModBusRegisterValueReturn>(await _master.ReadRegisters(DeviceId, register, attribute.RegisterLength));
            return (slaveRegisters, attribute.ConvertValueFromBus(read.DataShort.ToArray()));
        }

        [Theory]
        [InlineData((short)-1, new ushort[] { 0xFFFF })]
        [InlineData(short.MinValue, new ushort[] { 0x8000 })]
        [InlineData(short.MaxValue, new ushort[] { 0x7FFF })]
        [InlineData((short)-1234, new ushort[] { 0xFB2E })]
        public async Task Signed2Byte_OverTheWire_IsTwosComplement(short value, ushort[] expectedRegisters)
        {
            var attribute = await CreateAttribute(MbFactory.Register2ByteUnsignedGuid); // "±2 Byte"

            var (registers, readBack) = await WriteAndReadBack(attribute, value);

            Assert.Equal(expectedRegisters, registers);
            Assert.Equal(value, readBack);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(40000)]
        [InlineData(65535)]
        public async Task Unsigned2Byte_OverTheWire_KeepsTheFullRange(int value)
        {
            var attribute = await CreateAttribute(MbFactory.Register2ByteGuid); // "+2 Byte"

            var (registers, readBack) = await WriteAndReadBack(attribute, value);

            Assert.Equal(new[] { (ushort)value }, registers);
            Assert.Equal(value, Convert.ToInt32(readBack));
        }

        [Theory]
        [InlineData(-5, ModBus4ByteOrder.AB_CD, new ushort[] { 0xFFFF, 0xFFFB })]
        [InlineData(-5, ModBus4ByteOrder.CD_AB, new ushort[] { 0xFFFB, 0xFFFF })]
        [InlineData(int.MinValue, ModBus4ByteOrder.AB_CD, new ushort[] { 0x8000, 0x0000 })]
        [InlineData(0x12345678, ModBus4ByteOrder.AB_CD, new ushort[] { 0x1234, 0x5678 })]
        [InlineData(0x12345678, ModBus4ByteOrder.CD_AB, new ushort[] { 0x5678, 0x1234 })]
        [InlineData(0x12345678, ModBus4ByteOrder.BA_DC, new ushort[] { 0x3412, 0x7856 })]
        [InlineData(0x12345678, ModBus4ByteOrder.DC_BA, new ushort[] { 0x7856, 0x3412 })]
        public async Task Signed4Byte_OverTheWire_UsesTheByteOrderAndOneFc16Write(int value, ModBus4ByteOrder order, ushort[] expectedRegisters)
        {
            var attribute = await CreateAttribute(MbFactory.Register4ByteUnsignedGuid, byteOrder: order); // "±4 Byte"

            var (registers, readBack) = await WriteAndReadBack(attribute, value);

            Assert.Equal(expectedRegisters, registers);
            Assert.Equal(value, Convert.ToInt32(readBack));
        }

        [Theory]
        [InlineData(4000000000u)]
        [InlineData(uint.MaxValue)]
        [InlineData(0u)]
        public async Task Unsigned4Byte_OverTheWire_KeepsTheFullRange(uint value)
        {
            var attribute = await CreateAttribute(MbFactory.Register4ByteGuid, byteOrder: ModBus4ByteOrder.AB_CD);

            var (registers, readBack) = await WriteAndReadBack(attribute, value);

            Assert.Equal(new[] { (ushort)(value >> 16), (ushort)value }, registers);
            Assert.Equal(value, Convert.ToUInt32(readBack));
        }

        [Theory]
        [InlineData(-1L)]
        [InlineData(long.MinValue)]
        [InlineData(long.MaxValue)]
        [InlineData(-9007199254740993L)]
        public async Task Signed8Byte_OverTheWire_IsExact(long value)
        {
            var attribute = await CreateAttribute(MbFactory.Register8ByteUnsignedGuid, byteOrder: ModBus8ByteOrder.AB_CD_EF_GH); // "±8 Byte"

            var (registers, readBack) = await WriteAndReadBack(attribute, value);

            var expected = Enumerable.Range(0, 4).Select(i => (ushort)((ulong)value >> (48 - 16 * i))).ToArray();
            Assert.Equal(expected, registers);
            Assert.Equal(value, Convert.ToInt64(readBack));
        }

        [Fact]
        public async Task Unsigned8Byte_OverTheWire_MaxValue_IsExact()
        {
            var attribute = await CreateAttribute(MbFactory.Register8ByteGuid, byteOrder: ModBus8ByteOrder.GH_EF_CD_AB);

            var (registers, readBack) = await WriteAndReadBack(attribute, ulong.MaxValue);

            Assert.All(registers, r => Assert.Equal(0xFFFF, r));
            Assert.Equal(ulong.MaxValue, Convert.ToUInt64(readBack));
        }

        [Theory]
        [InlineData(-12.5)]
        [InlineData(0.0)]
        [InlineData(3.25)]
        [InlineData(1e10)]
        public async Task Float4Byte_OverTheWire_IsIeee754(double value)
        {
            var attribute = await CreateAttribute(MbFactory.Register4ByteFloat, byteOrder: ModBus4ByteOrder.AB_CD);

            var (registers, readBack) = await WriteAndReadBack(attribute, value);

            var bits = BitConverter.SingleToUInt32Bits((float)value);
            Assert.Equal(new[] { (ushort)(bits >> 16), (ushort)bits }, registers);
            Assert.Equal((float)value, Convert.ToSingle(readBack));
        }

        [Theory]
        [InlineData(Math.PI)]
        [InlineData(-273.15)]
        [InlineData(double.Epsilon)]
        public async Task Float8Byte_OverTheWire_IsIeee754(double value)
        {
            var attribute = await CreateAttribute(MbFactory.Register8ByteFloat, byteOrder: ModBus8ByteOrder.AB_CD_EF_GH);

            var (registers, readBack) = await WriteAndReadBack(attribute, value);

            var bits = BitConverter.DoubleToUInt64Bits(value);
            Assert.Equal(Enumerable.Range(0, 4).Select(i => (ushort)(bits >> (48 - 16 * i))), registers);
            Assert.Equal(value, Convert.ToDouble(readBack));
        }

        [Fact]
        public async Task Scaled2Byte_FactorAndOffset_RawRegisterIsScaledAndReadsBack()
        {
            // value = raw / 10 - 40  <=>  raw = (value + 40) * 10, e.g. a temperature sensor
            var attribute = await CreateAttribute(MbFactory.Register2ByteGuid, factor: 10, offset: -40);

            var (registers, readBack) = await WriteAndReadBack(attribute, 21.5);

            Assert.Equal(new ushort[] { 615 }, registers);
            Assert.Equal(21.5, Convert.ToDouble(readBack), 10);
        }

        [Fact]
        public async Task ScaledSigned2Byte_NegativeScaledValue_RoundTrips()
        {
            var attribute = await CreateAttribute(MbFactory.Register2ByteUnsignedGuid, factor: 100);

            var (registers, readBack) = await WriteAndReadBack(attribute, -12.34);

            Assert.Equal(new[] { unchecked((ushort)-1234) }, registers);
            Assert.Equal(-12.34, Convert.ToDouble(readBack), 10);
        }

        [Fact]
        public async Task ScaledSigned4Byte_RoundTrips()
        {
            var attribute = await CreateAttribute(MbFactory.Register4ByteUnsignedGuid, factor: 1000, offset: 1, byteOrder: ModBus4ByteOrder.CD_AB);

            var (registers, readBack) = await WriteAndReadBack(attribute, -999.999);

            // (-999.999 - 1) * 1000 = -1000999 = 0xFFF0B9D9, CD_AB: low word first
            Assert.Equal(new ushort[] { 0xB9D9, 0xFFF0 }, registers);
            Assert.Equal(-999.999, Convert.ToDouble(readBack), 6);
        }

        [Fact]
        public async Task ScaledFloat_RoundTrips()
        {
            var attribute = await CreateAttribute(MbFactory.Register4ByteFloat, factor: 2, offset: 10, byteOrder: ModBus4ByteOrder.AB_CD);

            var (registers, readBack) = await WriteAndReadBack(attribute, 20.5);

            var bits = BitConverter.SingleToUInt32Bits(21f); // (20.5 - 10) * 2
            Assert.Equal(new[] { (ushort)(bits >> 16), (ushort)bits }, registers);
            Assert.Equal(20.5, Convert.ToDouble(readBack), 4);
        }

        [Fact]
        public async Task SlaveSideValue_SetByTheSlave_IsReadByTheMaster()
        {
            var attribute = await CreateAttribute(MbFactory.Register4ByteUnsignedGuid, byteOrder: ModBus4ByteOrder.AB_CD);
            _slave.SetHoldingRegister(DeviceId, 0, 0xFFFE);
            _slave.SetHoldingRegister(DeviceId, 1, 0x1DC0); // -123456

            var read = Assert.IsType<ModBusRegisterValueReturn>(await _master.ReadRegisters(DeviceId, 0, attribute.RegisterLength));

            Assert.Equal(-123456, Convert.ToInt32(attribute.ConvertValueFromBus(read.DataShort.ToArray())));
        }
    }

    /// <summary>
    /// The complete master driver (factory driver, device, attribute) polling and writing a real TCP slave
    /// </summary>
    public class ModBusMasterDriverEndToEndTests : ModBusBaseTest
    {
        private async Task<(ModBusDriverFactory.Master.ModBusMasterDriver driver, ModBusMasterAttribute attribute)> StartMasterDriver(
            int port, Guid attributeTemplate, Action<NodeInstance> configureAttribute)
        {
            var factory = Factory;
            var masterFactory = new ModBusTcpMasterDriverFactory();
            masterFactory.InitNodeTemplates(factory);

            var driverNode = factory.CreateNodeInstance(ModBusTcpMasterDriverFactory.ModBusMasterTcpGuid);
            driverNode.GetProperty("modbus-master-ip").Value = "127.0.0.1";
            driverNode.GetProperty("modbus-master-port").Value = port;

            var device = factory.CreateNodeInstance(MbFactory.DeviceTemplate);
            device.GetProperty("modbus-device-id").Value = (int)DeviceId;
            device.GetProperty("modbus-poll-interval").Value = 60000; // only the initial poll on start

            var attributeNode = factory.CreateNodeInstance(attributeTemplate);
            attributeNode.ObjId = Guid.NewGuid();
            configureAttribute(attributeNode);

            driverNode.InverseThis2ParentNodeInstanceNavigation.Add(device);
            device.InverseThis2ParentNodeInstanceNavigation.Add(attributeNode);

            var context = new DriverContextMock(driverNode, DriverFactory, factory, Dispatcher, NullLoggerFactory.Instance, TimeProvider.System);
            var driver = (ModBusDriverFactory.Master.ModBusMasterDriver)masterFactory.CreateDriver(context);

            Assert.True(await driver.Init());
            Assert.True(await driver.Configure());
            await driver.Start();

            var attribute = (ModBusMasterAttribute)driver.Children[0].Children[0];
            return (driver, attribute);
        }

        private async Task<object> WaitForDispatchedValue(Guid id, Func<object, bool> predicate = null)
        {
            object value = null;
            await WaitUntil(() =>
            {
                value = Dispatcher.GetValue(DispatchableType.NodeInstance, id)?.Value;
                return value != null && (predicate == null || predicate(value));
            });
            return value;
        }

        [Fact]
        public async Task Start_PollsTheSlaveAndDispatchesTheSignedValue()
        {
            var slave = CreateSlave();
            slave.SetHoldingRegister(DeviceId, 2, 0xFFFF);
            slave.SetHoldingRegister(DeviceId, 3, 0xFFFB);

            var (driver, attribute) = await StartMasterDriver(slave.LocalPort, MbFactory.Register4ByteUnsignedGuid, node =>
            {
                node.GetProperty("modbus-register").Value = 2;
                node.GetProperty("modbus-table").Value = ModBusTable.HoldingRegister;
                node.GetProperty("modbus-byte-order").Value = ModBus4ByteOrder.AB_CD;
            });
            try
            {
                var value = await WaitForDispatchedValue(attribute.Id);
                Assert.Equal(-5, Convert.ToInt32(value));
            }
            finally
            {
                await driver.Stop();
                slave.Close();
            }
        }

        [Fact]
        public async Task Start_PollsAScaledInputRegister()
        {
            var slave = CreateSlave();
            slave.SetInputRegister(DeviceId, 0, 615);

            var (driver, attribute) = await StartMasterDriver(slave.LocalPort, MbFactory.Register2ByteGuid, node =>
            {
                node.GetProperty("modbus-register").Value = 0;
                node.GetProperty("modbus-table").Value = ModBusTable.InputRegister;
                node.GetProperty("factor").Value = 10d;
                node.GetProperty("offset").Value = -40d;
            });
            try
            {
                var value = await WaitForDispatchedValue(attribute.Id);
                Assert.Equal(21.5, Convert.ToDouble(value), 10);
            }
            finally
            {
                await driver.Stop();
                slave.Close();
            }
        }

        [Fact]
        public async Task WriteValue_4ByteValue_IsWrittenToTheSlaveWithOneFc16Request()
        {
            var slave = CreateSlave();
            var changes = new System.Collections.Concurrent.ConcurrentQueue<ModBusDriver.Slave.RegisterChangedEventArgs>();
            slave.HoldingRegisterChanged += (_, e) => changes.Enqueue(e);

            var (driver, attribute) = await StartMasterDriver(slave.LocalPort, MbFactory.Register4ByteUnsignedGuid, node =>
            {
                node.GetProperty("modbus-register").Value = 6;
                node.GetProperty("modbus-table").Value = ModBusTable.HoldingRegister;
                node.GetProperty("modbus-byte-order").Value = ModBus4ByteOrder.AB_CD;
            });
            try
            {
                // the initial poll dispatches 0
                await WaitForDispatchedValue(attribute.Id);

                await attribute.WriteValue(DispatchableMock.Instance, new DispatchValue(DispatchableMock.Instance.Id, DispatchableType.NodeInstance,
                    -100000, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), DispatchValueSource.Write));

                await WaitUntil(() => slave.GetHoldingRegister(DeviceId, 7) != 0);
                Assert.Equal(new ushort[] { 0xFFFE, 0x7960 }, slave.GetHoldingRegisters(DeviceId, 6, 2).ToArray());

                var change = Assert.Single(changes);
                Assert.Equal(((ushort)6, (ushort)2), (change.StartIndex, change.Length));

                var dispatched = await WaitForDispatchedValue(attribute.Id, v => Convert.ToInt32(v) == -100000);
                Assert.Equal(-100000, Convert.ToInt32(dispatched));
            }
            finally
            {
                await driver.Stop();
                slave.Close();
            }
        }

        [Fact]
        public async Task WriteValue_Coil_IsWrittenToTheSlave()
        {
            var slave = CreateSlave();

            var (driver, attribute) = await StartMasterDriver(slave.LocalPort, MbFactory.CoilsYesNo, node =>
            {
                node.GetProperty("modbus-register").Value = 3;
                node.GetProperty("modbus-table").Value = ModBusTable.Coil;
            });
            try
            {
                await WaitForDispatchedValue(attribute.Id);

                await attribute.WriteValue(DispatchableMock.Instance, new DispatchValue(DispatchableMock.Instance.Id, DispatchableType.NodeInstance,
                    true, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), DispatchValueSource.Write));

                await WaitUntil(() => slave.GetCoil(DeviceId, 3));
                Assert.False(slave.GetCoil(DeviceId, 2));
                Assert.False(slave.GetCoil(DeviceId, 4));
            }
            finally
            {
                await driver.Stop();
                slave.Close();
            }
        }

        [Fact]
        public async Task Stop_ClosesTheMasterConnection()
        {
            var slave = CreateSlave();
            var (driver, attribute) = await StartMasterDriver(slave.LocalPort, MbFactory.Register2ByteGuid, node =>
            {
                node.GetProperty("modbus-register").Value = 0;
                node.GetProperty("modbus-table").Value = ModBusTable.HoldingRegister;
            });
            try
            {
                await WaitForDispatchedValue(attribute.Id);
                await WaitUntil(() => slave.ConnectionCount == 1);

                await driver.Stop();

                await WaitUntil(() => slave.ConnectionCount == 0);
            }
            finally
            {
                slave.Close();
            }
        }
    }
}
