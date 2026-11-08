using System;
using System.Collections.Generic;
using System.Linq;
using Automatica.Core.Base.Templates;
using Automatica.Core.Logic;
using P3.Logic.Operations.Climate.Common;
using F = P3.Logic.Operations.Climate.AirConditioning.AcCentralLogicFactory;

namespace P3.Logic.Operations.Climate.AirConditioning
{
    public class AcCentralLogicFactory : LogicFactory
    {
        public static readonly Guid Id = new Guid("f661a87f-bd27-4ac7-950b-b1a1856c7dec");
        public const int Units = 8;

        public static readonly Guid InOutdoor = ClimateTemplateBuilder.InputId(Id, "to");
        public static readonly Guid InOff = ClimateTemplateBuilder.InputId(Id, "off");
        public static readonly Guid[] InDemands = ClimateTemplateBuilder.Numbered("d", Units).Select(k => ClimateTemplateBuilder.InputId(Id, k)).ToArray();

        public static readonly Guid OutHeating = ClimateTemplateBuilder.OutputId(Id, "h");
        public static readonly Guid OutCooling = ClimateTemplateBuilder.OutputId(Id, "c");
        public static readonly Guid OutAverageOutdoor = ClimateTemplateBuilder.OutputId(Id, "toa");
        public static readonly Guid[] OutPauses = ClimateTemplateBuilder.Numbered("p", Units).Select(k => ClimateTemplateBuilder.OutputId(Id, k)).ToArray();

        public static readonly Guid ParamMode = ClimateTemplateBuilder.ParamId(Id, "mode");
        public static readonly Guid ParamLimitHeating = ClimateTemplateBuilder.ParamId(Id, "limh");
        public static readonly Guid ParamLimitCooling = ClimateTemplateBuilder.ParamId(Id, "limc");
        public static readonly Guid ParamOutdoorMode = ClimateTemplateBuilder.ParamId(Id, "otm");
        public static readonly Guid ParamMinRuntime = ClimateTemplateBuilder.ParamId(Id, "minrt");

        public override string LogicName => "Climate.AcCentral";
        public override Guid LogicGuid => Id;
        public override Version LogicVersion => new Version(1, 0, 0, 0);

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            var b = new ClimateTemplateBuilder(factory, Id, "AC_CENTRAL", "climate.ac-central", 200, 100);
            var i = 1;
            b.Input("to", "To", i++);
            b.Input("off", "Off", i++);
            foreach (var key in ClimateTemplateBuilder.Numbered("d", Units))
            {
                b.Input(key, key.ToUpperInvariant(), i++);
            }

            var o = 1;
            b.Output("h", "H", o++);
            b.Output("c", "C", o++);
            b.Output("toa", "Toa", o++);
            foreach (var key in ClimateTemplateBuilder.Numbered("p", Units))
            {
                b.Output(key, key.ToUpperInvariant(), o++);
            }

            var p = 1;
            b.Enum("mode", p++, 1, (0, "OFF"), (1, "AUTO"), (2, "HEATING"), (3, "COOLING"));
            b.Number("limh", p++, 18);
            b.Number("limc", p++, 15);
            b.Enum("otm", p++, 2, (0, "DISABLED"), (1, "AVERAGE"), (2, "CURRENT"));
            b.Number("minrt", p, 10);
            b.VisuTile(ClimateTile.Central);
        }

        public override ILogic CreateLogicInstance(ILogicContext context) => new AcCentralLogic(context);
    }

    /// <summary>
    /// AC central controller (outdoor unit of a multi split system). The signed demands of up to 8 AC unit controllers (D1..D8,
    /// + heating, - cooling) decide whether the outdoor unit heats (H) or cools (C): the larger sum wins, the mode is kept for at least
    /// MinRt minutes and only changes through off (3 minutes restart delay). Units that request the other mode, or a mode the central
    /// does not allow (Mode, outdoor limits LimH / LimC), are paused through P1..P8 (link to the Pc input of the unit).
    /// </summary>
    public class AcCentralLogic : ClimateLogic
    {
        private readonly Changeover _changeover = new() { HeatThreshold = 0, CoolThreshold = 0, RestartDelaySeconds = 180 };
        private readonly OutdoorAverage _outdoorAverage = new();
        private bool _heatAllowed = true;
        private bool _coolAllowed = true;

        public AcCentralLogic(ILogicContext context) : base(context)
        {
        }

        protected override double TickSeconds => 1;

        internal SourceMode Mode => _changeover.Mode;

        protected override void OnTick(double elapsedSeconds)
        {
            var now = Now;
            var outdoor = InNumber(F.InOutdoor);
            if (outdoor.HasValue)
            {
                _outdoorAverage.Add(now, outdoor.Value);
            }

            var demands = F.InDemands.Select(InNumber).Where(a => a.HasValue).Select(a => a.Value).ToList();
            var heatSum = demands.Where(a => a > 0).Sum();
            var coolSum = -demands.Where(a => a < 0).Sum();

            var mode = ParamInt(F.ParamMode, 1);
            var used = SourceLimits.UsedOutdoor(ParamInt(F.ParamOutdoorMode, 2), outdoor, _outdoorAverage.Value(now));
            (_heatAllowed, _coolAllowed) = SourceLimits.Allowed(mode, used, Param(F.ParamLimitHeating, 18), Param(F.ParamLimitCooling, 15));

            _changeover.MinHeatRunSeconds = _changeover.MinCoolRunSeconds = Math.Max(0, Param(F.ParamMinRuntime, 10)) * 60;
            _changeover.Update(now, heatSum, coolSum, _heatAllowed, _coolAllowed, InBool(F.InOff) || mode == 0);
        }

        protected override void Compute(IDictionary<Guid, object> outputs)
        {
            var mode = _changeover.Mode;
            outputs[F.OutHeating] = mode == SourceMode.Heating;
            outputs[F.OutCooling] = mode == SourceMode.Cooling;

            var off = InBool(F.InOff);
            for (var unit = 0; unit < F.Units; unit++)
            {
                var demand = InNumber(F.InDemands[unit]) ?? 0;
                var pause = off
                            || demand > 0 && (!_heatAllowed || mode == SourceMode.Cooling)
                            || demand < 0 && (!_coolAllowed || mode == SourceMode.Heating);
                outputs[F.OutPauses[unit]] = pause;
            }

            var average = _outdoorAverage.Value(Now);
            if (average.HasValue)
            {
                Set(outputs, F.OutAverageOutdoor, average.Value, 1);
            }
        }
    }
}
