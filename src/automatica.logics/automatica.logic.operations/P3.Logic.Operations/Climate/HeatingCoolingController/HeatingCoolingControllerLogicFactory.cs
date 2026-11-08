using System;
using System.Collections.Generic;
using System.Linq;
using Automatica.Core.Base.Templates;
using Automatica.Core.Logic;
using P3.Logic.Operations.Climate.Common;
using F = P3.Logic.Operations.Climate.HeatingCoolingController.HeatingCoolingControllerLogicFactory;

namespace P3.Logic.Operations.Climate.HeatingCoolingController
{
    public class HeatingCoolingControllerLogicFactory : LogicFactory
    {
        public static readonly Guid Id = new Guid("9bc7a16c-3d73-415c-a6c0-a2b8e73ce494");
        public const int Rooms = 8;

        public static readonly Guid InOutdoor = ClimateTemplateBuilder.InputId(Id, "to");
        public static readonly Guid InBoost = ClimateTemplateBuilder.InputId(Id, "b");
        public static readonly Guid InOff = ClimateTemplateBuilder.InputId(Id, "off");
        public static readonly Guid InAdditionalHeating = ClimateTemplateBuilder.InputId(Id, "ah");
        public static readonly Guid InFan = ClimateTemplateBuilder.InputId(Id, "f");
        public static readonly Guid InExcessHeating = ClimateTemplateBuilder.InputId(Id, "eh");
        public static readonly Guid InExcessCooling = ClimateTemplateBuilder.InputId(Id, "ec");
        public static readonly Guid InManualHeating = ClimateTemplateBuilder.InputId(Id, "mh");
        public static readonly Guid InService = ClimateTemplateBuilder.InputId(Id, "service");
        public static readonly Guid[] InDemands = ClimateTemplateBuilder.Numbered("d", Rooms).Select(k => ClimateTemplateBuilder.InputId(Id, k)).ToArray();

        public static readonly Guid OutHeating = ClimateTemplateBuilder.OutputId(Id, "h");
        public static readonly Guid OutHeating2 = ClimateTemplateBuilder.OutputId(Id, "h2");
        public static readonly Guid OutCooling = ClimateTemplateBuilder.OutputId(Id, "c");
        public static readonly Guid OutCooling2 = ClimateTemplateBuilder.OutputId(Id, "c2");
        public static readonly Guid OutAdditionalHeating = ClimateTemplateBuilder.OutputId(Id, "ahout");
        public static readonly Guid OutSwitchingValve = ClimateTemplateBuilder.OutputId(Id, "sv");
        public static readonly Guid OutFan = ClimateTemplateBuilder.OutputId(Id, "fout");
        public static readonly Guid OutAverageOutdoor = ClimateTemplateBuilder.OutputId(Id, "toa");
        public static readonly Guid OutRoomSourceMode = ClimateTemplateBuilder.OutputId(Id, "rm");
        public static readonly Guid OutExcessHeating = ClimateTemplateBuilder.OutputId(Id, "ehout");
        public static readonly Guid OutExcessCooling = ClimateTemplateBuilder.OutputId(Id, "ecout");
        public static readonly Guid OutHeatingLoad = ClimateTemplateBuilder.OutputId(Id, "hl");
        public static readonly Guid OutCoolingLoad = ClimateTemplateBuilder.OutputId(Id, "cl");

        public static readonly Guid ParamMode = ClimateTemplateBuilder.ParamId(Id, "mode");
        public static readonly Guid ParamMinRuntime = ClimateTemplateBuilder.ParamId(Id, "minhr");
        public static readonly Guid ParamThreshold = ClimateTemplateBuilder.ParamId(Id, "sot");
        public static readonly Guid ParamValveDelay = ClimateTemplateBuilder.ParamId(Id, "vd");
        public static readonly Guid ParamFanOverrun = ClimateTemplateBuilder.ParamId(Id, "fod");
        public static readonly Guid ParamStage2 = ClimateTemplateBuilder.ParamId(Id, "tt2s");
        public static readonly Guid ParamOutdoorMode = ClimateTemplateBuilder.ParamId(Id, "otm");
        public static readonly Guid ParamLimitHeating = ClimateTemplateBuilder.ParamId(Id, "limh");
        public static readonly Guid ParamLimitCooling = ClimateTemplateBuilder.ParamId(Id, "limc");

