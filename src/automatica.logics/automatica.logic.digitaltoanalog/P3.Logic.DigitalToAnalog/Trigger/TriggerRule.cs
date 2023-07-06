using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using Microsoft.Extensions.Logging;

namespace P3.Logic.DigitalToAnalog.Trigger
{
    public class TriggerRule : Automatica.Core.Logic.Logic
    {
        private readonly ILogicContext _context;

        private object _outputValue;
        
        private readonly RuleInterfaceInstance _output1;
        private readonly RuleInterfaceInstance _paramDelay;

        public TriggerRule(ILogicContext context) : base(context)
        {
            _context = context;
            _output1 = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
                a.This2RuleInterfaceTemplate == TriggerLogicFactory.RuleOutput);

            _paramDelay = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
                a.This2RuleInterfaceTemplate == TriggerLogicFactory.DelayParameter);

            var paramValue = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
                a.This2RuleInterfaceTemplate == TriggerLogicFactory.ValueParameter);

            if (paramValue != null) _outputValue = paramValue.Value;
        }

        public int Delay
        {
            get
            {
                if (_paramDelay != null && _paramDelay.ValueInteger.HasValue)
                {
                    return (int)_paramDelay.ValueInteger.Value;
                }

                return 0;
            }
        }

        protected override IList<ILogicOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            if (_outputValue == null)
            {
                Context.Logger.LogError($"{Context.RuleInstance.Name} OutputValue is {null}, we ignore any incoming value..");
                return new List<ILogicOutputChanged>();
            }
            if (value != null && instance.This2RuleInterfaceTemplate == TriggerLogicFactory.RuleInput && Boolean.TryParse(value.ToString(), out bool bValue) && bValue)
            {
                var change = new LogicOutputChanged(_output1, _outputValue);
                Context.Logger.LogDebug($"{Context.RuleInstance.Name} Change output value to {_outputValue}");
                if (Delay > 0)
                {
                    Task.Run(async () =>
                    {
                        await Task.Delay(Delay);
                        await _context.Dispatcher.DispatchValue(change.Instance, change.Value);
                    });
                }
                else
                {
                    return new List<ILogicOutputChanged> {change};
                }
            }
            else if (instance.This2RuleInterfaceTemplate == TriggerLogicFactory.RuleValueInput)
            {
                Context.Logger.LogDebug($"{Context.RuleInstance.Name} Change value param to {_outputValue}");
                _outputValue = value;
            }

            return new List<ILogicOutputChanged>();
        }
    }
}
