using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Rule;

namespace P3.Rule.Math.BasicOperations.Division
{
    public class DivisionRule : Automatica.Core.Rule.Rule
    {
        private double _i1 = 1.0;
        private double _i2 = 1.0;


        private readonly RuleInterfaceInstance _output;

        public DivisionRule(IRuleContext context) : base(context)
        {
            _output = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
                a.This2RuleInterfaceTemplate == DivisionRuleFactory.RuleOutput);
        }
        public override Task<bool> Stop()
        {
            _i1 = 0;
            _i2 = 0;
            return Task.FromResult(true);
        }
        public override Task<bool> Start()
        {
            var curValue = _i1 + _i2;
            Context.Dispatcher.DispatchValue(new RuleOutputChanged(_output, curValue).Instance, curValue);

            return base.Start();
        }
        protected override IList<IRuleOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            if (value != null)
            {
                if (instance.This2RuleInterfaceTemplate == DivisionRuleFactory.RuleInput1)
                {
                    _i1 = Convert.ToDouble(value);
                }

                if (instance.This2RuleInterfaceTemplate == DivisionRuleFactory.RuleInput2)
                {
                    _i2 = Convert.ToDouble(value);
                }
            }

            return SingleOutputChanged(new RuleOutputChanged(_output, _i1 / _i2));
        }

    }
}
