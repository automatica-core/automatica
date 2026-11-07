using Automatica.Core.Control.Base;
using Automatica.Core.Shading.Definition;
using Microsoft.Extensions.Logging;

namespace Automatica.Core.Shading.Engine
{
    /// <summary>
    /// Sun and weather state of one evaluation
    /// </summary>
    internal sealed record EvaluationContext(
        DateTimeOffset Now,
        ShadingSettingsDefinition Settings,
        SunPosition? Sun,
        double? Wind,
        bool RainAlarm,
        bool Frost,
        bool Sunny,
        bool TemperatureOk,
        bool? DoorOpen,
        bool Winter = false,
        double? RoomTemperature = null);

    internal sealed record BlindMove(int? Position, int? Slat);

    /// <summary>
    /// The automatic of one blind: decides what to do, moves it and notices manual moves.
    /// Priorities: off → frost → wind/rain → paused → manual → door open → shading → idle.
    /// </summary>
    internal sealed class BlindRuntime : IDisposable
    {
        internal const int PositionTolerance = 3;
        internal const int SlatTolerance = 5;
        internal static readonly TimeSpan StartupGrace = TimeSpan.FromMinutes(2);

        private readonly IShadingEnvironment _environment;
        private readonly ILogger _logger;
        private readonly object _lock = new();

        private IBlind _blind;
        private Guid? _callbackId;

        private bool _paused;
        private bool _shadedByUs;
        private int? _preShadePosition;
        private int? _preShadeSlat;
        private bool _retracted;

        private (int Position, int? Slat, DateTimeOffset At)? _lastCommand;
        private DateTimeOffset? _lastMoveAt;

        private DateTimeOffset? _manualUntil;
        private bool _manualUntilChange;
        private bool _manualDemand;

        private int? _lastSeenPosition;
        private int? _lastSeenSlat;
        private bool _lastSeenMoving;
        private readonly DateTimeOffset _ignoreChangesUntil;

        private DateTimeOffset? _windUntil;
        private bool _lastDemand;

        public BlindRuntime(BlindConfig config, FacadeConfig facade, IShadingEnvironment environment, ILogger logger)
        {
            Config = config;
            Facade = facade;
            _environment = environment;
            _logger = logger;
            _ignoreChangesUntil = environment.Time.GetUtcNow() + StartupGrace;
            _paused = config.IsPaused;
            Status = CreateStatus(BlindMode.Unavailable, "Not evaluated yet", false, false, null);
        }

        public BlindConfig Config { get; private set; }
        public FacadeConfig Facade { get; private set; }
        public BlindStatus Status { get; private set; }

        /// <summary>
        /// Key of the sun state used for this blind: the facade, or the blind itself when it has no facade
        /// </summary>
        public Guid SunKey => Facade?.Id ?? Config.ControlId;

        public double? Azimuth => Config.Definition.Azimuth ?? Facade?.Azimuth;

        public bool IsShading => Status.Mode == BlindMode.Shading && _shadedByUs;

        private BlindDefinition Definition => Config.Definition;
        private FacadeDefinition FacadeDefinition => Facade?.Definition ?? new FacadeDefinition();
        private ISlatBlind SlatBlind => _blind is ISlatBlind { HasSlats: true } slatBlind ? slatBlind : null;

        public void Update(BlindConfig config, FacadeConfig facade)
        {
            lock (_lock)
            {
                Config = config;
                Facade = facade;
                _paused = config.IsPaused;
            }
        }

        public void SetPaused(bool paused)
        {
            lock (_lock)
            {
                _paused = paused;
            }
        }

        public void EndManualOverride()
        {
            lock (_lock)
            {
                EndManual();
            }
        }

        public bool IsSunOnWindow(SunPosition? sun)
        {
            var azimuth = Azimuth;
            if (!azimuth.HasValue || !sun.HasValue)
            {
                return false;
            }

            return SunGeometry.IsSunOnWindow(sun.Value, azimuth.Value,
                Definition.AcceptanceAngle ?? FacadeDefinition.AcceptanceAngle,
                Definition.MinElevation ?? FacadeDefinition.MinElevation,
                Definition.MaxElevation ?? FacadeDefinition.MaxElevation);
        }

