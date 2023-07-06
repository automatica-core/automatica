using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;

namespace P3.Logic.Math.BasicOperations.Addition
{
    public class AdditionRule : Automatica.Core.Logic.Logic
    {
        private double _i1 = 0.0;
        private double _i2 = 0.0;
        private double _i3 = 0.0;
        private double _i4 = 0.0;

        private readonly RuleInterfaceInstance _output;

        public AdditionRule(ILogicContext context) : base(context)
        {
            _output = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
                a.This2RuleInterfaceTemplate == AdditionLogicFactory.RuleOutput);
        }

        public override Task<bool> Stop(CancellationToken token = default)
        {
            _i1 = 0;
            _i2 = 0;
            _i3 = 0;
            _i4 = 0;
            return Task.FromResult(true);
        }

        public override Task<bool> Start(CancellationToken token = default)
        {
            var curValue = _i1 +_i2 + _i3 + _i4;
            Context.Dispatcher.DispatchValue(new LogicOutputChanged(_output, curValue).Instance, curValue);
            return Task.FromResult(true);
        }

        protected override IList<ILogicOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            if (instance.This2RuleInterfaceTemplate == AdditionLogicFactory.RuleInput1)
            {
                _i1 = Convert.ToDouble(value);
            }

            if (instance.This2RuleInterfaceTemplate == AdditionLogicFactory.RuleInput2)
            {
                _i2 = Convert.ToDouble(value);
            }
            if (instance.This2RuleInterfaceTemplate == AdditionLogicFactory.RuleInput3)
            {
                _i3 = Convert.ToDouble(value);
            }
            if (instance.This2RuleInterfaceTemplate == AdditionLogicFactory.RuleInput4)
            {
                _i4 = Convert.ToDouble(value);
            }


            return SingleOutputChanged(new LogicOutputChanged(_output, _i1 + _i2 + _i3 + _i4));
        }

    }
}
