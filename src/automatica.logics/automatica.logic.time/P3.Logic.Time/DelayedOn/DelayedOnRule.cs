using System;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.Extensions.Logging;

[assembly:InternalsVisibleTo("P3.Logic.Time.Tests")]

namespace P3.Logic.Time.DelayedOn
{
    public class DelayedOnRule : Automatica.Core.Logic.Logic
    {
        internal long Delay;
        private readonly RuleInterfaceInstance _output;
        private readonly System.Timers.Timer _timer;
        private bool _timerRunning;
        internal bool TriggerOnlyIfTrue = false;

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
            ExecuteAction();
        }

        private void ExecuteAction()
        {
            Context.Dispatcher.DispatchValue(new LogicOutputChanged(_output, false).Instance, true);

            Context.Logger.LogDebug($">>> Dispatching value <<<");
        }

        protected override void ParameterValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            if (instance.This2RuleInterfaceTemplate == DelayedOnLogicFactory.RuleParamDelay)
            {
                Delay = Convert.ToInt64(value);
                if (_timerRunning)
                {
                    StartStopTimer();
                }
            }
            else if (instance.This2RuleInterfaceTemplate == DelayedOnLogicFactory.TriggerOnlyIfTrue)
            {
                TriggerOnlyIfTrue = Convert.ToBoolean(value);
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

            if (Delay <= 0)
            {
                ExecuteAction();
            }
            else
            {
                _timer.Interval = Delay * 1000;
                _timer.Start();
                _timerRunning = true;
            }
        }

        protected override IList<ILogicOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            if (instance.This2RuleInterfaceTemplate == DelayedOnLogicFactory.RuleTrigger)
            {
                if (TriggerOnlyIfTrue)
                {
                    var inputValue = Convert.ToBoolean(value);

                    if (!inputValue)
                    {
                        return new List<ILogicOutputChanged>();
                    }
                }

                Context.Logger.LogDebug($">>> Starting timer - ticks in {Delay * 1000} <<<");
                StartStopTimer();
            }
            else if (instance.This2RuleInterfaceTemplate == DelayedOnLogicFactory.RuleReset)
            {
                Context.Logger.LogDebug($">>> Stopping timer <<<");
                _timer.Stop();
                _timerRunning = false;
            }
            return new List<ILogicOutputChanged>();
        }
    }
}
