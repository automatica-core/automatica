using System.Collections.Generic;
using System.Linq;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;

namespace P3.Logic.Math.BasicOperations.Counter
{
    /// <summary>
    /// Every 1 on + adds the step, every 1 on - subtracts it, 1 on R sets the count back to 0.
    /// Q is 1 while the count has reached the limit (limit 0 = Q unused). The count survives a reload.
    /// </summary>
    public class UpDownCounterRule : Automatica.Core.Logic.Logic
    {
        private readonly RuleInterfaceInstance _output;
        private readonly RuleInterfaceInstance _reached;

        internal double Step = UpDownCounterLogicFactory.DefaultStep;
        internal double Limit;
        private double _count;

        public UpDownCounterRule(ILogicContext context) : base(context)
        {
            _output = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a => a.This2RuleInterfaceTemplate == UpDownCounterLogicFactory.RuleOutput);
            _reached = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a => a.This2RuleInterfaceTemplate == UpDownCounterLogicFactory.RuleReached);
        }

        internal double Count => _count;

        protected override void ParameterValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            if (instance.This2RuleInterfaceTemplate == UpDownCounterLogicFactory.RuleParamStep)
            {
                Step = MathValue.ToDouble(value) ?? UpDownCounterLogicFactory.DefaultStep;
            }
            else if (instance.This2RuleInterfaceTemplate == UpDownCounterLogicFactory.RuleParamLimit)
            {
                Limit = MathValue.ToDouble(value) ?? 0;
            }

            base.ParameterValueChanged(instance, source, value);
        }

        protected override IList<ILogicOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            if (!MathValue.ToBool(value))
            {
                return new List<ILogicOutputChanged>();
            }

            var template = instance.This2RuleInterfaceTemplate;
            if (template == UpDownCounterLogicFactory.RuleReset)
            {
                _count = 0;
            }
            else if (template == UpDownCounterLogicFactory.RuleUp)
            {
                _count += Step;
            }
            else if (template == UpDownCounterLogicFactory.RuleDown)
            {
                _count -= Step;
            }
            else
            {
                return new List<ILogicOutputChanged>();
            }

            return new List<ILogicOutputChanged>
            {
                new LogicOutputChanged(_output, _count),
                new LogicOutputChanged(_reached, Limit != 0 && _count >= Limit)
            };
        }
    }
}
