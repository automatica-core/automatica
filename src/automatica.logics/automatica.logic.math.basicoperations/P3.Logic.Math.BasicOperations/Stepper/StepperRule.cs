using System.Collections.Generic;
using System.Linq;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;

namespace P3.Logic.Math.BasicOperations.Stepper
{
    /// <summary>
    /// Stepper (Loxone "Stepper"): every 1 on S changes V by the step size within 0..maximum.
    /// Upwards V starts at 0 again after the maximum, downwards it continues at the maximum after 0.
    /// Off = 1 sets V to 0 and locks the block until Off is 0 again.
    /// </summary>
    public class StepperRule : Automatica.Core.Logic.Logic
    {
        private readonly RuleInterfaceInstance _output;

        internal bool Down;
        internal double Step = StepperLogicFactory.DefaultStep;
        internal double Maximum = StepperLogicFactory.DefaultMaximum;

        private double _value;
        private bool _locked;

        public StepperRule(ILogicContext context) : base(context)
        {
            _output = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a => a.This2RuleInterfaceTemplate == StepperLogicFactory.RuleOutput);
        }

        protected override void ParameterValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            var template = instance.This2RuleInterfaceTemplate;
            if (template == StepperLogicFactory.RuleParamDown)
            {
                Down = MathValue.ToBool(value);
            }
            else if (template == StepperLogicFactory.RuleParamStep)
            {
                Step = System.Math.Abs(MathValue.ToDouble(value) ?? StepperLogicFactory.DefaultStep);
            }
            else if (template == StepperLogicFactory.RuleParamMaximum)
            {
                Maximum = MathValue.ToDouble(value) ?? StepperLogicFactory.DefaultMaximum;
            }

            base.ParameterValueChanged(instance, source, value);
        }

        protected override IList<ILogicOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            var template = instance.This2RuleInterfaceTemplate;
            var on = MathValue.ToBool(value);

            if (template == StepperLogicFactory.RuleOff)
            {
                _locked = on;
                if (!on)
                {
                    return new List<ILogicOutputChanged>();
                }

                _value = 0;
                return SingleOutputChanged(new LogicOutputChanged(_output, _value));
            }

            if (template != StepperLogicFactory.RuleStep || !on || _locked)
            {
                return new List<ILogicOutputChanged>();
            }

            var max = System.Math.Max(0, Maximum);
            double next;
            if (Down)
            {
                next = System.Math.Round(_value - Step, 10);
                if (next < 0)
                {
                    next = max;
                }
            }
            else
            {
                next = System.Math.Round(_value + Step, 10);
                if (next > max)
                {
                    next = 0;
                }
            }

            _value = next;
            return SingleOutputChanged(new LogicOutputChanged(_output, _value));
        }
    }
}
