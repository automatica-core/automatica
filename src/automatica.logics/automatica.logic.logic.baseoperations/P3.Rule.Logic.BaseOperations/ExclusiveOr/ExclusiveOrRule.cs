using System;
using System.Collections.Generic;
using System.Linq;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Rule;

namespace P3.Rule.Logic.BaseOperations.ExclusiveOr
{
    public class ExclusiveOrRule : Automatica.Core.Rule.Rule
    {
        private int? _i1 = null;
        private int? _i2 = null;

        private double? _o = null;

        private readonly RuleInterfaceInstance _output1;

        public ExclusiveOrRule(IRuleContext context) : base(context)
        {
            _output1 = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
                a.This2RuleInterfaceTemplate == ExclusiveOrRuleFactory.RuleOutput);
        }

        public override Task<bool> Start()
        {
            _i1 = null;
            _i2 = null;
            return base.Start();
        }
        protected override IList<IRuleOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            if (value != null)
            {
                if (instance.This2RuleInterfaceTemplate == ExclusiveOrRuleFactory.RuleInput1)
                {
                    _i1= Helper.ConvertValueToInt(value);
                }

                if (instance.This2RuleInterfaceTemplate == ExclusiveOrRuleFactory.RuleInput2)
                {
                    _i2 = Helper.ConvertValueToInt(value);
                }
            }

            if (_i1.HasValue && _i2.HasValue)
            {
                _o = _i1.Value ^ _i2.Value;
            }

            return new List<IRuleOutputChanged>
            {
                new RuleOutputChanged(_output1, _o)
            };
        }

    }
}
