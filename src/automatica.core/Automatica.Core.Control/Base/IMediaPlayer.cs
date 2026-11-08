namespace Automatica.Core.Control.Base
{
    public enum MediaPlaybackState
    {
        Unknown = -1,
        Stopped = 0,
        Playing = 1,
        Paused = 2
    }

    /// <summary>
    /// How smart home bridges show a media player. No voice assistant except Google knows a Matter media device type,
    /// so the default is a dimmable on/off device: on plays, off pauses, the level is the volume.
    /// </summary>
    public enum MediaPlayerBridgeType
    {
        /// <summary>
        /// A dimmable on/off device (Matter: dimmable plug-in unit): on = play, off = pause, level = volume. Every voice assistant can control it.
        /// </summary>
        OnOffVolume = 0,

        /// <summary>
        /// The Matter speaker device type: on/off = unmuted/muted, level = volume. Only Google Home shows it.
        /// </summary>
        Speaker = 1
    }

    /// <summary>
    /// A media player (music zone, speaker, TV audio...) that smart home bridges (Matter, HomeKit) and the visualization control
    /// without knowing the driver behind it. Bridges in plugins built against older cores find it by its full name,
    /// so do not rename or move it.
    /// </summary>
    public interface IMediaPlayer : IControl, IControlValueCallback
    {
        MediaPlaybackState PlaybackState { get; }

        /// <summary>
        /// The volume in percent (0..100)
        /// </summary>
        int Volume { get; }

        bool IsMuted { get; }

        /// <summary>
        /// The current title, null if unknown
        /// </summary>
        string? Title { get; }

        /// <summary>
        /// The current artist, null if unknown
        /// </summary>
        string? Artist { get; }

        MediaPlayerBridgeType BridgeType { get; }

        Task PlayAsync(CancellationToken token = default);
        Task PauseAsync(CancellationToken token = default);
        Task StopAsync(CancellationToken token = default);
        Task NextAsync(CancellationToken token = default);
        Task PreviousAsync(CancellationToken token = default);

        /// <param name="volume">Percent, clamped to 0..100</param>
        /// <param name="token">Cancellation</param>
        Task SetVolumeAsync(int volume, CancellationToken token = default);

        Task SetMuteAsync(bool muted, CancellationToken token = default);
    }
}
