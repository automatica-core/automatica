using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.Logic;

namespace P3.Logic.Compare.BaseOperations.SmallerOrEqual
{
    public class SmallerOrEqualLogicFactory : LogicFactory
    {
        public static readonly Guid RuleInput1 = new Guid("e6f88a1a-7176-4893-8897-1fc378e3fc49");
        public static readonly Guid RuleInput2 = new Guid("d9d3a46c-71d9-45e9-b404-c7b69c922c78");
        public static readonly Guid RuleOutput = new Guid("cf71ef57-7df0-47e6-b9d7-f7b670fadd6a");

        public override string LogicName => "Compare.SmallerOrEqual";
        public override Version LogicVersion => new Version(1, 0, 0, 0);
        public override Guid LogicGuid => new Guid("f44f80cd-4261-476b-99c2-fe7878f44724");
        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, "COMPARE.SMALLER_OR_EQUAL.NAME", "COMPARE.SMALLER_OR_EQUAL.DESCRIPTION",
                "compare.smaller_or_equal", "COMPARE.NAME", 100, 100);

            factory.CreateLogicInterfaceTemplate(RuleInput1, "I1", "COMPARE.SMALLER_OR_EQUAL.INPUT1.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, 1);
            factory.CreateLogicInterfaceTemplate(RuleInput2, "I2", "COMPARE.SMALLER_OR_EQUAL.INPUT2.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, 1);

            factory.CreateLogicInterfaceTemplate(RuleOutput, "O", "COMPARE.SMALLER_OR_EQUAL.OUTPUT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Output, 0, 1);
        }

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new SmallerOrEqualRule(context);
        }
    }
}
