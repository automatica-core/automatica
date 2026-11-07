using System.Collections.Concurrent;
using Automatica.Core.Shading.Definition;
using Microsoft.Extensions.Logging;

namespace Automatica.Core.Shading.Engine
{
    /// <summary>
    /// Automatic shading: every minute (and when wind, rain, temperature or a door contact changes) the sun position,
    /// the brightness per facade and the weather are read, and every configured blind decides what to do.
    /// </summary>
    internal sealed class ShadingEngine : IShadingEngine, IDisposable
    {
        internal static readonly TimeSpan DefaultInterval = TimeSpan.FromMinutes(1);

        private readonly IShadingEnvironment _environment;
        private readonly IShadingStore _store;
        private readonly ILogger<ShadingEngine> _logger;
        private readonly TimeSpan? _interval;
        private readonly SignalReader _reader;

        private readonly SemaphoreSlim _gate = new(1, 1);
        // read without the gate (IsShading may be called while an evaluation moves a blind)
        private readonly ConcurrentDictionary<Guid, BlindRuntime> _blinds = new();
        private readonly Dictionary<Guid, SunHysteresis> _sunStates = new();
        private readonly List<IDisposable> _subscriptions = new();

        private ShadingConfiguration _config = new();
        private ITimer _timer;
        private bool _started;
        private DateTimeOffset? _rainUntil;
        private int _evaluationQueued;
        private ShadingStatus _status;
        private bool _winterByTemperature;

        public ShadingEngine(IShadingEnvironment environment, IShadingStore store, ILogger<ShadingEngine> logger)
            : this(environment, store, logger, DefaultInterval)
        {
        }

        /// <param name="interval">Null: no timer, the tests call <see cref="EvaluateAsync"/></param>
        internal ShadingEngine(IShadingEnvironment environment, IShadingStore store, ILogger<ShadingEngine> logger, TimeSpan? interval)
        {
            _environment = environment;
            _store = store;
            _logger = logger;
            _interval = interval;
            _reader = new SignalReader(environment);
        }

        public async Task StartAsync(CancellationToken token = default)
        {
            await _gate.WaitAsync(token).ConfigureAwait(false);
            try
            {
                if (_started)
                {
                    return;
                }

                _started = true;
                await LoadAsync(token).ConfigureAwait(false);
                _logger.LogInformation($"Shading: {_blinds.Count} blinds on {_config.Facades.Count} facades");
            }
            finally
            {
                _gate.Release();
            }

            await EvaluateAsync(token).ConfigureAwait(false);

            if (_interval.HasValue)
            {
                _timer = _environment.Time.CreateTimer(_ => QueueEvaluation(), null, _interval.Value, _interval.Value);
            }
        }

        public async Task StopAsync()
        {
            await _gate.WaitAsync().ConfigureAwait(false);
            try
            {
                _started = false;
                _timer?.Dispose();
                _timer = null;
                Unload();
            }
            finally
            {
                _gate.Release();
            }
        }

        public async Task ReloadAsync(CancellationToken token = default)
        {
            await _gate.WaitAsync(token).ConfigureAwait(false);
            try
            {
                if (!_started)
                {
                    return;
                }

                await LoadAsync(token).ConfigureAwait(false);
            }
            finally
            {
                _gate.Release();
            }

            await EvaluateAsync(token).ConfigureAwait(false);
        }

