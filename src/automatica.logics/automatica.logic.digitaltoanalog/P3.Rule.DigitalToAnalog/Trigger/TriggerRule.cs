using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Rule;
using Microsoft.AspNetCore.Routing.Internal;

namespace P3.Rule.DigitalToAnalog.Trigger
{
    public class TriggerRule : Automatica.Core.Rule.Rule
    {
        private readonly IRuleContext _context;

        private object _outputValue;
        
        private readonly RuleInterfaceInstance _output1;
        private readonly RuleInterfaceInstance _paramDelay;

        public TriggerRule(IRuleContext context) : base(context)
        {
            _context = context;
            _output1 = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
                a.This2RuleInterfaceTemplate == TriggerRuleFactory.RuleOutput);

            _paramDelay = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
                a.This2RuleInterfaceTemplate == TriggerRuleFactory.DelayParameter);

            var paramValue = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
                a.This2RuleInterfaceTemplate == TriggerRuleFactory.ValueParameter);

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

        protected override IList<IRuleOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            if (value != null && instance.This2RuleInterfaceTemplate == TriggerRuleFactory.RuleInput && Boolean.TryParse(value.ToString(), out bool bValue) && bValue)
            {
                var change = new RuleOutputChanged(_output1, _outputValue);
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
                    return new List<IRuleOutputChanged> {change};
                }
            }
            else if (instance.This2RuleInterfaceTemplate == TriggerRuleFactory.RuleValueInput)
            {
                _outputValue = value;
            }

            return new List<IRuleOutputChanged>();
        }
    }
}
