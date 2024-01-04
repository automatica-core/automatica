using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.Control.Base;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using Newtonsoft.Json.Linq;

namespace P3.Logic.Operations.Dimmer
{
    public class DimmerLogic: Automatica.Core.Logic.Logic, IDimmer
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

        public Guid Id => Context.RuleInstance.ObjId;
        public string Name => Context.RuleInstance.Name;
        public Task<bool> SwitchAsync(bool state, CancellationToken cancellationToken = new CancellationToken())
        {
            _lastState = state;
            Context.Dispatcher.DispatchValue(new LogicInterfaceInstanceDispatchable(_dimmerState), _value);
            return Task.FromResult(true);
        }

        public Task<bool> SwitchAsync(SwitchState state, CancellationToken cancellationToken = new CancellationToken())
        {
            _lastState = state == SwitchState.On;
            Context.Dispatcher.DispatchValue(new LogicInterfaceInstanceDispatchable(_dimmerState), _value);
            return Task.FromResult(true);
        }

        public Task<bool> SwitchOnAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            _lastState = true;
            Context.Dispatcher.DispatchValue(new LogicInterfaceInstanceDispatchable(_dimmerState), _value);
            return Task.FromResult(true);
        }

        public Task<bool> SwitchOffAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            _lastState = false;
            Context.Dispatcher.DispatchValue(new LogicInterfaceInstanceDispatchable(_dimmerState), _value);
            return Task.FromResult(true);
        }

        public Task<bool> DimAsync(int value, CancellationToken cancellationToken = default)
        { 

            if (value > 0)
            {
                if (value > 100)
                {
                    value = 100;
                }
                _lastState = true;
            }
            else
            {
                _lastState = false;
            }
            _lastValue = value;

            Context.Dispatcher.DispatchValue(new LogicInterfaceInstanceDispatchable(_dimmerValue), _lastValue);
            Context.Dispatcher.DispatchValue(new LogicInterfaceInstanceDispatchable(_dimmerState), _lastState);
            return Task.FromResult(true);
        }

        public SwitchState State => _lastState.HasValue && _lastState.Value ? SwitchState.On : SwitchState.Off;
        public Guid InputId => _state.ObjId;
        public Guid OutputId => _dimmerState.ObjId;
        public Guid DimmerOutputValueId => _dimmerValue.ObjId;
        public Guid DimmerInputValueId => _value.ObjId;
        public bool DimmerState => _lastState ?? false;
        public int DimmerValue => _lastValue ?? 0;
    }
}
