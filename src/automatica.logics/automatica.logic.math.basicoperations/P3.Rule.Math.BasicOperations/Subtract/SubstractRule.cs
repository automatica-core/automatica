using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Rule;
using Microsoft.Extensions.Logging;

namespace P3.Rule.Math.BasicOperations.Subtract
{
    public class SubtractRule : Automatica.Core.Rule.Rule
    {
        private double _i1 = 0.0;
        private double _i2 = 0.0;

        private readonly RuleInterfaceInstance _output;

        public SubtractRule(IRuleContext context) : base(context)
        {
            _output = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
                a.This2RuleInterfaceTemplate == SubtractRuleFactory.RuleOutput);
        }
        public override Task<bool> Stop()
        {
            _i1 = 0;
            _i2 = 0;
            return Task.FromResult(true);
        }
        public override Task<bool> Start()
        {
            var curValue = _i1 + _i2 ;
            Context.Dispatcher.DispatchValue(new RuleOutputChanged(_output, curValue).Instance, curValue);

            return base.Start();
        }
        protected override IList<IRuleOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            if (instance.This2RuleInterfaceTemplate == SubtractRuleFactory.RuleInput1)
            {
                _i1 = Convert.ToDouble(value);
            }

            if (instance.This2RuleInterfaceTemplate == SubtractRuleFactory.RuleInput2)
            {
                _i2 = Convert.ToDouble(value);
            }

            var result = _i1 - _i2;

            Context.Logger.LogDebug($"Subtract {_i1} - {_i2} = {result}");

            return SingleOutputChanged(new RuleOutputChanged(_output, result));
        }

    }
}
