using System;
using System.Collections.Generic;
using System.Linq;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Rule;

namespace P3.Rule.Math.BasicOperations.Floor
{
    public class FloorRule : Automatica.Core.Rule.Rule
    {
        private double _i1 = 0.0;

        private readonly RuleInterfaceInstance _output;

        public FloorRule(IRuleContext context) : base(context)
        {
            _output = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
                a.This2RuleInterfaceTemplate == FloorRuleFactory.RuleOutput);
        }

        protected override IList<IRuleOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            if (instance.This2RuleInterfaceTemplate == FloorRuleFactory.RuleInput1 && value != null)
            {
                _i1 = Convert.ToDouble(value);
            }

            return SingleOutputChanged(new RuleOutputChanged(_output,  System.Math.Floor(_i1)));
        }

    }
}
