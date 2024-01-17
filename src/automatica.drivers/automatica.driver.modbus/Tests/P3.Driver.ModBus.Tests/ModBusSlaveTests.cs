using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.Driver.Exceptions;
using Automatica.Core.EF.Exceptions;
using Automatica.Core.UnitTests.Base.Common;
using P3.Driver.ModBusDriver;
using P3.Driver.ModBusDriver.Exceptions;
using P3.Driver.ModBusDriver.Slave;
using P3.Driver.ModBusDriver.Slave.Tcp;
using P3.Driver.ModBusDriverFactory;
using Xunit;

namespace P3.Driver.ModBus.Tests
{
    public class ModBusSlaveTests : ModBusBaseTest
    {
        [Fact]
        public async Task Test_HoldingRegisters_4Byte()
        {
            var attribute = await InitSlaveAttribute(ModBusDriverFactory.ModBusDriverFactory.Register4ByteGuid, instance =>
            {
                var register = instance.GetProperty("modbus-register");
                register.Value = 0;

                var table = instance.GetProperty("modbus-table");
                table.Value = ModBusTable.HoldingRegister;

                var prop = instance.GetProperty("modbus-byte-order");
                prop.Value = ModBus4ByteOrder.AB_CD;
                return instance;
            });

            await attribute.WriteValue(DispatchableMock.Instance, Create(123));
            await Task.Delay(100);

            Assert.True(attribute.Driver.GetHoldingRegister(0, 0) == 0);
            Assert.True(attribute.Driver.GetHoldingRegister(0, 1) == 123);

            Assert.True(Dispatcher.GetValues(DispatchableType.NodeInstance).Count > 0);
        }

        [Fact]
        public async Task Test_HoldingRegisters_2Byte()
        {
            var attribute = await InitSlaveAttribute(ModBusDriverFactory.ModBusDriverFactory.Register2ByteGuid, instance =>
            {
                var register = instance.GetProperty("modbus-register");
                register.Value = 0;

                var table = instance.GetProperty("modbus-table");
                table.Value = ModBusTable.HoldingRegister;
                return instance;
            });

            await attribute.WriteValue(DispatchableMock.Instance, Create(123));
            await Task.Delay(100);

            Assert.True(attribute.Driver.GetHoldingRegister(0, 0) == 123);
        }


        [Fact]
        public async Task Test_HoldingRegisters_8Byte()
        {
            var attribute = await InitSlaveAttribute(ModBusDriverFactory.ModBusDriverFactory.Register8ByteGuid, instance =>
            {
                var register = instance.GetProperty("modbus-register");
                register.Value = 0;

                var table = instance.GetProperty("modbus-table");
                table.Value = ModBusTable.HoldingRegister;


                var prop = instance.GetProperty("modbus-byte-order");
                prop.Value = ModBus8ByteOrder.AB_CD_EF_GH;
                return instance;
            });

            await attribute.WriteValue(DispatchableMock.Instance, Create(123));
            await Task.Delay(100);

            Assert.True(attribute.Driver.GetHoldingRegister(0, 0) == 0);
            Assert.True(attribute.Driver.GetHoldingRegister(0, 1) == 0);
            Assert.True(attribute.Driver.GetHoldingRegister(0, 2) == 0);
            Assert.True(attribute.Driver.GetHoldingRegister(0, 3) == 123);
        }

        [Fact]
        public async Task Test_InputRegisters_4Byte()
        {
            var attribute = await InitSlaveAttribute(ModBusDriverFactory.ModBusDriverFactory.Register4ByteGuid, instance =>
            {
                var register = instance.GetProperty("modbus-register");
                register.Value = 0;

                var table = instance.GetProperty("modbus-table");
                table.Value = ModBusTable.InputRegister;

                var prop = instance.GetProperty("modbus-byte-order");
                prop.Value = ModBus4ByteOrder.AB_CD;
                return instance;
            });

            await attribute.WriteValue(DispatchableMock.Instance, Create(999));
            await Task.Delay(100);

            Assert.True(attribute.Driver.GetInputRegister(0, 0) == 0);
            Assert.True(attribute.Driver.GetInputRegister(0, 1) == 999);


        }

        [Fact]
        public async Task Test_InputRegisters_2Byte()
        {
            var attribute = await InitSlaveAttribute(ModBusDriverFactory.ModBusDriverFactory.Register2ByteGuid, instance =>
            {
                var register = instance.GetProperty("modbus-register");
                register.Value = 0;

                var table = instance.GetProperty("modbus-table");
                table.Value = ModBusTable.InputRegister;
                return instance;
            });

            await attribute.WriteValue(DispatchableMock.Instance, Create(999));
            await Task.Delay(100);

            Assert.True(attribute.Driver.GetInputRegister(0, 0) == 999);
        }


