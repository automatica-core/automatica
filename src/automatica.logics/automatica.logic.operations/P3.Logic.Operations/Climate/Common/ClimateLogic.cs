using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Automatica.Core.Base.Calendar;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;

[assembly: InternalsVisibleTo("P3.Logic.Operations.Tests")]

namespace P3.Logic.Operations.Climate.Common
{
    public readonly record struct DemandLoads(double Heat, double Cool, double MaxHeat, double MaxCool, int Rooms);

    /// <summary>
    /// Base of the climate blocks. The derived block keeps its state, <see cref="Compute"/> writes the wanted value of every output,
    /// only outputs whose value changed are sent. Blocks with timers set <see cref="TickSeconds"/>: <see cref="Tick"/> runs on a
    /// timer and its changes go out through <see cref="ILogicContext.Dispatcher"/>.
    /// Inputs that never received a value count as unlinked, see <see cref="HasInput"/>.
    /// </summary>
    public abstract class ClimateLogic : Automatica.Core.Logic.Logic
    {
        protected readonly object Sync = new();

        private readonly Dictionary<Guid, RuleInterfaceInstance> _interfaces = new();
        private readonly Dictionary<Guid, object> _inputs = new();
        private readonly Dictionary<Guid, object> _params = new();
        private readonly Dictionary<Guid, object> _lastOutputs = new();
        private System.Timers.Timer _timer;
        private DateTime? _lastTick;

        /// <summary>
        /// The clock, UTC. Tests replace it - the shared fake time provider of the test base is static.
        /// </summary>
        internal Func<DateTime> Clock { get; set; } = () => DateTimeHelper.ProviderInstance.GetUtcNow().UtcDateTime;

        protected DateTime Now => Clock();

        protected ClimateLogic(ILogicContext context) : base(context)
        {
            foreach (var instance in context.RuleInstance.RuleInterfaceInstance)
            {
                _interfaces[instance.This2RuleInterfaceTemplate] = instance;
            }
        }

        /// <summary>
        /// 0 = the block has no timer
        /// </summary>
        protected virtual double TickSeconds => 0;

        /// <summary>
        /// Writes the wanted value of the outputs. Called under <see cref="Sync"/>.
        /// </summary>
        protected abstract void Compute(IDictionary<Guid, object> outputs);

        /// <summary>
        /// An input changed (edge detection, timestamps). Called under <see cref="Sync"/> before <see cref="Compute"/>.
        /// </summary>
        protected virtual void OnInput(Guid template, object value, object previous)
        {
        }

        /// <summary>
        /// Time based work, <paramref name="elapsedSeconds"/> since the last tick (0 on the first one). Called under <see cref="Sync"/>.
        /// </summary>
        protected virtual void OnTick(double elapsedSeconds)
        {
        }

        protected bool HasInput(Guid template) => _inputs.TryGetValue(template, out var v) && v != null;

        protected object RawInput(Guid template) => _inputs.TryGetValue(template, out var v) ? v : null;

        protected double? InNumber(Guid template) => ClimateValue.ToDouble(RawInput(template));

        protected bool InBool(Guid template, bool unlinked = false) => HasInput(template) ? ClimateValue.ToBool(RawInput(template)) : unlinked;

        protected int InInt(Guid template, int unlinked) => ClimateValue.ToInt(RawInput(template)) ?? unlinked;

        protected double Param(Guid template, double fallback) => ClimateValue.ToDouble(_params.TryGetValue(template, out var v) ? v : null) ?? fallback;

        protected int ParamInt(Guid template, int fallback) => ClimateValue.ToInt(_params.TryGetValue(template, out var v) ? v : null) ?? fallback;

        protected bool ParamBool(Guid template, bool fallback) => _params.TryGetValue(template, out var v) && v != null ? ClimateValue.ToBool(v) : fallback;

        /// <summary>
        /// The last value sent on an output, null before the first one
        /// </summary>
        internal object LastOutput(Guid template)
        {
            lock (Sync)
            {
                return _lastOutputs.TryGetValue(template, out var v) ? v : null;
            }
        }

        protected sealed override IList<ILogicOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            lock (Sync)
            {
                var template = instance.This2RuleInterfaceTemplate;
                _inputs.TryGetValue(template, out var previous);
                _inputs[template] = value;
                OnInput(template, value, previous);
                return Evaluate();
            }
        }

