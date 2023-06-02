using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Rule;

namespace P3.Rule.Math.BasicOperations.Ceiling
{
    public class CeilingRule : Automatica.Core.Rule.Rule
    {
        private double _i1 = 0.0;

        private readonly RuleInterfaceInstance _output;

        public CeilingRule(IRuleContext context) : base(context)
        {
            _output = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
                a.This2RuleInterfaceTemplate == CeilingRuleFactory.RuleOutput);
        }
        public override Task<bool> Stop()
        {
            _i1 = 0;
            return Task.FromResult(true);
        }
        public override Task<bool> Start()
        {
            var curValue = _i1;
            Context.Dispatcher.DispatchValue(new RuleOutputChanged(_output, curValue).Instance, curValue);

            return base.Start();
        }
        /// <summary>
        /// Callback when a input value has changed
        /// </summary>
        /// <param name="instance">The input value which has changed</param>
        /// <param name="source">The source from where the value is dispatched</param>
        /// <param name="value">The value itself</param>
        /// <returns></returns>
        protected override IList<IRuleOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            if (instance.This2RuleInterfaceTemplate == CeilingRuleFactory.RuleInput1 && value != null)
            {
                _i1 = Convert.ToDouble(value);
            }

            return SingleOutputChanged(new RuleOutputChanged(_output,  System.Math.Ceiling(_i1)));
        }
    }
}