        /// <summary>
        /// Slat position (percent) while shading at the sun position, null for roller blinds
        /// </summary>
        public int? ShadeSlat(SunPosition? sun)
        {
            if (Definition.Type != BlindType.Venetian)
            {
                return null;
            }

            var slats = Definition.Slats;
            double angle;
            if (!slats.TrackSun || !sun.HasValue || !Azimuth.HasValue)
            {
                angle = slats.FixedAngle;
            }
            else
            {
                var profile = SunGeometry.ProfileAngle(sun.Value, Azimuth.Value);
                angle = profile.HasValue
                    ? SunGeometry.CutOffSlatAngle(profile.Value, slats.Width, slats.Spacing) + slats.ExtraAngle
                    : slats.MaxAngle;
            }

            var minAngle = Math.Min(slats.MinAngle, slats.MaxAngle);
            var maxAngle = Math.Max(slats.MinAngle, slats.MaxAngle);
            angle = Math.Clamp(angle, minAngle, maxAngle);

            var span = slats.MaxAngle - slats.MinAngle;
            var ratio = Math.Abs(span) < 0.001 ? 1 : (angle - slats.MinAngle) / span;
            var percent = slats.PercentAtMinAngle + ratio * (slats.PercentAtMaxAngle - slats.PercentAtMinAngle);
            return (int)Math.Round(Math.Clamp(percent, 0, 100));
        }

        private bool EnsureBlind()
        {
            if (_blind != null)
            {
                return true;
            }

            _blind = _environment.GetBlind(Config.ControlId);
            if (_blind == null)
            {
                return false;
            }

            _lastSeenPosition = _blind.Position;
            _lastSeenSlat = SlatBlind?.SlatPosition;
            _lastSeenMoving = _blind.IsMoving;
            if (_blind is IControlValueCallback callback)
            {
                _callbackId = callback.RegisterValueCallback(OnBlindChanged);
            }

            return true;
        }

        /// <summary>
        /// The blind reported a change (position feedback, up/down from the visualization or a wall switch)
        /// </summary>
        internal void OnBlindChanged()
        {
            var now = _environment.Time.GetUtcNow();
            lock (_lock)
            {
                if (_blind == null)
                {
                    return;
                }

                var position = _blind.Position;
                var slat = SlatBlind?.SlatPosition;
                var moving = _blind.IsMoving;

                var startedMoving = moving && !_lastSeenMoving;
                var positionChanged = _lastSeenPosition.HasValue && Math.Abs(position - _lastSeenPosition.Value) >= PositionTolerance;
                var slatChanged = slat.HasValue && _lastSeenSlat.HasValue && Math.Abs(slat.Value - _lastSeenSlat.Value) >= SlatTolerance;

                _lastSeenPosition = position;
                _lastSeenSlat = slat;
                _lastSeenMoving = moving;

                if (now < _ignoreChangesUntil)
                {
                    return;
                }

                if (_lastCommand.HasValue && now - _lastCommand.Value.At <= TimeSpan.FromSeconds(Math.Max(5, Definition.TravelSeconds)))
                {
                    // the blind is still moving to where the automatic sent it
                    return;
                }

                var nearCommand = _lastCommand.HasValue &&
                                  Math.Abs(position - _lastCommand.Value.Position) < PositionTolerance &&
                                  (!_lastCommand.Value.Slat.HasValue || !slat.HasValue || Math.Abs(slat.Value - _lastCommand.Value.Slat.Value) < SlatTolerance);

                if (startedMoving || ((positionChanged || slatChanged) && !nearCommand))
                {
                    StartManual(now);
                }
            }
        }

        private void StartManual(DateTimeOffset now)
        {
            var settings = CurrentSettings ?? new ShadingSettingsDefinition();
            _manualUntilChange = false;
            _manualUntil = null;

            switch (settings.ManualOverride)
            {
                case ManualOverrideMode.Duration:
                    _manualUntil = now + TimeSpan.FromMinutes(Math.Max(1, settings.ManualOverrideMinutes));
                    break;
                case ManualOverrideMode.UntilEndOfDay:
                    var local = TimeZoneInfo.ConvertTime(now, _environment.Time.LocalTimeZone);
                    var midnight = local.Date.AddDays(1);
                    _manualUntil = new DateTimeOffset(midnight, _environment.Time.LocalTimeZone.GetUtcOffset(midnight));
                    break;
                default:
                    _manualUntilChange = true;
                    _manualDemand = _lastDemand;
                    break;
            }

            // the user took over: when the sun goes, the automatic does not restore anything
            _shadedByUs = false;
            _preShadePosition = null;
            _preShadeSlat = null;
            _logger.LogInformation($"Shading: {Name} was moved by hand, automatic paused");
        }

        private void EndManual()
        {
            _manualUntil = null;
            _manualUntilChange = false;
        }

        private bool ManualActive => _manualUntilChange || _manualUntil.HasValue;

        /// <summary>
        /// The settings of the latest evaluation (used when a manual move is noticed between evaluations)
        /// </summary>
        private ShadingSettingsDefinition CurrentSettings { get; set; }

        private string Name => _blind?.Name ?? Config.ControlId.ToString();

