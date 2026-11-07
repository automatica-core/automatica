using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Automatica.Core.EF.Models;
using Automatica.Core.UnitTests.Base.Drivers;
using P3.Driver.HueBridgeSimulator.DriverFactory.OnOff;
using P3.Driver.HueBridgeSimulatorDriverFactory;
using Xunit;
using HueFactory = P3.Driver.HueBridgeSimulator.DriverFactory.HueBridgeSimulatorDriverFactory;

namespace P3.Driver.HueBridge.Tests
{
    /// <summary>
    /// The automatica driver around <see cref="HueDriver"/>, built from node instances. Never started: Start() publishes SSDP on the LAN.
    /// </summary>
    public class HueBridgeDriverTests : DriverFactoryTestBase<HueFactory>
    {
        private NodeInstance CreateBridgeNode(string bindAddress = "127.0.0.1", int port = 18080, string users = null)
        {
            var node = CreateNodeInstance(HueFactory.BusId);
            node.SetProperty(HueFactory.BindAddressKey, bindAddress);
            node.SetProperty(HueFactory.PortKey, port);
            if (users != null)
            {
                node.SetProperty(HueFactory.UsersKey, users);
            }
            return node;
        }

        private async Task<(HueBridgeDriver Driver, HueDriver Hue)> CreateBridgeAsync(NodeInstance node)
        {
            var driver = (HueBridgeDriver)await CreateDriver(node);
            // HueDriver registers itself as the static instance on construction
            return (driver, HueDriver.Instance);
        }

        [Fact]
        public async Task Create_WithBindAddressAndPort_ConfiguresHueDriver()
        {
            var (_, hue) = await CreateBridgeAsync(CreateBridgeNode("127.0.0.1", 18081));

            Assert.Equal(IPAddress.Loopback, hue.BindAddress);
            Assert.Equal(18081, hue.Port);
        }

        [Theory]
        [InlineData("")]
        [InlineData("not-an-ip")]
        public async Task Create_InvalidBindAddress_FallsBackToAny(string bindAddress)
        {
            var (_, hue) = await CreateBridgeAsync(CreateBridgeNode(bindAddress));

            Assert.Equal(IPAddress.Any, hue.BindAddress);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-5)]
        [InlineData(70000)]
        public async Task Create_InvalidPort_FallsBackToDefaultPort(int port)
        {
            var (_, hue) = await CreateBridgeAsync(CreateBridgeNode(port: port));

            Assert.Equal(HueDriver.DefaultPort, hue.Port);
        }

        [Fact]
        public async Task Create_WithPersistedUsers_LoadsValidUsers()
        {
            var (_, hue) = await CreateBridgeAsync(CreateBridgeNode(users: $"{HueTestFactory.ValidUser},,bad,{HueTestFactory.OtherValidUser}"));

            Assert.Equal(new[] { HueTestFactory.OtherValidUser, HueTestFactory.ValidUser }, hue.GetUserNames().OrderBy(u => u, StringComparer.Ordinal));
            Assert.True(hue.IsAuthorized(HueTestFactory.ValidUser));
        }

        [Fact]
        public async Task Create_WithoutUsers_HasEmptyWhitelist()
        {
            var (_, hue) = await CreateBridgeAsync(CreateBridgeNode());

            Assert.Empty(hue.GetUserNames());
        }

        [Fact]
        public async Task CustomAction_LinkButton_OpensTheLinkButtonWindow()
        {
            var (driver, hue) = await CreateBridgeAsync(CreateBridgeNode());
            Assert.False(hue.IsLinkButtonPressed);

            // never awaited: see CustomAction_Awaited_Completes
            _ = driver.CustomAction(HueFactory.LinkButtonKey);

            Assert.True(hue.IsLinkButtonPressed);
        }

