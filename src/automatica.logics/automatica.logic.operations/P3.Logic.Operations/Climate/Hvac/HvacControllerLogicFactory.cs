using System;
using System.Collections.Generic;
using System.Linq;
using Automatica.Core.Base.Templates;
using Automatica.Core.Logic;
using P3.Logic.Operations.Climate.Common;
using F = P3.Logic.Operations.Climate.Hvac.HvacControllerLogicFactory;

namespace P3.Logic.Operations.Climate.Hvac
{
    public class HvacControllerLogicFactory : LogicFactory
    {
        public static readonly Guid Id = new Guid("05793da8-6436-470b-a705-0782ba9958b6");
        public const int Rooms = 8;

        public static readonly Guid InMode = ClimateTemplateBuilder.InputId(Id, "mode");
        public static readonly Guid InOutdoor = ClimateTemplateBuilder.InputId(Id, "to");
        public static readonly Guid InBoost = ClimateTemplateBuilder.InputId(Id, "b");
        public static readonly Guid InOff = ClimateTemplateBuilder.InputId(Id, "off");
        public static readonly Guid InEmergencyHeat = ClimateTemplateBuilder.InputId(Id, "emh");
        public static readonly Guid InFan = ClimateTemplateBuilder.InputId(Id, "fan");
        public static readonly Guid InHumidity = ClimateTemplateBuilder.InputId(Id, "hum");
        public static readonly Guid InExcessHeating = ClimateTemplateBuilder.InputId(Id, "eh");
        public static readonly Guid InExcessCooling = ClimateTemplateBuilder.InputId(Id, "ec");
        public static readonly Guid[] InDemands = ClimateTemplateBuilder.Numbered("d", Rooms).Select(k => ClimateTemplateBuilder.InputId(Id, k)).ToArray();

        public static readonly Guid OutW1 = ClimateTemplateBuilder.OutputId(Id, "w1");
        public static readonly Guid OutW2 = ClimateTemplateBuilder.OutputId(Id, "w2");
        public static readonly Guid OutY = ClimateTemplateBuilder.OutputId(Id, "y");
        public static readonly Guid OutY2 = ClimateTemplateBuilder.OutputId(Id, "y2");
        public static readonly Guid OutEmergency = ClimateTemplateBuilder.OutputId(Id, "e");
        public static readonly Guid OutReversingValve = ClimateTemplateBuilder.OutputId(Id, "ob");
        public static readonly Guid OutFan = ClimateTemplateBuilder.OutputId(Id, "g");
        public static readonly Guid OutHumidifier = ClimateTemplateBuilder.OutputId(Id, "hmd");
        public static readonly Guid OutHeatingCoolingMode = ClimateTemplateBuilder.OutputId(Id, "hcm");
        public static readonly Guid OutRoomSourceMode = ClimateTemplateBuilder.OutputId(Id, "rm");
        public static readonly Guid OutExcessHeating = ClimateTemplateBuilder.OutputId(Id, "ehout");
        public static readonly Guid OutExcessCooling = ClimateTemplateBuilder.OutputId(Id, "ecout");

        public static readonly Guid ParamType = ClimateTemplateBuilder.ParamId(Id, "type");
        public static readonly Guid ParamMinOff = ClimateTemplateBuilder.ParamId(Id, "mino");
        public static readonly Guid ParamThreshold = ClimateTemplateBuilder.ParamId(Id, "sot");
        public static readonly Guid ParamFanOverrun = ClimateTemplateBuilder.ParamId(Id, "fot");
        public static readonly Guid ParamStage2Time = ClimateTemplateBuilder.ParamId(Id, "tt2s");
        public static readonly Guid ParamStage2Demand = ClimateTemplateBuilder.ParamId(Id, "s2d");
        public static readonly Guid ParamMinOutdoorCooling = ClimateTemplateBuilder.ParamId(Id, "mioc");
        public static readonly Guid ParamMaxOutdoorHeating = ClimateTemplateBuilder.ParamId(Id, "maoh");
        public static readonly Guid ParamProtectMinCooling = ClimateTemplateBuilder.ParamId(Id, "pmic");
        public static readonly Guid ParamProtectMinHeating = ClimateTemplateBuilder.ParamId(Id, "pmih");
        public static readonly Guid ParamProtectMaxHeating = ClimateTemplateBuilder.ParamId(Id, "pmah");
        public static readonly Guid ParamHumidity = ClimateTemplateBuilder.ParamId(Id, "hs");
        public static readonly Guid ParamDirection = ClimateTemplateBuilder.ParamId(Id, "dirv");
        public static readonly Guid ParamOutdoorMode = ClimateTemplateBuilder.ParamId(Id, "otm");

