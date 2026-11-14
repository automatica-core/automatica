using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Automatica.Core.EF.Models;
using Automatica.Core.UnitTests.Base.Drivers;
using P3.Driver.Tuya.Driver;
using Xunit;

namespace P3.Driver.Tuya.Tests
{
    public class TuyaDriverTests : DriverFactoryTestBase<TuyaDriverFactory>, IAsyncLifetime
    {
        private const string LocalKey = "0123456789abcdef";
        private const string DeviceId = "bfaf2a5bb3cae875cas4jv";

        private readonly List<FakeTuyaDevice> _devices = new();

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            foreach (var d in _devices)
            {
                await d.DisposeAsync();
            }
        }

        private FakeTuyaDevice Device(string version)
        {
            var device = new FakeTuyaDevice(version, LocalKey);
            _devices.Add(device);
            return device;
        }

        private async Task<TuyaPlug> CreatePlugAsync(FakeTuyaDevice device, string version, string key = LocalKey)
        {
            var root = CreateNodeInstance(DriverFactory.DriverGuid);
            var plug = CreateNodeInstance(TuyaDriverFactory.PlugTemplate);
            plug.SetProperty(TuyaDriverFactory.HostKey, "127.0.0.1");
            plug.SetProperty(TuyaDriverFactory.DeviceIdKey, DeviceId);
            plug.SetProperty(TuyaDriverFactory.LocalKeyKey, key);
            plug.SetProperty(TuyaDriverFactory.VersionKey, version);
            plug.SetProperty(TuyaDriverFactory.PollKey, 3600);
            foreach (var dp in TuyaDataPoints.Plug)
            {
                plug.InverseThis2ParentNodeInstanceNavigation.Add(CreateNodeInstance(dp.Template));
            }
            root.InverseThis2ParentNodeInstanceNavigation.Add(plug);

            var driver = (TuyaDriver)await CreateDriver(root);
            var result = driver.Plugs.Single();
            result.Port = device.Port;
            return result;
        }

        private static object Value(TuyaPlug plug, string key) => plug.Values.Single(v => v.DataPoint.Key == key).LastValue;

        private static async Task Eventually(Func<bool> condition)
        {
            for (var i = 0; i < 100 && !condition(); i++)
            {
                await Task.Delay(20);
            }
            Assert.True(condition());
        }

        [Theory]
        [InlineData("3.4")]
        [InlineData("3.3")]
        public async Task Poll_ReadsTheStatus_InUnits(string version)
        {
            var device = Device(version);
            var plug = await CreatePlugAsync(device, version);

            await plug.PollAsync();

            await Eventually(() => Value(plug, "power") != null);
            Assert.Equal(true, Value(plug, TuyaDataPoints.ConnectedKey));
            Assert.Equal(true, Value(plug, "switch"));
            Assert.Equal(4.8, Value(plug, "power"));
            Assert.Equal(228.6, Value(plug, "voltage"));
            Assert.Equal(0.037, Value(plug, "current"));
            Assert.Equal(0.003, Value(plug, "energy_added"));
            Assert.Equal(0L, Value(plug, "countdown"));
            Assert.Equal("off", Value(plug, "power_on_state"));
            Assert.Contains(device.Received, r => r.Cmd == (version == "3.4" ? TuyaProtocol.DpQueryNew : TuyaProtocol.DpQuery));
        }

        [Theory]
        [InlineData("3.4")]
        [InlineData("3.3")]
        public async Task Write_SwitchesThePlug_AndTakesItsConfirmation(string version)
        {
            var device = Device(version);
            var plug = await CreatePlugAsync(device, version);
            await plug.PollAsync();
            await Eventually(() => Equals(Value(plug, "switch"), true));

            await plug.SetAsync(TuyaDataPoints.Plug.Single(d => d.Key == "switch"), false, default);

            await Eventually(() => Equals(Value(plug, "switch"), false));
            Assert.Equal(false, device.Dps["1"]);
            var control = device.Received.Single(r => r.Cmd is TuyaProtocol.Control or TuyaProtocol.ControlNew);
            using var doc = JsonDocument.Parse(control.Payload);
            var dps = version == "3.4" ? doc.RootElement.GetProperty("data").GetProperty("dps") : doc.RootElement.GetProperty("dps");
            Assert.False(dps.GetProperty("1").GetBoolean());
            Assert.Equal(1, device.Sessions);
        }

        [Fact]
        public async Task StatusSentByTheDevice_UpdatesTheValues()
        {
            var device = Device("3.4");
            var plug = await CreatePlugAsync(device, "3.4");
            await plug.PollAsync();
            await Eventually(() => Value(plug, "power") != null);

            await device.PushAsync(new Dictionary<string, object> { ["1"] = false, ["19"] = 0 });

            await Eventually(() => Equals(Value(plug, "switch"), false));
            Assert.Equal(0.0, Value(plug, "power"));
        }

        [Fact]
        public async Task WrongLocalKey_IsNotConnected()
        {
            var device = Device("3.4");
            var plug = await CreatePlugAsync(device, "3.4", "fedcba9876543210");

            await Assert.ThrowsAsync<TuyaProtocolException>(() => plug.PollAsync());

            Assert.Equal(false, Value(plug, TuyaDataPoints.ConnectedKey));
            Assert.Null(Value(plug, "power"));
        }

