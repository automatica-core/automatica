using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.EF.Models;
using Automatica.Core.UnitTests.Base.Drivers;
using Automatica.Driver.Shelly.Unit.Tests.Fakes;
using Automatica.Driver.ShellyFactory.Generic;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Automatica.Driver.Shelly.Unit.Tests
{
    /// <summary>
    /// The scan of one device: the recorded responses of one representative device per component type and generation
    /// become exactly the components and data points the device has.
    /// </summary>
    public class ShellyNodeBuilderTests : DriverFactoryTestBase<ShellyFactory.ShellyFactory>
    {
        /// <summary>component node key (without "shelly-gen1-"/"shelly-rpc-") and id → data point keys</summary>
        private static Dictionary<string, string[]> Tree(NodeInstance device)
        {
            var ret = new Dictionary<string, string[]>();
            foreach (var component in device.InverseThis2ParentNodeInstanceNavigation)
            {
                var key = component.This2NodeTemplateNavigation.Key;
                key = key.Substring(key.IndexOf('-', "shelly-".Length) + 1);
                var idProperty = component.PropertyInstance.FirstOrDefault(a => a.This2PropertyTemplateNavigation.Key == ShellyGenericTemplates.ComponentIdKey);
                var name = idProperty == null ? key : $"{key}:{idProperty.Value}";
                ret[name] = component.InverseThis2ParentNodeInstanceNavigation.Select(a => a.This2NodeTemplateNavigation.Key).OrderBy(a => a).ToArray();
            }
            return ret;
        }

        private NodeInstance BuildFixture(string fixture, string address = "192.168.1.50")
        {
            var json = FakeShellyDevice.Load(fixture);
            if (json.Value<int>("gen") >= 2)
            {
                return ShellyNodeBuilder.BuildRpc(Factory, (JObject)json["deviceinfo"], (JObject)json["status"], json["config"] as JObject, address, "secret");
            }

            return ShellyNodeBuilder.BuildGen1(Factory, (JObject)json["shelly"], (JObject)json["status"], json["settings"] as JObject,
                json["citd"]?.ToString(Newtonsoft.Json.Formatting.None), address, new ShellyCredentials("admin", "secret"));
        }

        private static string[] Keys(params string[] keys) => keys.OrderBy(a => a).ToArray();

        [Fact]
        public void Gen2Plus1Pm_SwitchWithMeteringInputSysWifi()
        {
            var device = BuildFixture("gen2_plus1pm");
            var tree = Tree(device);

            Assert.Equal(ShellyGenericTemplates.RpcDeviceTemplate, device.This2NodeTemplate);
            Assert.Equal("Kitchen", device.Name);
            Assert.Equal("A8032AB12345", device.GetPropertyValueString(ShellyFactory.ShellyFactory.DeviceIdPropertyKey));
            Assert.Equal("192.168.1.50", device.GetPropertyValueString(ShellyFactory.ShellyFactory.DeviceIpPropertyKey));
            Assert.Equal("SNSW-001P16EU (Plus1PM)", device.GetPropertyValueString(ShellyGenericTemplates.ModelKey));
            Assert.Equal("secret", device.GetPropertyValueString(ShellyGenericTemplates.PasswordKey));
            Assert.False(device.GetPropertyValueBool(ShellyGenericTemplates.SleepingKey));

            Assert.Equal(new[] { "input:0", "switch:0", "sys", "wifi" }, tree.Keys.OrderBy(a => a));
            Assert.Equal(Keys("output", "toggle", "power", "voltage", "current", "power_factor", "frequency", "energy", "returned_energy", "device_temperature"),
                tree["switch:0"]);
            Assert.Equal(Keys("state"), tree["input:0"]);
            Assert.Equal(Keys("restart_required", "uptime", "reboot", "has_update"), tree["sys"]);

            var sw = device.InverseThis2ParentNodeInstanceNavigation.Single(a => a.This2NodeTemplateNavigation.Key == "shelly-rpc-switch");
            Assert.Equal("Kitchen light", sw.Name);
            Assert.All(device.InverseThis2ParentNodeInstanceNavigation, c => Assert.Equal(device, c.This2ParentNodeInstanceNavigation));
        }

        [Fact]
        public void Gen2Plus2PmCoverProfile_CoverOnly()
        {
            var tree = Tree(BuildFixture("gen2_plus2pm_cover"));

            Assert.False(tree.ContainsKey("switch:0"));
            Assert.Equal(Keys("state", "position", "open", "close", "stop", "calibrate", "power", "voltage", "current", "power_factor", "frequency",
                "energy", "returned_energy", "device_temperature"), tree["cover:0"]);
        }

        [Fact]
        public void Gen2Pro3Em_ThreePhaseMeterAndTotals()
        {
            var tree = Tree(BuildFixture("gen2_pro3em"));

            Assert.Contains("em:0", tree.Keys);
            Assert.Contains("emdata:0", tree.Keys);
            Assert.Contains("temperature:0", tree.Keys);
            Assert.Contains("a_active_power", tree["em:0"]);
            Assert.Contains("c_voltage", tree["em:0"]);
            Assert.Contains("total_active_power", tree["em:0"]);
            // n_current is null without the neutral sensor
            Assert.DoesNotContain("n_current", tree["em:0"]);
            Assert.Contains("total_act", tree["emdata:0"]);
        }

        [Fact]
        public void Gen2ProEm50_TwoChannelsAndContactor()
        {
            var tree = Tree(BuildFixture("gen2_proem50"));

            Assert.Equal(Keys("active_power", "apparent_power", "voltage", "current", "power_factor", "frequency"), tree["em1:0"]);
            Assert.Equal(tree["em1:0"], tree["em1:1"]);
            Assert.Equal(Keys("energy", "returned_energy"), tree["em1data:1"]);
            Assert.Equal(Keys("output", "toggle", "device_temperature"), tree["switch:0"]);
        }

        [Fact]
        public void Gen2Lights_DimmerRgbwCct()
        {
            var dimmer = Tree(BuildFixture("gen2_prodimmer2pm"));
            Assert.Contains("light:0", dimmer.Keys);
            Assert.Contains("light:1", dimmer.Keys);
            Assert.Contains("brightness", dimmer["light:1"]);
            // button inputs (state null): event instead of state
            Assert.Equal(Keys("event"), dimmer["input:0"]);

            var rgbw = Tree(BuildFixture("gen2_plusrgbwpm"));
            Assert.Contains("red", rgbw["rgbw:0"]);
            Assert.Contains("blue", rgbw["rgbw:0"]);
            Assert.Contains("white", rgbw["rgbw:0"]);
            Assert.Contains("brightness", rgbw["rgbw:0"]);

            var cct = Tree(BuildFixture("gen2_prorgbwwpm_cct"));
            Assert.Contains("color_temperature", cct["cct:0"]);
            Assert.Contains("cct:1", cct.Keys);
        }

        [Fact]
        public void Gen2PlusHt_IsSleepingWithBattery()
        {
            var device = BuildFixture("gen2_plusht");
            var tree = Tree(device);

            Assert.True(device.GetPropertyValueBool(ShellyGenericTemplates.SleepingKey));
            Assert.Equal(Keys("temperature"), tree["temperature:0"]);
            Assert.Equal(Keys("humidity"), tree["humidity:0"]);
            Assert.Equal(Keys("battery", "battery_voltage", "external_power"), tree["devicepower:0"]);
        }

        [Fact]
        public void Gen2SmokeAndGen4Flood_AlarmMuteBattery()
        {
            var smoke = Tree(BuildFixture("gen2_plussmoke"));
            Assert.Equal(Keys("alarm", "muted", "mute"), smoke["smoke:0"]);

            var floodNode = BuildFixture("gen4_flood");
            Assert.True(floodNode.GetPropertyValueBool(ShellyGenericTemplates.SleepingKey));
            Assert.Equal(4, floodNode.GetPropertyValueInt(ShellyGenericTemplates.GenerationKey));
            Assert.Equal(Keys("alarm", "muted"), Tree(floodNode)["flood:0"]);
        }

        [Fact]
        public void Gen2WallDisplay_ThermostatAndSensors()
        {
            var tree = Tree(BuildFixture("gen2_walldisplay"));

            Assert.Equal(Keys("enable", "target_temperature", "temperature", "output"), tree["thermostat:0"]);
            Assert.Contains("illuminance:0", tree.Keys);
            Assert.Contains("humidity:0", tree.Keys);
            Assert.Contains("switch:0", tree.Keys);
        }

        [Fact]
        public void Gen3BluGateway_BtHomeDevicesAndSensorsNamedByTheirDevice()
        {
            var device = BuildFixture("gen3_blu_gateway");
            var tree = Tree(device);

            Assert.Equal(3, device.GetPropertyValueInt(ShellyGenericTemplates.GenerationKey));
            Assert.Equal(Keys("battery", "rssi", "event"), tree["bthomedevice:200"]);
            Assert.Equal(Keys("value"), tree["bthomesensor:200"]);
            Assert.Equal(Keys("state"), tree["bthomesensor:202"]);

            string Name(string key, int id) => device.InverseThis2ParentNodeInstanceNavigation.Single(a =>
                a.This2NodeTemplateNavigation.Key == key && a.GetPropertyValueInt(ShellyGenericTemplates.ComponentIdKey) == id).Name;

            Assert.Equal("BLU H&T bath", Name("shelly-rpc-bthomedevice", 200));
            Assert.Equal("BLU H&T bath Temperature", Name("shelly-rpc-bthomesensor", 200));
            Assert.Equal("BLU H&T bath Humidity", Name("shelly-rpc-bthomesensor", 201));
            Assert.Equal("Terrace window", Name("shelly-rpc-bthomesensor", 202));
            Assert.Equal("BLU 38:39:8f:70:b1:a2 Button", Name("shelly-rpc-bthomesensor", 203));
        }

        [Fact]
        public void Gen4VirtualComponents_ValueAndButton()
        {
            var tree = Tree(BuildFixture("gen4_1pm_virtual"));

            Assert.Equal(Keys("value"), tree["number:200"]);
            Assert.Equal(Keys("value"), tree["boolean:200"]);
            Assert.Equal(Keys("value"), tree["enum:200"]);
            Assert.Equal(Keys("value"), tree["text:200"]);
            Assert.Equal(Keys("trigger", "event"), tree["button:200"]);
        }

        [Fact]
        public void Gen2ProCbAndBluTrvGateway()
        {
            var cb = BuildFixture("gen2_pro2cb");
            var tree = Tree(cb);
            Assert.Equal(Keys("output", "total_cycles", "device_temperature"), tree["cb:0"]);
            Assert.Contains("cb:1", tree.Keys);
            Assert.Equal(Keys("voltage"), tree["voltmeter:0"]);
            Assert.Equal("Heat pump", cb.InverseThis2ParentNodeInstanceNavigation.Single(a => a.This2NodeTemplateNavigation.Key == "shelly-rpc-cb" &&
                a.GetPropertyValueInt(ShellyGenericTemplates.ComponentIdKey) == 0).Name);

            var trv = Tree(BuildFixture("gen3_blugw_trv"));
            Assert.Equal(Keys("temperature", "target_temperature", "position", "battery", "rssi"), trv["blutrv:200"]);
        }

        [Fact]
        public void Gen2PmMiniI4Uni()
        {
            Assert.Contains("energy", Tree(BuildFixture("gen2_pmmini"))["pm1:0"]);

            var i4 = Tree(BuildFixture("gen2_plusi4"));
            Assert.Equal(new[] { "input:0", "input:1", "input:2", "input:3", "sys", "wifi" }, i4.Keys.OrderBy(a => a));
            Assert.All(new[] { "input:0", "input:1", "input:2", "input:3" }, a => Assert.Equal(Keys("event"), i4[a]));

            var uni = Tree(BuildFixture("gen2_plusuni"));
            Assert.Equal(Keys("counts", "frequency"), uni["input:2"]);
            Assert.Equal(Keys("voltage"), uni["voltmeter:100"]);
            Assert.Contains("temperature:101", uni.Keys);
            Assert.Equal(Keys("output", "toggle"), uni["switch:1"]);
        }

        [Fact]
        public void Gen1Shelly1Pm_RelayMeterInputDevice()
        {
            var device = BuildFixture("gen1_shelly1pm");
            var tree = Tree(device);

            Assert.Equal(ShellyGenericTemplates.Gen1DeviceTemplate, device.This2NodeTemplate);
            Assert.Equal("Kitchen", device.Name);
            Assert.Equal("SHSW-PM", device.GetPropertyValueString(ShellyGenericTemplates.ModelKey));
            Assert.Equal("admin", device.GetPropertyValueString(ShellyGenericTemplates.UserNameKey));
            Assert.Contains("relay_0", device.GetPropertyValueString(ShellyGenericTemplates.CoIoTDescriptionKey));

            Assert.Equal(Keys("output", "has_timer", "overpower"), tree["relays:0"]);
            Assert.Equal(Keys("power", "energy", "overpower_value"), tree["meters:0"]);
            Assert.Equal(Keys("state", "event", "event_count"), tree["inputs:0"]);
            Assert.Equal(Keys("has_update", "rssi", "device_temperature", "overtemperature", "reboot"), tree["device"]);
            // the root tmp.tC is the device temperature here, no sensors component
            Assert.False(tree.ContainsKey("sensors"));

            var relay = device.InverseThis2ParentNodeInstanceNavigation.Single(a => a.This2NodeTemplateNavigation.Key == "shelly-gen1-relays");
            Assert.Equal("Ceiling", relay.Name);
        }

        [Fact]
        public void Gen1Shelly25RollerMode_NoRelays()
        {
            var device = BuildFixture("gen1_shelly25_roller");
            var tree = Tree(device);

            Assert.Equal("roller", device.GetPropertyValueString(ShellyGenericTemplates.ModeKey));
            Assert.False(tree.ContainsKey("relays:0"));
            Assert.Equal(Keys("state", "position", "open", "close", "stop", "power", "stop_reason"), tree["rollers:0"]);
            Assert.Contains("supply_voltage", tree["device"]);
        }

        [Fact]
        public void Gen1Lights_RgbwColorAndDimmer()
        {
            var rgbw = Tree(BuildFixture("gen1_rgbw2_color"));
            Assert.Equal(Keys("output", "gain", "red", "green", "blue", "white", "power", "overpower"), rgbw["lights:0"]);

            var dimmer = Tree(BuildFixture("gen1_dimmer2"));
            Assert.Equal(Keys("output", "brightness"), dimmer["lights:0"]);
            Assert.Contains("inputs:1", dimmer.Keys);
        }

        [Fact]
        public void Gen1BatterySensors()
        {
            var ht = BuildFixture("gen1_ht");
            Assert.True(ht.GetPropertyValueBool(ShellyGenericTemplates.SleepingKey));
            Assert.Equal(Keys("temperature", "humidity", "battery", "battery_voltage"), Tree(ht)["sensors"]);

            Assert.Equal(Keys("door_open", "illuminance", "illumination", "tilt", "accel_vibration", "temperature", "battery", "battery_voltage"),
                Tree(BuildFixture("gen1_dw2"))["sensors"]);
            Assert.Equal(Keys("flood", "temperature", "battery", "battery_voltage"), Tree(BuildFixture("gen1_flood"))["sensors"]);
            Assert.Equal(Keys("motion", "vibration", "illuminance", "illumination", "battery", "battery_voltage", "charger"),
                Tree(BuildFixture("gen1_motion"))["sensors"]);
        }

        [Fact]
        public void Gen1Trv_ThermostatWithoutRootTemperature()
        {
            var tree = Tree(BuildFixture("gen1_trv"));

            Assert.Equal(Keys("temperature", "target_temperature", "position", "window_open"), tree["thermostats:0"]);
            Assert.Equal(Keys("battery", "battery_voltage", "charger"), tree["sensors"]);
        }

        [Fact]
        public void Gen1UniAnd3Em_AdcExternalSensorsEmeters()
        {
            var uni = Tree(BuildFixture("gen1_uni"));
            Assert.Equal(Keys("voltage"), uni["adcs:0"]);
            Assert.Equal(Keys("temperature"), uni["ext_temperature:0"]);
            Assert.Equal(Keys("humidity"), uni["ext_humidity:0"]);
            Assert.Contains("relays:1", uni.Keys);

            var em = Tree(BuildFixture("gen1_3em"));
            Assert.Equal(Keys("power", "power_factor", "voltage", "current", "energy", "returned_energy"), em["emeters:2"]);
            Assert.Contains("relays:0", em.Keys);
        }

        [Fact]
        public void Gen1I3_InputsOnly()
        {
            var tree = Tree(BuildFixture("gen1_i3"));

            Assert.Equal(new[] { "device", "inputs:0", "inputs:1", "inputs:2" }, tree.Keys.OrderBy(a => a));
        }

        [Theory]
        [InlineData("A8:03:2A:B1:23:45")]
        [InlineData("a8032ab12345")]
        [InlineData("A8-03-2A-B1-23-45")]
        [InlineData("shellyplus1pm-a8032ab12345")]
        public void NormalizeMac_AcceptsMacsAndGen2DeviceIds(string id)
        {
            Assert.Equal("A8032AB12345", ShellyGenericDevice.NormalizeMac(id));
        }

        [Fact]
        public void EveryFixture_Builds()
        {
            foreach (var file in System.IO.Directory.GetFiles(System.IO.Path.Combine(AppContext.BaseDirectory, "Fixtures"), "*.json"))
            {
                var device = BuildFixture(System.IO.Path.GetFileNameWithoutExtension(file));
                Assert.NotEmpty(device.InverseThis2ParentNodeInstanceNavigation);
                Assert.All(device.InverseThis2ParentNodeInstanceNavigation, c => Assert.NotEmpty(c.InverseThis2ParentNodeInstanceNavigation));
            }
        }

        [Fact]
        public async Task Probe_ReadsAGen2DeviceWithDigestAuth()
        {
            await using var server = await FakeShellyServer.StartAsync();
            var fake = new FakeShellyDevice(server, "gen2_plus1pm") { Password = "secret" };

            var device = await ShellyNodeBuilder.ProbeAsync(Factory, server.Address, new ShellyCredentials(null, "secret"), NullLogger.Instance);

            Assert.NotNull(device);
            Assert.Equal(server.Address, device.GetPropertyValueString(ShellyFactory.ShellyFactory.DeviceIpPropertyKey));
            Assert.Contains("switch:0", Tree(device).Keys);
            Assert.Contains(fake.Calls, a => a.Method == "Shelly.GetConfig");
        }

        [Fact]
        public async Task Probe_ReadsAGen1DeviceWithBasicAuth()
        {
            await using var server = await FakeShellyServer.StartAsync();
            _ = new FakeShellyDevice(server, "gen1_3em") { Password = "pw", UserName = "user" };

            var device = await ShellyNodeBuilder.ProbeAsync(Factory, server.Address, new ShellyCredentials("user", "pw"), NullLogger.Instance);

            Assert.NotNull(device);
            Assert.Equal("House meter", device.Name);
            Assert.Contains("emeters:0", Tree(device).Keys);
        }

        [Fact]
        public async Task Probe_WrongPasswordOrNoShelly_ReturnsNull()
        {
            await using var server = await FakeShellyServer.StartAsync();
            var fake = new FakeShellyDevice(server, "gen2_plus1pm") { Password = "secret" };

            Assert.Null(await ShellyNodeBuilder.ProbeAsync(Factory, server.Address, new ShellyCredentials(null, "wrong"), NullLogger.Instance));
            Assert.Null(await ShellyNodeBuilder.ProbeAsync(Factory, server.Address, new ShellyCredentials(null, null), NullLogger.Instance));

            server.Handler = _ => FakeResponse.Json("{\"name\":\"some other device\"}");
            Assert.Null(await ShellyNodeBuilder.ProbeAsync(Factory, server.Address, new ShellyCredentials(null, null), NullLogger.Instance));

            using var offline = FakeShellyServer.StartOfflineDevice();
            Assert.Null(await ShellyNodeBuilder.ProbeAsync(Factory, offline.Address, new ShellyCredentials(null, null), NullLogger.Instance,
                timeout: TimeSpan.FromSeconds(2)));
            Assert.NotNull(fake);
        }
    }
}
