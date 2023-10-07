using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;

namespace P3.Logic.Logic.BaseOperations.Or
{
    public class OrRule : Automatica.Core.Logic.Logic
    {
        private bool? _i1 = null;
        private bool? _i2 = null;

        private bool _o;

        private readonly RuleInterfaceInstance _output1;

        public OrRule(ILogicContext context) : base(context)
        {
            _output1 = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
                a.This2RuleInterfaceTemplate == OrLogicFactory.RuleOutput);
            _i1 = null;
            _i2 = null;
        }

        protected override IList<ILogicOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            if (value != null)
            {
                if (instance.This2RuleInterfaceTemplate == OrLogicFactory.RuleInput1)
                {
                    _i1 = Helper.ConvertValueToBool(value);
                }

                if (instance.This2RuleInterfaceTemplate == OrLogicFactory.RuleInput2)
                {
                    _i2 = Helper.ConvertValueToBool(value);
                }
            }

            if (_i1.HasValue || _i2.HasValue)
            {
                _o = (_i1 ?? false) || (_i2 ?? false); 
                
                return new List<ILogicOutputChanged>
                {
                    new LogicOutputChanged(_output1, _o)
                };
            }

            return new List<ILogicOutputChanged>();
        }

    }
}
