using Automatica.Core.EF.Models;
using Automatica.Core.UnitTests.Base.Drivers;
using Xunit;

namespace P3.Driver.Oms.MBus.Tests
{
    public class OmsDriverFactoryTests : DriverFactoryTestBase<OmsDriverFactory.OmsDriverFactory>
    {
        [Fact]
        public void TestOmsDeviceProperties()
        {
            var node = CreateNodeInstance(OmsDriverFactory.OmsDriverFactory.DeviceDriverGuid);

            Assert.True(node.PropertyInstance.Count == 2);

            var propKey = node.GetProperty("mbus-oms-key");
            var propPort = node.GetProperty("mbus-oms-port");

            Assert.NotNull(propKey);
            Assert.NotNull(propPort);

            Assert.Equal((long)PropertyTemplateType.UsbPort, propPort.This2PropertyTemplateNavigation.This2PropertyType);
            Assert.Equal((long)PropertyTemplateType.Text, propKey.This2PropertyTemplateNavigation.This2PropertyType);
        }
    }
}
