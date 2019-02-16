using System;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Rule;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;

namespace P3.Rule.Time.DelayedOn
{
    public class DelayedOnRule : Automatica.Core.Rule.Rule
    {
        private long _delay;
        private readonly RuleInterfaceInstance _output;
        private readonly System.Timers.Timer _timer;
        private bool _timerRunning = false;

        public DelayedOnRule(IRuleContext context) : base(context)
        {

            _output = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
                a.This2RuleInterfaceTemplate == DelayedOnRuleFactory.RuleOutput);

            _timer = new System.Timers.Timer();
            _timer.Elapsed += _timer_Elapsed;
        }

        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            _timer.Stop();
            _timerRunning = false;
            Context.Dispatcher.DispatchValue(new RuleOutputChanged(_output, false).Instance, true);
        }

        public override Task<bool> Start()
        {
            _delay = Context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
                a.This2RuleInterfaceTemplate == DelayedOnRuleFactory.RuleParamDelay).ValueInteger.Value;
            return base.Start();
        }

        protected override void ParamterValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            if (instance.This2RuleInterfaceTemplate == DelayedOnRuleFactory.RuleParamDelay)
            {
                _delay = Convert.ToInt64(value);
                if (_timerRunning)
                {
                    StartStopTimer();
                }
            }
            base.ParamterValueChanged(instance, source, value);
        }

        public override Task<bool> Stop()
        {
            _timer.Elapsed -= _timer_Elapsed;
            _timer.Stop();
            _timerRunning = false;
            return base.Stop();
        }

        private void StartStopTimer()
        {
            _timer.Stop();
            _timer.Interval = _delay * 1000;
            _timer.Start();
            _timerRunning = false;
        }

        protected override IList<IRuleOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            StartStopTimer();
            return new List<IRuleOutputChanged>();
        }
    }
}
