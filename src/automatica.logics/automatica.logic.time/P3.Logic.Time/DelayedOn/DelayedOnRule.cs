using System;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.Extensions.Logging;

namespace P3.Logic.Time.DelayedOn
{
    public class DelayedOnRule : Automatica.Core.Logic.Logic
    {
        private long _delay;
        private readonly RuleInterfaceInstance _output;
        private readonly System.Timers.Timer _timer;
        private bool _timerRunning = false;

        public DelayedOnRule(ILogicContext context) : base(context)
        {

            _output = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
                a.This2RuleInterfaceTemplate == DelayedOnLogicFactory.RuleOutput);

            _timer = new System.Timers.Timer();
            _timer.Elapsed += _timer_Elapsed;
        }

        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            _timer.Stop();
            _timerRunning = false;
            Context.Dispatcher.DispatchValue(new LogicOutputChanged(_output, false).Instance, true);
        }

        protected override void ParameterValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            if (instance.This2RuleInterfaceTemplate == DelayedOnLogicFactory.RuleParamDelay)
            {
                _delay = Convert.ToInt64(value);
                if (_timerRunning)
                {
                    StartStopTimer();
                }
            }
            base.ParameterValueChanged(instance, source, value);
        }

        protected override Task<bool> Stop(RuleInstance ruleInstance, CancellationToken token = default)
        {
            _timer.Elapsed -= _timer_Elapsed;
            _timer.Stop();
            _timerRunning = false;
            return base.Stop(ruleInstance, token);
        }

        private void StartStopTimer()
        {
            _timer.Stop();
            _timer.Interval = _delay * 1000;
            _timer.Start();
            _timerRunning = false;
        }

        protected override IList<ILogicOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            StartStopTimer();
            return new List<ILogicOutputChanged>();
        }
    }
}
