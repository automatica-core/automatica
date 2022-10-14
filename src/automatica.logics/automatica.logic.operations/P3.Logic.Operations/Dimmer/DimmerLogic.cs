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

        private bool? _lastState;
        private int? _lastValue;

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
            if (instance.ObjId == _state.ObjId)
            {
                var booleanValue = Convert.ToBoolean(value);

                if (_lastState.HasValue && _lastState.Value == booleanValue)
                {
                    return new List<IRuleOutputChanged>();
                }

                _lastState = booleanValue;

                if (!_lastValue.HasValue && booleanValue || (_lastValue.HasValue && _lastValue.Value == 0))
                {
                    _lastValue = 100;
                }

                return SingleOutputChanged(new RuleOutputChanged(_output, booleanValue ? _lastValue.Value : 0));
            }

            if (instance.ObjId == _value.ObjId)
            {
                int intValue = Convert.ToInt32(value);

                if (_lastValue.HasValue && _lastValue.Value == intValue)
                {
                    return new List<IRuleOutputChanged>();
                }

                _lastValue = intValue;

                return SingleOutputChanged(new RuleOutputChanged(_output, value));
            }

            if (instance.ObjId == _reset.ObjId)
            {
                return SingleOutputChanged(new RuleOutputChanged(_output, 0));
            }

            return new List<IRuleOutputChanged>();
        }

        public override object GetDataForVisu()
        {
            return base.GetDataForVisu();
        }
    }
}
