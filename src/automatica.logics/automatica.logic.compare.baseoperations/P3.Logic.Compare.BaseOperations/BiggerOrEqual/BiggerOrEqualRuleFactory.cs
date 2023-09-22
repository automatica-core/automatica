using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.Logic;

namespace P3.Logic.Compare.BaseOperations.BiggerOrEqual
{
    public class BiggerOrEqualLogicFactory : LogicFactory
    {
        public static readonly Guid RuleInput1 = new Guid("bcb9b541-b51c-468a-9906-c6f31c9c77aa");
        public static readonly Guid RuleInput2 = new Guid("faeb38a1-bdc8-440a-a1c3-8fd75db9119c");
        public static readonly Guid RuleOutput = new Guid("f1002b54-506e-461d-9490-854ff8fc6042");

        public override string LogicName => "Compare.BiggerOrEqual";
        public override Version LogicVersion => new Version(1, 0, 0, 1);
        public override Guid LogicGuid => new Guid("f3dda9bc-5f7c-4100-8187-ef5b4ea46bd7");

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, "COMPARE.BIGGER_OR_EQUAL.NAME", "COMPARE.BIGGER_OR_EQUAL.DESCRIPTION",
                "compare.bigger_or_equal", "COMPARE.NAME", 100, 100);

            factory.CreateLogicInterfaceTemplate(RuleInput1, "I1", "COMPARE.BIGGER_OR_EQUAL.INPUT1.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, 1);
            factory.CreateLogicInterfaceTemplate(RuleInput2, "I2", "COMPARE.BIGGER_OR_EQUAL.INPUT2.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, 2);

            factory.CreateLogicInterfaceTemplate(RuleOutput, "O", "COMPARE.BIGGER_OR_EQUAL.OUTPUT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Output, 0, 1);
        }

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new BiggerOrEqualRule(context);
        }
    }
}
