using System;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.Shading.Definition;
using Automatica.Core.Shading.Engine;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using static Automatica.Core.Tests.Shading.ShadingTestHelper;

namespace Automatica.Core.Tests.Shading
{
    public class ShadingEngineTests : IDisposable
    {
        /// <summary>
        /// Solar noon in Vienna on midsummer: the sun is south, about 65° high
        /// </summary>
        private static readonly DateTimeOffset Noon = new(2026, 6, 21, 10, 56, 0, TimeSpan.Zero);

        private readonly FakeShadingEnvironment _environment;
        private readonly InMemoryShadingStore _store = new();
        private readonly ShadingEngine _engine;

        private readonly Guid _southBrightness = Guid.NewGuid();
        private readonly FacadeConfig _south;
        private readonly FacadeConfig _north;

        public ShadingEngineTests() : this(Noon)
        {
        }

        private ShadingEngineTests(DateTimeOffset start)
        {
            _environment = new FakeShadingEnvironment(CreateTime(start));
            _engine = CreateEngine(_environment, _store);

            // only the sensors of the tests, no calculated brightness
            _store.Settings.Brightness = new Signal();

            _south = new FacadeConfig(Guid.NewGuid(), "South", 180, true, new FacadeDefinition { Brightness = Signal.FromDatapoint(_southBrightness) });
            _north = new FacadeConfig(Guid.NewGuid(), "North", 0, true, new FacadeDefinition { Brightness = Signal.FromDatapoint(_southBrightness) });
            _store.Facades.Add(_south);
            _store.Facades.Add(_north);
        }

        public void Dispose() => _engine.Dispose();

        private FakeBlind AddBlind(FacadeConfig facade, BlindDefinition definition = null, int position = 0, bool slats = false, bool enabled = true)
        {
            var blind = new FakeBlind { Name = $"Blind {_environment.Blinds.Count + 1}", Position = position, HasSlats = slats };
            _environment.Blinds[blind.Id] = blind;
            _store.Blinds.Add(new BlindConfig(blind.Id, facade?.Id, enabled, definition ?? new BlindDefinition()));
            return blind;
        }

        private void Sun(double lux) => _environment.SetValue(_southBrightness, lux);

        private async Task StepAsync(double minutes = 0)
        {
            _environment.FakeTime.Advance(TimeSpan.FromMinutes(minutes));
            await _engine.EvaluateAsync();
        }

        private BlindStatus Status(FakeBlind blind) => _engine.GetStatus().Blinds.Single(a => a.ControlId == blind.Id);

        /// <summary>
        /// Starts the engine with direct sun and waits the on delay: the blind is shading
        /// </summary>
        private async Task StartShadingAsync()
        {
            Sun(50000);
            await _engine.StartAsync();
            await StepAsync(2);
        }

        [Fact]
        public async Task RollerBlind_ShadesAfterOnDelay_AndRestoresPositionAfterOffDelay()
        {
            var blind = AddBlind(_south, position: 20);

            Sun(50000);
            await _engine.StartAsync();
            Assert.Empty(blind.Commands);
            Assert.Equal(BlindMode.Idle, Status(blind).Mode);

            await StepAsync(1);
            Assert.Empty(blind.Commands);

            await StepAsync(1);
            Assert.Equal(new (int?, int?)[] { (100, null) }, blind.Commands);
            Assert.Equal(BlindMode.Shading, Status(blind).Mode);
            Assert.True(_engine.IsShading(blind.Id));

            // a short cloud does not open it
            Sun(5000);
            await StepAsync(5);
            Sun(50000);
            await StepAsync(5);
            Sun(5000);
            await StepAsync();
            await StepAsync(14);
            Assert.Single(blind.Commands);

            await StepAsync(1);
            Assert.Equal((20, (int?)null), blind.Commands.Last());
            Assert.Equal(BlindMode.Idle, Status(blind).Mode);
            Assert.False(_engine.IsShading(blind.Id));
        }

        [Fact]
        public async Task BetweenThresholds_KeepsTheState()
        {
            var blind = AddBlind(_south);
            await StartShadingAsync();
            Assert.Single(blind.Commands);

            // between off (12 klx) and on (20 klx): still shading
            Sun(15000);
            await StepAsync(60);
            Assert.Single(blind.Commands);
            Assert.Equal(BlindMode.Shading, Status(blind).Mode);
        }

