using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using LogicInterfaceDirection = Automatica.Core.Base.Templates.LogicInterfaceDirection;

namespace P3.Logic.Math.BasicOperations.Stepper
{
    public class StepperLogicFactory : LogicFactory
    {
        internal const double DefaultStep = 1;
        internal const double DefaultMaximum = 10;

        public static readonly Guid RuleStep = new Guid("b85d8ab1-745b-4606-ae4b-a010e6936646");
        public static readonly Guid RuleOff = new Guid("15eaa00c-3ff4-41f0-949b-1c6c9c673369");
        public static readonly Guid RuleOutput = new Guid("c3e37696-f15e-41a5-8c83-176408fa18cc");
        public static readonly Guid RuleParamDown = new Guid("53bb220f-c5f2-41ea-99df-0d29e14060fb");
        public static readonly Guid RuleParamStep = new Guid("5b7561f4-7db4-4c00-8418-644168b9c096");
        public static readonly Guid RuleParamMaximum = new Guid("a3fe429c-448b-4cc3-940d-72320a477e9c");

        public override string LogicName => "Math.Stepper";
        public override Version LogicVersion => new Version(1, 0, 0, 0);
        public override Guid LogicGuid => new Guid("264a1a18-1580-4451-8b38-cebcc3cf846a");

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, "MATH.STEPPER.NAME", "MATH.STEPPER.DESCRIPTION", "math.stepper", "MATH.NAME", 100, 100);

            factory.CreateLogicInterfaceTemplate(RuleStep, "S", "MATH.STEPPER.STEP_INPUT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, 1);
            factory.CreateLogicInterfaceTemplate(RuleOff, "Off", "MATH.LOXONE.OFF.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, 2);

            factory.CreateLogicInterfaceTemplate(RuleOutput, "V", "MATH.STEPPER.OUTPUT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Output, 0, 1);

            factory.CreateParameterLogicInterfaceTemplate(RuleParamDown, "MATH.STEPPER.DOWN.NAME", "MATH.STEPPER.DOWN.DESCRIPTION", "dir", LogicGuid, 1, RuleInterfaceParameterDataType.Bool, false);
            factory.CreateParameterLogicInterfaceTemplate(RuleParamStep, "MATH.STEPPER.STEP.NAME", "MATH.STEPPER.STEP.DESCRIPTION", "sts", LogicGuid, 2, RuleInterfaceParameterDataType.Double, DefaultStep);
            factory.CreateParameterLogicInterfaceTemplate(RuleParamMaximum, "MATH.STEPPER.MAXIMUM.NAME", "MATH.STEPPER.MAXIMUM.DESCRIPTION", "m", LogicGuid, 3, RuleInterfaceParameterDataType.Double, DefaultMaximum);
        }

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new StepperRule(context);
        }
    }
}
