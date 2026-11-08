using System;
using System.Collections.Generic;
using System.Linq;
using Automatica.Core.Base.Templates;
using Automatica.Core.Logic;
using P3.Logic.Operations.Climate.Common;
using F = P3.Logic.Operations.Climate.FanCoil.FanCoilCentralLogicFactory;

namespace P3.Logic.Operations.Climate.FanCoil
{
    public class FanCoilCentralLogicFactory : LogicFactory
    {
        public static readonly Guid Id = new Guid("0cc16731-c4f9-4262-ad4a-b38032fc1a80");
        public const int Units = 8;

        public static readonly Guid InOutdoor = ClimateTemplateBuilder.InputId(Id, "to");
        public static readonly Guid InOff = ClimateTemplateBuilder.InputId(Id, "off");
        public static readonly Guid InHeatingActive = ClimateTemplateBuilder.InputId(Id, "hac");
        public static readonly Guid InCoolingActive = ClimateTemplateBuilder.InputId(Id, "cac");
        public static readonly Guid[] InDemands = ClimateTemplateBuilder.Numbered("d", Units).Select(k => ClimateTemplateBuilder.InputId(Id, k)).ToArray();

        public static readonly Guid OutHeating = ClimateTemplateBuilder.OutputId(Id, "h");
        public static readonly Guid OutCooling = ClimateTemplateBuilder.OutputId(Id, "c");
        public static readonly Guid OutAverageOutdoor = ClimateTemplateBuilder.OutputId(Id, "toa");
        public static readonly Guid OutAvailableMode = ClimateTemplateBuilder.OutputId(Id, "avmode");
        public static readonly Guid OutSwitchingValve = ClimateTemplateBuilder.OutputId(Id, "sv");
        public static readonly Guid OutUnitsHeat = ClimateTemplateBuilder.OutputId(Id, "uh");
        public static readonly Guid OutUnitsCool = ClimateTemplateBuilder.OutputId(Id, "uc");

        public static readonly Guid ParamMode = ClimateTemplateBuilder.ParamId(Id, "mode");
        public static readonly Guid ParamPipes = ClimateTemplateBuilder.ParamId(Id, "pipes");
        public static readonly Guid ParamThresholdHeating = ClimateTemplateBuilder.ParamId(Id, "soth");
        public static readonly Guid ParamThresholdCooling = ClimateTemplateBuilder.ParamId(Id, "sotc");
        public static readonly Guid ParamMinHeatingRuntime = ClimateTemplateBuilder.ParamId(Id, "minhrt");
        public static readonly Guid ParamMinCoolingRuntime = ClimateTemplateBuilder.ParamId(Id, "mincrt");
        public static readonly Guid ParamOutdoorMode = ClimateTemplateBuilder.ParamId(Id, "otm");
        public static readonly Guid ParamLimitHeating = ClimateTemplateBuilder.ParamId(Id, "limh");
        public static readonly Guid ParamLimitCooling = ClimateTemplateBuilder.ParamId(Id, "limc");
        public static readonly Guid ParamValveDelay = ClimateTemplateBuilder.ParamId(Id, "vd");

        public override string LogicName => "Climate.FanCoilCentral";
        public override Guid LogicGuid => Id;
        public override Version LogicVersion => new Version(1, 0, 0, 0);

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            var b = new ClimateTemplateBuilder(factory, Id, "FAN_COIL_CENTRAL", "climate.fan-coil-central", 200, 100);
            var i = 1;
            b.Input("to", "To", i++);
            b.Input("off", "Off", i++);
            b.Input("hac", "Hac", i++);
            b.Input("cac", "Cac", i++);
            foreach (var key in ClimateTemplateBuilder.Numbered("d", Units))
            {
                b.Input(key, key.ToUpperInvariant(), i++);
            }

            var o = 1;
            b.Output("h", "H", o++);
            b.Output("c", "C", o++);
            b.Output("toa", "Toa", o++);
            b.Output("avmode", "AvMode", o++);
            b.Output("sv", "Sv", o++);
            b.Output("uh", "Uh", o++);
            b.Output("uc", "Uc", o);

            var p = 1;
            b.Enum("mode", p++, 1, (0, "OFF"), (1, "AUTO"), (2, "HEATING"), (3, "COOLING"));
            b.Enum("pipes", p++, 0, (0, "FOUR"), (1, "TWO"));
            b.Number("soth", p++, 30);
            b.Number("sotc", p++, 30);
            b.Number("minhrt", p++, 0);
            b.Number("mincrt", p++, 0);
            b.Enum("otm", p++, 2, (0, "DISABLED"), (1, "AVERAGE"), (2, "CURRENT"));
            b.Number("limh", p++, 18);
            b.Number("limc", p++, 15);
            b.Number("vd", p, 0);
            b.VisuTile(ClimateTile.Central);
        }

