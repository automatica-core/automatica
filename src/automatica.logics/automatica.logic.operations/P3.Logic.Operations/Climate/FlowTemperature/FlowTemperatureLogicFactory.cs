using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Automatica.Core.Base.Templates;
using Automatica.Core.Logic;
using P3.Logic.Operations.Climate.Common;
using F = P3.Logic.Operations.Climate.FlowTemperature.FlowTemperatureLogicFactory;

namespace P3.Logic.Operations.Climate.FlowTemperature
{
    public class FlowTemperatureLogicFactory : LogicFactory
    {
        public static readonly Guid Id = new Guid("6cdc80d0-3500-4958-9d03-3a3155466559");
        public const int Rooms = 6;

        public static readonly Guid InOutdoor = ClimateTemplateBuilder.InputId(Id, "to");
        public static readonly Guid InBoost = ClimateTemplateBuilder.InputId(Id, "ib");
        public static readonly Guid InStop = ClimateTemplateBuilder.InputId(Id, "st");
        public static readonly Guid InBuffer = ClimateTemplateBuilder.InputId(Id, "tb");
        public static readonly Guid[] InRoomTargets = Keys("tt").Select(k => ClimateTemplateBuilder.InputId(Id, k)).ToArray();
        public static readonly Guid[] InRoomCurrents = Keys("tc").Select(k => ClimateTemplateBuilder.InputId(Id, k)).ToArray();
        public static readonly Guid[] InRoomValves = Keys("v").Select(k => ClimateTemplateBuilder.InputId(Id, k)).ToArray();

        public static readonly Guid OutRoomTarget = ClimateTemplateBuilder.OutputId(Id, "aqt");
        public static readonly Guid OutFlow = ClimateTemplateBuilder.OutputId(Id, "aqf");
        public static readonly Guid OutBuffer = ClimateTemplateBuilder.OutputId(Id, "aqb");
        public static readonly Guid OutPump = ClimateTemplateBuilder.OutputId(Id, "qp");
        public static readonly Guid OutLoad = ClimateTemplateBuilder.OutputId(Id, "aql");
        public static readonly Guid OutIncrease = ClimateTemplateBuilder.OutputId(Id, "aqi");
        public static readonly Guid OutError = ClimateTemplateBuilder.OutputId(Id, "qe");

        public static readonly Guid ParamMode = ClimateTemplateBuilder.ParamId(Id, "mode");
        public static readonly Guid ParamMin = ClimateTemplateBuilder.ParamId(Id, "min");
        public static readonly Guid ParamMax = ClimateTemplateBuilder.ParamId(Id, "max");
        public static readonly Guid ParamBufferOffset = ClimateTemplateBuilder.ParamId(Id, "b");
        public static readonly Guid ParamSlope = ClimateTemplateBuilder.ParamId(Id, "s");
        public static readonly Guid ParamLevel = ClimateTemplateBuilder.ParamId(Id, "n");
        public static readonly Guid ParamThreshold = ClimateTemplateBuilder.ParamId(Id, "str");
        public static readonly Guid ParamGain = ClimateTemplateBuilder.ParamId(Id, "g");
        public static readonly Guid ParamPhaseIncrease = ClimateTemplateBuilder.ParamId(Id, "i");

        internal static IReadOnlyList<string> Keys(string prefix) => ClimateTemplateBuilder.Numbered(prefix, Rooms);

        public override string LogicName => "Climate.FlowTemperature";
        public override Guid LogicGuid => Id;
        public override Version LogicVersion => new Version(1, 0, 0, 0);

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            var b = new ClimateTemplateBuilder(factory, Id, "FLOW_TEMPERATURE", "climate.flow-temperature", 250, 100);
            var i = 1;
            b.Input("to", "To", i++);
            b.Input("ib", "Ib", i++);
            b.Input("st", "St", i++);
            b.Input("tb", "Tb", i++);
            for (var room = 1; room <= Rooms; room++)
            {
                var n = room.ToString(CultureInfo.InvariantCulture);
                b.Input("tt" + n, "Tt" + n, i++);
                b.Input("tc" + n, "Tc" + n, i++);
                b.Input("v" + n, "V" + n, i++);
            }

            var o = 1;
            b.Output("aqt", "AQt", o++);
            b.Output("aqf", "AQf", o++);
            b.Output("aqb", "AQb", o++);
            b.Output("qp", "Qp", o++);
            b.Output("aql", "AQl", o++);
            b.Output("aqi", "AQi", o++);
            b.Output("qe", "Qe", o);

            var p = 1;
            b.Enum("mode", p++, 0, (0, "HEATING"), (1, "COOLING"));
            b.Number("min", p++, 20);
            b.Number("max", p++, 40);
            b.Number("b", p++, 5);
            b.Number("s", p++, 0.5);
            b.Number("n", p++, 0);
            b.Number("str", p++, 35);
            b.Number("g", p++, 1);
            b.Number("i", p, 2);
        }

