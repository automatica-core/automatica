using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Rule;
using Microsoft.Extensions.Logging;

namespace P3.Rule.Time.DelayedOff
{
    public class DelayedOffRule : Automatica.Core.Rule.Rule
    {
        private long _delay;
        private readonly RuleInterfaceInstance _output;
        private readonly System.Timers.Timer _timer;

        private bool _timerRunning = false;
    

        public DelayedOffRule(IRuleContext context) : base(context)
        {

            _output = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
                a.This2RuleInterfaceTemplate == DelayedOffRuleFactory.RuleOutput);

            _timer = new System.Timers.Timer();
            _timer.Elapsed += _timer_Elapsed;
        }

        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            _timer.Stop();
            _timerRunning = false;
            Context.Dispatcher.DispatchValue(new RuleOutputChanged(_output, false).Instance, false);

            Context.Logger.LogDebug($">>> Dispatching value <<<");
        }

        public override Task<bool> Stop()
        {
            _timer.Elapsed -= _timer_Elapsed;
            _timer.Stop();
            _timerRunning = false;
            return base.Stop();
        }

        public override Task<bool> Start()
        {
            _delay = Context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
                a.This2RuleInterfaceTemplate == DelayedOffRuleFactory.RuleParamDelay).ValueInteger.Value;
            return base.Start();
        }

        protected override void ParamterValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            if (instance.This2RuleInterfaceTemplate == DelayedOffRuleFactory.RuleParamDelay)
            {
                _delay = Convert.ToInt64(value);
                if (_timerRunning)
                {
                    StartStopTimer();
                }
            }
            base.ParamterValueChanged(instance, source, value);
        }

        private void StartStopTimer()
        {
            _timer.Stop();
            _timer.Interval = _delay * 1000;
            _timer.Start();
            _timerRunning = true;
        }

        protected override IList<IRuleOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            if (instance.This2RuleInterfaceTemplate == DelayedOffRuleFactory.RuleTrigger)
            {
                if (!Convert.ToBoolean(value))
                {
                    Context.Logger.LogDebug($">>> Stoping timer <<<");
                    _timer.Stop();
                    return new List<IRuleOutputChanged>();
                }
                Context.Logger.LogDebug($">>> Starting timer - ticks in {_delay * 1000} <<<");
                StartStopTimer();
            }
            else if (instance.This2RuleInterfaceTemplate == DelayedOffRuleFactory.RuleReset)
            {
                Context.Logger.LogDebug($">>> Stoping timer <<<");
                _timer.Stop();
                _timerRunning = false;
            }
            return new List<IRuleOutputChanged>();
        }
    }
}
