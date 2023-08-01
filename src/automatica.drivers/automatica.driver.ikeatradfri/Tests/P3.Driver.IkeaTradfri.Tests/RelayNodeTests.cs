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
        public async Task TestWriteTimeout()
        {
            var gateway = await SetupGateway();
            Assert.NotNull(gateway);

            gateway.Driver = CreateMoq(async () => {
                await Task.Delay(200);
                return true;
            });

            var relayNode = gateway.Children[0].Children[0] as IkeaTradfriRelayNode;

            Assert.NotNull(relayNode);

            relayNode.WriteTimeout = 100;
            await relayNode.WriteValue(new DispatchableMock(), true);

            Assert.False(relayNode.LastWriteState);

        }
        [Fact]
        public async Task TestWriteOk()
        {
            var gateway = await SetupGateway();
            Assert.NotNull(gateway);

            gateway.Driver = CreateMoq(async () =>
            {
                await Task.Delay(1);
                return true;
            });

            var relayNode = gateway.Children[0].Children[0] as IkeaTradfriRelayNode;

            Assert.NotNull(relayNode);

            relayNode.WriteTimeout = 100;
            await relayNode.WriteValue(new DispatchableMock(), true);

            Assert.True(relayNode.LastWriteState);

        }
        [Fact]
        public async Task TestWriteFail()
        {
            var gateway = await SetupGateway();
            Assert.NotNull(gateway);

            gateway.Driver = CreateMoq(async () =>
            {
                await Task.Delay(1);
                return false;
            });

            var relayNode = gateway.Children[0].Children[0] as IkeaTradfriRelayNode;

            Assert.NotNull(relayNode);

            relayNode.WriteTimeout = 100;
            await relayNode.WriteValue(new DispatchableMock(), true);

            Assert.False(relayNode.LastWriteState);

        }

        private async Task<IkeaTradfriGateway> SetupGateway()
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

            var driver = await CreateDriver<IkeaTradfriDriverFactory.IkeaTradfri>(root);

            var gateway = driver.Children[0] as IkeaTradfriGateway;
            return gateway;
        }

        private IIkeaTradfriDriver CreateMoq(Func<Task<bool>> returns)
        {
            var tradfriMoq = new Mock<IIkeaTradfriDriver>();

            tradfriMoq.Setup(a => a.SwitchOn(0)).Returns(returns);

            var tradfri = tradfriMoq.Object;

            return tradfri;
        }
    }
}
