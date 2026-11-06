using P3.Driver.Matter.Crypto;
using P3.Driver.Matter.DataModel.Clusters;
using P3.Driver.Matter.InteractionModel;
using P3.Driver.Matter.Storage;
using P3.Driver.Matter.Tlv;

namespace P3.Driver.Matter.Tests
{
    /// <summary>The catalog devices (<see cref="MatterDeviceKinds"/>), commissioned and driven over UDP.</summary>
    public sealed class GenericDeviceTests : IAsyncLifetime
    {
        private readonly MatterBridge _bridge = new(new MatterNodeOptions { Port = 0, EnableMdns = false }, new InMemoryMatterStorage());
        private readonly List<(string Device, string Point, object Value)> _requests = new();
        private readonly Dictionary<string, BridgedGenericDevice> _devices = new();
        private MiniController _controller;
        private TestCertificateAuthority _ca;

        public async Task InitializeAsync()
        {
            await _bridge.InitializeAsync();
            foreach (var kind in MatterDeviceKinds.All)
            {
                var device = _bridge.Add(new BridgedGenericDevice(kind.Key, kind.DisplayName, kind, kind.Points.Select(p => p.Key)));
                device.ValueRequested = (point, value) =>
                {
                    lock (_requests) _requests.Add((kind.Key, point, value));
                    return Task.CompletedTask;
                };
                _devices[kind.Key] = device;
            }

            await _bridge.StartAsync();
            _controller = new MiniController(_bridge.Node.Port);
            _ca = new TestCertificateAuthority(0x1234);
            await CommissionAsync();
        }

        public async Task DisposeAsync()
        {
            _controller.Dispose();
            _ca.Dispose();
            await _bridge.DisposeAsync();
        }

        private async Task CommissionAsync()
        {
            const ulong controllerNodeId = 0x1111;
            const ulong deviceNodeId = 0x2222;
            Assert.NotNull(await _controller.EstablishPaseAsync(_bridge.Node.State.Passcode));
            Assert.Equal(0ul, (await _controller.InvokeWithResponseAsync(0, GeneralCommissioningCluster.Id, 0x00, w =>
            {
                w.PutUInt(0, 60);
                w.PutUInt(1, 1);
            })).GetUInt(0));
            var nocsr = TlvReader.Decode((await _controller.InvokeWithResponseAsync(0, OperationalCredentialsCluster.Id, 0x04,
                w => w.PutBytes(0, MatterCrypto.RandomBytes(32)))).GetBytes(0));
            var request = System.Security.Cryptography.X509Certificates.CertificateRequest.LoadSigningRequest(nocsr.GetBytes(1),
                System.Security.Cryptography.HashAlgorithmName.SHA256);
            using var csrKey = request.PublicKey.GetECDsaPublicKey()!;
            await _controller.InvokeAsync(0, OperationalCredentialsCluster.Id, 0x0B, w => w.PutBytes(0, _ca.RootCert));
            var ipk = MatterCrypto.RandomBytes(16);
            var noc = await _controller.InvokeWithResponseAsync(0, OperationalCredentialsCluster.Id, 0x06, w =>
            {
                w.PutBytes(0, _ca.IssueNoc(MatterCrypto.ExportPublicKey(csrKey), deviceNodeId));
                w.PutBytes(2, ipk);
                w.PutUInt(3, controllerNodeId);
                w.PutUInt(4, 0xFFF1);
            });
            Assert.Equal(0ul, noc.GetUInt(0));
            using var key = MatterCrypto.CreateSigningKey();
            Assert.NotNull(await _controller.EstablishCaseAsync(_ca, ipk, controllerNodeId, key, _ca.IssueNoc(MatterCrypto.ExportPublicKey(key), controllerNodeId),
                deviceNodeId));
            Assert.Equal(0ul, (await _controller.InvokeWithResponseAsync(0, GeneralCommissioningCluster.Id, 0x04)).GetUInt(0));
        }

        private ushort Ep(string kind) => _devices[kind].EndpointId;

        private List<object> Requests(string device, string point)
        {
            lock (_requests)
            {
                return _requests.Where(r => r.Device == device && r.Point == point).Select(r => r.Value).ToList();
            }
        }

        [Fact]
        public async Task Every_device_kind_can_be_read_completely()
        {
            foreach (var kind in MatterDeviceKinds.All)
            {
                var reports = await _controller.ReadAsync((Ep(kind.Key), null, null));
                Assert.DoesNotContain(reports, r => r.Get(0) != null); // no attribute status errors
                var deviceTypes = reports.Data(Ep(kind.Key), DescriptorCluster.Id, DescriptorCluster.AttributeDeviceTypeList).Children.Select(c => c.GetUInt(0)).ToList();
                Assert.Contains(kind.DeviceType.Id, deviceTypes);
            }
        }

