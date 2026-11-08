using System;
using System.Linq;
using System.Timers;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;

namespace P3.Logic.Operations.Common
{
    /// <summary>
    /// Runs the presence simulation of a Switch/Dimmer logic: one one-shot timer set to the next <see cref="PresenceStep.Next"/>,
    /// evaluated against <see cref="ILogicContext.TimeProvider"/>; <c>apply</c> switches the logic's outputs (on the timer thread too).
    /// </summary>
    internal sealed class PresenceSimulationHost
    {
        private readonly ILogicContext _context;
        private readonly Action<bool> _apply;
        private readonly object _sync = new();
        private System.Timers.Timer _timer;
        private bool _running;

        public PresenceSimulationHost(ILogicContext context, PresenceSimulationIds ids, Action<bool> apply)
        {
            _context = context;
            _apply = apply;
            Binding = new PresenceSimulationBinding(ids);
        }

        public PresenceSimulationBinding Binding { get; }

        /// <summary>
        /// false: the timer is never started, only <see cref="NextTick"/> is set (tests fire <see cref="Tick"/> themselves)
        /// </summary>
        internal bool TimerEnabled { get; set; } = true;

        /// <summary>
        /// The deadline the timer is set to (local wall time), null when nothing is pending
        /// </summary>
        internal DateTime? NextTick { get; private set; }

        private DateTime Now => _context.TimeProvider.GetLocalNow().DateTime;

        public void Start(RuleInstance instance)
        {
            lock (_sync)
            {
                _timer?.Dispose();
                _timer = new System.Timers.Timer { AutoReset = false };
                _timer.Elapsed += _timer_Elapsed;

                foreach (var param in instance.RuleInterfaceInstance.Where(a => Binding.Ids.Contains(a.This2RuleInterfaceTemplate)))
                {
                    Binding.ApplyParameter(param.This2RuleInterfaceTemplate, param.Value);
                }

                Binding.Attach(_context.Dispatcher, instance.ObjId);
                _running = true;
                Evaluate();
            }
        }

        public void Stop()
        {
            lock (_sync)
            {
                _running = false;
                NextTick = null;
                if (_timer != null)
                {
                    _timer.Elapsed -= _timer_Elapsed;
                    _timer.Dispose();
                    _timer = null;
                }
            }
        }

        /// <returns>true when the parameter belongs to the presence simulation</returns>
        public bool ApplyParameter(Guid template, object value)
        {
            lock (_sync)
            {
                if (!Binding.ApplyParameter(template, value))
                {
                    return false;
                }
                Evaluate();
                return true;
            }
        }

        /// <returns>true when the input is the PS input</returns>
        public bool ApplyInput(Guid template, object value)
        {
            lock (_sync)
            {
                if (!Binding.ApplyInput(template, value))
                {
                    return false;
                }
                Evaluate();
                return true;
            }
        }

        /// <summary>
        /// A change made by hand (visualization, links, control API) - replayed one week later in replay mode
        /// </summary>
        public void Record(bool on)
        {
            lock (_sync)
            {
                Binding.Record(Now, on);
            }
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
            lock (_sync)
            {
                Evaluate();
            }
        }

        private void Evaluate()
        {
            if (!_running)
            {
                return;
            }

            var step = Binding.Step(Now);
            if (step.State.HasValue)
            {
                _apply(step.State.Value);
            }

            NextTick = step.Next;
            if (_timer == null)
            {
                return;
            }
            _timer.Stop();
            if (step.Next == null || !TimerEnabled)
            {
                return;
            }
            _timer.Interval = LightingValues.ClampInterval((step.Next.Value - Now).TotalMilliseconds);
            _timer.Start();
        }
    }
}
