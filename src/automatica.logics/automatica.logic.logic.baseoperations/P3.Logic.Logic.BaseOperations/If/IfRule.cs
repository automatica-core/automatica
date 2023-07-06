using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;

namespace P3.Logic.Logic.BaseOperations.If
{
    public class IfRule : Automatica.Core.Logic.Logic
    {
        private readonly RuleInterfaceInstance _paramTrueValue;
        private readonly RuleInterfaceInstance _paramFalseValue;
        private readonly RuleInterfaceInstance _output1;

        private int? _i1 = null;
        private int? _i2 = null;

        private object _o1 = null;
        private object _o2 = null;

        private object _o = null;


        public IfRule(ILogicContext context) : base(context)
        {
            _paramTrueValue = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
                a.This2RuleInterfaceTemplate == IfLogicFactory.RuleParamTrue);

            _paramFalseValue = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
                a.This2RuleInterfaceTemplate == IfLogicFactory.RuleParamFalse);

            _output1 = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
                a.This2RuleInterfaceTemplate == IfLogicFactory.RuleOutput);
        }
        public override Task<bool> Start(CancellationToken token = default)
        {
            _i1 = null;
            _i2 = null;
            return Task.FromResult(true);
        }

        protected override IList<ILogicOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            if (value != null)
            {
                if (instance.This2RuleInterfaceTemplate == IfLogicFactory.RuleInput1)
                {
                    _o1 = value;
                }

                if (instance.This2RuleInterfaceTemplate == IfLogicFactory.RuleInput2)
                {
                    _o2 = value;
                }
            }

            if(_o1 != null && _o2 != null)
            {
                _i1 = ConvertToInt(_o1);
                _i2 = ConvertToInt(_o2);

                if (_i1 == _i2)
                {
                    _o = _paramTrueValue.Value;
                }
                else
                {
                    _o = _paramFalseValue.Value;
                }
            }
            else
            {
                _o = _paramFalseValue.Value;
            }

            return new List<ILogicOutputChanged>
            {
                new LogicOutputChanged(_output1, _o)
            };


        }

        private int ConvertToInt(object value)
        {
            return Helper.ConvertValueToInt(value);
        }

    }
}
