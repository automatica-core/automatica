using System;
using Automatica.Core.Base.IO;
using Automatica.Core.Driver.Exceptions;
using Automatica.Core.EF.Exceptions;
using Automatica.Core.UnitTests.Common;
using P3.Driver.ModBusDriver;
using P3.Driver.ModBusDriver.Exceptions;
using P3.Driver.ModBusDriver.Slave;
using P3.Driver.ModBusDriver.Slave.Tcp;
using P3.Driver.ModBusDriverFactory;
using P3.Driver.ModBusDriverFactory.Slave;
using Xunit;

namespace P3.Driver.ModBus.Tests
{
    public class ModBusSlaveTests : ModBusBaseTest
    {
        [Fact]
        public void Test_HoldingRegisters_4Byte()
        {
            var attribute = InitSlaveAttribute(ModBusDriverFactory.ModBusDriverFactory.Register4ByteGuid, instance =>
            {
                var register = instance.GetProperty("modbus-register");
                register.Value = 0;

                var table = instance.GetProperty("modbus-table");
                table.Value = ModBusTable.HoldingRegister;

                var prop = instance.GetProperty("modbus-byte-order");
                prop.Value = ModBus4ByteOrder.AB_CD;
                return instance;
            });

            attribute.WriteValue(DispatchableMock.Instance, 123);

            Assert.True(attribute.Driver.GetHoldingRegister(0, 0) == 0);
            Assert.True(attribute.Driver.GetHoldingRegister(0, 1) == 123);

            Assert.True(DispatcherMock.Instance.GetValues(DispatchableType.NodeInstance).Count > 0);
        }

        [Fact]
        public void Test_HoldingRegisters_2Byte()
        {
            var attribute = InitSlaveAttribute(ModBusDriverFactory.ModBusDriverFactory.Register2ByteGuid, instance =>
            {
                var register = instance.GetProperty("modbus-register");
                register.Value = 0;

                var table = instance.GetProperty("modbus-table");
                table.Value = ModBusTable.HoldingRegister;
                return instance;
            });

            attribute.WriteValue(DispatchableMock.Instance, 123);
            
            Assert.True(attribute.Driver.GetHoldingRegister(0, 0) == 123);
        }


        [Fact]
        public void Test_HoldingRegisters_8Byte()
        {
            var attribute = InitSlaveAttribute(ModBusDriverFactory.ModBusDriverFactory.Register8ByteGuid, instance =>
            {
                var register = instance.GetProperty("modbus-register");
                register.Value = 0;

                var table = instance.GetProperty("modbus-table");
                table.Value = ModBusTable.HoldingRegister;


                var prop = instance.GetProperty("modbus-byte-order");
                prop.Value = ModBus8ByteOrder.AB_CD_EF_GH;
                return instance;
            });

            attribute.WriteValue(DispatchableMock.Instance, 123);

            Assert.True(attribute.Driver.GetHoldingRegister(0, 0) == 0);
            Assert.True(attribute.Driver.GetHoldingRegister(0, 1) == 0);
            Assert.True(attribute.Driver.GetHoldingRegister(0, 2) == 0);
            Assert.True(attribute.Driver.GetHoldingRegister(0, 3) == 123);
        }

        [Fact]
        public void Test_InputRegisters_4Byte()
        {
            var attribute = InitSlaveAttribute(ModBusDriverFactory.ModBusDriverFactory.Register4ByteGuid, instance =>
            {
                var register = instance.GetProperty("modbus-register");
                register.Value = 0;

                var table = instance.GetProperty("modbus-table");
                table.Value = ModBusTable.InputRegister;

                var prop = instance.GetProperty("modbus-byte-order");
                prop.Value = ModBus4ByteOrder.AB_CD;
                return instance;
            });

            attribute.WriteValue(DispatchableMock.Instance, 999);

            Assert.True(attribute.Driver.GetInputRegister(0, 0) == 0);
            Assert.True(attribute.Driver.GetInputRegister(0, 1) == 999);


        }

        [Fact]
        public void Test_InputRegisters_2Byte()
        {
            var attribute = InitSlaveAttribute(ModBusDriverFactory.ModBusDriverFactory.Register2ByteGuid, instance =>
            {
                var register = instance.GetProperty("modbus-register");
                register.Value = 0;

                var table = instance.GetProperty("modbus-table");
                table.Value = ModBusTable.InputRegister;
                return instance;
            });

            attribute.WriteValue(DispatchableMock.Instance, 999);

            Assert.True(attribute.Driver.GetInputRegister(0, 0) == 999);
        }


