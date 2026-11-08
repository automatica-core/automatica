using System;
using System.Collections.Generic;
using System.Linq;
using Automatica.Core.Base.IO;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using LogicInterfaceDirection = Automatica.Core.Base.Templates.LogicInterfaceDirection;

namespace P3.Logic.Math.BasicOperations.Analog
{
    public class AnalogLimiterLogicFactory : LogicFactory
    {
        internal const double DefaultMinimum = 0;
        internal const double DefaultMaximum = 10;

        public static readonly Guid RuleValue = new Guid("e7a16a17-c672-4f39-a70a-c2feb7424687");
        public static readonly Guid RuleOutput = new Guid("77eb1276-396c-40bc-8303-855d9de2c384");
        public static readonly Guid RuleParamMinimum = new Guid("e15067c7-182c-4f91-9a81-83ef416791e1");
        public static readonly Guid RuleParamMaximum = new Guid("9edcc615-3704-42a2-b540-fec79e37d28d");

        public override string LogicName => "Math.AnalogLimiter";
        public override Version LogicVersion => new Version(1, 0, 0, 0);
        public override Guid LogicGuid => new Guid("3525d285-d6cc-44f1-a0b5-4fafb1224f6e");

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, "MATH.ANALOG_LIMITER.NAME", "MATH.ANALOG_LIMITER.DESCRIPTION", "math.analog_limiter", "MATH.NAME", 100, 100);

            factory.CreateLogicInterfaceTemplate(RuleValue, "V", "MATH.ANALOG_LIMITER.VALUE.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, 1);
            factory.CreateLogicInterfaceTemplate(RuleOutput, "V", "MATH.ANALOG_LIMITER.OUTPUT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Output, 0, 1);

            factory.CreateParameterLogicInterfaceTemplate(RuleParamMinimum, "MATH.ANALOG_LIMITER.MINIMUM.NAME", "MATH.ANALOG_LIMITER.MINIMUM.DESCRIPTION", "min", LogicGuid, 1, RuleInterfaceParameterDataType.Double, DefaultMinimum);
            factory.CreateParameterLogicInterfaceTemplate(RuleParamMaximum, "MATH.ANALOG_LIMITER.MAXIMUM.NAME", "MATH.ANALOG_LIMITER.MAXIMUM.DESCRIPTION", "max", LogicGuid, 2, RuleInterfaceParameterDataType.Double, DefaultMaximum);
        }

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new AnalogLimiterRule(context);
        }
    }

    /// <summary>
    /// Analog limiter (Loxone "Analogue Limiter (Min/Max)"): the output is V limited to minimum..maximum.
    /// Swapped limits are used the right way round.
    /// </summary>
    public class AnalogLimiterRule : Automatica.Core.Logic.Logic
    {
        private readonly RuleInterfaceInstance _output;

        internal double Minimum = AnalogLimiterLogicFactory.DefaultMinimum;
        internal double Maximum = AnalogLimiterLogicFactory.DefaultMaximum;

        public AnalogLimiterRule(ILogicContext context) : base(context)
        {
            _output = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a => a.This2RuleInterfaceTemplate == AnalogLimiterLogicFactory.RuleOutput);
        }

        protected override void ParameterValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            if (instance.This2RuleInterfaceTemplate == AnalogLimiterLogicFactory.RuleParamMinimum)
            {
                Minimum = MathValue.ToDouble(value) ?? AnalogLimiterLogicFactory.DefaultMinimum;
            }
            else if (instance.This2RuleInterfaceTemplate == AnalogLimiterLogicFactory.RuleParamMaximum)
            {
                Maximum = MathValue.ToDouble(value) ?? AnalogLimiterLogicFactory.DefaultMaximum;
            }

            base.ParameterValueChanged(instance, source, value);
        }

        protected override IList<ILogicOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            var v = MathValue.ToDouble(value);
            if (instance.This2RuleInterfaceTemplate != AnalogLimiterLogicFactory.RuleValue || !v.HasValue)
            {
                return new List<ILogicOutputChanged>();
            }

            var low = System.Math.Min(Minimum, Maximum);
            var high = System.Math.Max(Minimum, Maximum);
            return SingleOutputChanged(new LogicOutputChanged(_output, System.Math.Clamp(v.Value, low, high)));
        }
    }
}
