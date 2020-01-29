using System;
using Automatica.Core.UnitTests.Base.Common;
using P3.Driver.ModBusDriverFactory;
using Xunit;

namespace P3.Driver.ModBus.Tests
{
    public class ModBus4ByteRegisterTests : ModBusBaseTest
    {
        [Fact]
        public void ModBus4ByteRegister_AB_CD()
        {
            var attribute = InitAttribute(ModBusDriverFactory.ModBusDriverFactory.Register4ByteGuid, instance =>
            {
                var prop = instance.GetProperty("modbus-byte-order");
                prop.Value = ModBus4ByteOrder.AB_CD;
                return instance;
            } );
            var modBusAttribute = attribute.Attribute;
            var data = modBusAttribute.ConvertValueToBus(DispatchableMock.Instance, 123);
            var value = modBusAttribute.ConvertValueFromBus(DispatchableMock.Instance, data);

            Assert.True((uint)value == 123);

            Assert.True(data.Length == 2);
            Assert.True(data[0] == 0);
            Assert.True(data[1] == 123);
        }

        [Fact]
        public void ModBus4ByteRegister_CD_AB()
        {
            var attribute = InitAttribute(ModBusDriverFactory.ModBusDriverFactory.Register4ByteGuid, instance =>
            {
                var prop = instance.GetProperty("modbus-byte-order");
                prop.Value = ModBus4ByteOrder.CD_AB;
                return instance;
            });
            var modBusAttribute = attribute.Attribute;
            var data = modBusAttribute.ConvertValueToBus(DispatchableMock.Instance, 123);
            var value = modBusAttribute.ConvertValueFromBus(DispatchableMock.Instance, data);

            Assert.True((uint)value == 123);


            Assert.True(data.Length == 2);
            Assert.True(data[1] == 0);
            Assert.True(data[0] == 123);
        }

        [Fact]
        public void ModBus4ByteRegister_BA_DC()
        {
            var attribute = InitAttribute(ModBusDriverFactory.ModBusDriverFactory.Register4ByteGuid, instance =>
            {
                var prop = instance.GetProperty("modbus-byte-order");
                prop.Value = ModBus4ByteOrder.BA_DC;
                return instance;
            });
            var modBusAttribute = attribute.Attribute;
            var data = modBusAttribute.ConvertValueToBus(DispatchableMock.Instance, 123);
            var value = modBusAttribute.ConvertValueFromBus(DispatchableMock.Instance, data);

            Assert.True((uint)value == 123);


            Assert.True(data.Length == 2);
            Assert.True(data[1] == 31488);
            Assert.True(data[0] == 0);
        }


        [Fact]
        public void ModBus4ByteRegister_DC_BA()
        {
            var attribute = InitAttribute(ModBusDriverFactory.ModBusDriverFactory.Register4ByteGuid, instance =>
            {
                var prop = instance.GetProperty("modbus-byte-order");
                prop.Value = ModBus4ByteOrder.DC_BA;
                return instance;
            });
            var modBusAttribute = attribute.Attribute;
            var data = modBusAttribute.ConvertValueToBus(DispatchableMock.Instance, 123);
            var value = modBusAttribute.ConvertValueFromBus(DispatchableMock.Instance, data);

            Assert.True((uint)value == 123);


            Assert.True(data.Length == 2);
            Assert.True(data[0] == 31488);
            Assert.True(data[1] == 0);
        }


