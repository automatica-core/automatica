using System;
using System.Threading.Tasks;
using Automatica.Core.UnitTests.Base.Common;
using P3.Driver.ModBusDriverFactory;
using Xunit;

namespace P3.Driver.ModBus.Tests
{
    public class ModBus8ByteRegisterTests : ModBusBaseTest
    {
        [Fact]
        public async Task ModBus8ByteRegister_AB_CD_EF_GH()
        {
            var attribute = await InitAttribute(ModBusDriverFactory.ModBusDriverFactory.Register8ByteGuid, instance =>
            {
                var prop = instance.GetProperty("modbus-byte-order");
                prop.Value = ModBus8ByteOrder.AB_CD_EF_GH;
                return instance;
            } );
            var modBusAttribute = attribute.Attribute;
            var data = modBusAttribute.ConvertValueToBus(123, out var convertedValue);

            var value = modBusAttribute.ConvertValueFromBus( data);
            Assert.True(Convert.ToInt32(value) == 123);
        

            Assert.True(data.Length == 4);
            Assert.True(data[0] == 0);
            Assert.True(data[1] == 0);
            Assert.True(data[2] == 0);
            Assert.True(data[3] == 123);
        }

        [Fact]
        public async Task ModBus8ByteRegister_GH_EF_CD_AB()
        {
            var attribute = await InitAttribute(ModBusDriverFactory.ModBusDriverFactory.Register8ByteGuid, instance =>
            {
                var prop = instance.GetProperty("modbus-byte-order");
                prop.Value = ModBus8ByteOrder.GH_EF_CD_AB;
                return instance;
            });
            var modBusAttribute = attribute.Attribute;
            var data = modBusAttribute.ConvertValueToBus(123, out var convertedValue);
            var value = modBusAttribute.ConvertValueFromBus( data);
            Assert.True(Convert.ToInt32(value) == 123);

            Assert.True(data.Length == 4);
            Assert.True(data[0] == 123);
            Assert.True(data[1] == 0);
            Assert.True(data[2] == 0);
            Assert.True(data[3] == 0);
        }

        [Fact]
        public async Task ModBus8ByteRegister_BA_DC_FE_HG()
        {
            var attribute = await InitAttribute(ModBusDriverFactory.ModBusDriverFactory.Register8ByteGuid, instance =>
            {
                var prop = instance.GetProperty("modbus-byte-order");
                prop.Value = ModBus8ByteOrder.BA_DC_FE_HG;
                return instance;
            });
            var modBusAttribute = attribute.Attribute;
            var data = modBusAttribute.ConvertValueToBus(123, out var convertedValue);
            var value = modBusAttribute.ConvertValueFromBus( data);
            Assert.True(Convert.ToInt32(value) == 123);

            Assert.True(data.Length == 4);
            Assert.True(data[0] == 0);
            Assert.True(data[1] == 0);
            Assert.True(data[2] == 0);
            Assert.True(data[3] == 31488);
        }


        [Fact]
        public async Task ModBus8ByteRegister_HF_FE_DC_BA()
        {
            var attribute = await InitAttribute(ModBusDriverFactory.ModBusDriverFactory.Register8ByteGuid, instance =>
            {
                var prop = instance.GetProperty("modbus-byte-order");
                prop.Value = ModBus8ByteOrder.HF_FE_DC_BA;
                return instance;
            });
            var modBusAttribute = attribute.Attribute;
            var data = modBusAttribute.ConvertValueToBus(123, out var convertedValue);
            var value = modBusAttribute.ConvertValueFromBus( data);
            Assert.True(Convert.ToInt32(value) == 123);

            Assert.True(data.Length == 4);
            Assert.True(data[0] == 31488);
            Assert.True(data[1] == 0);
            Assert.True(data[2] == 0);
            Assert.True(data[3] == 0);
        }


        [Fact]
        public async Task ModBus8ByteRegister_AB_CD_EF_GH2()
        {
            var attribute = await InitAttribute(ModBusDriverFactory.ModBusDriverFactory.Register8ByteGuid, instance =>
            {
                var prop = instance.GetProperty("modbus-byte-order");
                prop.Value = ModBus8ByteOrder.AB_CD_EF_GH;
                return instance;
            });
            var modBusAttribute = attribute.Attribute;
            var data = modBusAttribute.ConvertValueToBus(912312316373, out var convertedValue);
            var value = modBusAttribute.ConvertValueFromBus( data);
            Assert.True(Convert.ToUInt64(value) == 912312316373);

            Assert.True(data.Length == 4);
            Assert.True(data[0] == 0);
            Assert.True(data[1] == 212);
            Assert.True(data[2] == 27149);
            Assert.True(data[3] == 12757);
        }

        [Fact]
        public async Task ModBus8ByteRegister_GH_EF_CD_AB2()
        {
            var attribute = await InitAttribute(ModBusDriverFactory.ModBusDriverFactory.Register8ByteGuid, instance =>
            {
                var prop = instance.GetProperty("modbus-byte-order");
                prop.Value = ModBus8ByteOrder.GH_EF_CD_AB;
                return instance;
            });
            var modBusAttribute = attribute.Attribute;

            var data = modBusAttribute.ConvertValueToBus(912312316373, out var convertedValue);
            var value = modBusAttribute.ConvertValueFromBus( data);
            Assert.True(Convert.ToUInt64(value) == 912312316373);

            Assert.True(data.Length == 4);
            Assert.True(data[3] == 0);
            Assert.True(data[2] == 212);
            Assert.True(data[1] == 27149);
            Assert.True(data[0] == 12757);
        }

