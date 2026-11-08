using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using P3.Logic.Operations.Garage;
using F = P3.Logic.Operations.MediaController.MediaControllerLogicFactory;

namespace P3.Logic.Operations.MediaController
{
    /// <summary>
    /// Universal remote. A mode (1-8, e.g. TV or radio) switches its "mode active" output on, the devices of the mode are
    /// linked to it. The command outputs (volume, channel, navigation, playback) send 1 per press while a mode is on;
    /// digits of the number pad are collected and sent as one channel number after the timeout.
    /// Every command (and power on/off of a mode) also goes out on the command output as "mode:command[:value]",
    /// a "Media Controller Mode" block per device passes on only the commands of its mode.
    /// </summary>
    public class MediaControllerLogic : DispatchingLogic
    {
        internal const int ModeCount = 8;
        private const int MaxDigits = 4;

        /// <summary>
        /// command input -> command output
        /// </summary>
        private static readonly Dictionary<Guid, Guid> Commands = new()
        {
            { F.InputVolumeDown, F.OutputVolumeDown },
            { F.InputMute, F.OutputMute },
            { F.InputChannelUp, F.OutputChannelUp },
            { F.InputChannelDown, F.OutputChannelDown },
            { F.InputPlayPause, F.OutputPlayPause },
            { F.InputNext, F.OutputNext },
            { F.InputPrevious, F.OutputPrevious },
            { F.InputNavUp, F.OutputNavUp },
            { F.InputNavDown, F.OutputNavDown },
            { F.InputNavLeft, F.OutputNavLeft },
            { F.InputNavRight, F.OutputNavRight },
            { F.InputNavOk, F.OutputNavOk },
            { F.InputBack, F.OutputBack },
            { F.InputMenu, F.OutputMenu },
            { F.InputHome, F.OutputHome },
        };

        /// <summary>
        /// command output -> name on the command output (see <see cref="MediaControllerCommand"/>)
        /// </summary>
        private static readonly Dictionary<Guid, string> CommandNames = new()
        {
            { F.OutputVolumeUp, MediaControllerCommand.VolumeUp },
            { F.OutputVolumeDown, MediaControllerCommand.VolumeDown },
            { F.OutputVolume, MediaControllerCommand.Volume },
            { F.OutputMute, MediaControllerCommand.Mute },
            { F.OutputChannelUp, MediaControllerCommand.ChannelUp },
            { F.OutputChannelDown, MediaControllerCommand.ChannelDown },
            { F.OutputChannel, MediaControllerCommand.Channel },
            { F.OutputPlayPause, MediaControllerCommand.PlayPause },
            { F.OutputNext, MediaControllerCommand.Next },
            { F.OutputPrevious, MediaControllerCommand.Previous },
            { F.OutputNavUp, MediaControllerCommand.NavUp },
            { F.OutputNavDown, MediaControllerCommand.NavDown },
            { F.OutputNavLeft, MediaControllerCommand.NavLeft },
            { F.OutputNavRight, MediaControllerCommand.NavRight },
            { F.OutputNavOk, MediaControllerCommand.NavOk },
            { F.OutputBack, MediaControllerCommand.Back },
            { F.OutputMenu, MediaControllerCommand.Menu },
            { F.OutputHome, MediaControllerCommand.Home },
        };

        private readonly System.Timers.Timer _timer = new() { AutoReset = false };
        private readonly string[] _modeNames = new string[ModeCount];
        private int _mode;
        private int? _lastMode;
        private double? _volume;
        private int? _channel;
        private string _digits = "";
        private bool _locked;
        private bool _disablePeriphery;
        internal int NumpadTimeoutMs = 3000;

        public MediaControllerLogic(ILogicContext context) : base(context)
        {
            _timer.Elapsed += _timer_Elapsed;
        }

        internal int Mode => _mode;
        internal string Digits => _digits;

        protected override Task<bool> Start(RuleInstance instance, CancellationToken token = default)
        {
            lock (Sync)
            {
                SendMode();
            }
            return base.Start(instance, token);
        }

        protected override Task<bool> Stop(RuleInstance instance, CancellationToken token = default)
        {
            lock (Sync)
            {
                _timer.Stop();
            }
            return base.Stop(instance, token);
        }

        protected override void ParameterValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            lock (Sync)
            {
                var template = TemplateOf(instance);
                var nameIndex = Array.IndexOf(F.ParamModeNames, template);
                if (nameIndex >= 0)
                {
                    _modeNames[nameIndex] = LogicValue.ToText(value).Trim();
                }
                else if (template == F.ParamNumpadTimeout)
                {
                    NumpadTimeoutMs = Math.Max(0, LogicValue.ToInt(value, 3000));
                }
            }
        }

        /// <summary>
        /// The mode that power on starts: the last one, else the first mode with a name, else mode 1
        /// </summary>
        internal int StartMode
        {
            get
            {
                if (_lastMode.HasValue)
                {
                    return _lastMode.Value;
                }
                var named = Array.FindIndex(_modeNames, a => !string.IsNullOrEmpty(a));
                return named >= 0 ? named + 1 : 1;
            }
        }

