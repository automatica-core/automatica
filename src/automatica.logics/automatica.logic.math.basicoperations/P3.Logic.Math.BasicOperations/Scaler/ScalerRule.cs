using System.Collections.Generic;
using System.Linq;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;

namespace P3.Logic.Math.BasicOperations.Scaler
{
    /// <summary>
    /// Maps V linearly from [input min, input max] to [output min, output max], e.g. 0-10 V to 0-100 %.
    /// With "limit" the result stays inside the output range.
    /// </summary>
    public class ScalerRule : Automatica.Core.Logic.Logic
    {
        private readonly RuleInterfaceInstance _output;

        internal double InMin;
        internal double InMax = ScalerLogicFactory.DefaultInMax;
        internal double OutMin;
        internal double OutMax = ScalerLogicFactory.DefaultOutMax;
        internal bool Limit = true;

        public ScalerRule(ILogicContext context) : base(context)
        {
            _output = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a => a.This2RuleInterfaceTemplate == ScalerLogicFactory.RuleOutput);
        }

        protected override void ParameterValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            var template = instance.This2RuleInterfaceTemplate;
            if (template == ScalerLogicFactory.RuleParamInMin)
            {
                InMin = MathValue.ToDouble(value) ?? 0;
            }
            else if (template == ScalerLogicFactory.RuleParamInMax)
            {
                InMax = MathValue.ToDouble(value) ?? ScalerLogicFactory.DefaultInMax;
            }
            else if (template == ScalerLogicFactory.RuleParamOutMin)
            {
                OutMin = MathValue.ToDouble(value) ?? 0;
            }
            else if (template == ScalerLogicFactory.RuleParamOutMax)
            {
                OutMax = MathValue.ToDouble(value) ?? ScalerLogicFactory.DefaultOutMax;
            }
            else if (template == ScalerLogicFactory.RuleParamLimit)
            {
                Limit = value == null || MathValue.ToBool(value);
            }

            base.ParameterValueChanged(instance, source, value);
        }

        internal double? Scale(double v)
        {
            if (InMax == InMin)
            {
                return null;
            }

            var result = OutMin + (v - InMin) * (OutMax - OutMin) / (InMax - InMin);
            if (Limit)
            {
                result = System.Math.Clamp(result, System.Math.Min(OutMin, OutMax), System.Math.Max(OutMin, OutMax));
            }

            return result;
        }

        protected override IList<ILogicOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            if (instance.This2RuleInterfaceTemplate != ScalerLogicFactory.RuleInput)
            {
                return new List<ILogicOutputChanged>();
            }

            var v = MathValue.ToDouble(value);
            var result = v.HasValue ? Scale(v.Value) : null;
            if (!result.HasValue)
            {
                return new List<ILogicOutputChanged>();
            }

            return SingleOutputChanged(new LogicOutputChanged(_output, result.Value));
        }
    }
}
