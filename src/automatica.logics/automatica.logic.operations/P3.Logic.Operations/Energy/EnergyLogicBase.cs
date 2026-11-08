using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;

namespace P3.Logic.Operations.Energy
{
    /// <summary>
    /// Base of the energy logics: evaluates on every input and once per second (delays), under one lock,
    /// and dispatches only the outputs that changed.
    /// </summary>
    public abstract class EnergyLogicBase : Automatica.Core.Logic.Logic
    {
        internal const double TickMilliseconds = 1000;

        private readonly Dictionary<Guid, RuleInterfaceInstance> _interfaces = new();
        private readonly Dictionary<Guid, object> _dispatched = new();
        private System.Timers.Timer _timer;
        private bool _running;

        protected EnergyLogicBase(ILogicContext context) : base(context)
        {
            foreach (var item in context.RuleInstance.RuleInterfaceInstance)
            {
                _interfaces[item.This2RuleInterfaceTemplate] = item;
            }
        }

        protected object Sync { get; } = new();

        /// <summary>
        /// The clock, replaced by the tests
        /// </summary>
        internal Func<DateTimeOffset> Clock { get; set; }

        protected DateTimeOffset Now() => Clock?.Invoke() ?? Context.TimeProvider.GetUtcNow();

        internal bool Running => _running;

        protected override Task<bool> Start(RuleInstance instance, CancellationToken token = default)
        {
            lock (Sync)
            {
                _running = true;
                _dispatched.Clear();
                _timer = new System.Timers.Timer(TickMilliseconds) { AutoReset = true };
                _timer.Elapsed += TimerElapsed;
                _timer.Start();
            }
            return base.Start(instance, token);
        }

        protected override Task<bool> Stop(RuleInstance instance, CancellationToken token = default)
        {
            lock (Sync)
            {
                _running = false;
                if (_timer != null)
                {
                    _timer.Elapsed -= TimerElapsed;
                    _timer.Dispose();
                    _timer = null;
                }
            }
            return base.Stop(instance, token);
        }

        private void TimerElapsed(object sender, ElapsedEventArgs e) => Tick();

        /// <summary>
        /// One timer step (called by the tests instead of the timer)
        /// </summary>
        internal void Tick()
        {
            lock (Sync)
            {
                if (_running)
                {
                    EvaluateAndDispatch();
                }
            }
        }

        protected override IList<ILogicOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            lock (Sync)
            {
                ApplyInput(instance.This2RuleInterfaceTemplate, value);
                if (_running)
                {
                    EvaluateAndDispatch();
                }
            }
            return new List<ILogicOutputChanged>();
        }

        protected override void ParameterValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            lock (Sync)
            {
                ApplyParameter(instance.This2RuleInterfaceTemplate, value);
            }
            base.ParameterValueChanged(instance, source, value);
        }

        protected abstract void ApplyInput(Guid template, object value);

        protected abstract void ApplyParameter(Guid template, object value);

        /// <summary>
        /// Evaluates at <see cref="Now"/> and dispatches the outputs, called under <see cref="Sync"/>
        /// </summary>
        protected abstract void EvaluateAndDispatch();

        protected static TimeSpan Seconds(object value, double fallback) => TimeSpan.FromSeconds(Math.Min(EnergyConvert.ToNonNegative(value, fallback), 7 * 86400));

        protected void Dispatch(Guid template, object value)
        {
            if (_dispatched.TryGetValue(template, out var last) && Equals(last, value))
            {
                return;
            }
            if (!_interfaces.TryGetValue(template, out var output))
            {
                return;
            }
            _dispatched[template] = value;
            Context.Dispatcher.DispatchValue(new LogicOutputChanged(output, value).Instance, value);
        }
    }
}
