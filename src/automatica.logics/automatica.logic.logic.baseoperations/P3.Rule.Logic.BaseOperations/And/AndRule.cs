using System;
using System.Collections.Generic;
using System.Linq;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Rule;

namespace P3.Rule.Logic.BaseOperations.And
{
    public class AndRule : Automatica.Core.Rule.Rule
    {
        private int? _i1 = null;
        private int? _i2 = null;

        private double? _o = null;

        private readonly RuleInterfaceInstance _output1;

        public AndRule(IRuleContext context) : base(context)
        {
            _output1 = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
                a.This2RuleInterfaceTemplate == AndRuleFactory.RuleOutput);

  
        }

        protected override IList<IRuleOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            if (value != null)
            {
                if (instance.This2RuleInterfaceTemplate == AndRuleFactory.RuleInput1)
                {
                    _i1 = Convert.ToInt32(value);
                }

                if (instance.This2RuleInterfaceTemplate == AndRuleFactory.RuleInput2)
                {
                    _i2 = Convert.ToInt32(value);
                }
            }

            if (_i1.HasValue && _i2.HasValue)
            {
                _o = _i1.Value & _i2.Value;
            }

            return new List<IRuleOutputChanged>
            {
                new RuleOutputChanged(_output1, _o)
            };
        }

    }
}
