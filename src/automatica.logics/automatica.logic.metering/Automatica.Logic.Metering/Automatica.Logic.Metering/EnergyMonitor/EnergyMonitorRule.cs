using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;

namespace Automatica.Logic.Metering.EnergyMonitor
{
    /// <summary>
    /// Balances grid, PV, battery and extra consumers (see <see cref="EnergyFlowCalculator"/>) and counts today's energy
    /// (see <see cref="EnergyDayCounter"/>). Input changes answer with the changed outputs, a timer books the held power
    /// and starts the new day at midnight even when no value comes in.
    /// </summary>
    public class EnergyMonitorRule : Automatica.Core.Logic.Logic
    {
        internal const double TickIntervalMs = 30_000;

        private readonly object _sync = new();
        private readonly EnergyFlowInputs _inputs = new();
        private readonly EnergyCircuitInputs _circuits = new();
        private readonly Dictionary<Guid, RuleInterfaceInstance> _outputs;
        /// <summary>last value sent per output, only changes are sent</summary>
        private readonly Dictionary<Guid, double> _sent = new();

        private EnergyDayCounter _counter;
        private EnergyFlowSnapshot _snapshot;
        private EnergyCircuitBreakdown _breakdown;
        private System.Timers.Timer _timer;

        public EnergyMonitorRule(ILogicContext context) : base(context)
        {
            _outputs = context.RuleInstance.RuleInterfaceInstance
                .Where(a => a.This2RuleInterfaceTemplateNavigation?.This2RuleInterfaceDirection == (long)Automatica.Core.Base.Templates.LogicInterfaceDirection.Output)
                .GroupBy(a => a.This2RuleInterfaceTemplate)
                .ToDictionary(a => a.Key, a => a.First());
            _counter = NewCounter();
            _snapshot = EnergyFlowCalculator.Calculate(_inputs);
            _breakdown = EnergyCircuitCalculator.Calculate(_circuits, _snapshot);
        }

        internal EnergyFlowSnapshot Snapshot
        {
            get { lock (_sync) { return _snapshot; } }
        }

        internal EnergyDayCounter Counter
        {
            get { lock (_sync) { return _counter; } }
        }

        internal EnergyCircuitBreakdown Breakdown
        {
            get { lock (_sync) { return _breakdown; } }
        }

        /// <summary>extra channels of the day counter: the circuits, then the rest of each group</summary>
        private EnergyDayCounter NewCounter() =>
            new(Context.TimeProvider?.LocalTimeZone, EnergyCircuitInputs.CircuitCount + EnergyCircuitInputs.GroupCount);

        private static int RestChannel(int group) => EnergyCircuitInputs.CircuitCount + group;

        private DateTimeOffset Now => Context.TimeProvider?.GetUtcNow() ?? DateTimeOffset.UtcNow;

        protected override Task<bool> Start(RuleInstance instance, CancellationToken token = default)
        {
            lock (_sync)
            {
                _counter = NewCounter();
                _counter.Advance(Now);
                HoldPower();
                _sent.Clear();

                _timer?.Dispose();
                _timer = new System.Timers.Timer(TickIntervalMs) { AutoReset = true };
                _timer.Elapsed += (_, _) => Tick();
                _timer.Start();
            }
            return base.Start(instance, token);
        }

        protected override Task<bool> Stop(RuleInstance instance, CancellationToken token = default)
        {
            lock (_sync)
            {
                _timer?.Stop();
                _timer?.Dispose();
                _timer = null;
            }
            return base.Stop(instance, token);
        }

        protected override void ParameterValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            lock (_sync)
            {
                if (instance.This2RuleInterfaceTemplate == EnergyMonitorLogicFactory.ParamInvertGrid)
                {
                    _inputs.InvertGrid = ToBool(value);
                    Recalculate();
                }
                else if (instance.This2RuleInterfaceTemplate == EnergyMonitorLogicFactory.ParamInvertBattery)
                {
                    _inputs.InvertBattery = ToBool(value);
                    Recalculate();
                }
                else if (Array.IndexOf(EnergyMonitorLogicFactory.ParamCircuitGroups, instance.This2RuleInterfaceTemplate) is var group and >= 0)
                {
                    _circuits.Group[group] = EnergyCircuitInputs.NormalizeGroup(ToDouble(value));
                    Recalculate();
                }
                else if (Array.IndexOf(EnergyMonitorLogicFactory.ParamCircuitNames, instance.This2RuleInterfaceTemplate) is var name and >= 0)
                {
                    _circuits.Name[name] = value?.ToString()?.Trim();
                    Recalculate();
                }
            }
            base.ParameterValueChanged(instance, source, value);
        }

