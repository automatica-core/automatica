using System;
using System.Collections.Generic;
using System.Linq;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Rule;

namespace P3.Rule.Compare.BaseOperations.Smaller
{
    public class SmallerRule : Automatica.Core.Rule.Rule
    {
        private double? _i1 = null;
        private double? _i2 = null;


        private readonly RuleInterfaceInstance _output;

        public SmallerRule(IRuleContext context) : base(context)
        {
            _output = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
                a.This2RuleInterfaceTemplate == SmallerRuleFactory.RuleOutput);
        }

        protected override IList<IRuleOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            if (value != null)
            {
                if (instance.This2RuleInterfaceTemplate == SmallerRuleFactory.RuleInput1)
                {
                    _i1 = Convert.ToDouble(value);
                }

                if (instance.This2RuleInterfaceTemplate == SmallerRuleFactory.RuleInput2)
                {
                    _i2 = Convert.ToDouble(value);
                }
            }
            if (_i1 == null || _i2 == null)
            {
                return new List<IRuleOutputChanged>();
            }

            return SingleOutputChanged(new RuleOutputChanged(_output, _i1 < _i2));
        }

    }
}