        public override string LogicName => "Climate.HvacController";
        public override Guid LogicGuid => Id;
        public override Version LogicVersion => new Version(1, 0, 0, 0);

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            var b = new ClimateTemplateBuilder(factory, Id, "HVAC", "climate.hvac", 200, 100);
            var i = 1;
            b.Input("mode", "Mode", i++);
            b.Input("to", "To", i++);
            b.Input("b", "B", i++);
            b.Input("off", "Off", i++);
            b.Input("emh", "Emh", i++);
            b.Input("fan", "Fan", i++);
            b.Input("hum", "H", i++);
            b.Input("eh", "Eh", i++);
            b.Input("ec", "Ec", i++);
            foreach (var key in ClimateTemplateBuilder.Numbered("d", Rooms))
            {
                b.Input(key, key.ToUpperInvariant(), i++);
            }

            var o = 1;
            b.Output("w1", "W1", o++);
            b.Output("w2", "W2", o++);
            b.Output("y", "Y", o++);
            b.Output("y2", "Y2", o++);
            b.Output("e", "E", o++);
            b.Output("ob", "O/B", o++);
            b.Output("g", "G", o++);
            b.Output("hmd", "Hmd", o++);
            b.Output("hcm", "HCm", o++);
            b.Output("rm", "Rm", o++);
            b.Output("ehout", "Eh", o++);
            b.Output("ecout", "Ec", o);

            var p = 1;
            b.Enum("type", p++, 0, (0, "CONVENTIONAL"), (1, "HEAT_PUMP_FOSSIL"), (2, "HEAT_PUMP_ELECTRIC"));
            b.Number("mino", p++, 300);
            b.Number("sot", p++, 30);
            b.Number("fot", p++, 90);
            b.Number("tt2s", p++, 300);
            b.Number("s2d", p++, 90);
            b.Number("mioc", p++, 15);
            b.Number("maoh", p++, 18);
            b.Number("pmic", p++, 12);
            b.Number("pmih", p++, 0);
            b.Number("pmah", p++, 19);
            b.Number("hs", p++, 45);
            b.Bool("dirv", p++, false);
            b.Enum("otm", p, 2, (0, "DISABLED"), (1, "AVERAGE"), (2, "CURRENT"));
            b.VisuTile(ClimateTile.Central);
        }

