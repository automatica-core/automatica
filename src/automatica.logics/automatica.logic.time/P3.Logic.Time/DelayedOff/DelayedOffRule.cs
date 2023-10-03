using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using Microsoft.Extensions.Logging;

namespace P3.Logic.Time.DelayedOff
{
    public class DelayedOffRule : Automatica.Core.Logic.Logic
    {
        private long _delay;
        private readonly RuleInterfaceInstance _output;
        private readonly System.Timers.Timer _timer;

        private bool _timerRunning = false;
    

        public DelayedOffRule(ILogicContext context) : base(context)
        {

            _output = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
                a.This2RuleInterfaceTemplate == DelayedOffLogicFactory.RuleOutput);

            _timer = new System.Timers.Timer();
            _timer.Elapsed += _timer_Elapsed;
        }

        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            _timer.Stop();
            _timerRunning = false;
            Context.Dispatcher.DispatchValue(new LogicOutputChanged(_output, false).Instance, false);

            Context.Logger.LogDebug($">>> Dispatching value <<<");
        }

        protected override Task<bool> Stop(RuleInstance ruleInstance, CancellationToken token = default)
        {
            _timer.Elapsed -= _timer_Elapsed;
            _timer.Stop();
            _timerRunning = false;
            return base.Stop(ruleInstance, token);
        }

        protected override Task<bool> Start(RuleInstance ruleInstance, CancellationToken token = default)
        {
            _delay = Context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
                a.This2RuleInterfaceTemplate == DelayedOffLogicFactory.RuleParamDelay).ValueInteger.Value;

            if (_delay <= 0)
            {
                Context.Logger.LogError($"Interval cannot be lower or equal to 0");
                return Task.FromResult(false);
            }
            return base.Start(ruleInstance, token);
        }

        protected override void ParameterValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            if (instance.This2RuleInterfaceTemplate == DelayedOffLogicFactory.RuleParamDelay)
            {
                _delay = Convert.ToInt64(value);
                if (_timerRunning)
                {
                    StartStopTimer();
                }
            }
            base.ParameterValueChanged(instance, source, value);
        }

        private void StartStopTimer()
        {
            _timer.Stop();
            _timer.Interval = _delay * 1000;
            _timer.Start();
            _timerRunning = true;
        }

        protected override IList<ILogicOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            if (instance.This2RuleInterfaceTemplate == DelayedOffLogicFactory.RuleTrigger)
            {
                if (!Convert.ToBoolean(value))
                {
                    Context.Logger.LogDebug($">>> Stoping timer <<<");
                    _timer.Stop();
                    return new List<ILogicOutputChanged>();
                }
                Context.Logger.LogDebug($">>> Starting timer - ticks in {_delay * 1000} <<<");
                StartStopTimer();
            }
            else if (instance.This2RuleInterfaceTemplate == DelayedOffLogicFactory.RuleReset)
            {
                Context.Logger.LogDebug($">>> Stoping timer <<<");
                _timer.Stop();
                _timerRunning = false;
            }
            return new List<ILogicOutputChanged>();
        }
    }
}
