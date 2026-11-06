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
        protected override Task<bool> Stop(RuleInstance logicInstance, CancellationToken token = default)
        {
            // keep the last input values - a reload calls Stop and Start on the same instance
            return Task.FromResult(true);
        }
        protected override Task<bool> Start(RuleInstance logicInstance, CancellationToken token = default)
        {
            if (_o.HasValue)
            {
                var o2 = ToInt(_o);
                Context.Dispatcher.DispatchValue(new LogicOutputChanged(_output1, _o).Instance, _o);
                Context.Dispatcher.DispatchValue(new LogicOutputChanged(_output2, o2).Instance, o2);
            }
            return Task.FromResult(true);
        }

        /// <summary>
        /// O2 is the integer part, a non finite result (divisor 0 gives NaN like Division gives NaN/Infinity) has none
        /// </summary>
        private static int? ToInt(double? value)
        {
            if (!value.HasValue || double.IsNaN(value.Value) || double.IsInfinity(value.Value) ||
                value.Value > int.MaxValue || value.Value < int.MinValue)
            {
                return null;
            }

            return Convert.ToInt32(value.Value);
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
                new LogicOutputChanged(_output2, ToInt(_o))
            };
        }

    }
}
