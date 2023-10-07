using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;

namespace P3.Logic.Logic.BaseOperations.Not
{
    public class NotRule : Automatica.Core.Logic.Logic
    {
        private bool? _i1 = null;

        private bool? _o = null;

        private readonly RuleInterfaceInstance _output1;

        public NotRule(ILogicContext context) : base(context)
        {
            _output1 = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
                a.This2RuleInterfaceTemplate == NotLogicFactory.RuleOutput);
            _i1 = null;
        }

        protected override IList<ILogicOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            if (value != null && instance.This2RuleInterfaceTemplate == NotLogicFactory.RuleInput1)
            {
                _i1 = Convert.ToBoolean(value);
            }

            if (_i1.HasValue)
            {
                _o = !_i1.Value;
            }

            return new List<ILogicOutputChanged>
            {
                new LogicOutputChanged(_output1, _o)
            };
        }

    }
}
