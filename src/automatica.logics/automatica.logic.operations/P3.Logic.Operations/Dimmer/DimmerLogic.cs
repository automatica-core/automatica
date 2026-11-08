using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.Control.Base;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using P3.Logic.Operations.Common;

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
        /// <summary>the last brightness above 0, the presence simulation switches on with it</summary>
        private int _lastOnValue = 100;

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

            PresenceSimulation = new PresenceSimulationHost(context, DimmerLogicFactory.PresenceSimulation, Simulate);
        }

        /// <summary>
        /// The presence simulation; its PS input and parameters are missing on instances created before 1.1, it then never runs
        /// </summary>
        internal PresenceSimulationHost PresenceSimulation { get; }

        protected override Task<bool> Start(RuleInstance instance, CancellationToken token = new CancellationToken())
        {
            PresenceSimulation.Start(instance);
            return base.Start(instance, token);
        }

        protected override Task<bool> Stop(RuleInstance instance, CancellationToken token = new CancellationToken())
        {
            PresenceSimulation.Stop();
            return base.Stop(instance, token);
        }

        protected override void ParameterValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            PresenceSimulation.ApplyParameter(instance.This2RuleInterfaceTemplate, value);
            base.ParameterValueChanged(instance, source, value);
        }

        /// <summary>
        /// Simulated on: the last brightness (100 at first), simulated off: 0
        /// </summary>
        private void Simulate(bool state)
        {
            _lastState = state;
            _lastValue = state ? _lastOnValue : 0;

            Context.Dispatcher.DispatchValue(new LogicInterfaceInstanceDispatchable(_dimmerValue), _lastValue);
            Context.Dispatcher.DispatchValue(new LogicInterfaceInstanceDispatchable(_dimmerState), _lastState);
        }

        /// <summary>
        /// A change made by hand: remembered for the simulation and recorded for the replay
        /// </summary>
        private void Manual(bool on, int? value)
        {
            if (value is > 0)
            {
                _lastOnValue = value.Value;
            }
            PresenceSimulation.Record(on);
        }

        protected override IList<ILogicOutputChanged> InputValueChanged(RuleInterfaceInstance instance,
            IDispatchable source, object value)
        {
            if (instance.ObjId == _state.ObjId && source.Source == DispatchableSource.Visualization)
            {
                var booleanValue = Convert.ToBoolean(value);

                _lastState = booleanValue;
                Manual(booleanValue, null);

                var ret = new List<ILogicOutputChanged> { new LogicOutputChanged(_dimmerState, booleanValue) };

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

            if (instance.ObjId == _value.ObjId && source.Source == DispatchableSource.Visualization)
            {
                var intValue = Convert.ToInt32(value);

                if (_lastValue.HasValue && _lastValue.Value == intValue)
                {
                    return new List<ILogicOutputChanged>();
                }
                _lastValue = intValue;
                Manual(intValue > 0, intValue);

                return new List<ILogicOutputChanged> { new LogicOutputChanged(_dimmerState, intValue > 0) , new LogicOutputChanged(_dimmerValue, intValue) };
            }

            if (instance.ObjId == _reset.ObjId)
            {
                Manual(false, null);
                return SingleOutputChanged(new LogicOutputChanged(_dimmerValue, 0));
            }

            if (PresenceSimulation.ApplyInput(instance.This2RuleInterfaceTemplate, value))
            {
                return new List<ILogicOutputChanged>();
            }

            return new List<ILogicOutputChanged>();
        }

        public Guid Id => Context.RuleInstance.ObjId;
        public string Name => Context.RuleInstance.Name;
        public Task<bool> SwitchAsync(bool state, CancellationToken cancellationToken = new CancellationToken())
        {
            _lastState = state;
            _lastValue = _lastState.Value ? 100 : 0;
            Manual(_lastState.Value, _lastValue);

            Context.Dispatcher.DispatchValue(new LogicInterfaceInstanceDispatchable(_dimmerValue), _lastValue);
            Context.Dispatcher.DispatchValue(new LogicInterfaceInstanceDispatchable(_dimmerState), _lastState);
            return Task.FromResult(true);
        }

        public Task<bool> SwitchAsync(SwitchState state, CancellationToken cancellationToken = new CancellationToken())
        {
            _lastState = state == SwitchState.On;
            _lastValue = _lastState.Value ? 100 : 0;
            Manual(_lastState.Value, _lastValue);

            Context.Dispatcher.DispatchValue(new LogicInterfaceInstanceDispatchable(_dimmerValue), _lastValue);
            Context.Dispatcher.DispatchValue(new LogicInterfaceInstanceDispatchable(_dimmerState), _lastState);
            return Task.FromResult(true);
        }

        public Task<bool> SwitchOnAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            _lastState = true;
            _lastValue = _lastState.Value ? 100 : 0;
            Manual(_lastState.Value, _lastValue);

            Context.Dispatcher.DispatchValue(new LogicInterfaceInstanceDispatchable(_dimmerValue), _lastValue);
            Context.Dispatcher.DispatchValue(new LogicInterfaceInstanceDispatchable(_dimmerState), _lastState);
            return Task.FromResult(true);
        }

        public Task<bool> SwitchOffAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            _lastState = false;
            _lastValue = _lastState.Value ? 100 : 0;
            Manual(_lastState.Value, _lastValue);

            Context.Dispatcher.DispatchValue(new LogicInterfaceInstanceDispatchable(_dimmerValue), _lastValue);
            Context.Dispatcher.DispatchValue(new LogicInterfaceInstanceDispatchable(_dimmerState), _lastState);
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
            Manual(_lastState.Value, value);

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
