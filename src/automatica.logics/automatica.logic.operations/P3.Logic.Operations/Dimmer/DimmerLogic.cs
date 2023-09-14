using System;
using System.Collections.Generic;
using System.Linq;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;

namespace P3.Logic.Operations.Dimmer
{
    public class DimmerLogic: Automatica.Core.Logic.Logic
    {
        private readonly RuleInterfaceInstance _value;
        private readonly RuleInterfaceInstance _state;
        private readonly RuleInterfaceInstance _reset;

        private readonly RuleInterfaceInstance _output;
        private readonly RuleInterfaceInstance _outputState;

        private bool? _lastState;
        private int? _lastValue;
        private bool? _lastOutputState;

        public DimmerLogic(ILogicContext context) : base(context)
        {
            _state = context.RuleInstance.RuleInterfaceInstance.Single(a =>
                a.This2RuleInterfaceTemplate == DimmerLogicFactory.RuleInputState);

            _value = context.RuleInstance.RuleInterfaceInstance.Single(a =>
                a.This2RuleInterfaceTemplate == DimmerLogicFactory.RuleInputValue);

            _reset = context.RuleInstance.RuleInterfaceInstance.Single(a =>
                a.This2RuleInterfaceTemplate == DimmerLogicFactory.RuleInputReset);

            _output = context.RuleInstance.RuleInterfaceInstance.Single(a =>
                a.This2RuleInterfaceTemplate == DimmerLogicFactory.RuleOutput);


            _outputState = context.RuleInstance.RuleInterfaceInstance.Single(a =>
                a.This2RuleInterfaceTemplate == DimmerLogicFactory.RuleState);
        }

        protected override IList<ILogicOutputChanged> InputValueChanged(RuleInterfaceInstance instance,
            IDispatchable source, object value)
        {
            if (instance.ObjId == _state.ObjId)
            {
                var booleanValue = Convert.ToBoolean(value);

                if (_lastState == booleanValue)
                {
                    return new List<ILogicOutputChanged>();
                }

                _lastState = booleanValue;

                return SingleOutputChanged(new LogicOutputChanged(_output, booleanValue));
            }

            if (instance.ObjId == _value.ObjId)
            {
                int intValue = Convert.ToInt32(value);

                if (_lastValue.HasValue && _lastValue.Value == intValue)
                {
                    return new List<ILogicOutputChanged>();
                }

                _lastValue = intValue;

                return SingleOutputChanged(new LogicOutputChanged(_outputState, value));
            }

            if (instance.ObjId == _reset.ObjId)
            {
                return SingleOutputChanged(new LogicOutputChanged(_output, 0));
            }

            return new List<ILogicOutputChanged>();
        }

        public override object GetDataForVisu()
        {
            return base.GetDataForVisu();
        }
    }
}
