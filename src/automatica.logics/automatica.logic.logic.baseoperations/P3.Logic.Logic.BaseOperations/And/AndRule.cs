using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;

namespace P3.Logic.Logic.BaseOperations.And
{
    public class AndRule : Automatica.Core.Logic.Logic
    {
        private int? _i1 = null;
        private int? _i2 = null;

        private double? _o = null;

        private readonly RuleInterfaceInstance _output1;

        public AndRule(ILogicContext context) : base(context)
        {
            _output1 = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
                a.This2RuleInterfaceTemplate == AndLogicFactory.RuleOutput);

            _i1 = null;
            _i2 = null;
        }
        protected override IList<ILogicOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            if (value != null)
            {
                if (instance.This2RuleInterfaceTemplate == AndLogicFactory.RuleInput1)
                {
                    _i1 = Helper.ConvertValueToInt(value);
                }

                if (instance.This2RuleInterfaceTemplate == AndLogicFactory.RuleInput2)
                {
                    _i2 = Helper.ConvertValueToInt(value);
                }
            }

            if (_i1.HasValue && _i2.HasValue)
            {
                _o = _i1.Value & _i2.Value;
            }

            return new List<ILogicOutputChanged>
            {
                new LogicOutputChanged(_output1, _o)
            };
        }

    }
}