        [Fact]
        public async Task Test_InputRegisters_8Byte()
        {
            var attribute = await InitSlaveAttribute(ModBusDriverFactory.ModBusDriverFactory.Register8ByteGuid, instance =>
            {
                var register = instance.GetProperty("modbus-register");
                register.Value = 0;

                var table = instance.GetProperty("modbus-table");
                table.Value = ModBusTable.InputRegister;


                var prop = instance.GetProperty("modbus-byte-order");
                prop.Value = ModBus8ByteOrder.AB_CD_EF_GH;
                return instance;
            });

            await attribute.WriteValue(DispatchableMock.Instance, Create(999));
            await Task.Delay(100);

            Assert.True(attribute.Driver.GetInputRegister(0, 0) == 0);
            Assert.True(attribute.Driver.GetInputRegister(0, 1) == 0);
            Assert.True(attribute.Driver.GetInputRegister(0, 2) == 0);
            Assert.True(attribute.Driver.GetInputRegister(0, 3) == 999);


            var modBusSlaveDriver = attribute.Driver as ModBusSlaveDriver<ModBusSlaveTcpConfig>;
            var bytes = modBusSlaveDriver.GetByteValues(modBusSlaveDriver.GetInputRegisters(0, 0, 4));

            Assert.True(bytes.Length == 8);

            Assert.True(bytes[0] == 0);
            Assert.True(bytes[1] == 0);
            Assert.True(bytes[2] == 0);
            Assert.True(bytes[4] == 0);
            Assert.True(bytes[5] == 0);
            Assert.True(bytes[6] == 3);
            Assert.True(bytes[7] == 231);
        }

        [Fact]
        public async Task Test_Coils()
        {
            var attribute = await InitSlaveAttribute(ModBusDriverFactory.ModBusDriverFactory.CoilsYesNo, instance =>
            {
                var register = instance.GetProperty("modbus-register");
                register.Value = 0;

                var table = instance.GetProperty("modbus-table");
                table.Value = ModBusTable.Coil;
                return instance;
            });

            await attribute.WriteValue(DispatchableMock.Instance, Create(true));
            await Task.Delay(100);

            Assert.True(attribute.Driver.GetCoil(0, 0));
            await attribute.WriteValue(DispatchableMock.Instance, Create(false));
            await Task.Delay(100);

            Assert.False(attribute.Driver.GetCoil(0, 0));
        }

        [Fact]
        public async Task Test_DiscreteInput()
        {
            var attribute = await InitSlaveAttribute(ModBusDriverFactory.ModBusDriverFactory.CoilsYesNo, instance =>
            {
                var register = instance.GetProperty("modbus-register");
                register.Value = 0;

                var table = instance.GetProperty("modbus-table");
                table.Value = ModBusTable.DiscreteInput;

                return instance;
            });
            await attribute.WriteValue(DispatchableMock.Instance, Create(true));
            await Task.Delay(100);
            Assert.True(attribute.Driver.GetDiscreteInput(0, 0));

            await attribute.WriteValue(DispatchableMock.Instance, Create(false));
            await Task.Delay(100);
            Assert.False(attribute.Driver.GetDiscreteInput(0, 0));

        }

        [Fact]
        public async Task Test_ModBusBinary_InvalidInputException()
        {
            var attribute = await InitSlaveAttribute(ModBusDriverFactory.ModBusDriverFactory.CoilsYesNo, instance =>
            {
                var register = instance.GetProperty("modbus-register");
                register.Value = 0;

                var table = instance.GetProperty("modbus-table");
                table.Value = ModBusTable.DiscreteInput;

                return instance;
            });

        }

        [Fact]
        public async Task Test_ModBusBinary_GetBinaryValues()
        {
            var attribute = await InitSlaveAttribute(ModBusDriverFactory.ModBusDriverFactory.CoilsYesNo, instance =>
            {
                var register = instance.GetProperty("modbus-register");
                register.Value = 0;

                var table = instance.GetProperty("modbus-table");
                table.Value = ModBusTable.DiscreteInput;

                return instance;
            });

            await attribute.WriteValue(DispatchableMock.Instance, Create(true));
            await Task.Delay(100);

            var modBusSlaveDriver = attribute.Driver as ModBusSlaveDriver<ModBusSlaveTcpConfig>;
            var bytes = modBusSlaveDriver.GetBinaryValues(modBusSlaveDriver.GetDiscreteInput(0, 0, 1));

            Assert.True(bytes.Length == 1);
            Assert.True(bytes[0] == 1);
        }

        [Fact]
        public async Task Test_ModBusBinary_IllegalAddressException()
        {
            var attribute = await InitSlaveAttribute(ModBusDriverFactory.ModBusDriverFactory.CoilsYesNo, instance =>
            {
                var register = instance.GetProperty("modbus-register");
                register.Value = 0;

                var table = instance.GetProperty("modbus-table");
                table.Value = ModBusTable.DiscreteInput;

                return instance;
            });

        }

        [Fact]
        public async Task Test_ModBusSlave_PropertyNotFoundException()
        {
            var attribute = await InitSlaveAttribute(ModBusDriverFactory.ModBusDriverFactory.CoilsYesNo, instance =>
            {
                var register = instance.GetProperty("modbus-register");
                register.Value = 0;

                var table = instance.GetProperty("modbus-table");
                table.Value = ModBusTable.DiscreteInput;

                return instance;
            });

            Assert.Throws<PropertyNotFoundException>(() => attribute.DriverContext.NodeInstance.GetProperty("invalid property"));

        }

    }
}
