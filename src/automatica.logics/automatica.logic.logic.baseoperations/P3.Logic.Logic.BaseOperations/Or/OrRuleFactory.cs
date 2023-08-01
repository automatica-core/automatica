using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using LogicInterfaceDirection = Automatica.Core.Base.Templates.LogicInterfaceDirection;

namespace P3.Logic.Logic.BaseOperations.Or
{
    public class OrLogicFactory : LogicFactory
    {
        public static readonly Guid RuleInput1 = new Guid("9d00f9d0-34f8-4f44-9d1f-b4a264d10c72");
        public static readonly Guid RuleInput2 = new Guid("a008582a-2f64-4212-94ee-9165dd39cfad");


        public static readonly Guid RuleOutput = new Guid("6b7874eb-f0c2-421f-a55f-4f1e0bfcd137");

        public override string LogicName => "Logic.Or";
        public override Version LogicVersion => new Version(2, 0, 0, 0);
        public override Guid LogicGuid => new Guid("aa37f47b-8d47-4d43-9121-1992ca89e2f5");

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, "LOGIC.OR.NAME", "LOGIC.OR.DESCRIPTION", "logic.or", "LOGIC.NAME", 100, 100);

            factory.CreateLogicInterfaceTemplate(RuleInput1, "I1", "LOGIC.OR.INPUT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, 1);
            factory.CreateLogicInterfaceTemplate(RuleInput2, "I2", "LOGIC.OR.INPUT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, 2);

            factory.CreateLogicInterfaceTemplate(RuleOutput, "O", "LOGIC.OR.OUTPUT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Output, 0, 1, RuleInterfaceType.Status);
        }

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new OrRule(context);
        }
    }
}
