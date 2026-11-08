using System;
using System.Collections.Generic;
using System.Globalization;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using P3.Logic.Operations.Garage;
using M = P3.Logic.Operations.MediaController.MediaControllerModeLogicFactory;

namespace P3.Logic.Operations.MediaController
{
    /// <summary>
    /// One device of a media controller: reads the command output of the controller and passes on the commands of its mode.
    /// Power is 1 while the mode is on; buttons send 1 per press, volume and channel their value.
    /// </summary>
    public class MediaControllerModeLogic : DispatchingLogic
    {
        private static readonly Dictionary<string, Guid> Buttons = new(StringComparer.OrdinalIgnoreCase)
        {
            { MediaControllerCommand.VolumeUp, M.OutputVolumeUp },
            { MediaControllerCommand.VolumeDown, M.OutputVolumeDown },
            { MediaControllerCommand.Mute, M.OutputMute },
            { MediaControllerCommand.ChannelUp, M.OutputChannelUp },
            { MediaControllerCommand.ChannelDown, M.OutputChannelDown },
            { MediaControllerCommand.PlayPause, M.OutputPlayPause },
            { MediaControllerCommand.Next, M.OutputNext },
            { MediaControllerCommand.Previous, M.OutputPrevious },
            { MediaControllerCommand.NavUp, M.OutputNavUp },
            { MediaControllerCommand.NavDown, M.OutputNavDown },
            { MediaControllerCommand.NavLeft, M.OutputNavLeft },
            { MediaControllerCommand.NavRight, M.OutputNavRight },
            { MediaControllerCommand.NavOk, M.OutputNavOk },
            { MediaControllerCommand.Back, M.OutputBack },
            { MediaControllerCommand.Menu, M.OutputMenu },
            { MediaControllerCommand.Home, M.OutputHome },
        };

        private int _mode = 1;
        private bool _power;

        public MediaControllerModeLogic(ILogicContext context) : base(context)
        {
        }

        internal int Mode => _mode;

        protected override void ParameterValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            lock (Sync)
            {
                if (TemplateOf(instance) == M.ParamMode)
                {
                    var mode = LogicValue.ToInt(value, 1);
                    if (mode != _mode)
                    {
                        _mode = Math.Clamp(mode, 1, MediaControllerLogic.ModeCount);
                        // the old mode's power state does not belong to the new one
                        if (_power)
                        {
                            SetPower(false);
                        }
                    }
                }
            }
        }

        protected override IList<ILogicOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            lock (Sync)
            {
                if (TemplateOf(instance) == M.InputCommand
                    && MediaControllerCommand.TryParse(value, out var mode, out var command, out var argument)
                    && mode == _mode)
                {
                    Handle(command, argument);
                }
            }
            return new List<ILogicOutputChanged>();
        }

        private void Handle(string command, string argument)
        {
            if (string.Equals(command, MediaControllerCommand.Power, StringComparison.OrdinalIgnoreCase))
            {
                SetPower(LogicValue.IsTruthy(argument));
            }
            else if (string.Equals(command, MediaControllerCommand.Volume, StringComparison.OrdinalIgnoreCase))
            {
                if (double.TryParse(argument, NumberStyles.Float, CultureInfo.InvariantCulture, out var volume))
                {
                    SendAlways(M.OutputVolume, volume);
                }
            }
            else if (string.Equals(command, MediaControllerCommand.Channel, StringComparison.OrdinalIgnoreCase))
            {
                if (int.TryParse(argument, NumberStyles.Integer, CultureInfo.InvariantCulture, out var channel))
                {
                    SendAlways(M.OutputChannel, channel);
                }
            }
            else if (Buttons.TryGetValue(command, out var output))
            {
                SendAlways(output, true);
            }
        }

        private void SetPower(bool power)
        {
            _power = power;
            Send(M.OutputPower, power);
        }

        public override object GetDataForVisu()
        {
            lock (Sync)
            {
                var data = SentValuesForVisu();
                data["mode"] = _mode;
                data["power"] = _power;
                return data;
            }
        }
    }
}
