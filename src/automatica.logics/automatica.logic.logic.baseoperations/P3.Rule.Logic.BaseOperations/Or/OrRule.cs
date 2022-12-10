using System;
using System.Collections.Generic;
using System.Linq;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Rule;

namespace P3.Rule.Logic.BaseOperations.Or
{
    public class OrRule : Automatica.Core.Rule.Rule
    {
        private bool? _i1 = null;
        private bool? _i2 = null;

        private bool _o;

        private readonly RuleInterfaceInstance _output1;

        public OrRule(IRuleContext context) : base(context)
        {
            _output1 = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
                a.This2RuleInterfaceTemplate == OrRuleFactory.RuleOutput);
        }

        protected override IList<IRuleOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            if (value != null)
            {
                if (instance.This2RuleInterfaceTemplate == OrRuleFactory.RuleInput1)
                {
                    _i1 = Helper.ConvertValueToBool(value);
                }

                if (instance.This2RuleInterfaceTemplate == OrRuleFactory.RuleInput2)
                {
                    _i2 = Helper.ConvertValueToBool(value);
                }
            }

            if (_i1.HasValue && _i2.HasValue)
            {
                _o = _i1.Value || _i2.Value; return new List<IRuleOutputChanged>
                {
                    new RuleOutputChanged(_output1, _o)
                };
            }

            return new List<IRuleOutputChanged>();
        }

    }
}
