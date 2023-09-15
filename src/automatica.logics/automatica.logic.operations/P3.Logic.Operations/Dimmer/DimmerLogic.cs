using System;
using System.Collections.Generic;
using System.Linq;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using Newtonsoft.Json.Linq;

namespace P3.Logic.Operations.Dimmer
{
    public class DimmerLogic: Automatica.Core.Logic.Logic
    {
        private readonly RuleInterfaceInstance _value;
        private readonly RuleInterfaceInstance _state;
        private readonly RuleInterfaceInstance _reset;

        private readonly RuleInterfaceInstance _dimmerValue;
        private readonly RuleInterfaceInstance _dimmerState;

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

            _dimmerValue = context.RuleInstance.RuleInterfaceInstance.Single(a =>
                a.This2RuleInterfaceTemplate == DimmerLogicFactory.DimmerValue);


            _dimmerState = context.RuleInstance.RuleInterfaceInstance.Single(a =>
                a.This2RuleInterfaceTemplate == DimmerLogicFactory.DimmerState);
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

                var ret = new List<ILogicOutputChanged>();
                ret.Add(new LogicOutputChanged(_dimmerState, booleanValue));

                if (booleanValue && _lastValue.HasValue)
                {
                    ret.Add( new LogicOutputChanged(_dimmerValue, _lastValue));
                }
                else if (!booleanValue)
                {
                    ret.Add(new LogicOutputChanged(_dimmerValue, 0));
                }

                return ret;
            }

            if (instance.ObjId == _value.ObjId)
            {
                int intValue = Convert.ToInt32(value);

                if (_lastValue.HasValue && _lastValue.Value == intValue)
                {
                    return new List<ILogicOutputChanged>();
                }
                _lastValue = intValue;

                return new List<ILogicOutputChanged> { new LogicOutputChanged(_dimmerState, intValue > 0) , new LogicOutputChanged(_dimmerValue, intValue) };
            }

            if (instance.ObjId == _reset.ObjId)
            {
                return SingleOutputChanged(new LogicOutputChanged(_dimmerValue, 0));
            }

            return new List<ILogicOutputChanged>();
        }

        public override object GetDataForVisu()
        {
            return base.GetDataForVisu();
        }
    }
}
