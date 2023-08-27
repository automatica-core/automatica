using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;

namespace P3.Logic.Operations.Push
{
    public class PushLogic : Automatica.Core.Logic.Logic
    {
        private readonly RuleInterfaceInstance _input;
        private readonly RuleInterfaceInstance _output;
        private readonly RuleInterfaceInstance _durationParam;

        private int _duration = 300;

        public PushLogic(ILogicContext context) : base(context)
        {
            _input = context.RuleInstance.RuleInterfaceInstance.Single(a =>
                a.This2RuleInterfaceTemplate == PushLogicFactory.RuleInput);
            
            _output = context.RuleInstance.RuleInterfaceInstance.Single(a =>
                a.This2RuleInterfaceTemplate == PushLogicFactory.RuleOutput);
            _durationParam = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
                a.This2RuleInterfaceTemplate == PushLogicFactory.Duration);
        }


        protected override void ParameterValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            if (instance.This2RuleInterfaceTemplate == _durationParam.This2RuleInterfaceTemplate)
            {
                _duration = Convert.ToInt32(value);
                if (_duration <= 0)
                {
                    _duration = 1;
                }
            }
            base.ParameterValueChanged(instance, source, value);
        }

        protected override IList<ILogicOutputChanged> InputValueChanged(RuleInterfaceInstance instance,
            IDispatchable source, object value)
        {
            if (instance.ObjId == _input.ObjId)
            {
                _ = Task.Run(() =>
                {
                    Context.Dispatcher.DispatchValue(new LogicOutputChanged(_output, true).Instance, true);
                    Thread.Sleep(_duration);
                    Context.Dispatcher.DispatchValue(new LogicOutputChanged(_output, false).Instance, false);
                });
            }

            return new List<ILogicOutputChanged>();
        }

        public override object GetDataForVisu()
        {
            return base.GetDataForVisu();
        }
    }
}
