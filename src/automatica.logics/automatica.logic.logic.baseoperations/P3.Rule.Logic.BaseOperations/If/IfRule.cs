using System;
using System.Collections.Generic;
using System.Linq;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Rule;

namespace P3.Rule.Logic.BaseOperations.If
{
    public class IfRule : Automatica.Core.Rule.Rule
    {
        private readonly RuleInterfaceInstance _paramTrueValue;
        private readonly RuleInterfaceInstance _paramFalseValue;
        private readonly RuleInterfaceInstance _output1;

        private int? _i1 = null;
        private int? _i2 = null;

        private object _o1 = null;
        private object _o2 = null;

        private object _o = null;


        public IfRule(IRuleContext context) : base(context)
        {
            _paramTrueValue = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
                a.This2RuleInterfaceTemplate == IfRuleFactory.RuleParamTrue);

            _paramFalseValue = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
                a.This2RuleInterfaceTemplate == IfRuleFactory.RuleParamFalse);

            _output1 = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
                a.This2RuleInterfaceTemplate == IfRuleFactory.RuleOutput);
        }

        protected override IList<IRuleOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            if (value != null)
            {
                if (instance.This2RuleInterfaceTemplate == IfRuleFactory.RuleInput1)
                {
                    _o1 = value;
                }

                if (instance.This2RuleInterfaceTemplate == IfRuleFactory.RuleInput2)
                {
                    _o2 = value;
                }
            }

            if(_o1 != null && _o2 != null)
            {
                _i1 = ConvertToInt(_o1);
                _i2 = ConvertToInt(_o2);

                if (_i1 == _i2)
                {
                    _o = _paramTrueValue.Value;
                }
                else
                {
                    _o = _paramFalseValue.Value;
                }
            }
            else
            {
                _o = _paramFalseValue.Value;
            }

            return new List<IRuleOutputChanged>
            {
                new RuleOutputChanged(_output1, _o)
            };


        }

        private int ConvertToInt(object value)
        {
            return Helper.ConvertValueToInt(value);
        }

    }
}
