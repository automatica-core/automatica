using System;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace P3.Logic.Time.Monoflop
{
    public class MonoflopRule : Automatica.Core.Logic.Logic
    {
        private long _delay;
        private readonly RuleInterfaceInstance _output;
        private System.Threading.Timer _timer;
        private bool _timerRunning = false;

        public MonoflopRule(ILogicContext context) : base(context)
        {
           
            _output = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
                a.This2RuleInterfaceTemplate == MonoflopLogicFactory.RuleOutput);

        }

        protected override void ParameterValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            if (instance.This2RuleInterfaceTemplate == MonoflopLogicFactory.RuleParamDelay)
            {
                _delay = Convert.ToInt64(value);
                if (_delay <= 0)
                {
                    Context.Logger.LogError($"Interval cannot be lower or equal to 0");
                    _delay = 5;
                }
                if (_timerRunning)
                {
                    StartStopTimer();
                }
            }
            base.ParameterValueChanged(instance, source, value);
        }

        protected override Task<bool> Stop(RuleInstance ruleInstance, CancellationToken token = default)
        {
            _timer?.Dispose();
            return base.Stop(ruleInstance, token);
        }

       
        private void StartStopTimer()
        {
            _timer?.Dispose();
            _timer = new System.Threading.Timer(state =>
            {
                Context.Dispatcher.DispatchValue(new LogicOutputChanged(_output, false).Instance, false);
            }, null, _delay * 1000,  -1);
           
            _timerRunning = false;
        }

        protected override IList<ILogicOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {   
            Context.Dispatcher.DispatchValue(new LogicOutputChanged(_output, false).Instance, true);

            StartStopTimer();

            return new List<ILogicOutputChanged>();
        }
    }
}