        public override string LogicName => "Climate.HeatingCoolingController";
        public override Guid LogicGuid => Id;
        public override Version LogicVersion => new Version(1, 0, 0, 0);

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            var b = new ClimateTemplateBuilder(factory, Id, "HEATING_COOLING", "climate.heating-cooling", 200, 100);
            var i = 1;
            b.Input("to", "To", i++);
            b.Input("b", "B", i++);
            b.Input("off", "Off", i++);
            b.Input("ah", "Ah", i++);
            b.Input("f", "F", i++);
            b.Input("eh", "Eh", i++);
            b.Input("ec", "Ec", i++);
            b.Input("mh", "Mh", i++);
            b.Input("service", "Sm", i++);
            foreach (var key in ClimateTemplateBuilder.Numbered("d", Rooms))
            {
                b.Input(key, key.ToUpperInvariant(), i++);
            }

            var o = 1;
            b.Output("h", "H", o++);
            b.Output("h2", "H2", o++);
            b.Output("c", "C", o++);
            b.Output("c2", "C2", o++);
            b.Output("ahout", "Ah", o++);
            b.Output("sv", "Sv", o++);
            b.Output("fout", "F", o++);
            b.Output("toa", "Toa", o++);
            b.Output("rm", "Rm", o++);
            b.Output("ehout", "Eh", o++);
            b.Output("ecout", "Ec", o++);
            b.Output("hl", "Hl", o++);
            b.Output("cl", "Cl", o);

            var p = 1;
            b.Enum("mode", p++, 1, (0, "OFF"), (1, "AUTO"), (2, "HEATING"), (3, "COOLING"));
            b.Number("minhr", p++, 0);
            b.Number("sot", p++, 30);
            b.Number("vd", p++, 0);
            b.Number("fod", p++, 120);
            b.Number("tt2s", p++, 60);
            b.Enum("otm", p++, 2, (0, "DISABLED"), (1, "AVERAGE"), (2, "CURRENT"));
            b.Number("limh", p++, 18);
            b.Number("limc", p, 15);
            b.VisuTile(ClimateTile.Central);
        }

