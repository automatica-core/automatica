using Automatica.Core.EF.Models;
using Automatica.Core.UnitTests.Base.Drivers;
using P3.Driver.Knx.DriverFactory.Factories.IpTunneling;
using Xunit;

namespace P3.Driver.Knx.Tests.Factory
{
    public class FactoryTest : DriverFactoryTestBase<KnxIpDriverFactory>
    {
        [Fact]
        public void TestGatewayProperties()
        {
            var node = CreateNodeInstance(KnxIpDriverFactory.KnxGatway);

            Assert.True(node.PropertyInstance.Count >= 3);

            var propIp = node.GetProperty("knx-ip");
            var propPort = node.GetProperty("knx-port");
            var propNat = node.GetProperty("knx-use-nat");

            Assert.NotNull(propIp);
            Assert.NotNull(propPort);
            Assert.NotNull(propNat);

            Assert.Equal((long)PropertyTemplateType.Ip, propIp.This2PropertyTemplateNavigation.This2PropertyType);
            Assert.Equal((long)PropertyTemplateType.Range, propPort.This2PropertyTemplateNavigation.This2PropertyType);
            Assert.Equal((long)PropertyTemplateType.Bool, propNat.This2PropertyTemplateNavigation.This2PropertyType);
        }
    }
}
