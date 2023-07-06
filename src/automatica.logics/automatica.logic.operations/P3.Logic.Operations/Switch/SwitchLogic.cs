using System.Collections.Generic;
using System.Linq;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;

namespace P3.Logic.Operations.Switch
{
    public class SwitchLogic : Automatica.Core.Logic.Logic
    {
        private readonly RuleInterfaceInstance _input;
        private readonly RuleInterfaceInstance _output;

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
                return SingleOutputChanged(new LogicOutputChanged(_output, value));
            }

            return new List<ILogicOutputChanged>();
        }

        public override object GetDataForVisu()
        {
            return base.GetDataForVisu();
        }
    }
}