        public override ILogic CreateLogicInstance(ILogicContext context) => new HeatingCoolingControllerLogic(context);
    }

    /// <summary>
    /// Heating and cooling controller: a central heating and/or cooling source fed by the demand outputs (D) of up to 8 room controllers.
    /// It starts heating or cooling when the average demand reaches the switch-on threshold (Sot, the larger demand wins), keeps the minimum
    /// runtime (MinHr, minutes), waits 3 minutes after a stop and respects the outdoor limits (Otm, LimH, LimC). Stage 2 (H2/C2) follows after
    /// Tt2s minutes or with Boost. The switching valve (Sv: 0 heating, 1 cooling) moves first, H/C follow after Vd seconds.
    /// The fan runs with H/C (or input F) and Fod seconds after them. Mh forces heating, the service mode (Sm: 1 heating, 2 cooling, 3 off)
    /// overrides everything except Off. Rm goes to the Sm input of the room controllers (1 heating, 2 cooling, 3 none); Eh/Ec are forwarded
    /// while the matching mode runs.
    /// </summary>
    public class HeatingCoolingControllerLogic : ClimateLogic
    {
        internal const double RestartDelaySeconds = 180;

        private readonly Changeover _changeover = new() { RestartDelaySeconds = RestartDelaySeconds };
        private readonly OutdoorAverage _outdoorAverage = new();

        private SourceMode _mode;
        private DateTime? _modeSince;
        private bool _valveCooling;
        private DateTime _valveReadyAt = DateTime.MinValue;
        private DateTime? _fanUntil;
        private bool _lastActive;
        private DemandLoads _loads;

        public HeatingCoolingControllerLogic(ILogicContext context) : base(context)
        {
        }

        protected override double TickSeconds => 1;

        internal SourceMode Mode => _mode;

        protected override void OnTick(double elapsedSeconds)
        {
            var now = Now;
            var outdoor = InNumber(F.InOutdoor);
            if (outdoor.HasValue)
            {
                _outdoorAverage.Add(now, outdoor.Value);
            }

            _loads = Loads(F.InDemands);
            var used = SourceLimits.UsedOutdoor(ParamInt(F.ParamOutdoorMode, 2), outdoor, _outdoorAverage.Value(now));
            var paramMode = ParamInt(F.ParamMode, 1);
            var (heat, cool) = SourceLimits.Allowed(paramMode, used, Param(F.ParamLimitHeating, 18), Param(F.ParamLimitCooling, 15));

            _changeover.HeatThreshold = _changeover.CoolThreshold = Param(F.ParamThreshold, 30);
            _changeover.MinHeatRunSeconds = _changeover.MinCoolRunSeconds = Math.Max(0, Param(F.ParamMinRuntime, 0)) * 60;

            var off = InBool(F.InOff);
            var service = InInt(F.InService, 0);
            var decided = _changeover.Update(now, _loads.Heat, _loads.Cool, heat, cool, off || paramMode == 0);

            var mode = off ? SourceMode.Off
                : service == 1 ? SourceMode.Heating
                : service == 2 ? SourceMode.Cooling
                : service == 3 ? SourceMode.Off
                : InBool(F.InManualHeating) ? SourceMode.Heating
                : decided;

            if (mode != _mode)
            {
                _mode = mode;
                _modeSince = mode == SourceMode.Off ? null : now;
            }

            if (mode != SourceMode.Off && _valveCooling != (mode == SourceMode.Cooling))
            {
                _valveCooling = mode == SourceMode.Cooling;
                _valveReadyAt = now.AddSeconds(Math.Max(0, Param(F.ParamValveDelay, 0)));
            }

            var active = mode != SourceMode.Off && now >= _valveReadyAt;
            if (_lastActive && !active)
            {
                _fanUntil = now.AddSeconds(Math.Max(0, Param(F.ParamFanOverrun, 120)));
            }
            _lastActive = active;
            if (_fanUntil.HasValue && now >= _fanUntil.Value)
            {
                _fanUntil = null;
            }
        }

        protected override void Compute(IDictionary<Guid, object> outputs)
        {
            var now = Now;
            var active = _mode != SourceMode.Off && now >= _valveReadyAt;
            var heating = active && _mode == SourceMode.Heating;
            var cooling = active && _mode == SourceMode.Cooling;
            var stage2 = active && (InBool(F.InBoost) ||
                                    _modeSince.HasValue && (now - _modeSince.Value).TotalMinutes >= Math.Max(0, Param(F.ParamStage2, 60)));

            outputs[F.OutHeating] = heating;
            outputs[F.OutHeating2] = heating && stage2;
            outputs[F.OutCooling] = cooling;
            outputs[F.OutCooling2] = cooling && stage2;
            outputs[F.OutAdditionalHeating] = !InBool(F.InOff) && (InBool(F.InAdditionalHeating) || heating && InBool(F.InBoost));
            outputs[F.OutSwitchingValve] = _valveCooling;
            outputs[F.OutFan] = !InBool(F.InOff) && (active || InBool(F.InFan) || _fanUntil.HasValue);
            outputs[F.OutRoomSourceMode] = _mode == SourceMode.Heating ? 1 : _mode == SourceMode.Cooling ? 2 : 3;
            outputs[F.OutExcessHeating] = _mode == SourceMode.Heating && InBool(F.InExcessHeating);
            outputs[F.OutExcessCooling] = _mode == SourceMode.Cooling && InBool(F.InExcessCooling);
            Set(outputs, F.OutHeatingLoad, _loads.Heat, 1);
            Set(outputs, F.OutCoolingLoad, _loads.Cool, 1);

            var average = _outdoorAverage.Value(now);
            if (average.HasValue)
            {
                Set(outputs, F.OutAverageOutdoor, average.Value, 1);
            }
        }
    }
}
