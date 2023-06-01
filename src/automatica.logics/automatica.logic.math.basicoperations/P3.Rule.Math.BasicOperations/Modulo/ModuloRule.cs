using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Rule;

namespace P3.Rule.Math.BasicOperations.Modulo
{
    public class ModuloRule : Automatica.Core.Rule.Rule
    {
        private double? _i1 = null;
        private double? _i2 = null;

        private double? _o = null;

        private readonly RuleInterfaceInstance _output1;
        private readonly RuleInterfaceInstance _output2;

        public ModuloRule(IRuleContext context) : base(context)
        {
            _output1 = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
                a.This2RuleInterfaceTemplate == ModuloRuleFactory.RuleOutput1);

            _output2 = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
                a.This2RuleInterfaceTemplate == ModuloRuleFactory.RuleOutput2);
        }
        public override Task<bool> Stop()
        {
            _i1 = 0;
            _i2 = 0;
            return Task.FromResult(true);
        }
        protected override IList<IRuleOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            if (value != null)
            {
                if (instance.This2RuleInterfaceTemplate == ModuloRuleFactory.RuleInput1)
                {
                    _i1 = Convert.ToDouble(value);
                }

                if (instance.This2RuleInterfaceTemplate == ModuloRuleFactory.RuleInput2)
                {
                    _i2 = Convert.ToDouble(value);
                }
            }

            if (_i1.HasValue && _i2.HasValue)
            {
                _o = _i1.Value % _i2.Value;
            }

            return new List<IRuleOutputChanged>
            {
                new RuleOutputChanged(_output1, _o),
                new RuleOutputChanged(_output2, _o.HasValue ? Convert.ToInt32(_o.Value) :  (int?)null)
            };
        }

    }
}
