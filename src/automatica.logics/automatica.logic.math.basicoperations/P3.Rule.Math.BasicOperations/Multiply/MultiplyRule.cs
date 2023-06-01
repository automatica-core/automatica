using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Rule;

namespace P3.Rule.Math.BasicOperations.Multiply
{
    public class MultiplyRule : Automatica.Core.Rule.Rule
    {
        private double _i1 = 1.0;
        private double _i2 = 1.0;


        private readonly RuleInterfaceInstance _output;

        public MultiplyRule(IRuleContext context) : base(context)
        {
            _output = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
                a.This2RuleInterfaceTemplate == MultiplyRuleFactory.RuleOutput);
        }
        public override Task<bool> Stop()
        {
            _i1 = 0;
            _i2 = 0;
            return Task.FromResult(true);
        }

        protected override IList<IRuleOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            if (value != null)
            {
                if (instance.This2RuleInterfaceTemplate == MultiplyRuleFactory.RuleInput1)
                {
                    _i1 = Convert.ToDouble(value);
                }

                if (instance.This2RuleInterfaceTemplate == MultiplyRuleFactory.RuleInput2)
                {
                    _i2 = Convert.ToDouble(value);
                }
            }

            return SingleOutputChanged(new RuleOutputChanged(_output, _i1 * _i2));
        }

    }
}
