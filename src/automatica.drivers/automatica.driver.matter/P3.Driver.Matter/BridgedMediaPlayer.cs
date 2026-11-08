using P3.Driver.Matter.DataModel;
using P3.Driver.Matter.DataModel.Clusters;

namespace P3.Driver.Matter
{
    public enum MediaPlayerDeviceKind
    {
        /// <summary>Dimmable plug-in unit: on = playing, level = volume. Every voice assistant can control it.</summary>
        OnOffVolume,

        /// <summary>Matter speaker (0x0022): on = unmuted, level = volume. Only Google Home shows it.</summary>
        Speaker
    }

    /// <summary>
    /// A media player behind the bridge. Matter has no media device type that Alexa or Apple Home know, so by default the
    /// player is a dimmable plug-in unit (a plug, so "turn off all lights" does not pause the music).
    /// </summary>
    public sealed class BridgedMediaPlayer : BridgedDevice
    {
        private readonly MediaPlayerDeviceKind _kind;
        private OnOffCluster _onOff;
        private LevelControlCluster _level;
        private bool _on;
        private int _volume = 50;

        public BridgedMediaPlayer(string uniqueId, string name, MediaPlayerDeviceKind kind = MediaPlayerDeviceKind.OnOffVolume) : base(uniqueId, name)
        {
            _kind = kind;
        }

        public MediaPlayerDeviceKind Kind => _kind;

        internal override DeviceType DeviceType => _kind == MediaPlayerDeviceKind.Speaker ? DeviceType.Speaker : DeviceType.DimmablePlugInUnit;

        protected override string ProductName => _kind == MediaPlayerDeviceKind.Speaker ? "Speaker" : "Media Player";

        /// <summary>
        /// Called when a controller switches the device: play/pause for <see cref="MediaPlayerDeviceKind.OnOffVolume"/>,
        /// unmute/mute for <see cref="MediaPlayerDeviceKind.Speaker"/>.
        /// </summary>
        public Func<bool, Task> SwitchRequested { get; set; }

        /// <summary>Called with the volume in percent (1..100).</summary>
        public Func<int, Task> VolumeRequested { get; set; }

        public bool IsOn => _onOff?.OnOff ?? _on;

        internal override void AddClusters(Endpoint endpoint)
        {
            AddCommonClusters(endpoint);
            if (_kind == MediaPlayerDeviceKind.OnOffVolume)
            {
                endpoint.Add(new GroupsCluster());
            }

            _onOff = new OnOffCluster { OnOffRequested = on => SwitchRequested?.Invoke(on) ?? Task.CompletedTask };
            _level = new LevelControlCluster(lighting: _kind == MediaPlayerDeviceKind.OnOffVolume)
            {
                LevelRequested = async (level, withOnOff) =>
                {
                    if (withOnOff && level <= LevelControlCluster.MinLevel)
                    {
                        if (SwitchRequested != null)
                        {
                            await SwitchRequested(false);
                        }

                        return;
                    }

                    if (VolumeRequested != null)
                    {
                        await VolumeRequested(Math.Max(1, LevelControlCluster.LevelToPercent(level)));
                    }

                    // "set the music to 30 %" also starts it (unmutes the speaker), like a dimmer that is off
                    if (withOnOff && !IsOn && SwitchRequested != null)
                    {
                        await SwitchRequested(true);
                    }
                }
            };
            _onOff.SetOnOff(_on);
            _level.SetLevel(LevelControlCluster.PercentToLevel(Math.Max(1, _volume)));
            endpoint.Add(_onOff);
            endpoint.Add(_level);
        }

        /// <summary>Updates the state from the player (on = playing or unmuted, volume 0..100).</summary>
        public void SetState(bool on, int volume)
        {
            _on = on;
            _volume = Math.Clamp(volume, 0, 100);
            Locked(() =>
            {
                _onOff?.SetOnOff(on);
                _level?.SetLevel(LevelControlCluster.PercentToLevel(Math.Max(1, _volume)));
            });
        }
    }
}
