using System;
using System.Collections.Generic;
using Automatica.Core.Base.Templates;
using Automatica.Core.Logic;
using P3.Logic.Operations.Climate.Common;
using F = P3.Logic.Operations.Climate.AirConditioning.AcUnitLogicFactory;

namespace P3.Logic.Operations.Climate.AirConditioning
{
    public class AcUnitLogicFactory : LogicFactory
    {
        public static readonly Guid Id = new Guid("85322ca4-ce63-4a30-87ff-83601cce7d42");

        public static readonly Guid InToggle = ClimateTemplateBuilder.InputId(Id, "tg");
        public static readonly Guid InOn = ClimateTemplateBuilder.InputId(Id, "on");
        public static readonly Guid InOff = ClimateTemplateBuilder.InputId(Id, "off");
        public static readonly Guid InTarget = ClimateTemplateBuilder.InputId(Id, "tt");
        public static readonly Guid InCurrent = ClimateTemplateBuilder.InputId(Id, "tc");
        public static readonly Guid InMode = ClimateTemplateBuilder.InputId(Id, "mode");
        public static readonly Guid InFan = ClimateTemplateBuilder.InputId(Id, "fan");
        public static readonly Guid InAirflow = ClimateTemplateBuilder.InputId(Id, "adir");
        public static readonly Guid InWindow = ClimateTemplateBuilder.InputId(Id, "dwc");
        public static readonly Guid InPauseTimer = ClimateTemplateBuilder.InputId(Id, "pt");
        public static readonly Guid InLoadShedding = ClimateTemplateBuilder.InputId(Id, "ls");
        public static readonly Guid InSilent = ClimateTemplateBuilder.InputId(Id, "sm");
        public static readonly Guid InCentralPause = ClimateTemplateBuilder.InputId(Id, "pc");
        public static readonly Guid InRoomMode = ClimateTemplateBuilder.InputId(Id, "hcm");

        public static readonly Guid OutStatus = ClimateTemplateBuilder.OutputId(Id, "status");
        public static readonly Guid OutMode = ClimateTemplateBuilder.OutputId(Id, "modeout");
        public static readonly Guid OutFan = ClimateTemplateBuilder.OutputId(Id, "fanout");
        public static readonly Guid OutAirflow = ClimateTemplateBuilder.OutputId(Id, "adirout");
        public static readonly Guid OutTarget = ClimateTemplateBuilder.OutputId(Id, "ttout");
        public static readonly Guid OutCurrent = ClimateTemplateBuilder.OutputId(Id, "tcout");
        public static readonly Guid OutPaused = ClimateTemplateBuilder.OutputId(Id, "paused");
        public static readonly Guid OutDemand = ClimateTemplateBuilder.OutputId(Id, "demand");

        public static readonly Guid ParamPauseDuration = ClimateTemplateBuilder.ParamId(Id, "ptd");
        public static readonly Guid ParamMinTarget = ClimateTemplateBuilder.ParamId(Id, "mint");
        public static readonly Guid ParamMaxTarget = ClimateTemplateBuilder.ParamId(Id, "maxt");
        public static readonly Guid ParamSilentFan = ClimateTemplateBuilder.ParamId(Id, "silent");

        public const int ModeAuto = 1;
        public const int ModeHeat = 2;
        public const int ModeCool = 3;
        public const int ModeDry = 4;
        public const int ModeFan = 5;

        public override string LogicName => "Climate.AcUnit";
        public override Guid LogicGuid => Id;
        public override Version LogicVersion => new Version(1, 0, 0, 0);

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            var b = new ClimateTemplateBuilder(factory, Id, "AC_UNIT", "climate.ac-unit", 200, 100);
            var i = 1;
            b.Input("tg", "Tg", i++);
            b.Input("on", "On", i++);
            b.Input("off", "Off", i++);
            b.Input("tt", "Tt", i++);
            b.Input("tc", "Tc", i++);
            b.Input("mode", "Mode", i++);
            b.Input("fan", "Fan", i++);
            b.Input("adir", "ADir", i++);
            b.Input("dwc", "Dwc", i++);
            b.Input("pt", "Pt", i++);
            b.Input("ls", "Ls", i++);
            b.Input("sm", "Sm", i++);
            b.Input("pc", "Pc", i++);
            b.Input("hcm", "HCm", i);

            var o = 1;
            b.Output("status", "Status", o++);
            b.Output("modeout", "Mode", o++);
            b.Output("fanout", "Fan", o++);
            b.Output("adirout", "ADir", o++);
            b.Output("ttout", "Tt", o++);
            b.Output("tcout", "Tc", o++);
            b.Output("paused", "Ps", o++);
            b.Output("demand", "D", o);

            var p = 1;
            b.Number("ptd", p++, 7200);
            b.Number("mint", p++, 16);
            b.Number("maxt", p++, 30);
            b.Integer("silent", p, 2);
            b.VisuTile(ClimateTile.Unit);
        }