        protected override IList<ILogicOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            lock (Sync)
            {
                var template = TemplateOf(instance);
                if (template == F.InputLock)
                {
                    _locked = LogicValue.IsTruthy(value);
                    if (_locked)
                    {
                        SetMode(0);
                    }
                }
                else if (template == F.InputDisablePeriphery)
                {
                    _disablePeriphery = LogicValue.IsTruthy(value);
                }
                else if (!_locked && (!_disablePeriphery || IsFromVisu(source)))
                {
                    Handle(template, value);
                }
            }
            return new List<ILogicOutputChanged>();
        }

        private void Handle(Guid template, object value)
        {
            var modeIndex = Array.IndexOf(F.InputModes, template);
            if (template == F.InputMode)
            {
                var mode = LogicValue.ToInt(value, -1);
                if (mode >= 0)
                {
                    SetMode(Math.Min(mode, ModeCount));
                }
                return;
            }
            if (template == F.InputVolume)
            {
                var volume = LogicValue.ToDouble(value);
                if (volume.HasValue)
                {
                    _volume = Math.Clamp(volume.Value, 0, 100);
                    SendCommand(F.OutputVolume, _volume.Value);
                }
                return;
            }
            if (template == F.InputChannel)
            {
                var channel = LogicValue.ToDouble(value);
                if (channel.HasValue && channel.Value >= 0)
                {
                    SendChannel((int)Math.Round(channel.Value));
                }
                return;
            }

            // the rest are buttons
            if (!LogicValue.IsTruthy(value) && !(template == F.InputDigit && LogicValue.ToDouble(value) == 0))
            {
                return;
            }

            if (modeIndex >= 0)
            {
                SetMode(modeIndex + 1);
            }
            else if (template == F.InputPowerToggle)
            {
                SetMode(_mode != 0 ? 0 : StartMode);
            }
            else if (template == F.InputPowerOn)
            {
                if (_mode == 0)
                {
                    SetMode(StartMode);
                }
            }
            else if (template == F.InputPowerOff)
            {
                SetMode(0);
            }
            else if (template == F.InputVolumeUp)
            {
                // like the remote: volume up starts the last mode when everything is off
                if (_mode == 0)
                {
                    SetMode(StartMode);
                }
                else
                {
                    SendCommand(F.OutputVolumeUp, true);
                }
            }
            else if (template == F.InputDigit)
            {
                Digit(LogicValue.ToInt(value, -1));
            }
            else if (_mode != 0 && Commands.TryGetValue(template, out var output))
            {
                SendCommand(output, true);
            }
        }

        private void Digit(int digit)
        {
            if (digit < 0 || digit > 9 || _mode == 0)
            {
                return;
            }
            if (_digits.Length >= MaxDigits)
            {
                _digits = "";
            }
            _digits += digit.ToString();
            _timer.Stop();
            if (NumpadTimeoutMs == 0 || _digits.Length >= MaxDigits)
            {
                FlushDigits();
                return;
            }
            _timer.Interval = LogicValue.TimerInterval(NumpadTimeoutMs);
            _timer.Start();
        }

        private void FlushDigits()
        {
            if (_digits.Length == 0)
            {
                return;
            }
            var channel = int.Parse(_digits);
            _digits = "";
            SendChannel(channel);
        }

        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            lock (Sync)
            {
                _timer.Stop();
                FlushDigits();
            }
        }

        private void SendChannel(int channel)
        {
            _channel = channel;
            SendCommand(F.OutputChannel, channel);
        }

        /// <summary>
        /// Sends a command on its own output and, for the active mode, on the command output
        /// </summary>
        private void SendCommand(Guid output, object value)
        {
            SendAlways(output, value);
            if (_mode == 0 || !CommandNames.TryGetValue(output, out var name))
            {
                return;
            }
            // values only for volume and channel, the buttons are just the name
            SendAlways(F.OutputCommand, value is bool
                ? MediaControllerCommand.Format(_mode, name)
                : MediaControllerCommand.Format(_mode, name, Convert.ToString(value, CultureInfo.InvariantCulture)));
        }

        private void SetMode(int mode)
        {
            _timer.Stop();
            _digits = "";
            var oldMode = _mode;
            _mode = mode;
            if (oldMode != mode)
            {
                // the device of the old mode goes off before the device of the new one comes on
                if (oldMode > 0)
                {
                    SendAlways(F.OutputCommand, MediaControllerCommand.Format(oldMode, MediaControllerCommand.Power, "0"));
                }
                if (mode > 0)
                {
                    SendAlways(F.OutputCommand, MediaControllerCommand.Format(mode, MediaControllerCommand.Power, "1"));
                }
            }
            if (mode > 0)
            {
                _lastMode = mode;
            }
            SendMode();
        }

        private void SendMode()
        {
            // devices of the old mode go off before the devices of the new one come on
            for (var i = 0; i < ModeCount; i++)
            {
                if (_mode != i + 1)
                {
                    Send(F.OutputModeActives[i], false);
                }
            }
            if (_mode > 0)
            {
                Send(F.OutputModeActives[_mode - 1], true);
            }
            Send(F.OutputMode, _mode);
            Send(F.OutputPower, _mode != 0);
        }

        public override object GetDataForVisu()
        {
            lock (Sync)
            {
                var data = SentValuesForVisu();
                data["mode"] = _mode;
                data["power"] = _mode != 0;
                data["startMode"] = StartMode;
                data["modeNames"] = _modeNames.Select(a => a ?? "").ToArray();
                data["volume"] = _volume;
                data["channel"] = _channel;
                data["locked"] = _locked;
                return data;
            }
        }
    }
}
