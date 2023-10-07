using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using Microsoft.Extensions.Logging;

namespace P3.Logic.Math.BasicOperations.Subtract
{
    public class SubtractRule : Automatica.Core.Logic.Logic
    {
        private double _i1 = 0.0;
        private double _i2 = 0.0;

        private readonly RuleInterfaceInstance _output;

        public SubtractRule(ILogicContext context) : base(context)
        {
            _output = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
                a.This2RuleInterfaceTemplate == SubtractLogicFactory.RuleOutput);
        }
        protected override Task<bool> Stop(RuleInstance logicInstance, CancellationToken token = default)
        {
            _i1 = 0;
            _i2 = 0;
            return Task.FromResult(true);
        }
        protected override Task<bool> Start(RuleInstance logicInstance, CancellationToken token = default)
        {
            var curValue = _i1 + _i2 ;
            Context.Dispatcher.DispatchValue(new LogicOutputChanged(_output, curValue).Instance, curValue);

            return Task.FromResult(true);
        }
        protected override IList<ILogicOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            if (instance.This2RuleInterfaceTemplate == SubtractLogicFactory.RuleInput1)
            {
                _i1 = Convert.ToDouble(value);
            }

            if (instance.This2RuleInterfaceTemplate == SubtractLogicFactory.RuleInput2)
            {
                _i2 = Convert.ToDouble(value);
            }

            var result = _i1 - _i2;

            Context.Logger.LogDebug($"Subtract {_i1} - {_i2} = {result}");

            return SingleOutputChanged(new LogicOutputChanged(_output, result));
        }

    }
}