        private async Task LoadAsync(CancellationToken token)
        {
            var config = await _store.LoadAsync(token).ConfigureAwait(false) ?? new ShadingConfiguration();
            _config = config;

            foreach (var subscription in _subscriptions)
            {
                subscription.Dispose();
            }
            _subscriptions.Clear();

            var facades = config.Facades.ToDictionary(a => a.Id);
            var configured = new HashSet<Guid>();
            foreach (var blind in config.Blinds)
            {
                configured.Add(blind.ControlId);
                var facade = blind.FacadeId.HasValue && facades.TryGetValue(blind.FacadeId.Value, out var f) ? f : null;
                if (_blinds.TryGetValue(blind.ControlId, out var runtime))
                {
                    // keep manual overrides and the position before the shading
                    runtime.Update(blind, facade);
                }
                else
                {
                    _blinds[blind.ControlId] = new BlindRuntime(blind, facade, _environment, _logger);
                }
            }

            foreach (var removed in _blinds.Keys.Where(a => !configured.Contains(a)).ToList())
            {
                if (_blinds.TryRemove(removed, out var runtime))
                {
                    runtime.Dispose();
                }
            }

            var sunKeys = _blinds.Values.Select(a => a.SunKey).ToHashSet();
            foreach (var key in _sunStates.Keys.Where(a => !sunKeys.Contains(a)).ToList())
            {
                _sunStates.Remove(key);
            }

            // safety and door contacts are evaluated right away, brightness every minute
            var immediate = new HashSet<Guid>();
            foreach (var signal in new[] { config.Settings.Wind, config.Settings.Rain, config.Settings.OutsideTemperature })
            {
                foreach (var source in signal?.Sources?.OfType<DatapointSource>() ?? Enumerable.Empty<DatapointSource>())
                {
                    immediate.Add(source.Node);
                }
            }
            foreach (var blind in config.Blinds.Where(a => a.Definition.DoorContact.HasValue))
            {
                immediate.Add(blind.Definition.DoorContact.Value);
            }

            foreach (var id in immediate.Where(a => a != Guid.Empty))
            {
                try
                {
                    _subscriptions.Add(await _environment.SubscribeAsync(id, _ => { QueueEvaluation(); return Task.CompletedTask; }).ConfigureAwait(false));
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e, $"Shading: could not subscribe to {id}");
                }
            }
        }

        private void Unload()
        {
            foreach (var subscription in _subscriptions)
            {
                subscription.Dispose();
            }
            _subscriptions.Clear();

            foreach (var blind in _blinds.Values)
            {
                blind.Dispose();
            }
            _blinds.Clear();
            _sunStates.Clear();
        }

        private void QueueEvaluation()
        {
            // coalesce: one evaluation waiting is enough
            if (Interlocked.Exchange(ref _evaluationQueued, 1) == 1)
            {
                return;
            }

            _ = Task.Run(async () =>
            {
                try
                {
                    await EvaluateAsync().ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Shading: evaluation failed");
                }
            });
        }

        public async Task EvaluateAsync(CancellationToken token = default)
        {
            await _gate.WaitAsync(token).ConfigureAwait(false);
            try
            {
                Interlocked.Exchange(ref _evaluationQueued, 0);
                if (!_started)
                {
                    return;
                }

                await EvaluateCoreAsync(token).ConfigureAwait(false);
            }
            finally
            {
                _gate.Release();
            }
        }

