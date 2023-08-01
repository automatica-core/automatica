using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using LogicInterfaceDirection = Automatica.Core.Base.Templates.LogicInterfaceDirection;

namespace P3.Logic.Logic.BaseOperations.Not
{
    public class NotLogicFactory : LogicFactory
    {
        public static readonly Guid RuleInput1 = new Guid("6987f1a9-f9fb-4b26-b040-367751890ef1");
        public static readonly Guid RuleOutput = new Guid("9304486a-977f-4205-a95a-7bfeae64f6c9");

        public override string LogicName => "Logic.Not";
        public override Version LogicVersion => new Version(2, 0, 0, 0);
        public override Guid LogicGuid => new Guid("19113617-96c2-4f31-9af9-ac00e0c04630");

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, "LOGIC.NOT.NAME", "LOGIC.NOT.DESCRIPTION", "logic.not", "LOGIC.NAME", 100, 100);
            factory.CreateLogicInterfaceTemplate(RuleInput1, "I1", "LOGIC.NOT.INPUT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, 1);

            factory.CreateLogicInterfaceTemplate(RuleOutput, "O", "LOGIC.NOT.OUTPUT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Output, 0, 1, RuleInterfaceType.Status);
        }

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new NotRule(context);
        }
    }
}
