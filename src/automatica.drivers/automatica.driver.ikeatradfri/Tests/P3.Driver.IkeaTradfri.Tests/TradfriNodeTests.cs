using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.UnitTests.Base.Drivers;
using Moq;
using P3.Driver.IkeaTradfriDriverFactory;
using P3.Driver.IkeaTradfriDriverFactory.Devices;
using P3.Driver.IkeaTradfriDriverFactory.Devices.Light;
using Tomidix.NetStandard.Tradfri.Models;
using Xunit;
using DispatchableMock = Automatica.Core.UnitTests.Base.Common.DispatchableMock;

namespace P3.Driver.IkeaTradfri.Tests
{
    public class TradfriNodeTests : DriverFactoryTestBase<IkeaTradfriFactory>
    {
        private async Task<(IkeaTradfriGateway Gateway, IkeaTradfriRelayNode Node)> Setup(Guid containerGuid, Guid nodeGuid, Mock<IIkeaTradfriDriver> driver, int deviceId = 65537)
        {
            var root = CreateNodeInstance(IkeaTradfriFactory.GatewayContainerGuid);
            var gw = CreateNodeInstance(IkeaTradfriFactory.GatewayGuid);
            var container = CreateNodeInstance(containerGuid);
            container.SetProperty(IkeaTradfriFactory.DeviceIdPropertyKey, (double)deviceId);
            var node = CreateNodeInstance(nodeGuid);

            root.InverseThis2ParentNodeInstanceNavigation.Add(gw);
            gw.InverseThis2ParentNodeInstanceNavigation.Add(container);
            container.InverseThis2ParentNodeInstanceNavigation.Add(node);

            var tradfri = await CreateDriver<IkeaTradfriDriverFactory.IkeaTradfri>(root);
            var gateway = (IkeaTradfriGateway)tradfri.Children[0];
            gateway.Driver = driver.Object;
            await gateway.Children[0].Init();
            var relay = (IkeaTradfriRelayNode)gateway.Children[0].Children[0];
            relay.WriteTimeout = 2000;
            await relay.Start();
            return (gateway, relay);
        }

        private static async Task Write(IkeaTradfriRelayNode node, object value)
        {
            await node.WriteValue(new DispatchableMock(), new DispatchValue(node.Id, DispatchableType.NodeInstance, value, DateTime.Now, DispatchValueSource.Write));
        }

        private static async Task WaitFor(Func<bool> condition)
        {
            var deadline = DateTime.UtcNow.AddSeconds(5);
            while (!condition() && DateTime.UtcNow < deadline)
            {
                await Task.Delay(10);
            }
        }

        [Fact]
        public async Task LightNode_Write_UsesTheLightControlNotTheOutlet()
        {
            var driver = new Mock<IIkeaTradfriDriver>();
            driver.Setup(d => d.SetLight(It.IsAny<long>(), It.IsAny<bool>())).ReturnsAsync(true);
            var (_, node) = await Setup(IkeaTradfriFactory.LightContainerGuid, IkeaTradfriFactory.LightGuid, driver);

            Assert.IsType<IkeaTradfriLightNode>(node);
            await Write(node, true);
            await WaitFor(() => node.LastWriteState);

            Assert.True(node.LastWriteState);
            driver.Verify(d => d.SetLight(65537, true), Times.Once);
            driver.Verify(d => d.SwitchOn(It.IsAny<long>()), Times.Never);
            driver.Verify(d => d.SwitchOff(It.IsAny<long>()), Times.Never);
        }

        [Fact]
        public async Task LightNode_WriteFalse_SwitchesTheBulbOff()
        {
            var driver = new Mock<IIkeaTradfriDriver>();
            driver.Setup(d => d.SetLight(It.IsAny<long>(), It.IsAny<bool>())).ReturnsAsync(true);
            var (_, node) = await Setup(IkeaTradfriFactory.LightContainerGuid, IkeaTradfriFactory.LightGuid, driver, 7);

            await Write(node, false);
            await WaitFor(() => node.LastWriteState);

            driver.Verify(d => d.SetLight(7, false), Times.Once);
            Assert.False(node.Value);
        }

