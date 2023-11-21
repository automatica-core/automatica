using System.Threading.Tasks;
using Automatica.Core.UnitTests.Base.Common;
using Xunit;

namespace P3.Driver.ModBus.Tests
{
    public class ModBus2ByteRegisterTests : ModBusBaseTest
    {
        [Fact]
        public async Task ModBus2ByteRegister()
        {
            var attribute = await InitAttribute(ModBusDriverFactory.ModBusDriverFactory.Register2ByteGuid);
            var modBusAttribute = attribute.Attribute;
            var data = modBusAttribute.ConvertValueToBus(123, out var convertedValue);

            var value = modBusAttribute.ConvertValueFromBus(data);

            Assert.True((ushort)value == 123);

            Assert.True(data.Length == 1);
            Assert.True(data[0] == 123);
        }
    }
}
