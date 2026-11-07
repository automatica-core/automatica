using System.Collections.Generic;
using System.Linq;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;

namespace P3.Logic.Logic.BaseOperations.PushSwitch
{
    /// <summary>
    /// Impulse relay: every 1 on T toggles the output, 1 on S switches on, 1 on R switches off.
    /// A 0 (button released) is ignored, so buttons that send only 1 on press work as well.
    /// </summary>
    public class PushSwitchRule : Automatica.Core.Logic.Logic
    {
        private readonly RuleInterfaceInstance _output;
        private bool _q;

        public PushSwitchRule(ILogicContext context) : base(context)
        {
            _output = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a => a.This2RuleInterfaceTemplate == PushSwitchLogicFactory.RuleOutput);
        }

        protected override IList<ILogicOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            if (!Helper.ConvertValueToBool(value))
            {
                return new List<ILogicOutputChanged>();
            }

            if (instance.This2RuleInterfaceTemplate == PushSwitchLogicFactory.RuleReset)
            {
                _q = false;
            }
            else if (instance.This2RuleInterfaceTemplate == PushSwitchLogicFactory.RuleSet)
            {
                _q = true;
            }
            else if (instance.This2RuleInterfaceTemplate == PushSwitchLogicFactory.RuleToggle)
            {
                _q = !_q;
            }
            else
            {
                return new List<ILogicOutputChanged>();
            }

            return SingleOutputChanged(new LogicOutputChanged(_output, _q));
        }
    }
}