        public override ILogic CreateLogicInstance(ILogicContext context) => new HvacControllerLogic(context);
    }

    /// <summary>
    /// HVAC controller for air based heating/cooling systems (furnace, AC, heat pump with backup), modelled on the Loxone block.
    /// Heating/cooling is decided from the demands (D1..D8) of the room controllers like the heating and cooling controller
    /// (switch-on threshold Sot, minimum off time MinO, outdoor limits MaoH / MioC). Stage 2 follows after Tt2s seconds, with Boost or when
    /// one room demands at least S2d %. The outputs follow the heating type:
    /// <list type="bullet">
    /// <item>Conventional: heating W1 (+ W2 stage 2), cooling compressor Y (+ Y2), no reversing valve.</item>
    /// <item>Heat pump with fossil backup: stage 1 heats with the compressor Y, stage 2 or outdoor below PmiH switches to the furnace W1.</item>
    /// <item>Heat pump with electric backup: stage 1 heats with Y, stage 2 adds the electric heater W1.</item>
    /// </list>
    /// The heat pump does not heat above PmaH and does not cool below PmiC. Emh replaces the heat pump by emergency heat (E).
    /// The reversing valve O/B is on for cooling (DirV inverts it), the fan G runs with every stage, with input Fan and Fot seconds after.
    /// The humidifier runs in heating mode below Hs % and stops at Hs + 2 %.
    /// </summary>
    public class HvacControllerLogic : ClimateLogic
    {
        private readonly Changeover _changeover = new();
        private readonly OutdoorAverage _outdoorAverage = new();
        private DemandLoads _loads;
        private DateTime? _fanUntil;
        private bool _lastActive;
        private bool _humidifier;

        public HvacControllerLogic(ILogicContext context) : base(context)
        {
        }

        protected override double TickSeconds => 1;

        internal SourceMode Mode => _changeover.Mode;

        private int InputMode => Math.Clamp(InInt(F.InMode, 1), 0, 3);

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
            var (heat, cool) = SourceLimits.Allowed(InputMode, used, Param(F.ParamMaxOutdoorHeating, 18), Param(F.ParamMinOutdoorCooling, 15));
            // the compressor must not cool below its protection temperature
            if (outdoor.HasValue && outdoor.Value < Param(F.ParamProtectMinCooling, 12))
            {
                cool = false;
            }

            _changeover.HeatThreshold = _changeover.CoolThreshold = Param(F.ParamThreshold, 30);
            _changeover.RestartDelaySeconds = Math.Max(0, Param(F.ParamMinOff, 300));
            _changeover.Update(now, _loads.Heat, _loads.Cool, heat, cool, InBool(F.InOff) || InputMode == 0);

            var active = _changeover.Mode != SourceMode.Off;
            if (_lastActive && !active)
            {
                _fanUntil = now.AddSeconds(Math.Max(0, Param(F.ParamFanOverrun, 90)));
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
            var mode = _changeover.Mode;
            var heating = mode == SourceMode.Heating;
            var cooling = mode == SourceMode.Cooling;
            var maxDemand = heating ? _loads.MaxHeat : cooling ? _loads.MaxCool : 0;
            var stage2 = mode != SourceMode.Off && (InBool(F.InBoost)
                                                    || _changeover.RunSeconds(now) >= Math.Max(0, Param(F.ParamStage2Time, 300))
                                                    || maxDemand >= Param(F.ParamStage2Demand, 90));

            var type = ParamInt(F.ParamType, 0);
            var outdoor = InNumber(F.InOutdoor);
            var emergency = heating && InBool(F.InEmergencyHeat);

            bool w1 = false, w2 = false, y = false, y2 = false;
            if (heating)
            {
                if (type == 0)
                {
                    w1 = true;
                    w2 = stage2;
                }
                else
                {
                    var heatPumpBlocked = emergency
                                          || outdoor.HasValue && outdoor.Value > Param(F.ParamProtectMaxHeating, 19)
                                          || type == 1 && outdoor.HasValue && outdoor.Value < Param(F.ParamProtectMinHeating, 0);
                    if (type == 1)
                    {
                        // dual fuel: the furnace replaces the heat pump
                        w1 = !emergency && (heatPumpBlocked || stage2);
                        y = !heatPumpBlocked && !stage2;
                    }
                    else
                    {
                        y = !heatPumpBlocked;
                        w1 = !emergency && (stage2 || heatPumpBlocked);
                    }
                }
            }
            else if (cooling)
            {
                y = true;
                y2 = stage2;
            }

            var reversing = type != 0 && (cooling ^ ParamBool(F.ParamDirection, false)) && mode != SourceMode.Off;

            var humidity = InNumber(F.InHumidity);
            var setpoint = Param(F.ParamHumidity, 45);
            if (!heating || !humidity.HasValue || humidity.Value >= setpoint + 2)
            {
                _humidifier = false;
            }
            else if (humidity.Value < setpoint)
            {
                _humidifier = true;
            }

            outputs[F.OutW1] = w1;
            outputs[F.OutW2] = w2;
            outputs[F.OutY] = y;
            outputs[F.OutY2] = y2;
            outputs[F.OutEmergency] = emergency;
            outputs[F.OutReversingValve] = reversing;
            outputs[F.OutFan] = !InBool(F.InOff) && (mode != SourceMode.Off || InBool(F.InFan) || _fanUntil.HasValue);
            outputs[F.OutHumidifier] = _humidifier;
            outputs[F.OutHeatingCoolingMode] = (int)mode;
            outputs[F.OutRoomSourceMode] = heating ? 1 : cooling ? 2 : 3;
            outputs[F.OutExcessHeating] = heating && InBool(F.InExcessHeating);
            outputs[F.OutExcessCooling] = cooling && InBool(F.InExcessCooling);
        }
    }
}