        public async Task EvaluateAsync(EvaluationContext context, CancellationToken token)
        {
            BlindMove move;
            lock (_lock)
            {
                CurrentSettings = context.Settings;
                move = Decide(context);
            }

            if (move != null)
            {
                await ExecuteAsync(move, context.Now, token).ConfigureAwait(false);
            }
        }

        private BlindMove Decide(EvaluationContext context)
        {
            var now = context.Now;
            var settings = context.Settings;
            var definition = Definition;

            if (!EnsureBlind())
            {
                Status = CreateStatus(BlindMode.Unavailable, "Blind control not loaded", false, false, null);
                return null;
            }

            if (!Azimuth.HasValue)
            {
                Status = CreateStatus(BlindMode.Unavailable, "No facade or direction", false, false, null);
                return null;
            }

            var sunOnWindow = IsSunOnWindow(context.Sun);
            var winterGain = WinterGainActive(definition, context.Winter, context.RoomTemperature);
            var demand = sunOnWindow && context.Sunny && context.TemperatureOk && !winterGain;

            // manual override ends
            if (_manualUntilChange && demand != _manualDemand)
            {
                EndManual();
            }
            if (_manualUntil.HasValue && now >= _manualUntil.Value)
            {
                EndManual();
            }
            _lastDemand = demand;

            var position = _blind.Position;
            var slat = SlatBlind?.SlatPosition;

            if (!settings.IsEnabled || !Config.IsEnabled || Facade is { IsEnabled: false })
            {
                Status = CreateStatus(BlindMode.Off, "Automatic shading is off", sunOnWindow, demand, null);
                return null;
            }

            if (context.Frost)
            {
                Status = CreateStatus(BlindMode.Frost, "Outside temperature below the frost limit", sunOnWindow, demand, null);
                return null;
            }

            // safety
            var windProtection = definition.WindProtection ?? definition.Type == BlindType.Venetian;
            var windLimit = definition.WindLimit ?? settings.WindLimit;
            if (windProtection && windLimit > 0 && context.Wind.HasValue && context.Wind.Value >= windLimit)
            {
                _windUntil = now + TimeSpan.FromMinutes(Math.Max(0, settings.WindReleaseMinutes));
            }

            var windAlarm = windProtection && _windUntil.HasValue && now < _windUntil.Value;
            var rainAlarm = definition.RainProtection && context.RainAlarm;
            if (windAlarm || rainAlarm)
            {
                var mode = windAlarm ? BlindMode.Wind : BlindMode.Rain;
                var reason = windAlarm ? $"Wind alarm ({context.Wind?.ToString("0.#") ?? "-"} m/s)" : "Rain alarm";

                _shadedByUs = false;
                _preShadePosition = null;
                _preShadeSlat = null;

                // retract, and again when someone lowers it during the alarm
                var retract = !_retracted || (position > PositionTolerance && MoveIntervalElapsed(now, settings) && !CommandInProgress(now));
                _retracted = true;
                Status = CreateStatus(mode, reason, sunOnWindow, demand, new BlindMove(0, null));
                return retract ? new BlindMove(0, null) : null;
            }
            _retracted = false;

            if (_paused)
            {
                Status = CreateStatus(BlindMode.Paused, "Paused", sunOnWindow, demand, null);
                return null;
            }

            if (ManualActive)
            {
                var reason = _manualUntil.HasValue ? "Moved by hand, automatic again at the end of the pause" : "Moved by hand, automatic again when the sun comes or goes";
                Status = CreateStatus(BlindMode.Manual, reason, sunOnWindow, demand, null);
                return null;
            }

            if (demand)
            {
                var targetSlat = SlatBlind != null ? ShadeSlat(context.Sun) : null;
                if (!_shadedByUs)
                {
                    if (context.DoorOpen == true)
                    {
                        Status = CreateStatus(BlindMode.DoorOpen, "Door is open, not lowered", sunOnWindow, demand, null);
                        return null;
                    }

                    if (position >= definition.ShadePosition - 1)
                    {
                        Status = CreateStatus(BlindMode.Shading, "Already closed", sunOnWindow, demand, null);
                        return null;
                    }

                    _preShadePosition = position;
                    _preShadeSlat = slat;
                    _shadedByUs = true;
                    var enter = new BlindMove(definition.ShadePosition, targetSlat);
                    Status = CreateStatus(BlindMode.Shading, "Direct sun", sunOnWindow, demand, enter);
                    return enter;
                }

                // follow the sun with the slats
                BlindMove track = null;
                if (targetSlat.HasValue && _lastCommand?.Slat is { } lastSlat &&
                    Math.Abs(targetSlat.Value - lastSlat) >= Math.Max(1, settings.MinSlatChange) &&
                    MoveIntervalElapsed(now, settings))
                {
                    track = new BlindMove(null, targetSlat);
                }

                Status = CreateStatus(BlindMode.Shading, "Direct sun", sunOnWindow, demand, new BlindMove(definition.ShadePosition, targetSlat ?? _lastCommand?.Slat));
                return track;
            }

            if (_shadedByUs)
            {
                var target = definition.RestorePosition && _preShadePosition.HasValue ? _preShadePosition.Value : definition.OpenPosition;
                int? targetSlat = null;
                if (SlatBlind != null)
                {
                    targetSlat = definition.RestorePosition && _preShadeSlat.HasValue ? _preShadeSlat.Value : definition.Slats.OpenPercent;
                }

                if (context.DoorOpen == true && target > position)
                {
                    // never lower while the door is open
                    target = position;
                }

                _shadedByUs = false;
                _preShadePosition = null;
                _preShadeSlat = null;

                var leave = new BlindMove(target, targetSlat);
                Status = winterGain && sunOnWindow && context.Sunny
                    ? CreateStatus(BlindMode.Winter, WinterReason, sunOnWindow, demand, leave)
                    : CreateStatus(BlindMode.Idle, "Sun has gone", sunOnWindow, demand, leave);
                return leave;
            }

            if (winterGain && sunOnWindow && context.Sunny)
            {
                Status = CreateStatus(BlindMode.Winter, WinterReason, sunOnWindow, demand, null);
                return null;
            }

            Status = CreateStatus(BlindMode.Idle, sunOnWindow ? "Not bright enough" : "No sun on the window", sunOnWindow, demand, null);
            return null;
        }

