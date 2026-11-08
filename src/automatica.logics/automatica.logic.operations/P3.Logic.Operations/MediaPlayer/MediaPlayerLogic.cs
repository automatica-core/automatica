using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.Control.Base;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using P3.Logic.Operations.Garage;
using F = P3.Logic.Operations.MediaPlayer.MediaPlayerLogicFactory;

namespace P3.Logic.Operations.MediaPlayer
{
    /// <summary>
    /// A media player of any driver as <see cref="IMediaPlayer"/>. The inputs take the state of the device (playing, volume,
    /// mute, title, artist) and never send anything, so the state the device reports cannot loop back into a command.
    /// The control commands (from Matter, HomeKit, the visualization) go out on the outputs: play (true) / pause (false),
    /// stop, next and previous as a pulse (true), volume in percent and mute. Without feedback inputs the state follows the
    /// commands.
    /// </summary>
    public class MediaPlayerLogic : DispatchingLogic, IMediaPlayer
    {
        private MediaPlaybackState _state = MediaPlaybackState.Unknown;
        private int _volume;
        private bool _muted;
        private string _title;
        private string _artist;
        private MediaPlayerBridgeType _bridgeType = MediaPlayerBridgeType.OnOffVolume;

        public MediaPlayerLogic(ILogicContext context) : base(context)
        {
        }

        public Guid Id => Context.RuleInstance.ObjId;
        public string Name => Context.RuleInstance.Name;

        public MediaPlaybackState PlaybackState
        {
            get { lock (Sync) { return _state; } }
        }

        public int Volume
        {
            get { lock (Sync) { return _volume; } }
        }

        public bool IsMuted
        {
            get { lock (Sync) { return _muted; } }
        }

        public string Title
        {
            get { lock (Sync) { return _title; } }
        }

        public string Artist
        {
            get { lock (Sync) { return _artist; } }
        }

        public MediaPlayerBridgeType BridgeType
        {
            get { lock (Sync) { return _bridgeType; } }
        }

        protected override void ParameterValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            if (TemplateOf(instance) == F.ParamBridgeType)
            {
                lock (Sync)
                {
                    _bridgeType = LogicValue.ToInt(value, 0) == (int)MediaPlayerBridgeType.Speaker
                        ? MediaPlayerBridgeType.Speaker
                        : MediaPlayerBridgeType.OnOffVolume;
                }
            }
        }

        protected override IList<ILogicOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            lock (Sync)
            {
                var template = TemplateOf(instance);
                if (template == F.InputPlaying)
                {
                    _state = LogicValue.IsTruthy(value) ? MediaPlaybackState.Playing : MediaPlaybackState.Paused;
                }
                else if (template == F.InputVolume)
                {
                    _volume = ClampVolume(LogicValue.ToInt(value, _volume));
                }
                else if (template == F.InputMute)
                {
                    _muted = LogicValue.IsTruthy(value);
                }
                else if (template == F.InputTitle)
                {
                    _title = EmptyToNull(value);
                }
                else if (template == F.InputArtist)
                {
                    _artist = EmptyToNull(value);
                }
            }

            // the base class notifies the value callbacks (bridges) after this
            return new List<ILogicOutputChanged>();
        }

        public Task PlayAsync(CancellationToken token = default)
        {
            lock (Sync)
            {
                _state = MediaPlaybackState.Playing;
                SendAlways(F.OutputPlay, true);
            }
            return Task.CompletedTask;
        }

        public Task PauseAsync(CancellationToken token = default)
        {
            lock (Sync)
            {
                _state = MediaPlaybackState.Paused;
                SendAlways(F.OutputPlay, false);
            }
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken token = default)
        {
            lock (Sync)
            {
                _state = MediaPlaybackState.Stopped;
                SendAlways(F.OutputStop, true);
            }
            return Task.CompletedTask;
        }

        public Task NextAsync(CancellationToken token = default)
        {
            lock (Sync)
            {
                SendAlways(F.OutputNext, true);
            }
            return Task.CompletedTask;
        }

        public Task PreviousAsync(CancellationToken token = default)
        {
            lock (Sync)
            {
                SendAlways(F.OutputPrevious, true);
            }
            return Task.CompletedTask;
        }

        public Task SetVolumeAsync(int volume, CancellationToken token = default)
        {
            lock (Sync)
            {
                _volume = ClampVolume(volume);
                SendAlways(F.OutputVolume, _volume);
            }
            return Task.CompletedTask;
        }

        public Task SetMuteAsync(bool muted, CancellationToken token = default)
        {
            lock (Sync)
            {
                _muted = muted;
                SendAlways(F.OutputMute, muted);
            }
            return Task.CompletedTask;
        }

        public override object GetDataForVisu() => SentValuesForVisu();

        private static int ClampVolume(int volume) => Math.Clamp(volume, 0, 100);

        private static string EmptyToNull(object value)
        {
            var text = LogicValue.ToText(value).Trim();
            return text.Length == 0 ? null : text;
        }
    }
}