        protected override IList<ILogicOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            lock (_sync)
            {
                var id = instance.This2RuleInterfaceTemplate;
                var number = ToDouble(value);

                if (TryMeter(id, out var flow))
                {
                    if (number.HasValue)
                    {
                        _counter.SetMeter(flow, number.Value, Now);
                    }
                    return Changes();
                }

                if (id == EnergyMonitorLogicFactory.InputGrid) _inputs.Grid = number;
                else if (id == EnergyMonitorLogicFactory.InputGridImport) _inputs.GridImport = number;
                else if (id == EnergyMonitorLogicFactory.InputGridExport) _inputs.GridExport = number;
                else if (id == EnergyMonitorLogicFactory.InputPv) _inputs.Pv = number;
                else if (id == EnergyMonitorLogicFactory.InputBattery) _inputs.Battery = number;
                else if (id == EnergyMonitorLogicFactory.InputSoc) _inputs.Soc = number;
                else if (Array.IndexOf(EnergyMonitorLogicFactory.InputConsumers, id) is var consumer and >= 0)
                {
                    _inputs.Consumers[consumer] = number;
                }
                else if (Array.IndexOf(EnergyMonitorLogicFactory.InputCircuits, id) is var circuit and >= 0)
                {
                    _circuits.Power[circuit] = number;
                }
                else
                {
                    return new List<ILogicOutputChanged>();
                }

                Recalculate();
                return Changes();
            }
        }

        /// <summary>books the held power, also starts the new day after midnight; sends what changed</summary>
        internal void Tick()
        {
            IList<ILogicOutputChanged> changes;
            lock (_sync)
            {
                _counter.Advance(Now);
                changes = Changes();
            }

            foreach (var change in changes)
            {
                Context.Dispatcher.DispatchValue(change.Instance, change.Value);
            }
        }

        public override object GetDataForVisu()
        {
            var data = base.GetDataForVisu() as Dictionary<string, object> ?? new Dictionary<string, object>();
            lock (_sync)
            {
                // the timer sends its values past the base class, so all current outputs are added here
                foreach (var (template, value) in CurrentValues())
                {
                    if (value.HasValue && _outputs.TryGetValue(template, out var output))
                    {
                        data[output.ObjId.ToString()] = value.Value;
                    }
                }

                data["hasBattery"] = _inputs.Battery.HasValue || _inputs.Soc.HasValue;
                for (var i = 0; i < EnergyFlowInputs.ConsumerCount; i++)
                {
                    data[$"hasConsumer{i + 1}"] = _inputs.Consumers[i].HasValue;
                }

                // today's energy of the circuits is up to date at the moment the popup asks
                _counter.Advance(Now);
                data["circuitGroups"] = CircuitGroupsForVisu();
            }
            return data;
        }

        /// <summary>
        /// The breakdown for the details popup of the tile: per group (0 = house, 1-6 = consumer) its total, the rest that is
        /// not measured and its circuits, all with today's energy. Groups without circuits are left out.
        /// </summary>
        internal List<Dictionary<string, object>> CircuitGroupsForVisu()
        {
            var c = _counter;
            return _breakdown.Groups.Values.OrderBy(g => g.Group).Select(g => new Dictionary<string, object>
            {
                ["group"] = g.Group,
                ["total"] = Power(g.Total),
                ["measured"] = Power(g.Measured),
                ["rest"] = Power(g.Rest),
                ["restToday"] = Power(c.TodayExtra(RestChannel(g.Group))),
                ["circuits"] = g.Circuits.Select(i => new Dictionary<string, object>
                {
                    ["circuit"] = i + 1,
                    ["name"] = string.IsNullOrWhiteSpace(_circuits.Name[i]) ? null : _circuits.Name[i],
                    ["hasValue"] = _circuits.Power[i].HasValue,
                    ["power"] = Power(_breakdown.Power[i]),
                    ["today"] = Power(c.TodayExtra(i))
                }).ToList()
            }).ToList();
        }

        private void Recalculate()
        {
            // the power held so far is booked before the new one applies
            _counter.Advance(Now);
            _snapshot = EnergyFlowCalculator.Calculate(_inputs);
            _breakdown = EnergyCircuitCalculator.Calculate(_circuits, _snapshot);
            HoldPower();
        }

        /// <summary>the flows, the circuits and the rests the day counter books from now on</summary>
        private void HoldPower()
        {
            _counter.SetPower(_snapshot);
            for (var i = 0; i < EnergyCircuitInputs.CircuitCount; i++)
            {
                _counter.SetExtraPower(i, _breakdown.Power[i]);
            }
            for (var g = 0; g < EnergyCircuitInputs.GroupCount; g++)
            {
                _counter.SetExtraPower(RestChannel(g), _breakdown.Rest(g) ?? 0);
            }
        }

        private IList<ILogicOutputChanged> Changes()
        {
            var ret = new List<ILogicOutputChanged>();
            foreach (var (template, value) in CurrentValues())
            {
                if (!value.HasValue || !_outputs.TryGetValue(template, out var output))
                {
                    continue;
                }
                if (_sent.TryGetValue(template, out var last) && last.Equals(value.Value))
                {
                    continue;
                }
                _sent[template] = value.Value;
                ret.Add(new LogicOutputChanged(output, value.Value));
            }
            return ret;
        }

        /// <summary>all outputs, rounded: power and energy to 3 decimals (1 W / 1 Wh), % to 1 decimal</summary>
        private IEnumerable<(Guid Template, double? Value)> CurrentValues()
        {
            var s = _snapshot;
            yield return (EnergyMonitorLogicFactory.OutputGrid, Power(s.Grid));
            yield return (EnergyMonitorLogicFactory.OutputPv, Power(s.Pv));
            yield return (EnergyMonitorLogicFactory.OutputBattery, Power(s.Battery));
            yield return (EnergyMonitorLogicFactory.OutputSoc, s.Soc.HasValue ? Percent(s.Soc.Value) : null);
            for (var i = 0; i < EnergyMonitorLogicFactory.OutputConsumers.Length; i++)
            {
                yield return (EnergyMonitorLogicFactory.OutputConsumers[i], Power(s.Consumers[i]));
            }
            yield return (EnergyMonitorLogicFactory.OutputHouse, Power(s.House));
            yield return (EnergyMonitorLogicFactory.OutputConsumption, Power(s.Consumption));
            yield return (EnergyMonitorLogicFactory.OutputSelfConsumption, Percent(s.SelfConsumption));
            yield return (EnergyMonitorLogicFactory.OutputAutarky, Percent(s.Autarky));

            var c = _counter;
            yield return (EnergyMonitorLogicFactory.OutputPvToday, Power(c.Today(EnergyFlowKind.Pv)));
            yield return (EnergyMonitorLogicFactory.OutputImportToday, Power(c.Today(EnergyFlowKind.GridImport)));
            yield return (EnergyMonitorLogicFactory.OutputExportToday, Power(c.Today(EnergyFlowKind.GridExport)));
            yield return (EnergyMonitorLogicFactory.OutputChargeToday, Power(c.Today(EnergyFlowKind.BatteryCharge)));
            yield return (EnergyMonitorLogicFactory.OutputDischargeToday, Power(c.Today(EnergyFlowKind.BatteryDischarge)));
            yield return (EnergyMonitorLogicFactory.OutputConsumptionToday, Power(c.ConsumptionToday));
            yield return (EnergyMonitorLogicFactory.OutputSelfConsumptionToday, Percent(c.SelfConsumptionToday));
            yield return (EnergyMonitorLogicFactory.OutputAutarkyToday, Percent(c.AutarkyToday));

            for (var g = 0; g < EnergyMonitorLogicFactory.OutputRests.Length; g++)
            {
                var rest = _breakdown.Rest(g);
                yield return (EnergyMonitorLogicFactory.OutputRests[g], rest.HasValue ? Power(rest.Value) : null);
            }
        }

        private static double Power(double value) => Math.Round(value, 3) + 0d; // + 0 turns -0 into 0

        private static double Percent(double value) => Math.Round(value, 1) + 0d;

        private static bool TryMeter(Guid id, out EnergyFlowKind flow)
        {
            if (id == EnergyMonitorLogicFactory.InputPvMeter) flow = EnergyFlowKind.Pv;
            else if (id == EnergyMonitorLogicFactory.InputImportMeter) flow = EnergyFlowKind.GridImport;
            else if (id == EnergyMonitorLogicFactory.InputExportMeter) flow = EnergyFlowKind.GridExport;
            else if (id == EnergyMonitorLogicFactory.InputChargeMeter) flow = EnergyFlowKind.BatteryCharge;
            else if (id == EnergyMonitorLogicFactory.InputDischargeMeter) flow = EnergyFlowKind.BatteryDischarge;
            else
            {
                flow = default;
                return false;
            }
            return true;
        }

        internal static double? ToDouble(object value)
        {
            switch (value)
            {
                case null:
                    return null;
                case bool b:
                    return b ? 1 : 0;
                case string s:
                    return double.TryParse(s.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) && double.IsFinite(parsed)
                        ? parsed
                        : null;
                default:
                    try
                    {
                        var d = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                        return double.IsFinite(d) ? d : null;
                    }
                    catch (Exception)
                    {
                        return null;
                    }
            }
        }

        internal static bool ToBool(object value)
        {
            return value switch
            {
                bool b => b,
                string s when bool.TryParse(s, out var b) => b,
                string s => s.Trim() == "1",
                null => false,
                _ => ToDouble(value) is { } d && d != 0
            };
        }
    }
}