        [Fact]
        public void ModBus4ByteRegister_AB_CD2()
        {
            var attribute = InitAttribute(ModBusDriverFactory.ModBusDriverFactory.Register4ByteGuid, instance =>
            {
                var prop = instance.GetProperty("modbus-byte-order");
                prop.Value = ModBus4ByteOrder.AB_CD;
                return instance;
            });
            var modBusAttribute = attribute.Attribute;
            var data = modBusAttribute.ConvertValueToBus(DispatchableMock.Instance, int.MaxValue);

            var value = modBusAttribute.ConvertValueFromBus(DispatchableMock.Instance, data);
            Assert.True(Convert.ToInt32(value) == int.MaxValue);

            Assert.True(data.Length == 2);
            Assert.True(data[0] == short.MaxValue);
            Assert.True(data[1] == ushort.MaxValue);

            data = modBusAttribute.ConvertValueToBus(DispatchableMock.Instance, uint.MaxValue);
            value = modBusAttribute.ConvertValueFromBus(DispatchableMock.Instance, data);

            Assert.True(data.Length == 2);
            Assert.True(data[0] == ushort.MaxValue);
            Assert.True(data[1] == ushort.MaxValue);
            Assert.True((uint)value == uint.MaxValue);
        }

        [Fact]
        public void ModBus4ByteRegister_CD_AB2()
        {
            var attribute = InitAttribute(ModBusDriverFactory.ModBusDriverFactory.Register4ByteGuid, instance =>
            {
                var prop = instance.GetProperty("modbus-byte-order");
                prop.Value = ModBus4ByteOrder.CD_AB;
                return instance;
            });
            var modBusAttribute = attribute.Attribute;
            var data = modBusAttribute.ConvertValueToBus(DispatchableMock.Instance, int.MaxValue);
            var value = modBusAttribute.ConvertValueFromBus(DispatchableMock.Instance, data);
            Assert.True(Convert.ToInt32(value) == int.MaxValue);

            Assert.True(data.Length == 2);
            Assert.True(data[1] == short.MaxValue);
            Assert.True(data[0] == ushort.MaxValue);

            data = modBusAttribute.ConvertValueToBus(DispatchableMock.Instance, uint.MaxValue);
            value = modBusAttribute.ConvertValueFromBus(DispatchableMock.Instance, data);
            Assert.True((uint)value == uint.MaxValue);

            Assert.True(data.Length == 2);
            Assert.True(data[1] == ushort.MaxValue);
            Assert.True(data[0] == ushort.MaxValue);
        }

        [Fact]
        public void ModBus4ByteRegister_BA_DC2()
        {
            var attribute = InitAttribute(ModBusDriverFactory.ModBusDriverFactory.Register4ByteGuid, instance =>
            {
                var prop = instance.GetProperty("modbus-byte-order");
                prop.Value = ModBus4ByteOrder.BA_DC;
                return instance;
            });
            var modBusAttribute = attribute.Attribute;
            var data = modBusAttribute.ConvertValueToBus(DispatchableMock.Instance, int.MaxValue);
            var value = modBusAttribute.ConvertValueFromBus(DispatchableMock.Instance, data);
            Assert.True(Convert.ToInt32(value) == int.MaxValue);

            Assert.True(data.Length == 2);
            Assert.True(data[0] == 65407);
            Assert.True(data[1] == ushort.MaxValue);

            data = modBusAttribute.ConvertValueToBus(DispatchableMock.Instance, uint.MaxValue);
            value = modBusAttribute.ConvertValueFromBus(DispatchableMock.Instance, data);
            Assert.True((uint)value == uint.MaxValue);

            Assert.True(data.Length == 2);
            Assert.True(data[0] == ushort.MaxValue);
            Assert.True(data[1] == ushort.MaxValue);
        }


        [Fact]
        public void ModBus4ByteRegister_DC_BA2()
        {
            var attribute = InitAttribute(ModBusDriverFactory.ModBusDriverFactory.Register4ByteGuid, instance =>
            {
                var prop = instance.GetProperty("modbus-byte-order");
                prop.Value = ModBus4ByteOrder.DC_BA;
                return instance;
            });
            var modBusAttribute = attribute.Attribute;
            var data = modBusAttribute.ConvertValueToBus(DispatchableMock.Instance, int.MaxValue);
            var value = modBusAttribute.ConvertValueFromBus(DispatchableMock.Instance, data);
            Assert.True(Convert.ToInt32(value) == int.MaxValue);

            Assert.True(data.Length == 2);
            Assert.True(data[1] == 65407);
            Assert.True(data[0] == ushort.MaxValue);

            data = modBusAttribute.ConvertValueToBus(DispatchableMock.Instance, uint.MaxValue);
            value = modBusAttribute.ConvertValueFromBus(DispatchableMock.Instance, data);
            Assert.True((uint)value == uint.MaxValue);

            Assert.True(data.Length == 2);
            Assert.True(data[0] == ushort.MaxValue);
            Assert.True(data[1] == ushort.MaxValue);
        }