        public override ILogic CreateLogicInstance(ILogicContext context) => new FanCoilCentralLogic(context);
    }

    /// <summary>
    /// Fan coil central controller: the central heating/cooling source of up to 8 fan coil units (D1..D8 = their D outputs).
    /// 4-pipe: heating (H) and cooling (C) are decided independently - each starts when the average demand reaches its threshold
    /// (SotH / SotC) and keeps its minimum runtime (MinHrt / MinCrt, minutes). 2-pipe: one changeover decides, the switching valve
    /// (Sv: 0 heating, 1 cooling) moves first and H/C follow after Vd seconds. Mode and outdoor limits (Otm, LimH, LimC) restrict the
    /// modes, AvMode shows what is allowed (0 none, 1 heating, 2 cooling, 3 both).
    /// Uh / Uc go to the Ha / Ca inputs of the units: on while H / C runs and the source confirms it (Hac / Cac, unlinked = confirmed).
    /// </summary>
    public class FanCoilCentralLogic : ClimateLogic
    {
        private readonly Changeover _heating = new() { RestartDelaySeconds = 180 };
        private readonly Changeover _cooling = new() { RestartDelaySeconds = 180 };
        private readonly Changeover _twoPipe = new() { RestartDelaySeconds = 180 };
        private readonly OutdoorAverage _outdoorAverage = new();

        private bool _heatAllowed = true;
        private bool _coolAllowed = true;
        private bool _valveCooling;
        private DateTime _valveReadyAt = DateTime.MinValue;

        public FanCoilCentralLogic(ILogicContext context) : base(context)
        {
        }

        protected override double TickSeconds => 1;

        private bool TwoPipe => ParamInt(F.ParamPipes, 0) == 1;

        protected override void OnTick(double elapsedSeconds)
        {
            var now = Now;
            var outdoor = InNumber(F.InOutdoor);
            if (outdoor.HasValue)
            {
                _outdoorAverage.Add(now, outdoor.Value);
            }

            var loads = Loads(F.InDemands);
            var mode = ParamInt(F.ParamMode, 1);
            var used = SourceLimits.UsedOutdoor(ParamInt(F.ParamOutdoorMode, 2), outdoor, _outdoorAverage.Value(now));
            (_heatAllowed, _coolAllowed) = SourceLimits.Allowed(mode, used, Param(F.ParamLimitHeating, 18), Param(F.ParamLimitCooling, 15));
            var force = InBool(F.InOff) || mode == 0;

            foreach (var changeover in new[] { _heating, _cooling, _twoPipe })
            {
                changeover.HeatThreshold = Param(F.ParamThresholdHeating, 30);
                changeover.CoolThreshold = Param(F.ParamThresholdCooling, 30);
                changeover.MinHeatRunSeconds = Math.Max(0, Param(F.ParamMinHeatingRuntime, 0)) * 60;
                changeover.MinCoolRunSeconds = Math.Max(0, Param(F.ParamMinCoolingRuntime, 0)) * 60;
            }

            if (TwoPipe)
            {
                _heating.Update(now, 0, 0, false, false, true);
                _cooling.Update(now, 0, 0, false, false, true);
                var decided = _twoPipe.Update(now, loads.Heat, loads.Cool, _heatAllowed, _coolAllowed, force);
                if (decided != SourceMode.Off && _valveCooling != (decided == SourceMode.Cooling))
                {
                    _valveCooling = decided == SourceMode.Cooling;
                    _valveReadyAt = now.AddSeconds(Math.Max(0, Param(F.ParamValveDelay, 0)));
                }
            }
            else
            {
                _twoPipe.Update(now, 0, 0, false, false, true);
                _heating.Update(now, loads.Heat, 0, _heatAllowed, false, force);
                _cooling.Update(now, 0, loads.Cool, false, _coolAllowed, force);
            }
        }

        protected override void Compute(IDictionary<Guid, object> outputs)
        {
            bool heating, cooling;
            if (TwoPipe)
            {
                var ready = Now >= _valveReadyAt;
                heating = ready && _twoPipe.Mode == SourceMode.Heating;
                cooling = ready && _twoPipe.Mode == SourceMode.Cooling;
                outputs[F.OutSwitchingValve] = _valveCooling;
            }
            else
            {
                heating = _heating.Mode == SourceMode.Heating;
                cooling = _cooling.Mode == SourceMode.Cooling;
                outputs[F.OutSwitchingValve] = false;
            }

            outputs[F.OutHeating] = heating;
            outputs[F.OutCooling] = cooling;
            outputs[F.OutUnitsHeat] = heating && InBool(F.InHeatingActive, true);
            outputs[F.OutUnitsCool] = cooling && InBool(F.InCoolingActive, true);
            outputs[F.OutAvailableMode] = SourceLimits.AvailableMode(_heatAllowed && !InBool(F.InOff), _coolAllowed && !InBool(F.InOff));

            var average = _outdoorAverage.Value(Now);
            if (average.HasValue)
            {
                Set(outputs, F.OutAverageOutdoor, average.Value, 1);
            }
        }
    }
}
