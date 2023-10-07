﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;

namespace P3.Logic.Math.BasicOperations.Multiply
{
    public class MultiplyRule : Automatica.Core.Logic.Logic
    {
        private double _i1 = 1.0;
        private double _i2 = 1.0;


        private readonly RuleInterfaceInstance _output;

        public MultiplyRule(ILogicContext context) : base(context)
        {
            _output = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
                a.This2RuleInterfaceTemplate == MultiplyLogicFactory.RuleOutput);
        }
        protected override Task<bool> Stop(RuleInstance logicInstance, CancellationToken token = default)
        {
            _i1 = 0;
            _i2 = 0;
            return Task.FromResult(true);
        }
        protected override Task<bool> Start(RuleInstance logicInstance, CancellationToken token = default)
        {
            var curValue = _i1 + _i2;
            Context.Dispatcher.DispatchValue(new LogicOutputChanged(_output, curValue).Instance, curValue);
            return Task.FromResult(true);
        }

        protected override IList<ILogicOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            if (value != null)
            {
                if (instance.This2RuleInterfaceTemplate == MultiplyLogicFactory.RuleInput1)
                {
                    _i1 = Convert.ToDouble(value);
                }

                if (instance.This2RuleInterfaceTemplate == MultiplyLogicFactory.RuleInput2)
                {
                    _i2 = Convert.ToDouble(value);
                }
            }

            return SingleOutputChanged(new LogicOutputChanged(_output, _i1 * _i2));
        }

    }
}
