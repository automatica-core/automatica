using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.Logic;

namespace P3.Logic.Compare.BaseOperations.Bigger
{
    public class BiggerLogicFactory : LogicFactory
    {
        public static readonly Guid RuleInput1 = new Guid("967ec11f-e1aa-48a8-bd5d-19cf403d0e86");
        public static readonly Guid RuleInput2 = new Guid("3531c209-4c5d-459c-a418-264691cbe5b2");
        public static readonly Guid RuleOutput = new Guid("ffdee98d-9f8c-4a9d-bd29-53814959a3f2");

        public override string LogicName => "Compare.Bigger";
        public override Version LogicVersion => new Version(1, 0, 0, 1);
        public override Guid LogicGuid => new Guid("255e4e8a-7f69-49d7-87d9-731917e7a6c0");

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, "COMPARE.BIGGER.NAME", "COMPARE.BIGGER.DESCRIPTION",
                "compare.bigger", "COMPARE.NAME", 100, 100);

            factory.CreateLogicInterfaceTemplate(RuleInput1, "I1", "COMPARE.BIGGER.INPUT1.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, 1);
            factory.CreateLogicInterfaceTemplate(RuleInput2, "I2", "COMPARE.BIGGER.INPUT2.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, 2);

            factory.CreateLogicInterfaceTemplate(RuleOutput, "O", "COMPARE.BIGGER.OUTPUT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Output, 0, 1);
        }

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new BiggerRule(context);
        }
    }
}