        [Fact]
        public async Task CustomAction_RemoveUsers_ClearsTheWhitelist()
        {
            var (driver, hue) = await CreateBridgeAsync(CreateBridgeNode(users: HueTestFactory.ValidUser));

            // never awaited: see CustomAction_Awaited_Completes
            _ = driver.CustomAction(HueFactory.RemoveUsersKey);

            Assert.Empty(hue.GetUserNames());
            Assert.False(hue.IsAuthorized(HueTestFactory.ValidUser));
        }

        [Fact(Skip = "BUG: DriverBase.CustomAction in the consumed Automatica.Core.Driver 10.0.0.2049 package returns an unstarted Task (new Task(...)), so awaiting HueBridgeDriver.CustomAction never completes (fixed in source by aaea5026, package not republished)")]
        public async Task CustomAction_Awaited_Completes()
        {
            var (driver, _) = await CreateBridgeAsync(CreateBridgeNode());

            var task = driver.CustomAction(HueFactory.LinkButtonKey);

            Assert.Same(task, await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(5))));
        }

        [Fact]
        public async Task CustomAction_Unknown_ChangesNothing()
        {
            var (driver, hue) = await CreateBridgeAsync(CreateBridgeNode(users: HueTestFactory.ValidUser));

            _ = driver.CustomAction("something-else");

            Assert.False(hue.IsLinkButtonPressed);
            Assert.Single(hue.GetUserNames());
        }

        [Fact]
        public async Task Stop_NeverStarted_ReturnsTrue()
        {
            var (driver, _) = await CreateBridgeAsync(CreateBridgeNode());

            var stopped = await driver.Stop();

            Assert.True(stopped);
        }

        [Fact(Skip = "BUG: DriverBase.Stop in the consumed Automatica.Core.Driver 10.0.0.2049 package disposes its CancellationTokenSource, a second Stop throws ObjectDisposedException (fixed in source by aaea5026, package not republished)")]
        public async Task Stop_CalledTwice_DoesNotThrow()
        {
            var (driver, _) = await CreateBridgeAsync(CreateBridgeNode());

            await driver.Stop();
            await driver.Stop();
        }

        [Fact]
        public async Task Configure_OnOffLightChild_RegistersAHueLight()
        {
            var node = CreateBridgeNode();
            var light = CreateNodeInstance(HueFactory.OnOffLight);
            light.Name = "Living room";
            node.InverseThis2ParentNodeInstanceNavigation.Add(light);

            var (driver, hue) = await CreateBridgeAsync(node);

            var onOff = Assert.IsType<HueOnOffNode>(Assert.Single(driver.Children));
            Assert.Equal(1, onOff.LightNumber);
            var hueLight = hue.Lights[onOff.LightNumber];
            Assert.Equal("Living room", hueLight.Name);
            Assert.Equal(light.ObjId.ToString(), hueLight.UniqueId);
        }

        [Fact]
        public async Task Configure_TwoOnOffLights_GetDistinctLightNumbers()
        {
            var node = CreateBridgeNode();
            node.InverseThis2ParentNodeInstanceNavigation.Add(CreateNodeInstance(HueFactory.OnOffLight));
            node.InverseThis2ParentNodeInstanceNavigation.Add(CreateNodeInstance(HueFactory.OnOffLight));

            var (driver, hue) = await CreateBridgeAsync(node);

            var numbers = driver.Children.Cast<HueOnOffNode>().Select(n => n.LightNumber).OrderBy(n => n).ToArray();
            Assert.Equal(new[] { 1, 2 }, numbers);
            Assert.Equal(2, hue.Lights.Count);
        }

        [Fact]
        public async Task SetLightState_ThroughBridgeDriver_DoesNotRaiseSwitchLight()
        {
            var node = CreateBridgeNode();
            node.InverseThis2ParentNodeInstanceNavigation.Add(CreateNodeInstance(HueFactory.OnOffLight));
            var (driver, hue) = await CreateBridgeAsync(node);
            var raised = 0;
            hue.SwitchLight += (_, _) => raised++;

            driver.SetLightState(1, new Data.HueSwitchLightData(true, 100));

            Assert.True(hue.Lights[1].State.On);
            Assert.Equal(0, raised);
        }
    }
}
