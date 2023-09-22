using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.Logic;

namespace P3.Logic.Compare.BaseOperations.Equal
{
    public class EqualLogicFactory : LogicFactory
    {
        public static readonly Guid RuleInput1 = new Guid("67443626-60fe-4aa1-b696-af5e41857508");
        public static readonly Guid RuleInput2 = new Guid("53376d1f-1684-4653-8c03-6497968cc142");
        public static readonly Guid RuleOutput = new Guid("ece069c5-7363-48a3-ab31-1f3202bfa2ad");

        public override string LogicName => "Compare.Equal";
        public override Version LogicVersion => new Version(1, 0, 0, 1);
        public override Guid LogicGuid => new Guid("c79e65da-41ed-4bfe-8a7e-ac5fec54539a");
        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, "COMPARE.EQUAL.NAME", "COMPARE.EQUAL.DESCRIPTION",
                "compare.equal", "COMPARE.NAME", 100, 100);

            factory.CreateLogicInterfaceTemplate(RuleInput1, "I1", "COMPARE.EQUAL.INPUT1.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, 1);
            factory.CreateLogicInterfaceTemplate(RuleInput2, "I2", "COMPARE.EQUAL.INPUT2.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, 2);

            factory.CreateLogicInterfaceTemplate(RuleOutput, "O", "COMPARE.EQUAL.OUTPUT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Output, 0, 1);
        }

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new EqualRule(context);
        }
    }
}