        [Fact]
        public async Task SunNotOnTheWindow_DoesNotShade()
        {
            var blind = AddBlind(_north);
            await StartShadingAsync();
            await StepAsync(10);

            Assert.Empty(blind.Commands);
            var status = Status(blind);
            Assert.Equal(BlindMode.Idle, status.Mode);
            Assert.False(status.SunOnWindow);
        }

        [Fact]
        public async Task OwnAzimuth_OverridesTheFacade()
        {
            var blind = AddBlind(_north, new BlindDefinition { Azimuth = 170 });
            await StartShadingAsync();

            Assert.Single(blind.Commands);
            Assert.True(Status(blind).SunOnWindow);
        }

        [Fact]
        public async Task Overhang_HighSunDoesNotReachTheWindow()
        {
            var blind = AddBlind(_south, new BlindDefinition { MaxElevation = 50 });
            await StartShadingAsync();

            Assert.Empty(blind.Commands);
            Assert.False(Status(blind).SunOnWindow);
        }

        [Fact]
        public async Task AlreadyClosed_IsNotTouched_AndNotOpenedAfterwards()
        {
            var blind = AddBlind(_south, position: 100);
            await StartShadingAsync();
            Assert.Empty(blind.Commands);
            Assert.Equal("Already closed", Status(blind).Reason);

            Sun(1000);
            await StepAsync();
            await StepAsync(15);
            Assert.Empty(blind.Commands);
        }

        [Fact]
        public async Task OpenPosition_IsUsedWhenRestoreIsOff()
        {
            var blind = AddBlind(_south, new BlindDefinition { ShadePosition = 80, RestorePosition = false, OpenPosition = 10 }, position: 40);
            await StartShadingAsync();
            Assert.Equal((80, (int?)null), blind.Commands.Single());

            Sun(1000);
            await StepAsync();
            await StepAsync(15);
            Assert.Equal((10, (int?)null), blind.Commands.Last());
        }

        /// <summary>
        /// West facade, from 14:56 UTC: the sun goes down in front of it
        /// </summary>
        private FacadeConfig WestAfternoon()
        {
            var west = new FacadeConfig(Guid.NewGuid(), "West", 270, true, new FacadeDefinition { Brightness = Signal.FromDatapoint(_southBrightness) });
            _store.Facades.Add(west);
            _environment.FakeTime.Advance(TimeSpan.FromHours(4));
            return west;
        }

        [Fact]
        public async Task VenetianBlind_HighSun_SlatsOpen()
        {
            var blind = AddBlind(_south, new BlindDefinition { Type = BlindType.Venetian }, slats: true);
            await StartShadingAsync();

            Assert.Equal(2, blind.Commands.Count);
            Assert.Equal((100, (int?)null), blind.Commands[0]);
            Assert.Null(blind.Commands[1].Position);

            // the sun at noon is 65° high: horizontal slats already block it, only the 5° margin is left
            Assert.Equal(6, blind.Commands[1].Slat);
        }

        [Fact]
        public async Task VenetianBlind_SetsSlatsAndFollowsTheSun()
        {
            var blind = AddBlind(WestAfternoon(), new BlindDefinition { Type = BlindType.Venetian }, slats: true);
            await StartShadingAsync();

            Assert.Equal(2, blind.Commands.Count);
            Assert.Equal((100, (int?)null), blind.Commands[0]);
            var first = blind.Commands[1].Slat!.Value;

            // an hour later the sun is lower: the slats close more, the blind itself does not move
            await StepAsync(60);
            var last = blind.Commands.Last();
            Assert.Null(last.Position);
            Assert.True(last.Slat > first + 10, $"first {first}, an hour later {last.Slat}");
        }

        [Fact]
        public async Task VenetianBlind_SlatsMoveAtMostEveryInterval()
        {
            var blind = AddBlind(WestAfternoon(), new BlindDefinition { Type = BlindType.Venetian, Slats = new SlatDefinition { TrackSun = true } }, slats: true);
            _store.Settings.MinSlatChange = 1;
            _store.Settings.MinMoveIntervalMinutes = 30;
            await StartShadingAsync();
            var count = blind.Commands.Count;

            for (var i = 0; i < 29; i++)
            {
                await StepAsync(1);
            }
            Assert.Equal(count, blind.Commands.Count);

            await StepAsync(1);
            Assert.Equal(count + 1, blind.Commands.Count);
        }