        public override ILogic CreateLogicInstance(ILogicContext context) => new FlowTemperatureLogic(context);
    }

    /// <summary>
    /// Flow temperature calculator of a mixing circuit for up to 6 rooms (target Tt, current Tc and valve V of each room controller).
    /// Heating: every room with demand (valve open, or below its target when V is unlinked) gets a corrected target of
    /// Tt + G * deviation, plus I while it is more than 1.5 K below (heat-up phase); its flow temperature comes from the heating curve
    /// (S, N) and the highest one wins. Cooling works the other way round with a linear cooling curve and the lowest flow.
    /// Without demand the flow is Min (heating) / Max (cooling). AQb = AQf ± B. Qp runs the pump/mixer once a valve reaches Str %
    /// (any demand when no valve is linked) and, if Tb is linked, the buffer has reached AQb. Ib forces Max (heating) / Min (cooling),
    /// St stops: Qp off and AQf/AQb at Min (heating) / Max (cooling). Qe = 1 while the outdoor temperature is missing (0 °C is used).
    /// </summary>
    public class FlowTemperatureLogic : ClimateLogic
    {
        internal const double PhaseDeviation = 1.5;

        public FlowTemperatureLogic(ILogicContext context) : base(context)
        {
        }

        protected override void Compute(IDictionary<Guid, object> outputs)
        {
            var cooling = ParamInt(F.ParamMode, 0) == 1;
            var min = Param(F.ParamMin, 20);
            var max = Param(F.ParamMax, 40);
            var low = Math.Min(min, max);
            var high = Math.Max(min, max);
            var outdoorValue = InNumber(F.InOutdoor);
            var outdoor = outdoorValue ?? 0;
            var slope = Param(F.ParamSlope, 0.5);
            var level = Param(F.ParamLevel, 0);
            var gain = Param(F.ParamGain, 1);
            var increase = Param(F.ParamPhaseIncrease, 2);
            var threshold = Param(F.ParamThreshold, 35);
            var offset = Math.Abs(Param(F.ParamBufferOffset, 5));

            outputs[F.OutError] = !outdoorValue.HasValue;

            double? best = null;
            double bestTarget = 0;
            double bestPlain = 0;
            var anyDemand = false;
            var anyValveOpen = false;
            var valves = new List<double>();

            for (var room = 0; room < F.Rooms; room++)
            {
                var valve = InNumber(F.InRoomValves[room]);
                if (valve.HasValue)
                {
                    valves.Add(valve.Value);
                    anyValveOpen |= valve.Value >= threshold;
                }
                var target = InNumber(F.InRoomTargets[room]);
                var current = InNumber(F.InRoomCurrents[room]);
                if (!target.HasValue || !current.HasValue)
                {
                    continue;
                }

                var deviation = cooling ? current.Value - target.Value : target.Value - current.Value;
                var demand = valve.HasValue ? valve.Value > 0 : deviation > 0;
                if (!demand)
                {
                    continue;
                }
                anyDemand = true;
                deviation = Math.Max(0, deviation);
                var phase = deviation > PhaseDeviation ? increase : 0;
                double flow, plain;
                if (cooling)
                {
                    var corrected = target.Value - gain * deviation - phase;
                    flow = HeatingCurveMath.Cooling(corrected, outdoor, slope, level);
                    plain = HeatingCurveMath.Cooling(target.Value, outdoor, slope, level);
                }
                else
                {
                    var corrected = target.Value + gain * deviation + phase;
                    flow = HeatingCurveMath.Heating(corrected, outdoor, slope, level);
                    plain = HeatingCurveMath.Heating(target.Value, outdoor, slope, level);
                }

                if (!best.HasValue || (cooling ? flow < best.Value : flow > best.Value))
                {
                    best = flow;
                    bestTarget = target.Value;
                    bestPlain = plain;
                }
            }

            var pump = valves.Count > 0 ? anyValveOpen : anyDemand;
            double flowSetpoint;
            if (InBool(F.InStop))
            {
                flowSetpoint = cooling ? high : low;
                pump = false;
            }
            else if (InBool(F.InBoost))
            {
                flowSetpoint = cooling ? low : high;
            }
            else if (best.HasValue)
            {
                flowSetpoint = Math.Clamp(best.Value, low, high);
            }
            else
            {
                flowSetpoint = cooling ? high : low;
            }

            var bufferSetpoint = InBool(F.InStop) ? flowSetpoint : cooling ? flowSetpoint - offset : flowSetpoint + offset;
            var buffer = InNumber(F.InBuffer);
            if (pump && buffer.HasValue)
            {
                pump = cooling ? buffer.Value <= bufferSetpoint : buffer.Value >= bufferSetpoint;
            }

            Set(outputs, F.OutFlow, flowSetpoint, 1);
            Set(outputs, F.OutBuffer, bufferSetpoint, 1);
            outputs[F.OutPump] = pump;
            if (best.HasValue)
            {
                Set(outputs, F.OutRoomTarget, bestTarget, 1);
                Set(outputs, F.OutIncrease, Math.Abs(best.Value - bestPlain), 1);
            }
            else
            {
                outputs[F.OutIncrease] = 0d;
            }
            Set(outputs, F.OutLoad, valves.Count == 0 ? 0 : valves.Average(), 1);
        }
    }
}
