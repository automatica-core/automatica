using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;

namespace P3.Logic.Lightning.Common
{
    /// <summary>
    /// Base of the lighting blocks: one one-shot timer that is always set to the next deadline of the block,
    /// all decisions are made against <see cref="ILogicContext.TimeProvider"/> (local wall time), so tests can move the clock and fire <see cref="Tick"/>.
    /// Outputs are dispatched through <see cref="ILogicContext.Dispatcher"/> (also from the timer), the last values are part of <see cref="GetDataForVisu"/>.
    /// </summary>
    public abstract class ClockedLogic : Automatica.Core.Logic.Logic
    {
        protected readonly object Sync = new();
        private readonly Dictionary<Guid, object> _lastOutputs = new();
        private System.Timers.Timer _timer;
        private bool _running;

        protected ClockedLogic(ILogicContext context) : base(context)
        {
        }

        /// <summary>
        /// false: the timer is never started, only <see cref="NextTick"/> is set (tests fire <see cref="Tick"/> themselves)
        /// </summary>
        internal bool TimerEnabled { get; set; } = true;

        /// <summary>
        /// The deadline the timer is set to (local wall time), null when nothing is pending
        /// </summary>
        internal DateTime? NextTick { get; private set; }

        internal bool Running => _running;

        /// <summary>
        /// The local wall time
        /// </summary>
        protected DateTime Now => Context.TimeProvider.GetLocalNow().DateTime;

        protected RuleInterfaceInstance Interface(Guid template)
        {
            return Context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a => a.This2RuleInterfaceTemplate == template);
        }

        protected sealed override Task<bool> Start(RuleInstance instance, CancellationToken token = default)
        {
            lock (Sync)
            {
                _timer?.Dispose();
                _timer = new System.Timers.Timer { AutoReset = false };
                _timer.Elapsed += _timer_Elapsed;

                foreach (var param in instance.RuleInterfaceInstance.Where(a => a.This2RuleInterfaceTemplateNavigation?.This2RuleInterfaceDirection == (long)Automatica.Core.Base.Templates.LogicInterfaceDirection.Param))
                {
                    ApplyParameter(param.This2RuleInterfaceTemplate, param.Value);
                }

                _running = true;
                OnStart();
                Reschedule(OnTick(Now));
            }
            return Task.FromResult(true);
        }

        protected sealed override Task<bool> Stop(RuleInstance instance, CancellationToken token = default)
        {
            lock (Sync)
            {
                _running = false;
                NextTick = null;
                if (_timer != null)
                {
                    _timer.Elapsed -= _timer_Elapsed;
                    _timer.Dispose();
                    _timer = null;
                }
                OnStop();
            }
            return Task.FromResult(true);
        }

        protected sealed override void ParameterValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            lock (Sync)
            {
                ApplyParameter(instance.This2RuleInterfaceTemplate, value);
                if (_running)
                {
                    Reschedule(OnTick(Now));
                }
            }
        }

        protected sealed override IList<ILogicOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            lock (Sync)
            {
                if (!_running)
                {
                    return new List<ILogicOutputChanged>();
                }

                var now = Now;
                OnInput(instance.This2RuleInterfaceTemplate, value, source, now);
                Reschedule(OnTick(now));
            }
            return new List<ILogicOutputChanged>();
        }

        /// <summary>
        /// Runs the timer handler, as if the timer had elapsed
        /// </summary>
        internal void Tick()
        {
            _timer_Elapsed(null, null);
        }

        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            lock (Sync)
            {
                if (!_running)
                {
                    return;
                }
                Reschedule(OnTick(Now));
            }
        }

        /// <summary>
        /// Re-evaluates the block outside of an input or tick (e.g. from the visualization control interface)
        /// </summary>
        protected void Evaluate()
        {
            lock (Sync)
            {
                if (_running)
                {
                    Reschedule(OnTick(Now));
                }
            }
        }

        private void Reschedule(DateTime? next)
        {
            NextTick = next;
            if (_timer == null)
            {
                return;
            }

            _timer.Stop();
            if (next == null || !TimerEnabled)
            {
                return;
            }

            _timer.Interval = LightingValues.ClampInterval((next.Value - Now).TotalMilliseconds);
            _timer.Start();
        }

        /// <summary>
        /// A parameter value, at start (before <see cref="OnStart"/>) and when it is changed
        /// </summary>
        protected abstract void ApplyParameter(Guid template, object value);

        /// <summary>
        /// An input value changed, <see cref="OnTick"/> runs right after it
        /// </summary>
        protected abstract void OnInput(Guid template, object value, IDispatchable source, DateTime now);

        /// <summary>
        /// Handles everything that is due at <paramref name="now"/> and returns the next deadline (after now) or null
        /// </summary>
        protected abstract DateTime? OnTick(DateTime now);

        protected virtual void OnStart()
        {
        }

        protected virtual void OnStop()
        {
        }

        /// <summary>
        /// Dispatches the value on the output if it differs from the last value dispatched on it
        /// </summary>
        protected void Output(RuleInterfaceInstance output, object value)
        {
            if (output == null)
            {
                return;
            }

            if (_lastOutputs.TryGetValue(output.ObjId, out var last) && Equals(last, value))
            {
                return;
            }

            Pulse(output, value);
        }

        /// <summary>
        /// Dispatches the value on the output, also when it did not change
        /// </summary>
        protected void Pulse(RuleInterfaceInstance output, object value)
        {
            if (output == null)
            {
                return;
            }

            _lastOutputs[output.ObjId] = value;
            Context.Dispatcher.DispatchValue(new LogicInterfaceInstanceDispatchable(output), value);
        }

        protected object LastOutput(RuleInterfaceInstance output)
        {
            return output != null && _lastOutputs.TryGetValue(output.ObjId, out var v) ? v : null;
        }

        public override object GetDataForVisu()
        {
            var data = base.GetDataForVisu() as IDictionary<string, object> ?? new Dictionary<string, object>();
            lock (Sync)
            {
                foreach (var output in _lastOutputs)
                {
                    data[output.Key.ToString()] = output.Value;
                }
            }
            return data;
        }
    }
}