        protected override void ParameterValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            IList<ILogicOutputChanged> changes;
            lock (Sync)
            {
                _params[instance.This2RuleInterfaceTemplate] = value;
                changes = Evaluate();
            }
            base.ParameterValueChanged(instance, source, value);

            // a changed setpoint or limit applies at once, not only with the next input value
            foreach (var change in changes)
            {
                Context.Dispatcher?.DispatchValue(change.Instance, change.Value);
            }
        }

        /// <summary>
        /// One timer step: time based work and the changed outputs
        /// </summary>
        internal IList<ILogicOutputChanged> Tick()
        {
            lock (Sync)
            {
                var now = Now;
                var elapsed = _lastTick.HasValue ? Math.Max(0, (now - _lastTick.Value).TotalSeconds) : 0;
                _lastTick = now;
                OnTick(elapsed);
                return Evaluate();
            }
        }

        private IList<ILogicOutputChanged> Evaluate()
        {
            var wanted = new Dictionary<Guid, object>();
            Compute(wanted);

            var ret = new List<ILogicOutputChanged>();
            foreach (var (template, value) in wanted)
            {
                if (value == null || !_interfaces.TryGetValue(template, out var instance))
                {
                    continue;
                }
                if (_lastOutputs.TryGetValue(template, out var last) && Equals(last, value))
                {
                    continue;
                }
                _lastOutputs[template] = value;
                ret.Add(new LogicOutputChanged(instance, value));
            }
            return ret;
        }

        public override object GetDataForVisu()
        {
            var data = base.GetDataForVisu();
            if (data is IDictionary<string, object> dict)
            {
                lock (Sync)
                {
                    // values sent by the timer are not known to the base class
                    foreach (var (template, value) in _lastOutputs)
                    {
                        if (_interfaces.TryGetValue(template, out var instance))
                        {
                            dict[instance.ObjId.ToString()] = value;
                        }
                    }
                }
            }
            return data;
        }

        protected override Task<bool> Start(RuleInstance instance, CancellationToken token = default)
        {
            StopTimer();
            if (TickSeconds > 0)
            {
                _timer = new System.Timers.Timer(TickSeconds * 1000) { AutoReset = true };
                _timer.Elapsed += TimerElapsed;
                _timer.Start();
            }
            return base.Start(instance, token);
        }

        protected override Task<bool> Stop(RuleInstance instance, CancellationToken token = default)
        {
            StopTimer();
            return base.Stop(instance, token);
        }

        private void StopTimer()
        {
            if (_timer != null)
            {
                _timer.Elapsed -= TimerElapsed;
                _timer.Dispose();
                _timer = null;
            }
        }

        private void TimerElapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                foreach (var change in Tick())
                {
                    Context.Dispatcher.DispatchValue(change.Instance, change.Value);
                }
            }
            catch (Exception ex)
            {
                Microsoft.Extensions.Logging.LoggerExtensions.LogError(Context.Logger, ex, "climate block tick failed");
            }
        }

        protected static void Set(IDictionary<Guid, object> outputs, Guid template, double value, int digits = 2) => outputs[template] = ClimateValue.Round(value, digits);

        protected static double Clamp(double value, double a, double b) => Math.Clamp(value, Math.Min(a, b), Math.Max(a, b));

        /// <summary>
        /// Loads of signed room demands (+ heating, - cooling, in %): the average heating and cooling demand over all rooms that
        /// sent a value, and the largest single heating and cooling demand
        /// </summary>
        protected DemandLoads Loads(IEnumerable<Guid> demandInputs)
        {
            var values = demandInputs.Select(InNumber).Where(a => a.HasValue).Select(a => a.Value).ToList();
            if (values.Count == 0)
            {
                return new DemandLoads(0, 0, 0, 0, 0);
            }
            return new DemandLoads(
                values.Sum(a => Math.Max(0, a)) / values.Count,
                values.Sum(a => Math.Max(0, -a)) / values.Count,
                values.Max(a => Math.Max(0, a)),
                values.Max(a => Math.Max(0, -a)),
                values.Count);
        }

        /// <summary>
        /// Average of the given numbers, null when none is known
        /// </summary>
        protected static double? Average(IEnumerable<double?> values)
        {
            var known = values.Where(a => a.HasValue).Select(a => a.Value).ToList();
            return known.Count == 0 ? null : known.Average();
        }
    }
}