        private async Task EvaluateCoreAsync(CancellationToken token)
        {
            var now = _environment.Time.GetUtcNow();
            var settings = _config.Settings;

            var (sun, sunSource) = ReadSun(settings, now);

            var wind = _reader.Read(settings.Wind, now);
            var rain = _reader.Read(settings.Rain, now);
            if (rain is { Value: > 0 })
            {
                _rainUntil = now + TimeSpan.FromMinutes(Math.Max(0, settings.RainReleaseMinutes));
            }
            var rainAlarm = _rainUntil.HasValue && now < _rainUntil.Value;

            var temperature = _reader.Read(settings.OutsideTemperature, now);
            var frost = settings.FrostBelow.HasValue && temperature != null && temperature.Value < settings.FrostBelow.Value;
            var winter = UpdateWinter(settings.Winter, temperature?.Value, now);

            // sun state per facade (or per blind without facade)
            var facadeStatus = new List<FacadeStatus>();
            var sunny = new Dictionary<Guid, (bool Sunny, bool TemperatureOk)>();
            foreach (var group in _blinds.Values.GroupBy(a => a.SunKey))
            {
                var first = group.First();
                var facade = first.Facade;
                var definition = facade?.Definition ?? new FacadeDefinition();
                var azimuth = facade?.Azimuth ?? first.Azimuth;

                var brightness = ReadBrightness(facade, azimuth, sun, settings, now);
                if (!_sunStates.TryGetValue(group.Key, out var hysteresis))
                {
                    hysteresis = _sunStates[group.Key] = new SunHysteresis();
                }

                var isSunny = hysteresis.Update(brightness?.Value, definition.Thresholds ?? settings.Thresholds, now);
                var temperatureOk = !definition.MinOutsideTemperature.HasValue || temperature == null || temperature.Value >= definition.MinOutsideTemperature.Value;
                sunny[group.Key] = (isSunny, temperatureOk);

                if (facade != null)
                {
                    var onFacade = sun.HasValue && SunGeometry.IsSunOnWindow(sun.Value, facade.Azimuth, definition.AcceptanceAngle, definition.MinElevation, definition.MaxElevation);
                    facadeStatus.Add(new FacadeStatus(facade.Id, facade.Name, facade.Azimuth, facade.IsEnabled, isSunny, onFacade, ToInfo(brightness), temperatureOk));
                }
            }

            foreach (var facade in _config.Facades.Where(a => facadeStatus.All(b => b.Id != a.Id)))
            {
                var brightness = ReadBrightness(facade, facade.Azimuth, sun, settings, now);
                var onFacade = sun.HasValue && SunGeometry.IsSunOnWindow(sun.Value, facade.Azimuth, facade.Definition.AcceptanceAngle, facade.Definition.MinElevation, facade.Definition.MaxElevation);
                facadeStatus.Add(new FacadeStatus(facade.Id, facade.Name, facade.Azimuth, facade.IsEnabled,
                    SunHysteresis.Instant(brightness?.Value, facade.Definition.Thresholds ?? settings.Thresholds), onFacade, ToInfo(brightness), true));
            }

            foreach (var blind in _blinds.Values)
            {
                var (isSunny, temperatureOk) = sunny[blind.SunKey];
                bool? doorOpen = null;
                if (blind.Config.Definition.DoorContact is { } door)
                {
                    var value = SignalReader.ToDouble(_environment.GetValue(door)?.Value);
                    doorOpen = value.HasValue ? value.Value > 0 : null;
                }

                var roomTemperature = _reader.Read(blind.Config.Definition.RoomTemperature, now)?.Value;
                var context = new EvaluationContext(now, settings, sun, wind?.Value, rainAlarm, frost, isSunny, temperatureOk, doorOpen, winter, roomTemperature);
                try
                {
                    await blind.EvaluateAsync(context, token).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, $"Shading: error evaluating blind {blind.Config.ControlId}");
                }
            }