        [Fact]
        public async Task DroppedSession_ReconnectsOnTheNextPoll()
        {
            var device = Device("3.4");
            var plug = await CreatePlugAsync(device, "3.4");
            await plug.PollAsync();
            await Eventually(() => Value(plug, "power") != null);

            device.DropSession();
            await Eventually(() => Equals(Value(plug, TuyaDataPoints.ConnectedKey), false));
            device.Dps["19"] = 1234;
            await plug.PollAsync();

            await Eventually(() => Equals(Value(plug, "power"), 123.4));
            Assert.Equal(true, Value(plug, TuyaDataPoints.ConnectedKey));
            Assert.Equal(2, device.Sessions);
        }

        [Fact]
        public async Task UnreachableDevice_IsNotConnected()
        {
            var device = Device("3.4");
            var plug = await CreatePlugAsync(device, "3.4");
            plug.Port = 1;

            await Assert.ThrowsAnyAsync<Exception>(() => plug.PollAsync());

            Assert.Equal(false, Value(plug, TuyaDataPoints.ConnectedKey));
        }

        /// <summary>
        /// A broadcast of a real LSC plug (UDP 6667, protocol 3.4), captured 2026-11-14
        /// </summary>
        [Fact]
        public void Discovery_DecodesARealBroadcast()
        {
            var packet = Convert.FromHexString("000055aa0000000000000023000000bc00000000f4ac6fd3f5e51601430ee97cfe736a9030d95b222207c33f9d4a3db869669e7675679bc4c34c83e9be1e2a27a2855644073e76c98611b115150e486af150cfb07256f992ef0bb3c9946f6ca8e2e148532e0dbc9a92c3317286ebf238be5797863e4a9d43c5263de269048b7c4f281c335cca9cf243b333079bed2b12b16ffb346fbdac3e9dd4c59ed6077a3713a1f534c5f75cc69c6a0e153160a17bc40acc9bf710aae30275845704b34769a7f2181ae096c22f0000aa55");

            var found = TuyaDriver.Parse(packet, "192.168.8.141");

            Assert.Equal("bfed82a56ff63039a4tt3h", found.Id);
            Assert.Equal("192.168.8.141", found.Ip);
            Assert.Equal("3.4", found.Version);
        }

        [Fact]
        public void Discovery_IgnoresOtherPackets()
        {
            Assert.Null(TuyaDriver.Parse(Encoding.ASCII.GetBytes("M-SEARCH * HTTP/1.1"), "192.168.8.1"));
            Assert.Null(TuyaDriver.Parse(new byte[40], "192.168.8.1"));
        }

        [Fact]
        public void Crc32_IsTheZlibOne() => Assert.Equal(0xCBF43926u, TuyaProtocol.Crc32(Encoding.ASCII.GetBytes("123456789")));

        [Theory]
        [InlineData("3.3", TuyaProtocol.Control)]
        [InlineData("3.3", TuyaProtocol.DpQuery)]
        [InlineData("3.4", TuyaProtocol.ControlNew)]
        [InlineData("3.4", TuyaProtocol.DpQueryNew)]
        public void Frames_RoundTrip(string version, uint cmd)
        {
            var key = Encoding.ASCII.GetBytes(LocalKey);
            var frame = TuyaProtocol.Encode(version, key, 7, cmd, "{\"dps\":{\"1\":true}}"u8.ToArray());

            var decoded = TuyaProtocol.Decode(version, key, frame[..16], frame[16..]);

            Assert.Equal(7u, decoded.Seq);
            Assert.Equal(cmd, decoded.Cmd);
            Assert.Equal("{\"dps\":{\"1\":true}}", Encoding.UTF8.GetString(decoded.Payload));
        }

        [Fact]
        public void TamperedFrame_IsRefused()
        {
            var key = Encoding.ASCII.GetBytes(LocalKey);
            var frame = TuyaProtocol.Encode("3.4", key, 1, TuyaProtocol.DpQueryNew, "{}"u8.ToArray());
            frame[20] ^= 1;

            Assert.Throws<TuyaProtocolException>(() => TuyaProtocol.Decode("3.4", key, frame[..16], frame[16..]));
        }

        [Fact]
        public void DataPoints_HaveUniqueTemplatesAndKeys()
        {
            Assert.Equal(TuyaDataPoints.Plug.Count, TuyaDataPoints.Plug.Select(d => d.Template).Distinct().Count());
            Assert.Equal(TuyaDataPoints.Plug.Count, TuyaDataPoints.Plug.Select(d => d.Key).Distinct().Count());
        }

        [Theory]
        [InlineData("en")]
        [InlineData("de")]
        public void AllTextsExist(string lang)
        {
            var assembly = typeof(TuyaDriverFactory).Assembly;
            var name = assembly.GetManifestResourceNames().Single(n => n.EndsWith($"Tuya-{lang}.json"));
            using var reader = new StreamReader(assembly.GetManifestResourceStream(name)!);
            using var doc = JsonDocument.Parse(reader.ReadToEnd());
            var root = doc.RootElement.GetProperty("TUYA");
            Assert.False(string.IsNullOrEmpty(root.GetProperty("NAME").GetString()));
            Assert.False(string.IsNullOrEmpty(root.GetProperty("SCAN").GetProperty("NAME").GetString()));
            var plug = root.GetProperty("PLUG");
            foreach (var section in new[] { "HOST", "DEVICE_ID", "LOCAL_KEY", "VERSION", "POLL" }.Concat(TuyaDataPoints.Plug.Select(d => d.I18N)))
            {
                Assert.True(plug.TryGetProperty(section, out var s), section);
                Assert.False(string.IsNullOrEmpty(s.GetProperty("NAME").GetString()), section);
                Assert.True(s.TryGetProperty("DESCRIPTION", out _), section);
            }
        }
    }
}