        [Fact]
        public void ShadeSlat_MapsTheCutOffAngleToPercent()
        {
            var config = new BlindConfig(Guid.NewGuid(), _south.Id, true, new BlindDefinition { Type = BlindType.Venetian });
            var runtime = new BlindRuntime(config, _south, _environment, NullLogger.Instance);

            // profile 30°, 80/72 mm slats: asin(0.9 * cos 30°) - 30° = 21.2°, + 5° margin = 26.2° → 29 %
            Assert.Equal(29, runtime.ShadeSlat(new SunPosition(180, 30)));

            // actuator with 0..180° range, 100 % at 0°
            config.Definition.Slats = new SlatDefinition { ExtraAngle = 0, MinAngle = 0, MaxAngle = 90, PercentAtMinAngle = 100, PercentAtMaxAngle = 0 };
            Assert.Equal(76, runtime.ShadeSlat(new SunPosition(180, 30)));

            // fixed angle
            config.Definition.Slats = new SlatDefinition { TrackSun = false, FixedAngle = 45 };
            Assert.Equal(50, runtime.ShadeSlat(new SunPosition(180, 30)));

            // roller blind has no slats
            config.Definition.Type = BlindType.Roller;
            Assert.Null(runtime.ShadeSlat(new SunPosition(180, 30)));
        }

        [Fact]
        public async Task WindAlarm_RetractsVenetianBlind_UntilReleaseTime()
        {
            var wind = Guid.NewGuid();
            _store.Settings.Wind = Signal.FromDatapoint(wind);
            _environment.SetValue(wind, 2.0);
            var blind = AddBlind(_south, new BlindDefinition { Type = BlindType.Venetian }, slats: true);
            var roller = AddBlind(_south);
            await StartShadingAsync();
            Assert.Equal(1, _environment.SubscriberCount(wind));
            var count = blind.Commands.Count;

            _environment.SetValue(wind, 15.0);
            await StepAsync();
            Assert.Equal((0, (int?)null), blind.Commands.Last());
            Assert.Equal(BlindMode.Wind, Status(blind).Mode);
            Assert.True(_engine.GetStatus().WindAlarm);

            // roller blinds have no wind protection by default
            Assert.Equal(BlindMode.Shading, Status(roller).Mode);

            // calm again, but the release time is not over
            _environment.SetValue(wind, 2.0);
            await StepAsync(9);
            Assert.Equal(count + 1, blind.Commands.Count);
            Assert.Equal(BlindMode.Wind, Status(blind).Mode);

            await StepAsync(1);
            Assert.Equal(BlindMode.Shading, Status(blind).Mode);
            Assert.Equal((100, (int?)null), blind.Commands[count + 1]);
        }

        [Fact]
        public async Task WindAlarm_RetractsAgain_WhenLoweredDuringTheAlarm()
        {
            var wind = Guid.NewGuid();
            _store.Settings.Wind = Signal.FromDatapoint(wind);
            _environment.SetValue(wind, 20.0);
            var blind = AddBlind(_south, new BlindDefinition { Type = BlindType.Venetian }, position: 50, slats: true);

            await _engine.StartAsync();
            Assert.Equal((0, (int?)null), blind.Commands.Single());

            blind.Position = 60;
            await StepAsync(10);
            Assert.Equal(2, blind.Commands.Count);
            Assert.Equal((0, (int?)null), blind.Commands.Last());
        }

        [Fact]
        public async Task RainAlarm_OnlyForBlindsWithRainProtection()
        {
            var rain = Guid.NewGuid();
            _store.Settings.Rain = Signal.FromDatapoint(rain);
            var awning = AddBlind(_south, new BlindDefinition { RainProtection = true });
            var roller = AddBlind(_south);
            await StartShadingAsync();

            _environment.SetValue(rain, true);
            await StepAsync();

            Assert.Equal(BlindMode.Rain, Status(awning).Mode);
            Assert.Equal((0, (int?)null), awning.Commands.Last());
            Assert.Equal(BlindMode.Shading, Status(roller).Mode);
        }

        [Fact]
        public async Task Frost_NoMoves()
        {
            var temperature = Guid.NewGuid();
            _store.Settings.OutsideTemperature = Signal.FromDatapoint(temperature);
            _store.Settings.FrostBelow = 1;
            _environment.SetValue(temperature, -3.0);
            var blind = AddBlind(_south);

            await StartShadingAsync();
            Assert.Empty(blind.Commands);
            Assert.Equal(BlindMode.Frost, Status(blind).Mode);
            Assert.True(_engine.GetStatus().Frost);
        }

