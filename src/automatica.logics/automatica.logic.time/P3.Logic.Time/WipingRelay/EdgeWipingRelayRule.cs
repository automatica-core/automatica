using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;

namespace P3.Logic.Time.WipingRelay
{
    /// <summary>
    /// Edge-triggered wiping relay (Loxone "Edge-Triggered Wiping Relay").
    /// Every pulse on Tr sends a burst of C pulses on P: 1 for the on time, 0 for the off time.
    /// A pulse during a running burst starts the burst again from the first pulse.
    /// Off = 1 ends the burst with 0 and locks the block until Off is 0 again.
    /// </summary>
    public class EdgeWipingRelayRule : Automatica.Core.Logic.Logic
    {
        internal const double MinimumSeconds = 0.1;

        private readonly RuleInterfaceInstance _output;
        private readonly object _sync = new();
        private System.Timers.Timer _timer;

        internal double OnSeconds = EdgeWipingRelayLogicFactory.DefaultOn;
        internal double OffSeconds = EdgeWipingRelayLogicFactory.DefaultOff;
        internal int Cycles = EdgeWipingRelayLogicFactory.DefaultCycles;

        private bool _running;
        private bool _state;
        private bool _locked;
        private int _pulsesDone;

        public EdgeWipingRelayRule(ILogicContext context) : base(context)
        {
            _output = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a => a.This2RuleInterfaceTemplate == EdgeWipingRelayLogicFactory.RuleOutput);
        }

        protected override Task<bool> Start(RuleInstance instance, CancellationToken token = default)
        {
            _timer = new System.Timers.Timer { AutoReset = false };
            _timer.Elapsed += _timer_Elapsed;
            return base.Start(instance, token);
        }

        protected override Task<bool> Stop(RuleInstance instance, CancellationToken token = default)
        {
            lock (_sync)
            {
                _running = false;
                if (_timer != null)
                {
                    _timer.Elapsed -= _timer_Elapsed;
                    _timer.Dispose();
                    _timer = null;
                }
            }
            return base.Stop(instance, token);
        }

        protected override void ParameterValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            var template = instance.This2RuleInterfaceTemplate;
            if (template == EdgeWipingRelayLogicFactory.RuleParamOn)
            {
                OnSeconds = TimeValue.ToSeconds(value, EdgeWipingRelayLogicFactory.DefaultOn, MinimumSeconds);
            }
            else if (template == EdgeWipingRelayLogicFactory.RuleParamOff)
            {
                OffSeconds = TimeValue.ToSeconds(value, EdgeWipingRelayLogicFactory.DefaultOff, MinimumSeconds);
            }
            else if (template == EdgeWipingRelayLogicFactory.RuleParamCycles)
            {
                var cycles = TimeValue.ToDouble(value) ?? EdgeWipingRelayLogicFactory.DefaultCycles;
                Cycles = (int)System.Math.Max(1, System.Math.Min(cycles, int.MaxValue));
            }

            base.ParameterValueChanged(instance, source, value);
        }

        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            lock (_sync)
            {
                if (!_running || _timer == null)
                {
                    return;
                }

                if (_state)
                {
                    Dispatch(false);
                    _pulsesDone++;
                    if (_pulsesDone >= Cycles)
                    {
                        _running = false;
                        _timer.Stop();
                        return;
                    }
                    Schedule(OffSeconds);
                }
                else
                {
                    Dispatch(true);
                    Schedule(OnSeconds);
                }
            }
        }

        private void Schedule(double seconds)
        {
            _timer.Stop();
            _timer.Interval = TimerHelper.ClampInterval(seconds * 1000);
            _timer.Start();
        }

        private void Dispatch(bool value)
        {
            _state = value;
            Context.Dispatcher.DispatchValue(new LogicOutputChanged(_output, value).Instance, value);
        }

        protected override IList<ILogicOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            var template = instance.This2RuleInterfaceTemplate;
            var on = TimerHelper.IsTruthy(value);
            lock (_sync)
            {
                if (_timer == null)
                {
                    return new List<ILogicOutputChanged>();
                }

                if (template == EdgeWipingRelayLogicFactory.RuleOff)
                {
                    if (on)
                    {
                        _locked = true;
                        _running = false;
                        _timer.Stop();
                        Dispatch(false);
                    }
                    else
                    {
                        _locked = false;
                    }
                }
                else if (template == EdgeWipingRelayLogicFactory.RuleTrigger && on && !_locked)
                {
                    _running = true;
                    _pulsesDone = 0;
                    Dispatch(true);
                    Schedule(OnSeconds);
                }
            }

            return new List<ILogicOutputChanged>();
        }
    }
}
