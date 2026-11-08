using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using LogicInterfaceDirection = Automatica.Core.Base.Templates.LogicInterfaceDirection;

namespace P3.Logic.Math.BasicOperations.Analog
{
    public class MovingAverageLogicFactory : LogicFactory
    {
        internal const double DefaultCycle = 60;
        internal const int DefaultCount = 60;
        internal const int MaximumCount = 1000;

        public static readonly Guid RuleValue = new Guid("0433ac16-50ee-425f-8d53-2211b30641cd");
        public static readonly Guid RuleReset = new Guid("06722ee5-b414-426f-97f1-983af4a81938");
        public static readonly Guid RuleOutput = new Guid("359b3aa5-8654-4ab3-9336-36f7e3b6cbd7");
        public static readonly Guid RuleParamCycle = new Guid("3f2a6d16-367c-40f8-b2a1-405527876f88");
        public static readonly Guid RuleParamCount = new Guid("779198e8-3438-4b96-aae1-73b79f95fdbe");

        public override string LogicName => "Math.MovingAverage";
        public override Version LogicVersion => new Version(1, 0, 0, 0);
        public override Guid LogicGuid => new Guid("32f71e08-8702-4839-b643-54b282c8d382");

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, "MATH.MOVING_AVERAGE.NAME", "MATH.MOVING_AVERAGE.DESCRIPTION", "math.moving_average", "MATH.NAME", 100, 100);

            factory.CreateLogicInterfaceTemplate(RuleValue, "V", "MATH.MOVING_AVERAGE.VALUE.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, 1);
            factory.CreateLogicInterfaceTemplate(RuleReset, "R", "MATH.MOVING_AVERAGE.RESET.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, 2);

            factory.CreateLogicInterfaceTemplate(RuleOutput, "Avg", "MATH.MOVING_AVERAGE.OUTPUT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Output, 0, 1);

            factory.CreateParameterLogicInterfaceTemplate(RuleParamCycle, "MATH.MOVING_AVERAGE.CYCLE.NAME", "MATH.MOVING_AVERAGE.CYCLE.DESCRIPTION", "c", LogicGuid, 1, RuleInterfaceParameterDataType.Double, DefaultCycle);
            factory.CreateParameterLogicInterfaceTemplate(RuleParamCount, "MATH.MOVING_AVERAGE.COUNT.NAME", "MATH.MOVING_AVERAGE.COUNT.DESCRIPTION", "n", LogicGuid, 2, RuleInterfaceParameterDataType.Integer, DefaultCount);
        }

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new MovingAverageRule(context);
        }
    }
}