        [Fact]
        public void Test_InputRegisters_8Byte()
        {
            var attribute = InitSlaveAttribute(ModBusDriverFactory.ModBusDriverFactory.Register8ByteGuid, instance =>
            {
                var register = instance.GetProperty("modbus-register");
                register.Value = 0;

                var table = instance.GetProperty("modbus-table");
                table.Value = ModBusTable.InputRegister;


                var prop = instance.GetProperty("modbus-byte-order");
                prop.Value = ModBus8ByteOrder.AB_CD_EF_GH;
                return instance;
            });

            attribute.WriteValue(DispatchableMock.Instance, 999);

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
        public void Test_Coils()
        {
            var attribute = InitSlaveAttribute(ModBusDriverFactory.ModBusDriverFactory.CoilsYesNo, instance =>
            {
                var register = instance.GetProperty("modbus-register");
                register.Value = 0;

                var table = instance.GetProperty("modbus-table");
                table.Value = ModBusTable.Coil;
                return instance;
            });

            attribute.WriteValue(DispatchableMock.Instance, true);

            Assert.True(attribute.Driver.GetCoil(0, 0));
            attribute.WriteValue(DispatchableMock.Instance, false);

            Assert.False(attribute.Driver.GetCoil(0, 0));

            Assert.ThrowsAsync<InvalidInputValueException>(async () => await attribute.WriteValue(DispatchableMock.Instance, "asdf"));
        }

        [Fact]
        public void Test_DiscreteInput()
        {
            var attribute = InitSlaveAttribute(ModBusDriverFactory.ModBusDriverFactory.CoilsYesNo, instance =>
            {
                var register = instance.GetProperty("modbus-register");
                register.Value = 0;

                var table = instance.GetProperty("modbus-table");
                table.Value = ModBusTable.DiscreteInput;

                return instance;
            });
            attribute.WriteValue(DispatchableMock.Instance, true);
            Assert.True(attribute.Driver.GetDiscreteInput(0, 0));

            attribute.WriteValue(DispatchableMock.Instance, false);
            Assert.False(attribute.Driver.GetDiscreteInput(0, 0));

        }

        [Fact]
        public void Test_ModBusBinary_InvalidInputException()
        {
            var attribute = InitSlaveAttribute(ModBusDriverFactory.ModBusDriverFactory.CoilsYesNo, instance =>
            {
                var register = instance.GetProperty("modbus-register");
                register.Value = 0;

                var table = instance.GetProperty("modbus-table");
                table.Value = ModBusTable.DiscreteInput;

                return instance;
            });

            Assert.ThrowsAsync<InvalidInputValueException>(async () => await attribute.WriteValue(DispatchableMock.Instance, "asdf"));
        }

        [Fact]
        public void Test_ModBusBinary_GetBinaryValues()
        {
            var attribute = InitSlaveAttribute(ModBusDriverFactory.ModBusDriverFactory.CoilsYesNo, instance =>
            {
                var register = instance.GetProperty("modbus-register");
                register.Value = 0;

                var table = instance.GetProperty("modbus-table");
                table.Value = ModBusTable.DiscreteInput;

                return instance;
            });

            attribute.WriteValue(DispatchableMock.Instance, true);
            var modBusSlaveDriver = attribute.Driver as ModBusSlaveDriver<ModBusSlaveTcpConfig>;
            var bytes = modBusSlaveDriver.GetBinaryValues(modBusSlaveDriver.GetDiscreteInput(0, 0, 1));

            Assert.True(bytes.Length == 1);
            Assert.True(bytes[0] == 1);
        }

        [Fact]
        public void Test_ModBusBinary_IllegalAddressException()
        {
            var attribute = InitSlaveAttribute(ModBusDriverFactory.ModBusDriverFactory.CoilsYesNo, instance =>
            {
                var register = instance.GetProperty("modbus-register");
                register.Value = 0;

                var table = instance.GetProperty("modbus-table");
                table.Value = ModBusTable.DiscreteInput;

                return instance;
            });

            var modBusSlaveDriver = attribute.Driver as ModBusSlaveDriver<ModBusSlaveTcpConfig>;
            Assert.Throws<IllegalDataAddressException>(() =>
                modBusSlaveDriver.GetCoilValues(modBusSlaveDriver.GetCoils(0, 0, 1)));
        }

        [Fact]
        public void Test_ModBusSlave_PropertyNotFoundException()
        {
            var attribute = InitSlaveAttribute(ModBusDriverFactory.ModBusDriverFactory.CoilsYesNo, instance =>
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
