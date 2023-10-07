using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.Logic;

namespace P3.Logic.Compare.BaseOperations.Smaller
{
    public class SmallerLogicFactory : LogicFactory
    {
        public static readonly Guid RuleInput1 = new Guid("e558ee37-2fdd-47d9-a923-674e72947f38");
        public static readonly Guid RuleInput2 = new Guid("cca4474a-d9c3-44d3-86c3-4d20c8d33fe1");
        public static readonly Guid RuleOutput = new Guid("bbaf45f0-44ac-4d5f-9572-9bd20aeabac5");

        public override string LogicName => "Compare.Smaller";
        public override Version LogicVersion => new Version(1, 0, 0, 1);
        public override Guid LogicGuid => new Guid("09c0af42-153c-45e1-b8d7-292f682a2eff");

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, "COMPARE.SMALLER.NAME", "COMPARE.SMALLER.DESCRIPTION",
                "compare.smaller", "COMPARE.NAME", 100, 100);

            factory.CreateLogicInterfaceTemplate(RuleInput1, "I1", "COMPARE.SMALLER.INPUT1.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, 1);
            factory.CreateLogicInterfaceTemplate(RuleInput2, "I2", "COMPARE.SMALLER.INPUT2.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, 2);

            factory.CreateLogicInterfaceTemplate(RuleOutput, "O", "COMPARE.SMALLER.OUTPUT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Output, 0, 1);
        }

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new SmallerRule(context);
        }
    }
}
