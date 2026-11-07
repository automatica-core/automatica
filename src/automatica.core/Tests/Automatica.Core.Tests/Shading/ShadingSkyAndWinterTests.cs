using System;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.Shading.Definition;
using Automatica.Core.Shading.Engine;
using Xunit;
using static Automatica.Core.Tests.Shading.ShadingTestHelper;

namespace Automatica.Core.Tests.Shading
{
    /// <summary>
    /// Phase 3: the sky model, irradiance and PV as brightness sources, winter mode
    /// </summary>
    public class ShadingSkyAndWinterTests : IDisposable
    {
        /// <summary>
        /// Solar noon in Vienna on midsummer: the sun is south, about 65° high
        /// </summary>
        private static readonly DateTimeOffset Noon = new(2026, 6, 21, 10, 56, 0, TimeSpan.Zero);

        private static readonly SunPosition HighSouthSun = new(180, 65);

        private readonly FakeShadingEnvironment _environment;
        private readonly InMemoryShadingStore _store = new();
        private readonly ShadingEngine _engine;
        private readonly FacadeConfig _south;
        private readonly Guid _sensor = Guid.NewGuid();

        public ShadingSkyAndWinterTests()
        {
            _environment = new FakeShadingEnvironment(CreateTime(Noon));
            _engine = CreateEngine(_environment, _store);
            _store.Settings.Brightness = new Signal();
            _south = new FacadeConfig(Guid.NewGuid(), "South", 180, true, new FacadeDefinition());
            _store.Facades.Add(_south);
        }

        public void Dispose() => _engine.Dispose();

        private FakeBlind AddBlind(BlindDefinition definition = null, int position = 0)
        {
            var blind = new FakeBlind { Name = $"Blind {_environment.Blinds.Count + 1}", Position = position };
            _environment.Blinds[blind.Id] = blind;
            _store.Blinds.Add(new BlindConfig(blind.Id, _south.Id, true, definition ?? new BlindDefinition()));
            return blind;
        }

        private async Task RunAsync(double minutes)
        {
            await _engine.StartAsync();
            await _engine.EvaluateAsync();
            _environment.FakeTime.Advance(TimeSpan.FromMinutes(minutes));
            await _engine.EvaluateAsync();
        }

        private BlindStatus Status(FakeBlind blind) => _engine.GetStatus().Blinds.Single(a => a.ControlId == blind.Id);

        // ---------- sky model ----------

        [Fact]
        public void CloudlessSky_FromCloudCover_MatchesClearSky_AndOvercast_HasNoDirectSun()
        {
            var clear = SunGeometry.ClearSky(65);
            var cloudless = SunGeometry.FromCloudCover(65, 0, Noon);
            var overcast = SunGeometry.FromCloudCover(65, 100, Noon);

            Assert.InRange(cloudless.DirectNormal, clear.DirectNormal * 0.99, clear.DirectNormal * 1.01);
            Assert.True(overcast.DirectNormal < clear.DirectNormal * 0.1, $"overcast DNI {overcast.DirectNormal}");
            Assert.True(overcast.DiffuseHorizontal > 0);
        }

        [Fact]
        public void GlobalHorizontal_IsSplit_IntoDirectAndDiffuse()
        {
            var clear = SunGeometry.ClearSky(65);

            var sunny = SunGeometry.FromGlobalHorizontal(clear.GlobalHorizontal, 65, Noon);
            var dull = SunGeometry.FromGlobalHorizontal(150, 65, Noon);
            var night = SunGeometry.FromGlobalHorizontal(20, -3, Noon);

            Assert.InRange(sunny.DirectNormal, clear.DirectNormal * 0.99, clear.DirectNormal * 1.01);
            Assert.True(dull.DirectNormal < 50, $"dull DNI {dull.DirectNormal}");
            Assert.Equal(0, night.DirectNormal);
        }

        [Fact]
        public void PlaneOfArray_ClearPanels_GiveFullSun_DarkPanels_None_AndSunBehindPanels_IsUnknown()
        {
            var clear = SunGeometry.FromPlaneOfArray(1000, HighSouthSun, 180, 30);
            var dark = SunGeometry.FromPlaneOfArray(60, HighSouthSun, 180, 30);
            var behind = SunGeometry.FromPlaneOfArray(500, new SunPosition(10, 20), 180, 30);

            Assert.NotNull(clear);
            Assert.InRange(clear.Value.DirectNormal / SunGeometry.ClearSky(65).DirectNormal, 0.9, 1.1);
            Assert.NotNull(dark);
            Assert.Equal(0, dark.Value.DirectNormal);
            Assert.Null(behind);
        }