        [Fact]
        public async Task MinOutsideTemperature_OnlyShadesWhenWarm()
        {
            var temperature = Guid.NewGuid();
            _store.Settings.OutsideTemperature = Signal.FromDatapoint(temperature);
            _store.Facades[0] = _south with { Definition = new FacadeDefinition { Brightness = Signal.FromDatapoint(_southBrightness), MinOutsideTemperature = 22 } };
            _environment.SetValue(temperature, 18.0);
            var blind = AddBlind(_store.Facades[0]);

            await StartShadingAsync();
            Assert.Empty(blind.Commands);

            _environment.SetValue(temperature, 25.0);
            await StepAsync();
            Assert.Single(blind.Commands);
        }

        [Fact]
        public async Task ManualMove_PausesUntilTheSunGoes_ThenTheAutomaticTakesOver()
        {
            var blind = AddBlind(_south, position: 10);
            await StartShadingAsync();
            Assert.Single(blind.Commands);

            // the user opens the blind (after the travel time of the automatic move)
            _environment.FakeTime.Advance(TimeSpan.FromMinutes(3));
            blind.MoveByHand(30);
            await StepAsync(1);
            Assert.Equal(BlindMode.Manual, Status(blind).Mode);

            await StepAsync(30);
            Assert.Single(blind.Commands);

            // sun goes: the override ends, nothing is restored (the user took over)
            Sun(1000);
            await StepAsync();
            await StepAsync(15);
            Assert.Single(blind.Commands);
            Assert.Equal(BlindMode.Idle, Status(blind).Mode);

            // sun comes again: shades and afterwards restores the user's position
            Sun(50000);
            await StepAsync();
            await StepAsync(2);
            Assert.Equal((100, (int?)null), blind.Commands.Last());

            Sun(1000);
            await StepAsync();
            await StepAsync(15);
            Assert.Equal((30, (int?)null), blind.Commands.Last());
        }

        [Fact]
        public async Task PositionChangesDuringTheAutomaticMove_AreNotManual()
        {
            var blind = AddBlind(_south);
            await StartShadingAsync();

            // feedback while the blind travels to 100 %
            _environment.FakeTime.Advance(TimeSpan.FromSeconds(20));
            blind.MoveByHand(50);
            _environment.FakeTime.Advance(TimeSpan.FromSeconds(20));
            blind.MoveByHand(100);

            // late final feedback of the target position
            _environment.FakeTime.Advance(TimeSpan.FromMinutes(3));
            blind.MoveByHand(100);

            await StepAsync();
            Assert.Equal(BlindMode.Shading, Status(blind).Mode);
        }

        [Fact]
        public async Task UpFromTheVisualization_IsManual()
        {
            var blind = AddBlind(_south);
            await StartShadingAsync();

            _environment.FakeTime.Advance(TimeSpan.FromMinutes(3));
            blind.StartMovingByHand();
            await StepAsync();

            Assert.Equal(BlindMode.Manual, Status(blind).Mode);
        }

        [Fact]
        public async Task ChangesRightAfterStartup_AreNotManual()
        {
            var blind = AddBlind(_south, position: 0);
            await _engine.StartAsync();

            // the first position feedback after the start
            blind.MoveByHand(40);
            await StepAsync(3);
            Assert.NotEqual(BlindMode.Manual, Status(blind).Mode);
        }

        [Fact]
        public async Task ManualMove_DurationMode_AutomaticAgainAfterTheDuration()
        {
            _store.Settings.ManualOverride = ManualOverrideMode.Duration;
            _store.Settings.ManualOverrideMinutes = 60;
            var blind = AddBlind(_south);
            await StartShadingAsync();

            _environment.FakeTime.Advance(TimeSpan.FromMinutes(3));
            blind.MoveByHand(30);
            await StepAsync();
            var status = Status(blind);
            Assert.Equal(BlindMode.Manual, status.Mode);
            Assert.Equal(_environment.FakeTime.GetUtcNow().AddMinutes(60), status.ManualUntil);

            await StepAsync(59);
            Assert.Single(blind.Commands);

            await StepAsync(1);
            Assert.Equal(2, blind.Commands.Count);
            Assert.Equal((100, (int?)null), blind.Commands.Last());
        }