        [Fact]
        public async Task Sensor_values_are_encoded_in_cluster_units()
        {
            _devices["temperature-sensor"].SetValue("temperature", 21.5);
            _devices["humidity-sensor"].SetValue("humidity", 45.25);
            _devices["light-sensor"].SetValue("illuminance", 1000.0);
            _devices["contact-sensor"].SetValue("contact", true);
            _devices["contact-sensor"].SetValue("battery", 80);
            _devices["occupancy-sensor"].SetValue("occupied", true);
            _devices["air-quality-sensor"].SetValue("co2", 612.0);

            Assert.Equal(2150, (await _controller.ReadValueAsync(Ep("temperature-sensor"), MeasurementCluster.TemperatureId, 0)).AsInt());
            Assert.Equal(4525ul, (await _controller.ReadValueAsync(Ep("humidity-sensor"), MeasurementCluster.HumidityId, 0)).AsUInt());
            Assert.Equal(30001ul, (await _controller.ReadValueAsync(Ep("light-sensor"), MeasurementCluster.IlluminanceId, 0)).AsUInt());
            Assert.True((await _controller.ReadValueAsync(Ep("contact-sensor"), BooleanStateCluster.Id, 0)).AsBool());
            Assert.Equal(160ul, (await _controller.ReadValueAsync(Ep("contact-sensor"), PowerSourceCluster.Id, PowerSourceCluster.AttributeBatPercentRemaining)).AsUInt());
            Assert.Equal(1ul, (await _controller.ReadValueAsync(Ep("occupancy-sensor"), OccupancySensingCluster.Id, 0)).AsUInt());
            Assert.Equal(612.0, (await _controller.ReadValueAsync(Ep("air-quality-sensor"), ConcentrationMeasurementCluster.CarbonDioxideId, 0)).AsDouble(), 3);
        }

        [Fact]
        public async Task Thermostat_setpoint_and_mode_writes_reach_the_host()
        {
            var ep = Ep("thermostat-heat-cool");
            _devices["thermostat-heat-cool"].SetValue("local-temperature", 19.8);
            Assert.Equal(1980, (await _controller.ReadValueAsync(ep, ThermostatCluster.Id, ThermostatCluster.AttributeLocalTemperature)).AsInt());

            Assert.Equal(StatusCode.Success, await _controller.WriteAsync(ep, ThermostatCluster.Id, ThermostatCluster.AttributeOccupiedHeatingSetpoint, (w, t) => w.PutInt(t, 2250)));
            Assert.Equal(new object[] { 22.5 }, Requests("thermostat-heat-cool", "heating-setpoint"));
            Assert.Equal(StatusCode.ConstraintError,
                await _controller.WriteAsync(ep, ThermostatCluster.Id, ThermostatCluster.AttributeOccupiedHeatingSetpoint, (w, t) => w.PutInt(t, 9000)));

            Assert.Equal(StatusCode.Success, await _controller.WriteAsync(ep, ThermostatCluster.Id, ThermostatCluster.AttributeSystemMode, (w, t) => w.PutUInt(t, 3)));
            Assert.Equal(new object[] { 3 }, Requests("thermostat-heat-cool", "system-mode"));
            Assert.Equal(StatusCode.ConstraintError,
                await _controller.WriteAsync(Ep("thermostat"), ThermostatCluster.Id, ThermostatCluster.AttributeSystemMode, (w, t) => w.PutUInt(t, 3)));

            // SetpointRaiseLower heat +1.0 °C
            await _controller.InvokeAsync(ep, ThermostatCluster.Id, 0x00, w =>
            {
                w.PutUInt(0, 0);
                w.PutInt(1, 10);
            });
            Assert.Equal(2350, (await _controller.ReadValueAsync(ep, ThermostatCluster.Id, ThermostatCluster.AttributeOccupiedHeatingSetpoint)).AsInt());
        }

        [Fact]
        public async Task Color_light_commands_report_rgb_and_kelvin()
        {
            var ep = Ep("extended-color-light");
            // hue 0 (red), full saturation
            await _controller.InvokeAsync(ep, ColorControlCluster.Id, 0x06, w =>
            {
                w.PutUInt(0, 0);
                w.PutUInt(1, 254);
                w.PutUInt(2, 0);
                w.PutUInt(3, 0);
                w.PutUInt(4, 0);
            });
            Assert.Equal(new object[] { "#FF0000" }, Requests("extended-color-light", "color"));

            await _controller.InvokeAsync(ep, ColorControlCluster.Id, 0x0A, w =>
            {
                w.PutUInt(0, 250);
                w.PutUInt(1, 0);
                w.PutUInt(2, 0);
                w.PutUInt(3, 0);
            });
            Assert.Equal(new object[] { 4000 }, Requests("extended-color-light", "color-temperature"));
            Assert.Equal((ulong)ColorMode.ColorTemperature, (await _controller.ReadValueAsync(ep, ColorControlCluster.Id, ColorControlCluster.AttributeColorMode)).AsUInt());

            _devices["extended-color-light"].SetValue("color", "#0000FF");
            Assert.Equal(169ul, (await _controller.ReadValueAsync(ep, ColorControlCluster.Id, ColorControlCluster.AttributeCurrentHue)).AsUInt());
        }

