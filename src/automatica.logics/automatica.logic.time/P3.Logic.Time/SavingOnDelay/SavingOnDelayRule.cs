using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;

namespace P3.Logic.Time.SavingOnDelay
{
    /// <summary>
    /// Saving switch-on delay (Loxone "Saving Switch-On Delay").
    /// A pulse on Tr is remembered: O switches on after the delay, even if Tr went back to 0, and stays on until Off.
    /// Further pulses while the delay runs or while O is on change nothing.
    /// Off = 1 switches O off, cancels a running delay and locks the block until Off is 0 again.
    /// </summary>
    public class SavingOnDelayRule : Automatica.Core.Logic.Logic
    {
        private readonly RuleInterfaceInstance _output;
        private readonly object _sync = new();
        private System.Timers.Timer _timer;

        internal double DelaySeconds = SavingOnDelayLogicFactory.DefaultDelay;

        private bool _state;
        private bool _locked;
        private bool _pending;

        public SavingOnDelayRule(ILogicContext context) : base(context)
        {
            _output = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a => a.This2RuleInterfaceTemplate == SavingOnDelayLogicFactory.RuleOutput);
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
                _pending = false;
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
            if (instance.This2RuleInterfaceTemplate == SavingOnDelayLogicFactory.RuleParamDelay)
            {
                DelaySeconds = TimeValue.ToSeconds(value, SavingOnDelayLogicFactory.DefaultDelay);
            }

            base.ParameterValueChanged(instance, source, value);
        }

        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            lock (_sync)
            {
                if (!_pending || _timer == null)
                {
                    return;
                }

                _pending = false;
                _timer.Stop();
                SwitchOn();
            }
        }

        private void SwitchOn()
        {
            _state = true;
            Context.Dispatcher.DispatchValue(new LogicOutputChanged(_output, true).Instance, true);
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

                if (template == SavingOnDelayLogicFactory.RuleOff)
                {
                    if (on)
                    {
                        _locked = true;
                        _pending = false;
                        _timer.Stop();
                        _state = false;
                        Context.Dispatcher.DispatchValue(new LogicOutputChanged(_output, false).Instance, false);
                    }
                    else
                    {
                        _locked = false;
                    }
                }
                else if (template == SavingOnDelayLogicFactory.RuleTrigger && on && !_locked && !_state && !_pending)
                {
                    if (DelaySeconds <= 0)
                    {
                        SwitchOn();
                    }
                    else
                    {
                        _pending = true;
                        _timer.Stop();
                        _timer.Interval = TimerHelper.ClampInterval(DelaySeconds * 1000);
                        _timer.Start();
                    }
                }
            }

            return new List<ILogicOutputChanged>();
        }
    }
}
