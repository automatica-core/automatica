using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using F = P3.Logic.Sonos.AudioCentral.AudioCentralLogicFactory;

namespace P3.Logic.Sonos.AudioCentral
{
    /// <summary>
    /// Central for several Sonos Control blocks. The outputs are linked to the inputs of every player block, the play and
    /// volume outputs of the players come back as feedback: they give the number of active players and the ratio that a
    /// new volume keeps (the loudest players get the new value, the others are scaled with them).
    /// </summary>
    public class AudioCentralLogic : Automatica.Core.Logic.Logic
    {
        private readonly object _sync = new();
        private readonly Dictionary<Guid, RuleInterfaceInstance> _byTemplate;
        private readonly bool?[] _playing = new bool?[F.InputPlayings.Length];
        private readonly double?[] _volumes = new double?[F.InputPlayerVolumes.Length];
        private string[] _favorites = Array.Empty<string>();
        private int _favorite = -1;
        private bool _locked;
        private bool _disablePeriphery;
        private int? _lastActive;
        internal string BellUrl = "";
        internal string AlarmUrl = "";

        public AudioCentralLogic(ILogicContext context) : base(context)
        {
            _byTemplate = context.RuleInstance.RuleInterfaceInstance
                .GroupBy(a => a.This2RuleInterfaceTemplate)
                .ToDictionary(a => a.Key, a => a.First());
        }

        internal IReadOnlyList<string> Favorites => _favorites;

        protected override Task<bool> Start(RuleInstance instance, CancellationToken token = default)
        {
            lock (_sync)
            {
                SendActiveCount();
            }
            return base.Start(instance, token);
        }

        protected override void ParameterValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            lock (_sync)
            {
                var template = instance.This2RuleInterfaceTemplate;
                if (template == F.ParamFavorites)
                {
                    _favorites = Convert.ToString(value, CultureInfo.InvariantCulture)?
                        .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? Array.Empty<string>();
                    _favorite = -1;
                }
                else if (template == F.ParamBellUrl)
                {
                    BellUrl = Convert.ToString(value, CultureInfo.InvariantCulture)?.Trim() ?? "";
                }
                else if (template == F.ParamAlarmUrl)
                {
                    AlarmUrl = Convert.ToString(value, CultureInfo.InvariantCulture)?.Trim() ?? "";
                }
            }
        }

        protected override IList<ILogicOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            lock (_sync)
            {
                var template = instance.This2RuleInterfaceTemplate;
                var playingIndex = Array.IndexOf(F.InputPlayings, template);
                var volumeIndex = Array.IndexOf(F.InputPlayerVolumes, template);
                if (playingIndex >= 0)
                {
                    _playing[playingIndex] = value == null ? null : IsTruthy(value);
                    SendActiveCount();
                }
                else if (volumeIndex >= 0)
                {
                    var volume = ToDouble(value);
                    _volumes[volumeIndex] = volume.HasValue ? Math.Clamp(volume.Value, 0, 100) : null;
                }
                else if (template == F.InputLock)
                {
                    _locked = IsTruthy(value);
                    if (_locked)
                    {
                        Send(F.OutputPlay, false);
                    }
                }
                else if (template == F.InputDisablePeriphery)
                {
                    _disablePeriphery = IsTruthy(value);
                }
                else if (_locked || (_disablePeriphery && source?.Source != DispatchableSource.Visualization))
                {
                    // locked or child lock
                }
                else if (template == F.InputVolume)
                {
                    var volume = ToDouble(value);
                    if (volume.HasValue)
                    {
                        SetVolume(Math.Clamp(volume.Value, 0, 100));
                    }
                }
                else if (IsTruthy(value))
                {
                    Button(template);
                }
            }
            return new List<ILogicOutputChanged>();
        }

        private void Button(Guid template)
        {
            if (template == F.InputToggle)
            {
                Send(F.OutputPlay, !AnyPlaying);
            }
            else if (template == F.InputOn || template == F.InputPlay)
            {
                Send(F.OutputPlay, true);
            }
            else if (template == F.InputOff || template == F.InputPause)
            {
                Send(F.OutputPlay, false);
            }
            else if (template == F.InputVolumeUp)
            {
                Send(F.OutputVolumeUp, true);
            }
            else if (template == F.InputVolumeDown)
            {
                Send(F.OutputVolumeDown, true);
            }
            else if (template == F.InputNext)
            {
                Send(F.OutputNext, true);
            }
            else if (template == F.InputPrevious)
            {
                Send(F.OutputPrevious, true);
            }
            else if (template == F.InputNextFavorite)
            {
                if (_favorites.Length > 0)
                {
                    _favorite = (_favorite + 1) % _favorites.Length;
                    Send(F.OutputRadioStation, _favorites[_favorite]);
                    Send(F.OutputPlay, true);
                }
            }
            else if (template == F.InputBell)
            {
                PlaySound(BellUrl);
            }
            else if (template == F.InputAlarm)
            {
                PlaySound(AlarmUrl);
            }
        }

        private bool AnyPlaying => _playing.Any(a => a == true);

        /// <summary>
        /// The common output gets the value; the loudest players get it too and the others keep their ratio to them
        /// </summary>
        private void SetVolume(double volume)
        {
            Send(F.OutputVolume, volume);
            var loudest = _volumes.Where(a => a.HasValue).Select(a => a.Value).DefaultIfEmpty(0).Max();
            for (var i = 0; i < _volumes.Length; i++)
            {
                var player = _volumes[i];
                var value = player.HasValue && loudest > 0 ? Math.Round(player.Value * volume / loudest) : Math.Round(volume);
                Send(F.OutputPlayerVolumes[i], value);
                if (player.HasValue)
                {
                    // until the feedback arrives the next change starts from here
                    _volumes[i] = value;
                }
            }
        }

        private void PlaySound(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return;
            }
            Send(F.OutputSoundUrl, url);
            Send(F.OutputSoundTrigger, true);
        }

        private void SendActiveCount()
        {
            var active = _playing.Count(a => a == true);
            if (_lastActive == active)
            {
                return;
            }
            _lastActive = active;
            Send(F.OutputActiveCount, active);
        }

        /// <summary>
        /// Every command is sent, also the same value again: the player blocks react to every telegram
        /// </summary>
        private void Send(Guid output, object value)
        {
            if (_byTemplate.TryGetValue(output, out var instance))
            {
                Context.Dispatcher.DispatchValue(new LogicOutputChanged(instance, value).Instance, value);
            }
        }

        internal static bool IsTruthy(object value)
        {
            var d = ToDouble(value);
            return d.HasValue && d.Value != 0;
        }

        internal static double? ToDouble(object value)
        {
            try
            {
                double d = value switch
                {
                    null => double.NaN,
                    bool b => b ? 1 : 0,
                    string s when bool.TryParse(s, out var sb) => sb ? 1 : 0,
                    string s => double.Parse(s, NumberStyles.Float, CultureInfo.InvariantCulture),
                    IConvertible c => c.ToDouble(CultureInfo.InvariantCulture),
                    _ => double.NaN
                };
                return double.IsNaN(d) || double.IsInfinity(d) ? null : d;
            }
            catch (Exception)
            {
                return null;
            }
        }

    }
}