        [Fact]
        public async Task RelayNode_WriteFalse_SwitchesTheOutletOff()
        {
            var driver = new Mock<IIkeaTradfriDriver>();
            driver.Setup(d => d.SwitchOff(It.IsAny<long>())).ReturnsAsync(true);
            var (_, node) = await Setup(IkeaTradfriFactory.RelayContainerGuid, IkeaTradfriFactory.RelayGuid, driver, 12);

            await Write(node, false);
            await WaitFor(() => node.LastWriteState);

            Assert.True(node.LastWriteState);
            driver.Verify(d => d.SwitchOff(12), Times.Once);
            driver.Verify(d => d.SetLight(It.IsAny<long>(), It.IsAny<bool>()), Times.Never);
        }

        [Fact(Skip = "BUG: with the referenced Automatica.Core.Base 10.0.0.2049 package Task.WithCancellation swallows the task exception, so a failing gateway write keeps LastWriteState (IkeaTradfriRelayNode.cs:64; fixed in src/automatica.core/Automatica.Core.Base/Extensions/AsyncExtensions.cs:30, the driver still builds against the old package)")]
        public async Task RelayNode_DriverThrows_LastWriteStateIsFalse()
        {
            var driver = new Mock<IIkeaTradfriDriver>();
            driver.Setup(d => d.SwitchOn(It.IsAny<long>())).ThrowsAsync(new InvalidOperationException("gateway gone"));
            var (_, node) = await Setup(IkeaTradfriFactory.RelayContainerGuid, IkeaTradfriFactory.RelayGuid, driver);
            node.LastWriteState = true;

            await Write(node, true);
            await WaitFor(() => !node.LastWriteState);

            Assert.False(node.LastWriteState);
        }

        [Fact]
        public async Task Start_ReadsTheDeviceStateFromTheGateway()
        {
            var device = new TradfriDevice { ID = 65537, DeviceType = DeviceType.ControlOutlet };
            device.Control = new();
            device.Control.Add(new() { State = Bool.True });
            var driver = new Mock<IIkeaTradfriDriver>();
            driver.Setup(d => d.GetDevice(65537)).ReturnsAsync(device);
            var (gateway, node) = await Setup(IkeaTradfriFactory.RelayContainerGuid, IkeaTradfriFactory.RelayGuid, driver);

            await gateway.Children[0].Start();

            Assert.True(node.Value);
            driver.Verify(d => d.RegisterChange(It.IsAny<Action<TradfriDevice>>(), 65537), Times.AtLeastOnce);
        }

        [Fact]
        public async Task Update_OutletWithoutControl_IsIgnored()
        {
            var driver = new Mock<IIkeaTradfriDriver>();
            var (_, node) = await Setup(IkeaTradfriFactory.RelayContainerGuid, IkeaTradfriFactory.RelayGuid, driver);

            node.Update(new TradfriDevice());
            node.Update(null);

            Assert.False(node.Value);
        }

        [Fact]
        public async Task Update_LightUsesTheLightControlState()
        {
            var driver = new Mock<IIkeaTradfriDriver>();
            var (_, node) = await Setup(IkeaTradfriFactory.LightContainerGuid, IkeaTradfriFactory.LightGuid, driver);

            var device = new TradfriDevice();
            device.LightControl = new();
            device.LightControl.Add(new() { State = Bool.True });
            node.Update(device);
            Assert.True(node.Value);

            device.LightControl[0].State = Bool.False;
            node.Update(device);
            Assert.False(node.Value);
        }

        [Fact]
        public async Task ChangeCallback_UpdatesTheNode()
        {
            Action<TradfriDevice> callback = null;
            var driver = new Mock<IIkeaTradfriDriver>();
            driver.Setup(d => d.RegisterChange(It.IsAny<Action<TradfriDevice>>(), It.IsAny<long>()))
                .Callback<Action<TradfriDevice>, long>((a, _) => callback = a)
                .Returns(Task.CompletedTask);
            var (gateway, node) = await Setup(IkeaTradfriFactory.RelayContainerGuid, IkeaTradfriFactory.RelayGuid, driver);
            await gateway.Children[0].Start();
            Assert.NotNull(callback);

            var device = new TradfriDevice();
            device.Control = new();
            device.Control.Add(new() { State = Bool.True });
            callback(device);
            Assert.True(node.Value);

            // null and broken devices must not throw out of the gateway callback
            callback(null);
            Assert.True(node.Value);
        }

