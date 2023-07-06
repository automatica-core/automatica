using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;

namespace P3.Logic.Math.BasicOperations.Ceiling
{
    public class CeilingRule : Automatica.Core.Logic.Logic
    {
        private double _i1 = 0.0;

        private readonly RuleInterfaceInstance _output;

        public CeilingRule(ILogicContext context) : base(context)
        {
            _output = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
                a.This2RuleInterfaceTemplate == CeilingLogicFactory.RuleOutput);
        }
        public override Task<bool> Stop(CancellationToken token = default)
        {
            _i1 = 0;
            return Task.FromResult(true);
        }
        public override Task<bool> Start(CancellationToken token = default)
        {
            var curValue = _i1;
            Context.Dispatcher.DispatchValue(new LogicOutputChanged(_output, curValue).Instance, curValue);
            return Task.FromResult(true);
        }
        /// <summary>
        /// Callback when a input value has changed
        /// </summary>
        /// <param name="instance">The input value which has changed</param>
        /// <param name="source">The source from where the value is dispatched</param>
        /// <param name="value">The value itself</param>
        /// <returns></returns>
        protected override IList<ILogicOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            if (instance.This2RuleInterfaceTemplate == CeilingLogicFactory.RuleInput1 && value != null)
            {
                _i1 = Convert.ToDouble(value);
            }

            return SingleOutputChanged(new LogicOutputChanged(_output,  System.Math.Ceiling(_i1)));
        }
    }
}
