using System;
using System.Collections.Generic;
using System.Linq;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;

namespace P3.Logic.Math.BasicOperations.Random
{
    public class RandomRule : Automatica.Core.Logic.Logic
    {
        private double? _i1;

        private readonly RuleInterfaceInstance _output;
        private readonly RuleInterfaceInstance _minInstance;
        private readonly RuleInterfaceInstance _maxInstance;
        

        private bool _disabled = false;

        private readonly System.Random _random = new System.Random();

        public RandomRule(ILogicContext context) : base(context)
        {
            _output = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
                a.This2RuleInterfaceTemplate == RandomLogicFactory.RuleOutput);

            _minInstance = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
                a.This2RuleInterfaceTemplate == RandomLogicFactory.RuleParamMin);

            _maxInstance = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
                a.This2RuleInterfaceTemplate == RandomLogicFactory.RuleParamMax);
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

        protected override IList<ILogicOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {

            if (instance.This2RuleInterfaceTemplate == RandomLogicFactory.RuleInputDisabled)
            {
                _disabled = Convert.ToBoolean(value);
            }

            if (!_disabled && instance.This2RuleInterfaceTemplate == RandomLogicFactory.RuleInputTrigger)
            {
                _i1 = _random.Next(Min, Max);
            }
            return SingleOutputChanged(new LogicOutputChanged(_output,  _i1));
        }

    }
}
