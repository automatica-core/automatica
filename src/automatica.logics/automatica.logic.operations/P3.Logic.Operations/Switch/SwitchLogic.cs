using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.Control.Base;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;

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
        }

        protected override IList<ILogicOutputChanged> InputValueChanged(RuleInterfaceInstance instance,
            IDispatchable source, object value)
        {
            if (instance.ObjId == _input.ObjId)
            {
                _value = Convert.ToBoolean(value);
                return SingleOutputChanged(new LogicOutputChanged(_output, value));
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
            Context.Dispatcher.DispatchValue(new LogicInterfaceInstanceDispatchable(_output), _value);
            return Task.FromResult(true);
        }

        public Task<bool> SwitchAsync(SwitchState state, CancellationToken cancellationToken = new CancellationToken())
        {
            _value = state == SwitchState.On;
            Context.Dispatcher.DispatchValue(new LogicInterfaceInstanceDispatchable(_output), _value);
            return Task.FromResult(true);
        }

        public Task<bool> SwitchOnAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            _value = true;
            Context.Dispatcher.DispatchValue(new LogicInterfaceInstanceDispatchable(_output), _value);
            return Task.FromResult(true);
        }

        public Task<bool> SwitchOffAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            _value = false;
            Context.Dispatcher.DispatchValue(new LogicInterfaceInstanceDispatchable(_output), _value);
            return Task.FromResult(true);
        }

        public SwitchState State => _value ? SwitchState.On : SwitchState.Off;
    }
}
