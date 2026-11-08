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

namespace P3.Logic.Operations.Switch
{
    public class SwitchLogic : Automatica.Core.Logic.Logic, ISwitch
    {
        private readonly RuleInterfaceInstance _input;
        private readonly RuleInterfaceInstance _output;
        private bool _value;

        public SwitchLogic(ILogicContext context) : base(context)
        {
            _input = context.RuleInstance.RuleInterfaceInstance.Single(a =>
                a.This2RuleInterfaceTemplate == SwitchLogicFactory.RuleInput);
            
            _output = context.RuleInstance.RuleInterfaceInstance.Single(a =>
                a.This2RuleInterfaceTemplate == SwitchLogicFactory.RuleOutput);

            PresenceSimulation = new PresenceSimulationHost(context, SwitchLogicFactory.PresenceSimulation, Simulate);
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

        private void Simulate(bool state)
        {
            _value = state;
            Context.Dispatcher.DispatchValue(new LogicInterfaceInstanceDispatchable(_output), _value);
        }

        protected override IList<ILogicOutputChanged> InputValueChanged(RuleInterfaceInstance instance,
            IDispatchable source, object value)
        {
            if (instance.ObjId == _input.ObjId)
            {
                _value = Convert.ToBoolean(value);
                PresenceSimulation.Record(_value);
                return SingleOutputChanged(new LogicOutputChanged(_output, value));
            }

            if (PresenceSimulation.ApplyInput(instance.This2RuleInterfaceTemplate, value))
            {
                return new List<ILogicOutputChanged>();
            }

            return new List<ILogicOutputChanged>();
        }

        public Guid Id => Context.RuleInstance.ObjId;
        public string Name => Context.RuleInstance.Name;
        public Guid InputId => _input.ObjId;
        public Guid OutputId => _output.ObjId;
        public Task<bool> SwitchAsync(bool state, CancellationToken cancellationToken = new CancellationToken())
        {
            _value = state;
            PresenceSimulation.Record(_value);
            Context.Dispatcher.DispatchValue(new LogicInterfaceInstanceDispatchable(_output), _value);
            return Task.FromResult(true);
        }

        public Task<bool> SwitchAsync(SwitchState state, CancellationToken cancellationToken = new CancellationToken())
        {
            _value = state == SwitchState.On;
            PresenceSimulation.Record(_value);
            Context.Dispatcher.DispatchValue(new LogicInterfaceInstanceDispatchable(_output), _value);
            return Task.FromResult(true);
        }

        public Task<bool> SwitchOnAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            _value = true;
            PresenceSimulation.Record(_value);
            Context.Dispatcher.DispatchValue(new LogicInterfaceInstanceDispatchable(_output), _value);
            return Task.FromResult(true);
        }

        public Task<bool> SwitchOffAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            _value = false;
            PresenceSimulation.Record(_value);
            Context.Dispatcher.DispatchValue(new LogicInterfaceInstanceDispatchable(_output), _value);
            return Task.FromResult(true);
        }

        public SwitchState State => _value ? SwitchState.On : SwitchState.Off;
    }
}
