using System;
using System.Collections.Generic;
using System.Linq;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Rule;

namespace P3.Rule.Math.BasicOperations.Addition
{
    public class AdditionRule : Automatica.Core.Rule.Rule
    {
        private double _i1 = 0.0;
        private double _i2 = 0.0;
        private double _i3 = 0.0;
        private double _i4 = 0.0;

        private readonly RuleInterfaceInstance _output;

        public AdditionRule(IRuleContext context) : base(context)
        {
            _output = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
                a.This2RuleInterfaceTemplate == AdditionRuleFactory.RuleOutput);
        }

        protected override IList<IRuleOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            if (instance.This2RuleInterfaceTemplate == AdditionRuleFactory.RuleInput1)
            {
                _i1 = Convert.ToDouble(value);
            }

            if (instance.This2RuleInterfaceTemplate == AdditionRuleFactory.RuleInput2)
            {
                _i2 = Convert.ToDouble(value);
            }
            if (instance.This2RuleInterfaceTemplate == AdditionRuleFactory.RuleInput3)
            {
                _i3 = Convert.ToDouble(value);
            }
            if (instance.This2RuleInterfaceTemplate == AdditionRuleFactory.RuleInput4)
            {
                _i4 = Convert.ToDouble(value);
            }


            return SingleOutputChanged(new RuleOutputChanged(_output, _i1 + _i2 + _i3 + _i4));
        }

    }
}
