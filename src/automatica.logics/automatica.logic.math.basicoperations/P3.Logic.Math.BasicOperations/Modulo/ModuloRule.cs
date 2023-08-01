using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;

namespace P3.Logic.Math.BasicOperations.Modulo
{
    public class ModuloRule : Automatica.Core.Logic.Logic
    {
        private double? _i1 = null;
        private double? _i2 = null;

        private double? _o = null;

        private readonly RuleInterfaceInstance _output1;
        private readonly RuleInterfaceInstance _output2;

        public ModuloRule(ILogicContext context) : base(context)
        {
            _output1 = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
                a.This2RuleInterfaceTemplate == ModuloLogicFactory.RuleOutput1);

            _output2 = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
                a.This2RuleInterfaceTemplate == ModuloLogicFactory.RuleOutput2);
        }
        public override Task<bool> Stop(CancellationToken token = default)
        {
            _i1 = 0;
            _i2 = 0;
            return Task.FromResult(true);
        }
        public override Task<bool> Start(CancellationToken token = default)
        {
            var curValue = _i1 + _i2;
            Context.Dispatcher.DispatchValue(new LogicOutputChanged(_output1, curValue).Instance, curValue);
            return Task.FromResult(true);
        }
        protected override IList<ILogicOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            if (value != null)
            {
                if (instance.This2RuleInterfaceTemplate == ModuloLogicFactory.RuleInput1)
                {
                    _i1 = Convert.ToDouble(value);
                }

                if (instance.This2RuleInterfaceTemplate == ModuloLogicFactory.RuleInput2)
                {
                    _i2 = Convert.ToDouble(value);
                }
            }

            if (_i1.HasValue && _i2.HasValue)
            {
                _o = _i1.Value % _i2.Value;
            }

            return new List<ILogicOutputChanged>
            {
                new LogicOutputChanged(_output1, _o),
                new LogicOutputChanged(_output2, _o.HasValue ? Convert.ToInt32(_o.Value) :  (int?)null)
            };
        }

    }
}
