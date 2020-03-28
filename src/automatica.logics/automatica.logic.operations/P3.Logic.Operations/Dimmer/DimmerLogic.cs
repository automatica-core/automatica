using System;
using System.Collections.Generic;
using System.Linq;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Rule;

namespace P3.Logic.Operations.Dimmer
{
    public class DimmerLogic : Rule
    {
        private readonly RuleInterfaceInstance _value;
        private readonly RuleInterfaceInstance _state;
        private readonly RuleInterfaceInstance _reset;

        private readonly RuleInterfaceInstance _output;

        public DimmerLogic(IRuleContext context) : base(context)
        {
            _state = context.RuleInstance.RuleInterfaceInstance.Single(a =>
                a.This2RuleInterfaceTemplate == DimmerLogicFactory.RuleInputState);

            _value = context.RuleInstance.RuleInterfaceInstance.Single(a =>
                a.This2RuleInterfaceTemplate == DimmerLogicFactory.RuleInputValue);

            _reset = context.RuleInstance.RuleInterfaceInstance.Single(a =>
                a.This2RuleInterfaceTemplate == DimmerLogicFactory.RuleInputReset);

            _output = context.RuleInstance.RuleInterfaceInstance.Single(a =>
                a.This2RuleInterfaceTemplate == DimmerLogicFactory.RuleOutput);
        }

        protected override IList<IRuleOutputChanged> InputValueChanged(RuleInterfaceInstance instance,
            IDispatchable source, object value)
        {
            if (instance == _state)
            {
                var booleanValue = Convert.ToBoolean(value);

                return ValueChanged(_output, booleanValue ? 100 : 0);
            }

            if (instance == _value)
            {
                return ValueChanged(_output, value);
            }

            if (instance == _reset)
            {
                return ValueChanged(_output, 0);
            }

            return new List<IRuleOutputChanged>();
        }

        public override object GetDataForVisu()
        {
            return base.GetDataForVisu();
        }
    }
}
