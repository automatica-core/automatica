using System.Collections.Generic;
using System.Linq;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;

namespace P3.Logic.Logic.BaseOperations.EdgeDetection
{
    /// <summary>
    /// Sends a short pulse (1 then 0) on the rising output when the input goes 0 -> 1
    /// and on the falling output when it goes 1 -> 0. Repeated equal values send nothing.
    /// </summary>
    public class EdgeDetectionRule : Automatica.Core.Logic.Logic
    {
        private readonly RuleInterfaceInstance _rising;
        private readonly RuleInterfaceInstance _falling;
        private bool _level;

        public EdgeDetectionRule(ILogicContext context) : base(context)
        {
            _rising = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a => a.This2RuleInterfaceTemplate == EdgeDetectionLogicFactory.RuleRising);
            _falling = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a => a.This2RuleInterfaceTemplate == EdgeDetectionLogicFactory.RuleFalling);
        }

        protected override IList<ILogicOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            if (instance.This2RuleInterfaceTemplate != EdgeDetectionLogicFactory.RuleInput)
            {
                return new List<ILogicOutputChanged>();
            }

            var level = Helper.ConvertValueToBool(value);
            if (level == _level)
            {
                return new List<ILogicOutputChanged>();
            }

            _level = level;
            Pulse(level ? _rising : _falling);
            return new List<ILogicOutputChanged>();
        }

        private void Pulse(RuleInterfaceInstance output)
        {
            // dispatched here and in this order - returned values could let the 0 overtake the 1
            var on = new LogicOutputChanged(output, true);
            Context.Dispatcher.DispatchValue(on.Instance, true);
            Context.Dispatcher.DispatchValue(on.Instance, false);
        }
    }
}