        [Fact]
        public async Task Door_lock_needs_a_timed_invoke()
        {
            var ep = Ep("door-lock");
            StatusCode untimed;
            try
            {
                untimed = MiniController.InvokeStatus(await _controller.InvokeAsync(ep, DoorLockCluster.Id, 0x01));
            }
            catch (MatterStatusException e)
            {
                untimed = e.Status;
            }

            Assert.Equal(StatusCode.NeedsTimedInteraction, untimed);
            Assert.Empty(Requests("door-lock", "locked"));

            var unlock = await _controller.InvokeAsync(ep, DoorLockCluster.Id, 0x01, timed: true);
            Assert.Equal(StatusCode.Success, MiniController.InvokeStatus(unlock));
            Assert.Equal(new object[] { false }, Requests("door-lock", "locked"));
            Assert.Equal(2ul, (await _controller.ReadValueAsync(ep, DoorLockCluster.Id, DoorLockCluster.AttributeLockState)).AsUInt());
        }

        [Fact]
        public async Task Fan_valve_and_covering_requests_reach_the_host()
        {
            Assert.Equal(StatusCode.Success, await _controller.WriteAsync(Ep("fan"), FanControlCluster.Id, FanControlCluster.AttributeFanMode, (w, t) => w.PutUInt(t, 2)));
            Assert.Equal(new object[] { 66 }, Requests("fan", "speed"));
            Assert.Equal(StatusCode.Success, await _controller.WriteAsync(Ep("fan"), FanControlCluster.Id, FanControlCluster.AttributePercentSetting, (w, t) => w.PutUInt(t, 100)));
            Assert.Equal(3ul, (await _controller.ReadValueAsync(Ep("fan"), FanControlCluster.Id, FanControlCluster.AttributeFanMode)).AsUInt());

            await _controller.InvokeAsync(Ep("water-valve"), ValveCluster.Id, 0x00);
            Assert.Equal(new object[] { true }, Requests("water-valve", "open"));

            await _controller.InvokeAsync(Ep("window-covering"), WindowCoveringCluster.Id, 0x05, w => w.PutUInt(0, 2500));
            await _controller.InvokeAsync(Ep("window-covering"), WindowCoveringCluster.Id, 0x00);
            await _controller.InvokeAsync(Ep("window-covering"), WindowCoveringCluster.Id, 0x02);
            Assert.Equal(new object[] { 25 }, Requests("window-covering", "position"));
            Assert.Equal(new object[] { true }, Requests("window-covering", "open"));
            Assert.Equal(new object[] { true }, Requests("window-covering", "stop"));
        }

        [Fact]
        public async Task Dimmable_plug_on_off_and_level()
        {
            var ep = Ep("dimmable-plug");
            await _controller.InvokeAsync(ep, OnOffCluster.Id, 0x01);
            await _controller.InvokeAsync(ep, LevelControlCluster.Id, 0x00, w =>
            {
                w.PutUInt(0, 127);
                w.PutUInt(1, 0);
                w.PutUInt(2, 0);
                w.PutUInt(3, 0);
            });
            Assert.Equal(new object[] { true }, Requests("dimmable-plug", "on-off"));
            Assert.Equal(new object[] { 50 }, Requests("dimmable-plug", "level"));
        }
    }

    public class ColorConversionTests
    {
        [Theory]
        [InlineData("#FF0000", 0, 1)]
        [InlineData("#00FF00", 120, 1)]
        [InlineData("#0000FF", 240, 1)]
        [InlineData("#FFFFFF", 0, 0)]
        public void Rgb_to_hsv(string rgb, double hue, double saturation)
        {
            Assert.True(ColorConversion.TryParseRgb(rgb, out var r, out var g, out var b));
            var (h, s, _) = ColorConversion.RgbToHsv(r, g, b);
            Assert.Equal(hue, h, 3);
            Assert.Equal(saturation, s, 3);
            Assert.Equal(rgb, ColorConversion.HsToRgb(h, s));
        }

        [Theory]
        [InlineData("#FF0000")]
        [InlineData("#00FF00")]
        [InlineData("#0000FF")]
        [InlineData("#FFFFFF")]
        public void Rgb_round_trips_through_xy(string rgb)
        {
            ColorConversion.TryParseRgb(rgb, out var r, out var g, out var b);
            var (x, y) = ColorConversion.RgbToXy(r, g, b);
            Assert.Equal(rgb, ColorConversion.XyToRgb(x, y));
        }

        [Fact]
        public void Kelvin_and_mireds()
        {
            Assert.Equal(250, ColorControlCluster.KelvinToMireds(4000));
            Assert.Equal(ColorControlCluster.MinMireds, ColorControlCluster.KelvinToMireds(10000));
            Assert.Equal(2000, ColorControlCluster.MiredsToKelvin(500));
        }
    }
}