        [Fact]
        public async Task Resume_EndsTheManualOverride()
        {
            var blind = AddBlind(_south);
            await StartShadingAsync();

            _environment.FakeTime.Advance(TimeSpan.FromMinutes(3));
            blind.MoveByHand(30);
            await StepAsync();
            Assert.Equal(BlindMode.Manual, Status(blind).Mode);

            Assert.True(await _engine.ResumeAsync(blind.Id));
            Assert.Equal(BlindMode.Shading, Status(blind).Mode);
            Assert.Equal((100, (int?)null), blind.Commands.Last());
        }

        [Fact]
        public async Task DoorOpen_IsNotLowered()
        {
            var door = Guid.NewGuid();
            _environment.SetValue(door, true);
            var blind = AddBlind(_south, new BlindDefinition { DoorContact = door });

            await StartShadingAsync();
            Assert.Empty(blind.Commands);
            Assert.Equal(BlindMode.DoorOpen, Status(blind).Mode);

            _environment.SetValue(door, false);
            await StepAsync();
            Assert.Equal((100, (int?)null), blind.Commands.Single());
        }

        [Fact]
        public async Task Paused_NoMoves_UntilResumed()
        {
            var blind = AddBlind(_south);
            Sun(50000);
            await _engine.StartAsync();
            Assert.True(await _engine.SetAutomaticAsync(blind.Id, false));
            await StepAsync(5);
            Assert.Empty(blind.Commands);
            Assert.Equal(BlindMode.Paused, Status(blind).Mode);

            Assert.True(await _engine.SetAutomaticAsync(blind.Id, true));
            Assert.Single(blind.Commands);

            Assert.False(await _engine.SetAutomaticAsync(Guid.NewGuid(), false));
        }

        [Fact]
        public async Task Disabled_GloballyOrPerBlindOrFacade_NoMoves()
        {
            var blind = AddBlind(_south, enabled: false);
            _store.Facades.Add(_south with { Id = Guid.NewGuid(), IsEnabled = false });
            var second = AddBlind(_store.Facades.Last());

            await StartShadingAsync();
            Assert.Empty(blind.Commands);
            Assert.Empty(second.Commands);
            Assert.Equal(BlindMode.Off, Status(blind).Mode);
            Assert.Equal(BlindMode.Off, Status(second).Mode);
        }

        [Fact]
        public async Task StaleFacadeSensor_FallsBackToTheGlobalBrightness()
        {
            var global = Guid.NewGuid();
            _store.Settings.Brightness = Signal.FromDatapoint(global);
            _store.Facades[0] = _south with { Definition = new FacadeDefinition { Brightness = Signal.FromDatapoint(_southBrightness, staleAfterMinutes: 10) } };
            var blind = AddBlind(_store.Facades[0]);

            _environment.SetValue(_southBrightness, 1000.0, _environment.FakeTime.GetUtcNow().AddMinutes(-30));
            _environment.SetValue(global, 60000.0);

            await _engine.StartAsync();
            await StepAsync(2);

            Assert.Single(blind.Commands);
            var facade = _engine.GetStatus().Facades.Single(a => a.Id == _south.Id);
            Assert.Equal($"datapoint:{global}", facade.Brightness.Source);
        }

        [Fact]
        public async Task DatapointFactor_ConvertsKiloLux()
        {
            _store.Facades[0] = _south with { Definition = new FacadeDefinition { Brightness = Signal.FromDatapoint(_southBrightness, factor: 1000) } };
            var blind = AddBlind(_store.Facades[0]);

            Sun(45);
            await _engine.StartAsync();
            await StepAsync(2);

            Assert.Single(blind.Commands);
            Assert.Equal(45000, _engine.GetStatus().Facades.Single(a => a.Id == _south.Id).Brightness.Value);
        }

        [Fact]
        public async Task NoSensors_CalculatedBrightness_ShadesOnAClearDay_ButNotWhenOvercast()
        {
            var clouds = Guid.NewGuid();
            _store.Settings.Brightness = new Signal { Sources = { new CalculatedSource { CloudCover = Signal.FromDatapoint(clouds) } } };
            _store.Facades[0] = _south with { Definition = new FacadeDefinition() };
            var blind = AddBlind(_store.Facades[0]);

            _environment.SetValue(clouds, 100.0);
            await _engine.StartAsync();
            await StepAsync(5);
            Assert.Empty(blind.Commands);

            _environment.SetValue(clouds, 0.0);
            await StepAsync();
            await StepAsync(2);
            Assert.Single(blind.Commands);
            Assert.StartsWith("calculated+clouds:", _engine.GetStatus().Facades.Single(a => a.Id == _south.Id).Brightness.Source);
        }

