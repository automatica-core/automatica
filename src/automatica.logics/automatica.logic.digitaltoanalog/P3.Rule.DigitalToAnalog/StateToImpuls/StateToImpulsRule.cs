using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Rule;

namespace P3.Rule.DigitalToAnalog.StateToImpuls
{
    public class StateToImpulsRule : Automatica.Core.Rule.Rule
    {
        private readonly IRuleContext _context;
        private int? _i1 = null;
        
        private readonly RuleInterfaceInstance _output1;
        private readonly RuleInterfaceInstance _paramDelay;
        
        public StateToImpulsRule(IRuleContext context) : base(context)
        {
            _context = context;
            _output1 = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
                a.This2RuleInterfaceTemplate == StateToImpulsRuleFactory.RuleOutput);


            _paramDelay = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
                a.This2RuleInterfaceTemplate == StateToImpulsRuleFactory.DelayParameter);

        }

        public int Delay
        {
            get
            {
                if (_paramDelay != null && _paramDelay.ValueInteger.HasValue)
                {
                    return (int)_paramDelay.ValueInteger.Value;
                }

                return 1000;
            }
        }

        protected override IList<IRuleOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            if (value != null && instance.This2RuleInterfaceTemplate == StateToImpulsRuleFactory.RuleInput)
            {
                _i1 = Convert.ToInt32(value);
            }

            if (_i1.HasValue && _i1.Value > 0)
            {
                Task.Run(() =>
                {
                    var change = new RuleOutputChanged(_output1, 0);
                    Thread.Sleep(Delay);
                    _context.Dispatcher.DispatchValue(change.Instance, change.Value);
                });
            }

            return new List<IRuleOutputChanged>();
        }
    }
}
