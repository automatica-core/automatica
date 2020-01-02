using System;
using System.Threading.Tasks;
using Automatica.Core.UnitTests.Base.Drivers;
using Moq;
using P3.Driver.IkeaTradfriDriverFactory;
using P3.Driver.IkeaTradfriDriverFactory.Devices;
using Xunit;
using DispatchableMock = Automatica.Core.UnitTests.Base.Common.DispatchableMock;

namespace P3.Driver.IkeaTradfri.Tests
{
    public class RelayNodeTests : DriverFactoryTestBase<IkeaTradfriFactory>
    {
        [Fact]
        public async Task TestWrite()
        {
            var root = CreateNodeInstance(IkeaTradfriFactory.GatewayContainerGuid);

            var gw = CreateNodeInstance(IkeaTradfriFactory.GatewayGuid);
                
            root.SetProperty(IkeaTradfriFactory.IdAddressPropertyKey, "id");
            root.SetProperty(IkeaTradfriFactory.SecretPropertyKey, "secret");

            var relay = CreateNodeInstance(IkeaTradfriFactory.RelayContainerGuid);
            var relayState = CreateNodeInstance(IkeaTradfriFactory.RelayGuid);
            
            root.InverseThis2ParentNodeInstanceNavigation.Add(gw);
            gw.InverseThis2ParentNodeInstanceNavigation.Add(relay);
            relay.InverseThis2ParentNodeInstanceNavigation.Add(relayState);

            var driver = CreateDriver<IkeaTradfriDriverFactory.IkeaTradfri>(root);

            var gateway = driver.Children[0] as IkeaTradfriGateway;

            Assert.NotNull(gateway);

            gateway.Driver = CreateMoq();

            var relayNode = driver.Children[0].Children[0].Children[0] as IkeaTradfriRelayNode;

            Assert.NotNull(relayNode);

            relayNode.WriteTimeout = 100;
            await relayNode.WriteValue(new DispatchableMock(), true);

            Assert.True(relayNode.Value);

        }

        private IIkeaTradfriDriver CreateMoq()
        {
            var tradfriMoq = new Mock<IIkeaTradfriDriver>();

            tradfriMoq.Setup(a => a.SwitchOn(0)).Returns(async () =>
            {
                await Task.Delay(200);
            });

            var tradfri = tradfriMoq.Object;

            return tradfri;
        }
    }
}