        [Fact]
        public async Task SunPositionFromDatapoints_WinsOverTheCalculation()
        {
            var azimuth = Guid.NewGuid();
            var elevation = Guid.NewGuid();
            _store.Settings.SunAzimuth = new Signal { Sources = { new DatapointSource { Node = azimuth }, new CalculatedSource() } };
            _store.Settings.SunElevation = new Signal { Sources = { new DatapointSource { Node = elevation }, new CalculatedSource() } };

            // the station says the sun is north (calculation: south)
            _environment.SetValue(azimuth, 10.0);
            _environment.SetValue(elevation, 20.0);
            var north = AddBlind(_north);
            var south = AddBlind(_south);

            await StartShadingAsync();
            Assert.Single(north.Commands);
            Assert.Empty(south.Commands);
            Assert.Equal($"datapoint:{azimuth},datapoint:{elevation}", _engine.GetStatus().SunSource);
        }

        [Fact]
        public async Task NoLocation_AndNoSunSensor_NothingHappens()
        {
            _environment.Location = null;
            var blind = AddBlind(_south);
            await StartShadingAsync();

            Assert.Empty(blind.Commands);
            Assert.Null(_engine.GetStatus().Sun);
            Assert.False(_engine.GetStatus().HasLocation);
        }

        [Fact]
        public async Task BlindNotLoaded_IsUnavailable_AndPickedUpLater()
        {
            var blind = AddBlind(_south);
            _environment.Blinds.Remove(blind.Id);
            await StartShadingAsync();
            Assert.Equal(BlindMode.Unavailable, Status(blind).Mode);

            _environment.Blinds[blind.Id] = blind;
            await StepAsync();
            Assert.Single(blind.Commands);
            Assert.Equal(1, blind.CallbackCount);
        }

        [Fact]
        public async Task Reload_KeepsTheStateOfUnchangedBlinds_AndReleasesRemovedOnes()
        {
            var blind = AddBlind(_south, position: 15);
            var removed = AddBlind(_south);
            await StartShadingAsync();
            Assert.Equal(1, removed.CallbackCount);

            _store.Blinds[0] = _store.Blinds[0] with { Definition = new BlindDefinition { ShadePosition = 90 } };
            _store.Blinds.RemoveAt(1);
            await _engine.ReloadAsync();

            Assert.Equal(0, removed.CallbackCount);
            Assert.Single(blind.Commands);
            Assert.True(_engine.IsShading(blind.Id));
            Assert.False(_engine.IsConfigured(removed.Id));

            Sun(1000);
            await StepAsync();
            await StepAsync(15);
            Assert.Equal((15, (int?)null), blind.Commands.Last());
        }

        [Fact]
        public async Task Preview_SimulatesTheDay()
        {
            _store.Settings.Brightness = Signal.Calculated();
            _store.Facades[0] = _south with { Definition = new FacadeDefinition() };
            _store.Facades[1] = _north with { Definition = new FacadeDefinition() };
            var south = AddBlind(_store.Facades[0]);
            var north = AddBlind(_store.Facades[1]);
            await _engine.StartAsync();

            var noon = _engine.Preview(Noon);
            Assert.True(noon.Blinds.Single(a => a.ControlId == south.Id).Shade);
            Assert.Equal(100, noon.Blinds.Single(a => a.ControlId == south.Id).TargetPosition);
            Assert.False(noon.Blinds.Single(a => a.ControlId == north.Id).Shade);

            var night = _engine.Preview(Noon.AddHours(12));
            Assert.All(night.Blinds, a => Assert.False(a.Shade));
            Assert.True(night.Sun!.Value.Elevation < 0);

            // a preview moves nothing
            Assert.Empty(south.Commands);
        }

        [Fact]
        public async Task Stop_UnregistersEverything()
        {
            var wind = Guid.NewGuid();
            _store.Settings.Wind = Signal.FromDatapoint(wind);
            var blind = AddBlind(_south);
            await StartShadingAsync();
            Assert.Equal(1, _environment.SubscriberCount(wind));
            Assert.Equal(1, blind.CallbackCount);

            await _engine.StopAsync();
            Assert.Equal(0, _environment.SubscriberCount(wind));
            Assert.Equal(0, blind.CallbackCount);
            Assert.False(_engine.IsConfigured(blind.Id));
        }
    }
}