        [Fact]
        public async Task ModBus8ByteRegister_BA_DC_FE_HG2()
        {
            var attribute = await InitAttribute(ModBusDriverFactory.ModBusDriverFactory.Register8ByteGuid, instance =>
            {
                var prop = instance.GetProperty("modbus-byte-order");
                prop.Value = ModBus8ByteOrder.BA_DC_FE_HG;
                return instance;
            });
            var modBusAttribute = attribute.Attribute;
            var data = modBusAttribute.ConvertValueToBus(912312316373, out var convertedValue);
            var value = modBusAttribute.ConvertValueFromBus( data);
            Assert.True(Convert.ToUInt64(value) == 912312316373);

            Assert.True(data.Length == 4);
            Assert.True(data[0] == 0);
            Assert.True(data[1] == 54272);
            Assert.True(data[2] == 3434);
            Assert.True(data[3] == 54577); 
        }


        [Fact]
        public async Task ModBus8ByteRegister_HF_FE_DC_BA2()
        {
            var attribute = await InitAttribute(ModBusDriverFactory.ModBusDriverFactory.Register8ByteGuid, instance =>
            {
                var prop = instance.GetProperty("modbus-byte-order");
                prop.Value = ModBus8ByteOrder.HF_FE_DC_BA;
                return instance;
            });
            var modBusAttribute = attribute.Attribute;
            var data = modBusAttribute.ConvertValueToBus(912312316373, out var convertedValue);
            var value = modBusAttribute.ConvertValueFromBus( data);
            Assert.True(Convert.ToUInt64(value) == 912312316373);

            Assert.True(data.Length == 4);
            Assert.True(data[3] == 0);
            Assert.True(data[2] == 54272);
            Assert.True(data[1] == 3434);
            Assert.True(data[0] == 54577);
        }

        [Fact]
        public async Task ModBus8ByteFloat_AB_CD_EF_GH()
        {
            var attribute = await InitAttribute(ModBusDriverFactory.ModBusDriverFactory.Register8ByteFloat, instance =>
            {
                var prop = instance.GetProperty("modbus-byte-order");
                prop.Value = ModBus8ByteOrder.AB_CD_EF_GH;
                return instance;
            });
            var modBusAttribute = attribute.Attribute;
            var data = modBusAttribute.ConvertValueToBus(912312316373.123, out var convertedValue);
            var value = modBusAttribute.ConvertValueFromBus( data);
            Assert.True(Convert.ToDouble(value) == 912312316373.123);

            Assert.True(data.Length == 4);
            Assert.True(data[0] == 17002);
            Assert.True(data[1] == 36161);
            Assert.True(data[2] == 42554);
            Assert.True(data[3] == 41968);

        }

        [Fact]
        public async Task ModBus8ByteFloat_GH_EF_CD_AB()
        {
            var attribute = await InitAttribute(ModBusDriverFactory.ModBusDriverFactory.Register8ByteFloat, instance =>
            {
                var prop = instance.GetProperty("modbus-byte-order");
                prop.Value = ModBus8ByteOrder.GH_EF_CD_AB;
                return instance;
            });
            var modBusAttribute = attribute.Attribute;
            var data = modBusAttribute.ConvertValueToBus(912312316373.123, out var convertedValue);
            var value = modBusAttribute.ConvertValueFromBus( data);
            Assert.True(Convert.ToDouble(value) == 912312316373.123);

            Assert.True(data.Length == 4);
            Assert.True(data[3] == 17002);
            Assert.True(data[2] == 36161);
            Assert.True(data[1] == 42554);
            Assert.True(data[0] == 41968);
        }

        [Fact]
        public async Task ModBus8ByteFloat_BA_DC_FE_HG()
        {
            var attribute = await InitAttribute(ModBusDriverFactory.ModBusDriverFactory.Register8ByteFloat, instance =>
            {
                var prop = instance.GetProperty("modbus-byte-order");
                prop.Value = ModBus8ByteOrder.BA_DC_FE_HG;
                return instance;
            });
            var modBusAttribute = attribute.Attribute;
            var data = modBusAttribute.ConvertValueToBus(912312316373.123, out var convertedValue);
            var value = modBusAttribute.ConvertValueFromBus( data);
            Assert.True(Convert.ToDouble(value) == 912312316373.123);

            Assert.True(data.Length == 4);
            Assert.True(data[0] == 27202);
            Assert.True(data[1] == 16781);
            Assert.True(data[2] == 15014);
            Assert.True(data[3] == 61603);
        }

        [Fact]
        public async Task ModBus8ByteFloat_HF_FE_DC_BA()
        {
            var attribute = await InitAttribute(ModBusDriverFactory.ModBusDriverFactory.Register8ByteFloat, instance =>
            {
                var prop = instance.GetProperty("modbus-byte-order");
                prop.Value = ModBus8ByteOrder.HF_FE_DC_BA;
                return instance;
            });
            var modBusAttribute = attribute.Attribute;
            var data = modBusAttribute.ConvertValueToBus(912312316373.123, out var convertedValue);
            var value = modBusAttribute.ConvertValueFromBus( data);
            Assert.True(Convert.ToDouble(value) == 912312316373.123);

            Assert.True(data.Length == 4);
            Assert.True(data[3] == 27202);
            Assert.True(data[2] == 16781);
            Assert.True(data[1] == 15014);
            Assert.True(data[0] == 61603);
        }
    }
}