        public override ILogic CreateLogicInstance(ILogicContext context) => new AcUnitLogic(context);
    }

    /// <summary>
    /// AC unit controller (indoor unit). It only sets the values for the unit - the unit regulates the temperature itself.
    /// Tg toggles, On switches on, Off switches off (Off = 1 locks). Mode 1 auto, 2 heat, 3 cool, 4 dry, 5 fan; Fan 0..7; ADir 1..8.
    /// The unit pauses (Status 0) while a window is open (Dwc), during load shedding (Ls), for Ptd seconds after Pt and while the AC central
    /// pauses it (Pc). With a room controller linked to HCm (1 heating, -1 cooling, 0 idle) the unit runs only on demand, in its mode.
    /// D is the signed demand for the AC central (+ heating, - cooling, in K, at least 0.1 while requested), it stays while the central pauses the unit.
    /// </summary>
    public class AcUnitLogic : ClimateLogic
    {
        internal const double MinimumDemand = 0.1;

        private bool _on;
        private DateTime? _pauseUntil;

        public AcUnitLogic(ILogicContext context) : base(context)
        {
        }

        protected override double TickSeconds => 1;

        protected override void OnInput(Guid template, object value, object previous)
        {
            var rising = ClimateValue.ToBool(value) && !ClimateValue.ToBool(previous);
            if (!rising)
            {
                if (template == F.InPauseTimer && !ClimateValue.ToBool(value) && ClimateValue.ToBool(previous))
                {
                    // the pause timer starts with the falling edge
                    _pauseUntil = Now.AddSeconds(Math.Max(0, Param(F.ParamPauseDuration, 7200)));
                }
                return;
            }
            if (template == F.InToggle)
            {
                _on = !_on;
            }
            else if (template == F.InOn)
            {
                _on = true;
            }
            else if (template == F.InOff)
            {
                _on = false;
            }
            else if (template == F.InPauseTimer)
            {
                _pauseUntil = Now.AddSeconds(Math.Max(0, Param(F.ParamPauseDuration, 7200)));
            }
        }

        protected override void OnTick(double elapsedSeconds)
        {
            if (_pauseUntil.HasValue && Now >= _pauseUntil.Value && !InBool(F.InPauseTimer))
            {
                _pauseUntil = null;
            }
        }

        private bool RoomManaged => HasInput(F.InRoomMode);

        /// <summary>
        /// 1 heating, 2 cooling, 0 none
        /// </summary>
        internal int RequestedMode(int mode, double? target, double? current)
        {
            if (RoomManaged)
            {
                var hcm = InInt(F.InRoomMode, 0);
                return hcm > 0 ? 1 : hcm < 0 ? 2 : 0;
            }
            return mode switch
            {
                F.ModeHeat => 1,
                F.ModeCool or F.ModeDry => 2,
                F.ModeAuto when target.HasValue && current.HasValue => current.Value < target.Value ? 1 : current.Value > target.Value ? 2 : 0,
                _ => 0
            };
        }

        protected override void Compute(IDictionary<Guid, object> outputs)
        {
            var locked = InBool(F.InOff);
            var mode = Math.Clamp(InInt(F.InMode, F.ModeAuto), 1, 5);
            var target = InNumber(F.InTarget);
            var current = InNumber(F.InCurrent);

            var on = !locked && (RoomManaged ? InInt(F.InRoomMode, 0) != 0 : _on);
            if (RoomManaged && on)
            {
                mode = InInt(F.InRoomMode, 0) > 0 ? F.ModeHeat : F.ModeCool;
            }

            var localPause = InBool(F.InWindow) || InBool(F.InLoadShedding) || InBool(F.InPauseTimer) || _pauseUntil.HasValue;
            var paused = on && (localPause || InBool(F.InCentralPause));
            outputs[F.OutStatus] = on && !paused;
            outputs[F.OutPaused] = paused;
            outputs[F.OutMode] = mode;

            var fan = Math.Clamp(InInt(F.InFan, 1), 0, 7);
            if (InBool(F.InSilent))
            {
                fan = Math.Clamp(ParamInt(F.ParamSilentFan, 2), 0, 7);
            }
            outputs[F.OutFan] = fan;
            outputs[F.OutAirflow] = Math.Clamp(InInt(F.InAirflow, 1), 1, 8);

            if (target.HasValue)
            {
                Set(outputs, F.OutTarget, Clamp(target.Value, Param(F.ParamMinTarget, 16), Param(F.ParamMaxTarget, 30)), 1);
            }
            if (current.HasValue)
            {
                Set(outputs, F.OutCurrent, current.Value, 1);
            }

            var requested = on && !localPause ? RequestedMode(mode, target, current) : 0;
            var magnitude = target.HasValue && current.HasValue ? Math.Max(MinimumDemand, Math.Abs(target.Value - current.Value)) : 1;
            Set(outputs, F.OutDemand, requested == 1 ? magnitude : requested == 2 ? -magnitude : 0, 1);
        }
    }
}
