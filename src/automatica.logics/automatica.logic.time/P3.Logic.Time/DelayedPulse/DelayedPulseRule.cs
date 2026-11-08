using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;

namespace P3.Logic.Time.DelayedPulse
{
    /// <summary>
    /// Delayed pulse (Loxone "Delayed Pulse").
    /// A pulse on the input sends a pulse of the pulse duration on the output once the delay has run out.
    /// Another input pulse during the delay starts the delay again; during the output pulse it is ignored.
    /// Off = 1 cancels a pending pulse, sends 0 and locks the block until Off is 0 again.
    /// The Trigger block of the digital-to-analog plugin only sends a value after a delay, it never goes back to 0.
    /// </summary>
    public class DelayedPulseRule : Automatica.Core.Logic.Logic
    {
        internal const double MinimumPulseSeconds = 0.1;

        private enum Phase
        {
            Idle,
            Delay,
            Pulse
        }

        private readonly RuleInterfaceInstance _output;
        private readonly object _sync = new();
        private System.Timers.Timer _timer;

        internal double DelaySeconds = DelayedPulseLogicFactory.DefaultDelay;
        internal double PulseSeconds = DelayedPulseLogicFactory.DefaultPulse;

        private Phase _phase = Phase.Idle;
        private bool _locked;

        public DelayedPulseRule(ILogicContext context) : base(context)
        {
            _output = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a => a.This2RuleInterfaceTemplate == DelayedPulseLogicFactory.RuleOutput);
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
                _phase = Phase.Idle;
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
            if (instance.This2RuleInterfaceTemplate == DelayedPulseLogicFactory.RuleParamDelay)
            {
                DelaySeconds = TimeValue.ToSeconds(value, DelayedPulseLogicFactory.DefaultDelay);
            }
            else if (instance.This2RuleInterfaceTemplate == DelayedPulseLogicFactory.RuleParamPulse)
            {
                PulseSeconds = TimeValue.ToSeconds(value, DelayedPulseLogicFactory.DefaultPulse, MinimumPulseSeconds);
            }

            base.ParameterValueChanged(instance, source, value);
        }

        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            lock (_sync)
            {
                if (_timer == null)
                {
                    return;
                }

                switch (_phase)
                {
                    case Phase.Delay:
                        StartPulse();
                        break;
                    case Phase.Pulse:
                        _phase = Phase.Idle;
                        _timer.Stop();
                        Dispatch(false);
                        break;
                }
            }
        }

        private void StartPulse()
        {
            _phase = Phase.Pulse;
            Dispatch(true);
            Schedule(PulseSeconds);
        }

        private void Schedule(double seconds)
        {
            _timer.Stop();
            _timer.Interval = TimerHelper.ClampInterval(seconds * 1000);
            _timer.Start();
        }

        private void Dispatch(bool value)
        {
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

                if (template == DelayedPulseLogicFactory.RuleOff)
                {
                    if (on)
                    {
                        _locked = true;
                        _phase = Phase.Idle;
                        _timer.Stop();
                        Dispatch(false);
                    }
                    else
                    {
                        _locked = false;
                    }
                }
                else if (template == DelayedPulseLogicFactory.RuleTrigger && on && !_locked && _phase != Phase.Pulse)
                {
                    if (DelaySeconds <= 0)
                    {
                        StartPulse();
                    }
                    else
                    {
                        _phase = Phase.Delay;
                        Schedule(DelaySeconds);
                    }
                }
            }

            return new List<ILogicOutputChanged>();
        }
    }
}