        private const string WinterReason = "Winter: the sun warms the room";

        /// <summary>
        /// In winter mode the blind lets the sun in, unless it opted out or its room is already too warm
        /// </summary>
        internal static bool WinterGainActive(BlindDefinition definition, bool winter, double? roomTemperature)
        {
            if (!winter || !definition.WinterGain)
            {
                return false;
            }

            return !(definition.MaxRoomTemperature.HasValue && roomTemperature.HasValue && roomTemperature.Value > definition.MaxRoomTemperature.Value);
        }

        private bool MoveIntervalElapsed(DateTimeOffset now, ShadingSettingsDefinition settings)
        {
            return !_lastMoveAt.HasValue || now - _lastMoveAt.Value >= TimeSpan.FromMinutes(Math.Max(0, settings.MinMoveIntervalMinutes));
        }

        private bool CommandInProgress(DateTimeOffset now)
        {
            return _lastCommand.HasValue && now - _lastCommand.Value.At <= TimeSpan.FromSeconds(Math.Max(5, Definition.TravelSeconds));
        }

        private async Task ExecuteAsync(BlindMove move, DateTimeOffset now, CancellationToken token)
        {
            var blind = _blind;
            if (blind == null)
            {
                return;
            }

            lock (_lock)
            {
                var position = move.Position ?? _lastCommand?.Position ?? blind.Position;
                var slat = move.Slat ?? _lastCommand?.Slat;
                _lastCommand = (position, slat, now);
                _lastMoveAt = now;
            }

            try
            {
                if (move.Position.HasValue)
                {
                    _logger.LogInformation($"Shading: {blind.Name} to {move.Position}% ({Status.Reason})");
                    await blind.MoveAbsoluteAsync(move.Position.Value, token).ConfigureAwait(false);
                }

                if (move.Slat.HasValue && blind is ISlatBlind { HasSlats: true } slatBlind)
                {
                    _logger.LogInformation($"Shading: {blind.Name} slats to {move.Slat}%");
                    await slatBlind.MoveSlatAsync(move.Slat.Value, token).ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Shading: could not move {blind.Name}");
            }
        }

        private BlindStatus CreateStatus(BlindMode mode, string reason, bool sunOnWindow, bool demand, BlindMove target)
        {
            return new BlindStatus(
                Config.ControlId,
                _blind?.Name ?? Config.ControlId.ToString(),
                Config.FacadeId,
                Config.Definition.Type,
                mode,
                reason,
                sunOnWindow,
                demand,
                _blind?.Position ?? 0,
                SlatBlind?.SlatPosition,
                target?.Position,
                target?.Slat,
                _manualUntil,
                _lastMoveAt);
        }

        public void Dispose()
        {
            lock (_lock)
            {
                if (_callbackId.HasValue && _blind is IControlValueCallback callback)
                {
                    callback.UnregisterValueCallback(_callbackId.Value);
                }

                _callbackId = null;
                _blind = null;
            }
        }
    }
}
