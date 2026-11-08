using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.Driver.LeanMode;
using Automatica.Core.EF.Models;
using Automatica.Core.UnitTests.Base.Common;
using Automatica.Core.UnitTests.Base.Drivers;
using Automatica.Driver.Shelly.CoIoT;
using Automatica.Driver.Shelly.Unit.Tests.Fakes;
using Automatica.Driver.ShellyFactory;
using Automatica.Driver.ShellyFactory.Generic;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Automatica.Driver.Shelly.Unit.Tests
{
    /// <summary>
    /// The generic Gen1 and Gen2+ devices running in the real driver against recorded fake devices on 127.0.0.1:
    /// status → data points, notifications, CoIoT, writes, auth, sleeping devices, learning and the scan.
    /// </summary>
    public class ShellyGenericDeviceTests : DriverFactoryTestBase<ShellyFactory.ShellyFactory>, IAsyncLifetime
    {
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

        private FakeShellyServer _server;
        private ShellyDriver _driver;
        private readonly CapturingLogger _logger = new();
        private readonly ConcurrentQueue<(Guid Source, object Value)> _dispatched = new();
        private readonly Mock<IDispatcher> _dispatcher = new();
        private readonly Mock<ILearnMode> _learnMode = new();

        public ShellyGenericDeviceTests()
        {
            _dispatcher.Setup(a => a.DispatchValue(It.IsAny<IDispatchable>(), It.IsAny<object>(), It.IsAny<DispatchValueSource>()))
                .Callback<IDispatchable, object, DispatchValueSource>((source, value, _) => _dispatched.Enqueue((source.Id, value)))
                .Returns(Task.CompletedTask);
        }

        public async Task InitializeAsync() => _server = await FakeShellyServer.StartAsync();

        public async Task DisposeAsync()
        {
            if (_driver != null)
            {
                await _driver.Stop();
            }
            await _server.DisposeAsync();
        }

        private NodeInstance Build(string fixture, string address, string password = "", Action<JObject> change = null)
        {
            var json = FakeShellyDevice.Load(fixture);
            change?.Invoke(json);
            if (json.Value<int>("gen") >= 2)
            {
                return ShellyNodeBuilder.BuildRpc(Factory, (JObject)json["deviceinfo"], (JObject)json["status"], json["config"] as JObject, address, password);
            }

            return ShellyNodeBuilder.BuildGen1(Factory, (JObject)json["shelly"], (JObject)json["status"], json["settings"] as JObject,
                json["citd"]?.ToString(Newtonsoft.Json.Formatting.None), address, new ShellyCredentials(null, password));
        }

        private async Task<ShellyDriver> StartDriver(Action<ShellyDriver> setup, params NodeInstance[] devices)
        {
            var root = CreateNodeInstance(ShellyFactory.ShellyFactory.DriverId);
            foreach (var device in devices)
            {
                device.SetProperty(ShellyGenericTemplates.PollingIntervalKey, 3_600_000); // no timer poll during a test
                device.This2ParentNodeInstanceNavigation = root;
                device.This2ParentNodeInstance = root.ObjId;
                root.InverseThis2ParentNodeInstanceNavigation.Add(device);
            }

            var ctx = new TestDriverContext(root, DriverFactory, Factory, _dispatcher.Object, _logger) { LearnMode = _learnMode.Object };
            _driver = (ShellyDriver)DriverFactory.CreateDriver(ctx);
            _driver.CoIoTPort = 0;
            _driver.CoIoTJoinMulticast = false;
            _driver.ProbeTimeout = TimeSpan.FromSeconds(3);
            setup?.Invoke(_driver);

            Assert.True(await _driver.Configure());
            Assert.True(await _driver.Start());
            await _driver.Started();
            return _driver;
        }

        private static async Task WaitFor(Func<bool> condition, string what)
        {
            var until = DateTime.UtcNow + Timeout;
            while (!condition())
            {
                if (DateTime.UtcNow > until)
                {
                    Assert.Fail($"timeout waiting for {what}");
                }
                await Task.Delay(20);
            }
        }

        private ShellyGenericDevice Device(int index = 0) => _driver.GenericDevices[index];

        private static ShellyDataPointNode Dp(ShellyGenericDevice device, string component, string key) =>
            device.Components.Single(a => a.Key == component).DataPoints.Single(a => a.DataPoint.Key == key);

        private static Task Write(ShellyDataPointNode node, object value) =>
            node.WriteValue(DispatchableMock.Instance, new DispatchValue(Guid.NewGuid(), DispatchableType.NodeInstance, value, DateTime.Now, DispatchValueSource.Write));

        private async Task WaitForValue(ShellyGenericDevice device, string component, string key, object expected)
        {
            await WaitFor(() => device.Components.Any(a => a.Key == component) && Equals(Dp(device, component, key).LastValue, expected),
                $"{component}.{key} = {expected} (is {(device.Components.Any(a => a.Key == component) ? Dp(device, component, key).LastValue : "missing")})");
        }

        // ---------------------------------------------------------------- Gen2+

        [Fact]
        public async Task Rpc_Start_ReadsTheStatusIntoTheDataPoints()
        {
            var fake = new FakeShellyDevice(_server, "gen2_plus1pm");
            await StartDriver(null, Build("gen2_plus1pm", _server.Address));

            var device = Device();
            await WaitFor(() => device.Online, "online");
            Assert.IsType<ShellyRpcDevice>(device);
            await WaitForValue(device, "switch:0", "output", true);
            await WaitForValue(device, "switch:0", "power", 12.5);
            await WaitForValue(device, "switch:0", "energy", 1234.567);
            await WaitForValue(device, "input:0", "state", false);
            await WaitForValue(device, "wifi", "rssi", -58);

            var output = Dp(device, "switch:0", "output");
            Assert.Contains(_dispatched, a => a.Source == output.Id && Equals(a.Value, true));
            Assert.Contains(fake.Calls, a => a.Method == "Shelly.GetDeviceInfo");
        }

        [Fact]
        public async Task Rpc_WebSocketNotifications_UpdateOnlyTheChangedValues()
        {
            _ = new FakeShellyDevice(_server, "gen2_plus1pm");
            await StartDriver(null, Build("gen2_plus1pm", _server.Address));
            var device = Device();
            await WaitForValue(device, "switch:0", "output", true);

            var hello = JObject.Parse(await _server.ReceiveWebSocketMessageAsync(Timeout));
            Assert.Equal("Shelly.GetStatus", hello.Value<string>("method"));

            await _server.SendWebSocketMessageAsync("{\"src\":\"shellyplus1pm-a8032ab12345\",\"method\":\"NotifyStatus\",\"params\":{\"ts\":1,\"switch:0\":{\"id\":0,\"apower\":20.25,\"aenergy\":{\"total\":1240.0}}}}", Timeout);
            await WaitForValue(device, "switch:0", "power", 20.25);
            await WaitForValue(device, "switch:0", "energy", 1240.0);
            Assert.Equal(true, Dp(device, "switch:0", "output").LastValue);

            await _server.SendWebSocketMessageAsync("{\"src\":\"shellyplus1pm-a8032ab12345\",\"method\":\"NotifyStatus\",\"params\":{\"switch:0\":{\"id\":0,\"output\":false}}}", Timeout);
            await WaitForValue(device, "switch:0", "output", false);
        }

        [Fact]
        public async Task Rpc_NotifyEvent_DispatchesButtonEventsEveryTime()
        {
            _ = new FakeShellyDevice(_server, "gen2_plusi4");
            await StartDriver(null, Build("gen2_plusi4", _server.Address));
            var device = Device();
            await WaitFor(() => device.Online, "online");
            await _server.ReceiveWebSocketMessageAsync(Timeout);

            var e = Dp(device, "input:2", "event");
            const string push = "{\"src\":\"shellyplusi4-b0b21c1a2b3c\",\"method\":\"NotifyEvent\",\"params\":{\"ts\":1,\"events\":[{\"component\":\"input:2\",\"id\":2,\"event\":\"single_push\",\"ts\":1}]}}";
            await _server.SendWebSocketMessageAsync(push, Timeout);
            await _server.SendWebSocketMessageAsync(push, Timeout);

            await WaitFor(() => _dispatched.Count(a => a.Source == e.Id && Equals(a.Value, "single_push")) == 2, "two single_push events");
            Assert.Null(Dp(device, "input:0", "event").LastValue);
        }

        [Fact]
        public async Task Rpc_Writes_CallTheComponentMethods()
        {
            var fake = new FakeShellyDevice(_server, "gen2_plus1pm");
            await StartDriver(null, Build("gen2_plus1pm", _server.Address));
            var device = Device();
            await WaitForValue(device, "switch:0", "output", true);

            await Write(Dp(device, "switch:0", "output"), false);
            await WaitFor(() => fake.Calls.Any(a => a.Method == "Switch.Set"), "Switch.Set");
            var set = fake.Calls.Single(a => a.Method == "Switch.Set");
            Assert.Equal(0, set.Params.Value<int>("id"));
            Assert.False(set.Params.Value<bool>("on"));

            await Write(Dp(device, "switch:0", "toggle"), true);
            await WaitFor(() => fake.Calls.Any(a => a.Method == "Switch.Toggle"), "Switch.Toggle");

            await Write(Dp(device, "sys", "reboot"), false);
            await Task.Delay(200);
            Assert.DoesNotContain(fake.Calls, a => a.Method == "Shelly.Reboot");
        }

        [Fact]
        public async Task Rpc_CoverAndRgbw_WriteWithTheCachedStatus()
        {
            var cover = new FakeShellyDevice(_server, "gen2_plus2pm_cover");
            await StartDriver(null, Build("gen2_plus2pm_cover", _server.Address));
            var device = Device();
            await WaitForValue(device, "cover:0", "position", 40);
            await WaitForValue(device, "cover:0", "state", "stopped");

            await Write(Dp(device, "cover:0", "position"), 70.4);
            await WaitFor(() => cover.Calls.Any(a => a.Method == "Cover.GoToPosition"), "Cover.GoToPosition");
            Assert.Equal(70, cover.Calls.Single(a => a.Method == "Cover.GoToPosition").Params.Value<int>("pos"));

            await Write(Dp(device, "cover:0", "stop"), true);
            await WaitFor(() => cover.Calls.Any(a => a.Method == "Cover.Stop"), "Cover.Stop");
        }

        [Fact]
        public async Task Rpc_RgbChannelWrite_KeepsTheOtherChannels()
        {
            var fake = new FakeShellyDevice(_server, "gen2_plusrgbwpm");
            await StartDriver(null, Build("gen2_plusrgbwpm", _server.Address));
            var device = Device();
            await WaitForValue(device, "rgbw:0", "green", 128);

            await Write(Dp(device, "rgbw:0", "red"), 10);
            await WaitFor(() => fake.Calls.Any(a => a.Method == "RGBW.Set"), "RGBW.Set");

            var rgb = fake.Calls.Single(a => a.Method == "RGBW.Set").Params["rgb"]!.Values<int>().ToArray();
            Assert.Equal(new[] { 10, 128, 0 }, rgb);
        }

        [Fact]
        public async Task Rpc_VirtualComponentsAndThermostat_Write()
        {
            var fake = new FakeShellyDevice(_server, "gen4_1pm_virtual");
            await StartDriver(null, Build("gen4_1pm_virtual", _server.Address));
            var device = Device();
            await WaitForValue(device, "number:200", "value", 42.5);
            await WaitForValue(device, "enum:200", "value", "auto");

            await Write(Dp(device, "number:200", "value"), 50);
            await Write(Dp(device, "boolean:200", "value"), false);
            await Write(Dp(device, "button:200", "trigger"), true);
            await WaitFor(() => fake.Calls.Any(a => a.Method == "Button.Trigger"), "Button.Trigger");

            Assert.Equal(50d, fake.Calls.Single(a => a.Method == "Number.Set").Params.Value<double>("value"));
            Assert.Equal(200, fake.Calls.Single(a => a.Method == "Boolean.Set").Params.Value<int>("id"));
        }

        [Fact]
        public async Task Rpc_CircuitBreaker_Write()
        {
            var cb = new FakeShellyDevice(_server, "gen2_pro2cb");
            await StartDriver(null, Build("gen2_pro2cb", _server.Address));
            var device = Device();
            await WaitForValue(device, "cb:1", "total_cycles", 3);

            await Write(Dp(device, "cb:1", "output"), true);
            await WaitFor(() => cb.Calls.Any(a => a.Method == "CB.Set"), "CB.Set");
            var set = cb.Calls.Single(a => a.Method == "CB.Set").Params;
            Assert.Equal(1, set.Value<int>("id"));
            Assert.True(set.Value<bool>("output"));
        }

        [Fact]
        public async Task Rpc_BluTrvTarget_GoesThroughTheGateway()
        {
            var gw = new FakeShellyDevice(_server, "gen3_blugw_trv");
            await StartDriver(null, Build("gen3_blugw_trv", _server.Address));
            var device = Device();
            await WaitForValue(device, "blutrv:200", "temperature", 19.5);
            await WaitForValue(device, "blutrv:200", "position", 35);

            await Write(Dp(device, "blutrv:200", "target_temperature"), 22.5);
            await WaitFor(() => gw.Calls.Any(a => a.Method == "BluTrv.Call"), "BluTrv.Call");
            var call = gw.Calls.Single(a => a.Method == "BluTrv.Call").Params;
            Assert.Equal(200, call.Value<int>("id"));
            Assert.Equal("TRV.SetTarget", call.Value<string>("method"));
            Assert.Equal(22.5, call.SelectToken("params.target_C")!.Value<double>());
        }

        [Fact]
        public async Task Rpc_DigestProtectedDevice_StartsWithThePassword()
        {
            var fake = new FakeShellyDevice(_server, "gen2_pro3em") { Password = "secret" };
            await StartDriver(null, Build("gen2_pro3em", _server.Address, "secret"));
            var device = Device();

            await WaitForValue(device, "em:0", "a_active_power", 250.3);
            await WaitForValue(device, "emdata:0", "total_act", 1850.5);
            Assert.Contains(fake.Calls, a => a.Method == "Shelly.GetStatus");
        }

        [Fact]
        public async Task Rpc_WrongDevice_DoesNotStart()
        {
            _ = new FakeShellyDevice(_server, "gen2_plus1pm");
            var node = Build("gen2_plus1pm", _server.Address);
            node.SetProperty(ShellyFactory.ShellyFactory.DeviceIdPropertyKey, "AABBCCDDEEFF");
            await StartDriver(null, node);

            await WaitFor(() => _logger.Contains(LogLevel.Error, "Invalid shelly id"), "invalid id error");
            Assert.False(Device().Online);
        }

        [Fact]
        public async Task Rpc_SleepingDevice_ReportsThroughTheOutboundWebSocket()
        {
            var node = Build("gen2_plusht", "");
            Assert.True(node.GetPropertyValueBool(ShellyGenericTemplates.SleepingKey));
            await StartDriver(d => d.OutboundWebSocketPortOverride = 0, node);
            var device = Device();
            Assert.NotNull(_driver.OutboundWebSocketPort);

            using var ws = await ConnectOutbound();
            var status = (JObject)FakeShellyDevice.Load("gen2_plusht")["status"];
            status["temperature:0"]!["tC"] = 19.5;
            await SendOutbound(ws, "shellyplusht-c049ef8b1d2e", "NotifyFullStatus", status);

            await WaitForValue(device, "temperature:0", "temperature", 19.5);
            await WaitForValue(device, "humidity:0", "humidity", 45.6);
            await WaitForValue(device, "devicepower:0", "battery", 87);
            Assert.Equal("127.0.0.1", device.Address);
            Assert.True(device.Online);
        }

        [Fact]
        public async Task Rpc_UnknownSleepingDevice_IsLearnedAndReportedInLearnMode()
        {
            await StartDriver(d => d.OutboundWebSocketPortOverride = 0);
            Assert.True(await _driver.EnableLearnMode());

            using var ws = await ConnectOutbound();
            await SendOutbound(ws, "shellyplussmoke-80646f1a2b3c", "NotifyFullStatus", (JObject)FakeShellyDevice.Load("gen2_plussmoke")["status"]);

            await WaitFor(() => _driver.LearnedDevices.Count == 1, "learned device");
            var learned = _driver.LearnedDevices.Single();
            Assert.Equal("80646F1A2B3C", learned.GetPropertyValueString(ShellyFactory.ShellyFactory.DeviceIdPropertyKey));
            Assert.True(learned.GetPropertyValueBool(ShellyGenericTemplates.SleepingKey));
            Assert.Contains(learned.InverseThis2ParentNodeInstanceNavigation, a => a.This2NodeTemplateNavigation.Key == "shelly-rpc-smoke");
            _learnMode.Verify(a => a.NotifyLearnNode(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<NodeInstance>(),
                It.Is<IList<NodeTemplate>>(t => t.Single().ObjId == ShellyGenericTemplates.RpcDeviceTemplate), It.IsAny<IList<PropertyInstance>>()), Times.Once);

            // the next scan adds it (once)
            var scanned = await _driver.Scan();
            Assert.Single(scanned);
            Assert.Empty(await _driver.Scan());
        }

        private async Task<ClientWebSocket> ConnectOutbound()
        {
            var ws = new ClientWebSocket();
            await ws.ConnectAsync(new Uri($"ws://127.0.0.1:{_driver.OutboundWebSocketPort}/"), CancellationToken.None);
            return ws;
        }

        private static Task SendOutbound(ClientWebSocket ws, string source, string method, JObject parameters)
        {
            var frame = new JObject { ["src"] = source, ["dst"] = "automatica", ["method"] = method, ["params"] = parameters };
            return ws.SendAsync(Encoding.UTF8.GetBytes(frame.ToString(Newtonsoft.Json.Formatting.None)), WebSocketMessageType.Text, true, CancellationToken.None);
        }

        // ---------------------------------------------------------------- Gen1

        [Fact]
        public async Task Gen1_Start_ReadsTheStatusIntoTheDataPoints()
        {
            _ = new FakeShellyDevice(_server, "gen1_shelly1pm");
            await StartDriver(null, Build("gen1_shelly1pm", _server.Address));
            var device = Device();

            Assert.IsType<ShellyGen1Device>(device);
            await WaitForValue(device, "relays:0", "output", false);
            await WaitForValue(device, "meters:0", "energy", 200d); // 12000 Wmin
            await WaitForValue(device, "device", "device_temperature", 41.3);
            await WaitForValue(device, "device", "rssi", -61);
            Assert.NotNull(((ShellyGen1Device)device).Description);
        }

        [Fact]
        public async Task Gen1_RelayWrite_CallsTheUrlAndAppliesTheAnswer()
        {
            var fake = new FakeShellyDevice(_server, "gen1_shelly1pm");
            await StartDriver(null, Build("gen1_shelly1pm", _server.Address));
            var device = Device();
            await WaitForValue(device, "relays:0", "output", false);

            await Write(Dp(device, "relays:0", "output"), true);

            await WaitFor(() => fake.Calls.Any(a => a.Method == "/relay/0?turn=on"), "/relay/0?turn=on");
            await WaitForValue(device, "relays:0", "output", true);
        }

        [Fact]
        public async Task Gen1_LightAndRollerWrites_UseTheModeUrls()
        {
            var fake = new FakeShellyDevice(_server, "gen1_rgbw2_color");
            await StartDriver(null, Build("gen1_rgbw2_color", _server.Address));
            var device = Device();
            await WaitForValue(device, "lights:0", "red", 255);

            await Write(Dp(device, "lights:0", "green"), 300);
            await WaitFor(() => fake.Calls.Any(a => a.Method == "/color/0?green=255"), "/color/0?green=255");
        }

        [Fact]
        public async Task Gen1_TrvTarget_UsesTheThermostatsUrl()
        {
            var fake = new FakeShellyDevice(_server, "gen1_trv");
            await StartDriver(null, Build("gen1_trv", _server.Address));
            var device = Device();
            await WaitForValue(device, "thermostats:0", "target_temperature", 21.5);
            await WaitForValue(device, "sensors", "battery", 80);

            await Write(Dp(device, "thermostats:0", "target_temperature"), 22.5);
            await WaitFor(() => fake.Calls.Any(a => a.Method == "/thermostats/0?target_t=22.5"), "/thermostats/0?target_t=22.5");
        }

        [Fact]
        public async Task Gen1_BasicAuth_UsesTheCredentials()
        {
            var fake = new FakeShellyDevice(_server, "gen1_3em") { Password = "pw" };
            await StartDriver(null, Build("gen1_3em", _server.Address, "pw"));
            var device = Device();

            await WaitForValue(device, "emeters:0", "power", 250.3);
            await WaitForValue(device, "emeters:0", "returned_energy", 3.1);
            Assert.All(_server.Requests.Where(a => a.Path != "/shelly"), r => Assert.StartsWith("Basic ", r.Authorization));
            Assert.Equal("pw", fake.Password);
        }

        [Fact]
        public async Task Gen1_CoIoTPacket_UpdatesTheValuesWithoutAPoll()
        {
            var fake = new FakeShellyDevice(_server, "gen1_shelly1pm");
            await StartDriver(null, Build("gen1_shelly1pm", _server.Address));
            var device = Device();
            await WaitForValue(device, "relays:0", "output", false);
            fake.Offline = true; // only the packet can change the values now

            await SendCoIoT("SHSW-PM", "F3D2B1", "{\"G\":[[0,1101,1],[0,4101,55.5],[0,4103,120],[0,3104,44.0],[0,2102,\"L\"]]}");

            await WaitForValue(device, "relays:0", "output", true);
            await WaitForValue(device, "meters:0", "power", 55.5);
            await WaitForValue(device, "meters:0", "energy", 2d);
            await WaitForValue(device, "device", "device_temperature", 44.0);
            await WaitForValue(device, "inputs:0", "event", "L");
        }

        [Fact]
        public async Task Gen1_SleepingHt_CoIoTValuesThenReadsWhileAwake()
        {
            var fake = new FakeShellyDevice(_server, "gen1_ht") { Offline = true };
            var node = Build("gen1_ht", _server.Address);
            Assert.True(node.GetPropertyValueBool(ShellyGenericTemplates.SleepingKey));
            await StartDriver(null, node);
            var device = Device();
            await WaitFor(() => _logger.Contains(LogLevel.Information, "is not awake"), "asleep at start");

            await SendCoIoT("SHHT-1", "D5A9F0", "{\"G\":[[0,9103,1],[1,3101,23.5],[1,3103,51.0],[2,3111,88]]}");
            await WaitForValue(device, "sensors", "temperature", 23.5);
            await WaitForValue(device, "sensors", "humidity", 51.0);
            await WaitForValue(device, "sensors", "battery", 88);

            // awake: the next packet reads /status too
            fake.Offline = false;
            await SendCoIoT("SHHT-1", "D5A9F0", "{\"G\":[]}");
            await WaitForValue(device, "sensors", "battery_voltage", 2.92);
            await WaitForValue(device, "sensors", "humidity", 45.0);
        }

        private async Task SendCoIoT(string type, string id, string payload)
        {
            using var udp = new UdpClient(AddressFamily.InterNetwork);
            await udp.SendAsync(CoIoTMessage.CreateStatusPacket(type, id, Environment.TickCount & 0xFFFF, payload),
                new IPEndPoint(IPAddress.Loopback, _driver.CoIoTListenerPort!.Value));
        }

        // ---------------------------------------------------------------- scan

        [Fact]
        public async Task Scan_ProbesTheConfiguredAddressesAndSkipsConfiguredDevices()
        {
            await using var gen1Server = await FakeShellyServer.StartAsync();
            await using var configuredServer = await FakeShellyServer.StartAsync();
            using var offline = FakeShellyServer.StartOfflineDevice();
            _ = new FakeShellyDevice(_server, "gen2_plus2pm_cover");
            _ = new FakeShellyDevice(gen1Server, "gen1_shelly25_roller");
            _ = new FakeShellyDevice(configuredServer, "gen2_pmmini");

            await StartDriver(null, Build("gen2_pmmini", configuredServer.Address));
            _driver.DriverContext.NodeInstance.SetProperty(ShellyGenericTemplates.ScanAddressesKey,
                $"{_server.Address}, {gen1Server.Address};{configuredServer.Address} {offline.Address}");

            var nodes = await _driver.Scan();

            Assert.Equal(2, nodes.Count);
            var cover = nodes.Single(a => a.This2NodeTemplate == ShellyGenericTemplates.RpcDeviceTemplate);
            Assert.Equal("Office", cover.Name);
            Assert.Equal(_server.Address, cover.GetPropertyValueString(ShellyFactory.ShellyFactory.DeviceIpPropertyKey));
            var roller = nodes.Single(a => a.This2NodeTemplate == ShellyGenericTemplates.Gen1DeviceTemplate);
            Assert.Contains(roller.InverseThis2ParentNodeInstanceNavigation, a => a.This2NodeTemplateNavigation.Key == "shelly-gen1-rollers");
        }

        [Fact]
        public async Task Scan_UsesTheDefaultPassword()
        {
            _ = new FakeShellyDevice(_server, "gen2_plus1pm") { Password = "secret" };
            await StartDriver(null);
            _driver.DriverContext.NodeInstance.SetProperty(ShellyGenericTemplates.ScanAddressesKey, _server.Address);

            Assert.Empty(await _driver.Scan());

            _driver.DriverContext.NodeInstance.SetProperty(ShellyGenericTemplates.DefaultPasswordKey, "secret");
            var node = Assert.Single(await _driver.Scan());
            Assert.Equal("secret", node.GetPropertyValueString(ShellyGenericTemplates.PasswordKey));
        }
    }
}