            _status = new ShadingStatus(
                settings.IsEnabled,
                _environment.Location.HasValue,
                now,
                sun,
                sunSource,
                ToInfo(wind),
                _blinds.Values.Any(a => a.Status.Mode == BlindMode.Wind) || (wind != null && settings.WindLimit > 0 && wind.Value >= settings.WindLimit),
                ToInfo(rain),
                rainAlarm,
                ToInfo(temperature),
                frost,
                winter,
                facadeStatus,
                _blinds.Values.Select(a => a.Status).ToList());
        }

        private (SunPosition? Sun, string Source) ReadSun(ShadingSettingsDefinition settings, DateTimeOffset at)
        {
            var location = _environment.Location;
            SunPosition? calculated = location.HasValue ? SunGeometry.Calculate(location.Value.Latitude, location.Value.Longitude, at) : null;

            var azimuth = _reader.Read(settings.SunAzimuth, at, _ => calculated.HasValue ? new SignalReading(calculated.Value.Azimuth, "calculated") : null);
            var elevation = _reader.Read(settings.SunElevation, at, _ => calculated.HasValue ? new SignalReading(calculated.Value.Elevation, "calculated") : null);

            if (azimuth == null || elevation == null)
            {
                return (null, null);
            }

            var source = azimuth.Source == elevation.Source ? azimuth.Source : $"{azimuth.Source},{elevation.Source}";
            return (new SunPosition(SunGeometry.Normalize360(azimuth.Value), elevation.Value), source);
        }

        /// <summary>
        /// Facade brightness, then the global one. Calculated, irradiance and PV sources estimate the brightness on this
        /// facade from the sky state they measure or assume.
        /// </summary>
        private SignalReading ReadBrightness(FacadeConfig facade, double? azimuth, SunPosition? sun, ShadingSettingsDefinition settings, DateTimeOffset at)
        {
            SignalReading Calculate(CalculatedSource source)
            {
                if (!sun.HasValue)
                {
                    return null;
                }

                var cloudCover = _reader.Read(source.CloudCover, at);
                var lux = SunGeometry.EstimateIlluminance(sun.Value, azimuth, cloudCover?.Value, at);
                return new SignalReading(lux, cloudCover == null ? "calculated" : $"calculated+clouds:{cloudCover.Source}");
            }

            SignalReading Convert(SignalSource source, double value)
            {
                if (!sun.HasValue)
                {
                    return null;
                }

                switch (source)
                {
                    case IrradianceSource irradiance:
                    {
                        var sky = SunGeometry.FromGlobalHorizontal(value, sun.Value.Elevation, at);
                        return new SignalReading(SunGeometry.Illuminance(sky, sun.Value, azimuth), $"irradiance:{irradiance.Node}");
                    }
                    case PvSource pv:
                    {
                        var plane = SunGeometry.PlaneIrradianceFromPower(value, pv.PeakPower, pv.PerformanceRatio);
                        var sky = SunGeometry.FromPlaneOfArray(plane, sun.Value, pv.Azimuth, pv.Tilt);
                        return sky.HasValue ? new SignalReading(SunGeometry.Illuminance(sky.Value, sun.Value, azimuth), $"pv:{pv.Node}") : null;
                    }
                    default:
                        return null;
                }
            }

            return _reader.Read(facade?.Definition.Brightness, at, Calculate, Convert) ?? _reader.Read(settings.Brightness, at, Calculate, Convert);
        }

        /// <summary>
        /// Winter mode by month or by outside temperature (with 1 °C hysteresis, no temperature keeps the last state)
        /// </summary>
        private bool UpdateWinter(WinterDefinition winter, double? temperature, DateTimeOffset now)
        {
            switch (winter?.Mode ?? WinterMode.Off)
            {
                case WinterMode.Months:
                    return IsWinterMonth(winter, now);
                case WinterMode.Temperature:
                    if (temperature.HasValue)
                    {
                        if (temperature.Value < winter.Below)
                        {
                            _winterByTemperature = true;
                        }
                        else if (temperature.Value >= winter.Below + 1)
                        {
                            _winterByTemperature = false;
                        }
                    }
                    return _winterByTemperature;
                default:
                    return false;
            }
        }

        internal static bool IsWinterMonth(WinterDefinition winter, DateTimeOffset now)
        {
            var month = now.Month;
            return winter.FromMonth <= winter.ToMonth
                ? month >= winter.FromMonth && month <= winter.ToMonth
                : month >= winter.FromMonth || month <= winter.ToMonth;
        }

        private static SignalInfo ToInfo(SignalReading reading) => reading == null ? null : new SignalInfo(reading.Value, reading.Source);

        private ShadingStatus EmptyStatus(DateTimeOffset now) =>
            new(false, _environment.Location.HasValue, now, null, null, null, false, null, false, null, false, false,
                Array.Empty<FacadeStatus>(), Array.Empty<BlindStatus>());

        // Built lazily: the location comes from the settings database, which a constructor must not touch.
        public ShadingStatus GetStatus() => _status ?? EmptyStatus(_environment.Time.GetUtcNow());

        public async Task<bool> SetAutomaticAsync(Guid controlId, bool enabled, CancellationToken token = default)
        {
            if (!TryGetBlind(controlId, out var blind))
            {
                return false;
            }

            blind.SetPaused(!enabled);
            try
            {
                await _store.SetPausedAsync(controlId, !enabled, token).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                // the pause still holds until the next restart
                _logger.LogError(e, $"Shading: could not save the pause of blind {controlId}");
            }

            await EvaluateAsync(token).ConfigureAwait(false);
            return true;
        }

        public async Task<bool> ResumeAsync(Guid controlId, CancellationToken token = default)
        {
            if (!TryGetBlind(controlId, out var blind))
            {
                return false;
            }

            blind.EndManualOverride();
            await EvaluateAsync(token).ConfigureAwait(false);
            return true;
        }

        public bool IsShading(Guid controlId) => TryGetBlind(controlId, out var blind) && blind.IsShading;

        public bool IsConfigured(Guid controlId) => _blinds.ContainsKey(controlId);

        private bool TryGetBlind(Guid controlId, out BlindRuntime blind) => _blinds.TryGetValue(controlId, out blind);

        public ShadingPreview Preview(DateTimeOffset at)
        {
            _gate.Wait();
            try
            {
                var settings = _config.Settings;
                var (sun, _) = ReadSun(settings, at);
                var temperature = _reader.Read(settings.OutsideTemperature, _environment.Time.GetUtcNow());
                var winter = settings.Winter?.Mode switch
                {
                    WinterMode.Months => IsWinterMonth(settings.Winter, at),
                    WinterMode.Temperature => _winterByTemperature,
                    _ => false
                };

                var facades = new List<FacadeStatus>();
                foreach (var facade in _config.Facades)
                {
                    var brightness = ReadBrightness(facade, facade.Azimuth, sun, settings, at);
                    var onFacade = sun.HasValue && SunGeometry.IsSunOnWindow(sun.Value, facade.Azimuth, facade.Definition.AcceptanceAngle, facade.Definition.MinElevation, facade.Definition.MaxElevation);
                    var temperatureOk = !facade.Definition.MinOutsideTemperature.HasValue || temperature == null || temperature.Value >= facade.Definition.MinOutsideTemperature.Value;
                    facades.Add(new FacadeStatus(facade.Id, facade.Name, facade.Azimuth, facade.IsEnabled,
                        SunHysteresis.Instant(brightness?.Value, facade.Definition.Thresholds ?? settings.Thresholds), onFacade, ToInfo(brightness), temperatureOk));
                }

                var blinds = new List<BlindPreview>();
                foreach (var blind in _blinds.Values)
                {
                    var facade = blind.Facade;
                    var definition = facade?.Definition ?? new FacadeDefinition();
                    var brightness = ReadBrightness(facade, facade?.Azimuth ?? blind.Azimuth, sun, settings, at);
                    var sunny = SunHysteresis.Instant(brightness?.Value, definition.Thresholds ?? settings.Thresholds);
                    var temperatureOk = !definition.MinOutsideTemperature.HasValue || temperature == null || temperature.Value >= definition.MinOutsideTemperature.Value;
                    var onWindow = blind.IsSunOnWindow(sun);
                    var roomTemperature = _reader.Read(blind.Config.Definition.RoomTemperature, _environment.Time.GetUtcNow())?.Value;
                    var shade = settings.IsEnabled && blind.Config.IsEnabled && onWindow && sunny && temperatureOk &&
                                !BlindRuntime.WinterGainActive(blind.Config.Definition, winter, roomTemperature);

                    blinds.Add(new BlindPreview(blind.Config.ControlId, blind.Status.Name, onWindow, shade,
                        shade ? blind.Config.Definition.ShadePosition : null,
                        shade ? blind.ShadeSlat(sun) : null));
                }

                return new ShadingPreview(at, sun, winter, facades, blinds);
            }
            finally
            {
                _gate.Release();
            }
        }

        public void Dispose()
        {
            _timer?.Dispose();
            Unload();
            _gate.Dispose();
        }
    }
}