        [Fact]
        public void PvPower_ToPlaneIrradiance()
        {
            Assert.Equal(1000, SunGeometry.PlaneIrradianceFromPower(8500, 10000, 0.85), 3);
            Assert.Equal(0, SunGeometry.PlaneIrradianceFromPower(-50, 10000, 0.85));
        }

        // ---------- irradiance and PV sources ----------

        [Theory]
        [InlineData(900, true)]
        [InlineData(150, false)]
        public async Task IrradianceSource_ShadesOnBrightSky(double globalIrradiance, bool shades)
        {
            _store.Settings.Brightness = new Signal { Sources = { new IrradianceSource { Node = _sensor } } };
            _environment.SetValue(_sensor, globalIrradiance);
            var blind = AddBlind();

            await RunAsync(3);

            Assert.Equal(shades ? BlindMode.Shading : BlindMode.Idle, Status(blind).Mode);
            Assert.StartsWith("irradiance:", _engine.GetStatus().Facades.Single().Brightness.Source);
        }

        [Theory]
        [InlineData(8.5, true)]
        [InlineData(1.5, false)]
        public async Task PvSource_ShadesWhenThePanelsProduceClearSkyPower(double kilowatts, bool shades)
        {
            _store.Settings.Brightness = new Signal { Sources = { new PvSource { Node = _sensor, Factor = 1000, PeakPower = 10000 } } };
            _environment.SetValue(_sensor, kilowatts);
            var blind = AddBlind();

            await RunAsync(3);

            Assert.Equal(shades ? BlindMode.Shading : BlindMode.Idle, Status(blind).Mode);
        }

        [Fact]
        public async Task PvSource_WithoutSunOnThePanels_FallsBackToTheNextSource()
        {
            // north facing panels never see the noon sun
            _store.Settings.Brightness = new Signal
            {
                Sources =
                {
                    new PvSource { Node = _sensor, PeakPower = 10000, Azimuth = 0, Tilt = 80 },
                    new CalculatedSource()
                }
            };
            _environment.SetValue(_sensor, 300);
            var blind = AddBlind();

            await RunAsync(3);

            Assert.Equal("calculated", _engine.GetStatus().Facades.Single().Brightness.Source);
            Assert.Equal(BlindMode.Shading, Status(blind).Mode);
        }

        // ---------- winter ----------

        private void SunnyAndWinterMonths()
        {
            _store.Settings.Brightness = Signal.Calculated();
            _store.Settings.Winter = new WinterDefinition { Mode = WinterMode.Months, FromMonth = 5, ToMonth = 7 };
        }

        [Fact]
        public async Task Winter_LetsTheSunIn()
        {
            SunnyAndWinterMonths();
            var blind = AddBlind(position: 0);

            await RunAsync(5);

            Assert.True(_engine.GetStatus().Winter);
            Assert.Equal(BlindMode.Winter, Status(blind).Mode);
            Assert.Empty(blind.Commands);
        }

        [Fact]
        public async Task Winter_BlindWithoutWinterGain_StillShades()
        {
            SunnyAndWinterMonths();
            var blind = AddBlind(new BlindDefinition { WinterGain = false });

            await RunAsync(5);

            Assert.Equal(BlindMode.Shading, Status(blind).Mode);
        }

        [Fact]
        public async Task Winter_ShadesAnyway_WhenTheRoomIsTooWarm()
        {
            SunnyAndWinterMonths();
            var room = Guid.NewGuid();
            var blind = AddBlind(new BlindDefinition { RoomTemperature = Signal.FromDatapoint(room), MaxRoomTemperature = 24 });

            _environment.SetValue(room, 22.0);
            await RunAsync(5);
            Assert.Equal(BlindMode.Winter, Status(blind).Mode);

            _environment.SetValue(room, 25.5);
            _environment.FakeTime.Advance(TimeSpan.FromMinutes(1));
            await _engine.EvaluateAsync();
            _environment.FakeTime.Advance(TimeSpan.FromMinutes(3));
            await _engine.EvaluateAsync();
            Assert.Equal(BlindMode.Shading, Status(blind).Mode);
        }

