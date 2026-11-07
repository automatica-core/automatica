using Automatica.Core.Control.Base;
using Automatica.Core.Shading.Definition;

namespace Automatica.Core.Shading.Engine
{
    /// <summary>
    /// A datapoint value and when it was updated (null when unknown)
    /// </summary>
    public sealed record ShadingValue(object Value, DateTimeOffset? Timestamp);

    /// <summary>
    /// Everything the shading engine needs from the running core. Implemented by <see cref="CoreShadingEnvironment"/>,
    /// replaced by a fake in the tests.
    /// </summary>
    public interface IShadingEnvironment
    {
        TimeProvider Time { get; }

        /// <summary>
        /// Latitude/longitude of the server settings, null when not configured
        /// </summary>
        (double Latitude, double Longitude)? Location { get; }

        /// <summary>
        /// Current value of a datapoint, null when unknown
        /// </summary>
        ShadingValue GetValue(Guid id);

        /// <summary>
        /// Values read from the device (or logic output). Disposing the result unsubscribes.
        /// </summary>
        Task<IDisposable> SubscribeAsync(Guid id, Func<object, Task> callback);

        IBlind GetBlind(Guid controlId);
    }

    public sealed record FacadeConfig(Guid Id, string Name, double Azimuth, bool IsEnabled, FacadeDefinition Definition);

    public sealed record BlindConfig(Guid ControlId, Guid? FacadeId, bool IsEnabled, BlindDefinition Definition, bool IsPaused = false);

    public sealed class ShadingConfiguration
    {
        public ShadingSettingsDefinition Settings { get; init; } = new();
        public IReadOnlyList<FacadeConfig> Facades { get; init; } = Array.Empty<FacadeConfig>();
        public IReadOnlyList<BlindConfig> Blinds { get; init; } = Array.Empty<BlindConfig>();
    }

    public interface IShadingStore
    {
        Task<ShadingConfiguration> LoadAsync(CancellationToken token = default);

        /// <summary>
        /// Remembers that the automatic of a blind is paused
        /// </summary>
        Task SetPausedAsync(Guid controlId, bool paused, CancellationToken token = default);
    }

    public enum BlindMode
    {
        /// <summary>Automatic shading is switched off (globally or for the blind)</summary>
        Off,
        /// <summary>Paused by an automation or the user</summary>
        Paused,
        /// <summary>Blind control not loaded or not a blind</summary>
        Unavailable,
        /// <summary>Wind alarm: retracted</summary>
        Wind,
        /// <summary>Rain alarm: retracted</summary>
        Rain,
        /// <summary>Too cold: not moved</summary>
        Frost,
        /// <summary>Moved by hand: the automatic waits</summary>
        Manual,
        /// <summary>Door behind the blind is open: not lowered</summary>
        DoorOpen,
        /// <summary>Direct sun on the window: shading</summary>
        Shading,
        /// <summary>No direct sun on the window</summary>
        Idle,
        /// <summary>Winter mode: the sun is let in to warm the room</summary>
        Winter
    }

    public sealed record SignalInfo(double? Value, string Source);

    public sealed record BlindStatus(
        Guid ControlId,
        string Name,
        Guid? FacadeId,
        BlindType Type,
        BlindMode Mode,
        string Reason,
        bool SunOnWindow,
        bool Demand,
        int Position,
        int? Slat,
        int? TargetPosition,
        int? TargetSlat,
        DateTimeOffset? ManualUntil,
        DateTimeOffset? LastMoveAt);

    public sealed record FacadeStatus(Guid Id, string Name, double Azimuth, bool IsEnabled, bool Sunny, bool SunOnFacade, SignalInfo Brightness, bool TemperatureOk);

    public sealed record ShadingStatus(
        bool IsEnabled,
        bool HasLocation,
        DateTimeOffset EvaluatedAt,
        SunPosition? Sun,
        string SunSource,
        SignalInfo Wind,
        bool WindAlarm,
        SignalInfo Rain,
        bool RainAlarm,
        SignalInfo OutsideTemperature,
        bool Frost,
        bool Winter,
        IReadOnlyList<FacadeStatus> Facades,
        IReadOnlyList<BlindStatus> Blinds);

    /// <summary>
    /// What a blind would do at a given time (simulation without delays and manual overrides)
    /// </summary>
    public sealed record BlindPreview(Guid ControlId, string Name, bool SunOnWindow, bool Shade, int? TargetPosition, int? TargetSlat);

    public sealed record ShadingPreview(DateTimeOffset At, SunPosition? Sun, bool Winter, IReadOnlyList<FacadeStatus> Facades, IReadOnlyList<BlindPreview> Blinds);

    public interface IShadingEngine
    {
        Task StartAsync(CancellationToken token = default);
        Task StopAsync();

        /// <summary>
        /// Reads the configuration again. Runtime state (manual overrides, shaded blinds) of unchanged blinds is kept.
        /// </summary>
        Task ReloadAsync(CancellationToken token = default);

        /// <summary>
        /// Evaluates all blinds now
        /// </summary>
        Task EvaluateAsync(CancellationToken token = default);

        ShadingStatus GetStatus();

        /// <summary>
        /// Pauses (false) or resumes (true) the automatic of a blind until the server restarts, e.g. from an automation
        /// </summary>
        /// <returns>False when the blind is not configured for shading</returns>
        Task<bool> SetAutomaticAsync(Guid controlId, bool enabled, CancellationToken token = default);

        /// <summary>
        /// Ends a manual override, the automatic takes over again
        /// </summary>
        Task<bool> ResumeAsync(Guid controlId, CancellationToken token = default);

        bool IsShading(Guid controlId);

        /// <summary>
        /// The blind has shading settings
        /// </summary>
        bool IsConfigured(Guid controlId);

        /// <summary>
        /// What the blinds would do at <paramref name="at"/> with the calculated sun position and the current sensor values
        /// </summary>
        ShadingPreview Preview(DateTimeOffset at);
    }
}