        [Fact]
        public void ModBus4ByteFloatRegister_AB_CD()
        {
            var attribute = InitAttribute(ModBusDriverFactory.ModBusDriverFactory.Register4ByteFloat, instance =>
            {
                var prop = instance.GetProperty("modbus-byte-order");
                prop.Value = ModBus4ByteOrder.AB_CD;
                return instance;
            });
            var modBusAttribute = attribute.Attribute;
            var data = modBusAttribute.ConvertValueToBus(DispatchableMock.Instance, 1234567.8912);
            var value = modBusAttribute.ConvertValueFromBus(DispatchableMock.Instance, data);
            Assert.True(Convert.ToDouble(value) == 1234567.875d);


            Assert.True(data.Length == 2);
            Assert.True(data[0] == 18838);
            Assert.True(data[1] == 46143);
        }

        [Fact]
        public void ModBus4ByteFloatRegister_CD_AB()
        {
            var attribute = InitAttribute(ModBusDriverFactory.ModBusDriverFactory.Register4ByteFloat, instance =>
            {
                var prop = instance.GetProperty("modbus-byte-order");
                prop.Value = ModBus4ByteOrder.CD_AB;
                return instance;
            });
            var modBusAttribute = attribute.Attribute;
            var data = modBusAttribute.ConvertValueToBus(DispatchableMock.Instance, 1234567.8912);
            var value = modBusAttribute.ConvertValueFromBus(DispatchableMock.Instance, data);
            Assert.True(Convert.ToDouble(value) == 1234567.875d);

            Assert.True(data.Length == 2);
            Assert.True(data[1] == 18838);
            Assert.True(data[0] == 46143);
        }

        [Fact]
        public void ModBus4ByteFloatRegiste_BA_DC()
        {
            var attribute = InitAttribute(ModBusDriverFactory.ModBusDriverFactory.Register4ByteFloat, instance =>
            {
                var prop = instance.GetProperty("modbus-byte-order");
                prop.Value = ModBus4ByteOrder.BA_DC;
                return instance;
            });
            var modBusAttribute = attribute.Attribute;
            var data = modBusAttribute.ConvertValueToBus(DispatchableMock.Instance, 1234567.8912);
            var value = modBusAttribute.ConvertValueFromBus(DispatchableMock.Instance, data);
            Assert.True(Convert.ToDouble(value) == 1234567.875d);


            Assert.True(data.Length == 2);
            Assert.True(data[0] == 38473);
            Assert.True(data[1] == 16308);
        }

        [Fact]
        public void ModBus4ByteFloatRegiste_DC_BA()
        {
            var attribute = InitAttribute(ModBusDriverFactory.ModBusDriverFactory.Register4ByteFloat, instance =>
            {
                var prop = instance.GetProperty("modbus-byte-order");
                prop.Value = ModBus4ByteOrder.DC_BA;
                return instance;
            });
            var modBusAttribute = attribute.Attribute;
            var data = modBusAttribute.ConvertValueToBus(DispatchableMock.Instance, 1234567.8912);
            var value = modBusAttribute.ConvertValueFromBus(DispatchableMock.Instance, data);
            Assert.True(Convert.ToDouble(value) == 1234567.875d);

            Assert.True(data.Length == 2);
            Assert.True(data[1] == 38473);
            Assert.True(data[0] == 16308);
        }
    }
}