        [Fact]
        public async Task WinterByTemperature_HasOneDegreeHysteresis()
        {
            var outside = Guid.NewGuid();
            _store.Settings.Brightness = Signal.Calculated();
            _store.Settings.OutsideTemperature = Signal.FromDatapoint(outside);
            _store.Settings.Winter = new WinterDefinition { Mode = WinterMode.Temperature, Below = 15 };
            AddBlind();

            _environment.SetValue(outside, 14.0);
            await RunAsync(1);
            Assert.True(_engine.GetStatus().Winter);

            _environment.SetValue(outside, 15.5);
            await _engine.EvaluateAsync();
            Assert.True(_engine.GetStatus().Winter);

            _environment.SetValue(outside, 16.0);
            await _engine.EvaluateAsync();
            Assert.False(_engine.GetStatus().Winter);
        }

        [Theory]
        [InlineData(10, 4, 12, true)]
        [InlineData(10, 4, 2, true)]
        [InlineData(10, 4, 6, false)]
        [InlineData(11, 2, 11, true)]
        [InlineData(3, 5, 6, false)]
        public void WinterMonths_MayWrapTheYear(int from, int to, int month, bool winter)
        {
            var definition = new WinterDefinition { Mode = WinterMode.Months, FromMonth = from, ToMonth = to };
            Assert.Equal(winter, ShadingEngine.IsWinterMonth(definition, new DateTimeOffset(2026, month, 15, 12, 0, 0, TimeSpan.Zero)));
        }

        [Fact]
        public async Task Preview_KnowsWinter()
        {
            SunnyAndWinterMonths();
            var blind = AddBlind();
            await _engine.StartAsync();

            var preview = _engine.Preview(Noon);

            Assert.True(preview.Winter);
            Assert.False(preview.Blinds.Single(a => a.ControlId == blind.Id).Shade);
        }

        // ---------- pause ----------

        [Fact]
        public async Task Pause_IsSaved_AndHoldsAfterARestart()
        {
            _store.Settings.Brightness = Signal.Calculated();
            var blind = AddBlind();
            await _engine.StartAsync();

            Assert.True(await _engine.SetAutomaticAsync(blind.Id, false));
            Assert.True(_store.Blinds.Single().IsPaused);

            using var restarted = CreateEngine(_environment, _store);
            await restarted.StartAsync();
            _environment.FakeTime.Advance(TimeSpan.FromMinutes(5));
            await restarted.EvaluateAsync();

            Assert.Equal(BlindMode.Paused, restarted.GetStatus().Blinds.Single().Mode);
            Assert.Empty(blind.Commands);

            Assert.True(await restarted.SetAutomaticAsync(blind.Id, true));
            Assert.False(_store.Blinds.Single().IsPaused);
        }

        // ---------- definitions ----------

        [Fact]
        public void PvAndIrradiance_RoundTrip_AndAreBrightnessOnly()
        {
            var settings = new ShadingSettingsDefinition
            {
                Brightness = new Signal { Sources = { new PvSource { Node = Guid.NewGuid(), PeakPower = 9600, Azimuth = 200, Tilt = 25 }, new IrradianceSource { Node = Guid.NewGuid() } } },
                Wind = new Signal { Sources = { new IrradianceSource { Node = Guid.NewGuid() } } },
                Winter = new WinterDefinition { Mode = WinterMode.Temperature }
            };

            var json = ShadingJson.Serialize(settings);
            Assert.Contains("\"type\":\"pv\"", json);
            Assert.Contains("\"type\":\"irradiance\"", json);
            Assert.Contains("\"mode\":\"temperature\"", json);

            var parsed = ShadingJson.Parse<ShadingSettingsDefinition>(json);
            var pv = Assert.IsType<PvSource>(parsed.Brightness.Sources[0]);
            Assert.Equal(9600, pv.PeakPower);
            Assert.Equal(200, pv.Azimuth);
            Assert.Equal(WinterMode.Temperature, parsed.Winter.Mode);

            var problems = ShadingValidator.Validate(parsed);
            Assert.Contains(problems, a => a.Path == "wind/0" && a.Code == "BRIGHTNESS_ONLY");
            Assert.Contains(problems, a => a.Path == "winter" && a.Code == "TEMPERATURE_REQUIRED");
            Assert.DoesNotContain(problems, a => a.Path.StartsWith("brightness"));
        }

        [Fact]
        public void OldDocuments_GetWinterAndRoomTemperatureDefaults()
        {
            var settings = ShadingJson.Parse<ShadingSettingsDefinition>("{\"schema\":1,\"winter\":null}");
            var blind = ShadingJson.Parse<BlindDefinition>("{\"type\":\"roller\",\"roomTemperature\":null}");

            Assert.Equal(WinterMode.Off, settings.Winter.Mode);
            Assert.True(blind.WinterGain);
            Assert.Empty(blind.RoomTemperature.Sources);
        }
    }
}
