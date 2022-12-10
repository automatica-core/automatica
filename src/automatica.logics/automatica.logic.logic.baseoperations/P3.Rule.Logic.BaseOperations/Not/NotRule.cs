using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Rule;

namespace P3.Rule.Logic.BaseOperations.Not
{
    public class NotRule : Automatica.Core.Rule.Rule
    {
        private bool? _i1 = null;

        private bool? _o = null;

        private readonly RuleInterfaceInstance _output1;

        public NotRule(IRuleContext context) : base(context)
        {
            _output1 = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
                a.This2RuleInterfaceTemplate == NotRuleFactory.RuleOutput);
        }

        public override Task<bool> Start()
        {
            _i1 = null;
            return base.Start();
        }
        protected override IList<IRuleOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            if (value != null && instance.This2RuleInterfaceTemplate == NotRuleFactory.RuleInput1)
            {
                _i1 = Convert.ToBoolean(value);
            }

            if (_i1.HasValue)
            {
                _o = !_i1.Value;
            }

            return new List<IRuleOutputChanged>
            {
                new RuleOutputChanged(_output1, _o)
            };
        }

    }
}
