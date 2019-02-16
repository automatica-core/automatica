using System;
using System.Collections.Generic;
using System.Linq;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Rule;

namespace P3.Rule.Math.BasicOperations.Random
{
    public class RandomRule : Automatica.Core.Rule.Rule
    {
        private double? _i1;

        private readonly RuleInterfaceInstance _output;
        private readonly RuleInterfaceInstance _minInstance;
        private readonly RuleInterfaceInstance _maxInstance;
        

        private bool _disabled = false;

        private readonly System.Random _random = new System.Random();

        public RandomRule(IRuleContext context) : base(context)
        {
            _output = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
                a.This2RuleInterfaceTemplate == RandomRuleFactory.RuleOutput);

            _minInstance = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
                a.This2RuleInterfaceTemplate == RandomRuleFactory.RuleParamMin);

            _maxInstance = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
                a.This2RuleInterfaceTemplate == RandomRuleFactory.RuleParamMax);
        }

        public int Min
        {
            get
            {
                if (_minInstance != null && _minInstance.ValueInteger.HasValue)
                {
                    return (int)_minInstance.ValueInteger.Value;
                }

                return 0;
            }
        }
        public int Max
        {
            get
            {
                if (_maxInstance != null && _maxInstance.ValueInteger.HasValue)
                {
                    return (int)_maxInstance.ValueInteger.Value;
                }

                return 100;
            }
        }

        protected override IList<IRuleOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {

            if (instance.This2RuleInterfaceTemplate == RandomRuleFactory.RuleInputDisabled)
            {
                _disabled = Convert.ToBoolean(value);
            }

            if (!_disabled && instance.This2RuleInterfaceTemplate == RandomRuleFactory.RuleInputTrigger)
            {
                _i1 = _random.Next(Min, Max);
            }
            return SingleOutputChanged(new RuleOutputChanged(_output,  _i1));
        }

    }
}