        private static (string Identity, string Psk) Credentials(IkeaTradfriGateway gateway)
        {
            var identity = typeof(IkeaTradfriGateway).GetField("_identity", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(gateway) as string;
            var psk = typeof(IkeaTradfriGateway).GetField("_appKey", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(gateway) as string;
            return (identity, psk);
        }

        [Theory]
        [InlineData("Automatica.Core-1700000000|made-up-psk", "Automatica.Core-1700000000", "made-up-psk")]
        [InlineData("id|psk|with|pipes", "id", "psk|with|pipes")]
        [InlineData("bare-psk-from-an-old-version", null, null)]
        [InlineData("|psk", null, null)]
        [InlineData("identity|", null, null)]
        [InlineData("", null, null)]
        public async Task Init_ConnectionKey_IsSplitIntoIdentityAndPsk(string stored, string identity, string psk)
        {
            var (gateway, _) = await Setup(IkeaTradfriFactory.RelayContainerGuid, IkeaTradfriFactory.RelayGuid, new Mock<IIkeaTradfriDriver>());
            gateway.DriverContext.NodeInstance.SetProperty(IkeaTradfriFactory.ConnectionPropertyKey, stored);

            await gateway.Init();

            Assert.Equal((identity, psk), Credentials(gateway));
        }

        [Fact]
        public void Factory_SecretAndConnectionKey_ArePasswordProperties()
        {
            var gw = CreateNodeInstance(IkeaTradfriFactory.GatewayGuid);

            Assert.Equal((long)PropertyTemplateType.Password, gw.GetProperty(IkeaTradfriFactory.SecretPropertyKey).This2PropertyTemplateNavigation.This2PropertyType);
            Assert.Equal((long)PropertyTemplateType.Password, gw.GetProperty(IkeaTradfriFactory.ConnectionPropertyKey).This2PropertyTemplateNavigation.This2PropertyType);
            Assert.Equal((long)PropertyTemplateType.Text, gw.GetProperty(IkeaTradfriFactory.IdAddressPropertyKey).This2PropertyTemplateNavigation.This2PropertyType);
        }

        [Fact]
        public async Task Scan_CreatesNodesForLightsAndOutletsOnly()
        {
            var light = new TradfriDevice { ID = 1, Name = "Bulb", DeviceType = DeviceType.Light };
            var outlet = new TradfriDevice { ID = 2, Name = "Plug", DeviceType = DeviceType.ControlOutlet };
            var remote = new TradfriDevice { ID = 3, Name = "Remote", DeviceType = DeviceType.Remote };
            var driver = new Mock<IIkeaTradfriDriver>();
            driver.Setup(d => d.LoadDevices()).ReturnsAsync(new System.Collections.Generic.List<TradfriDevice> { light, outlet, remote });
            var (gateway, _) = await Setup(IkeaTradfriFactory.RelayContainerGuid, IkeaTradfriFactory.RelayGuid, driver, deviceId: 99);

            var nodes = await gateway.Scan();

            Assert.Equal(new[] { "Bulb", "Plug" }, nodes.Select(n => n.Name).ToArray());
            Assert.Equal(IkeaTradfriFactory.LightContainerGuid, nodes[0].This2NodeTemplate);
            Assert.Single(nodes[0].InverseThis2ParentNodeInstanceNavigation);
            Assert.Equal(IkeaTradfriFactory.RelayContainerGuid, nodes[1].This2NodeTemplate);
        }

        [Fact]
        public async Task Scan_SkipsDevicesThatAreAlreadyConfigured()
        {
            var outlet = new TradfriDevice { ID = 65537, Name = "Plug", DeviceType = DeviceType.ControlOutlet };
            var driver = new Mock<IIkeaTradfriDriver>();
            driver.Setup(d => d.LoadDevices()).ReturnsAsync(new System.Collections.Generic.List<TradfriDevice> { outlet });
            var (gateway, _) = await Setup(IkeaTradfriFactory.RelayContainerGuid, IkeaTradfriFactory.RelayGuid, driver);

            Assert.Empty(await gateway.Scan());
        }
    }
}
