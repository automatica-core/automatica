using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Rule;
using RuleInterfaceDirection = Automatica.Core.Base.Templates.RuleInterfaceDirection;

namespace P3.Rule.Logic.BaseOperations.Not
{
    public class NotRuleFactory : RuleFactory
    {
        public static readonly Guid RuleInput1 = new Guid("6987f1a9-f9fb-4b26-b040-367751890ef1");
        public static readonly Guid RuleOutput = new Guid("9304486a-977f-4205-a95a-7bfeae64f6c9");

        public override string RuleName => "Logic.Not";
        public override Version RuleVersion => new Version(2, 0, 0, 0);
        public override Guid RuleGuid => new Guid("19113617-96c2-4f31-9af9-ac00e0c04630");

        public override void InitTemplates(IRuleTemplateFactory factory)
        {
            factory.CreateRuleTemplate(RuleGuid, "LOGIC.NOT.NAME", "LOGIC.NOT.DESCRIPTION", "logic.not", "LOGIC.NAME", 100, 100);
            factory.CreateRuleInterfaceTemplate(RuleInput1, "I1", "LOGIC.NOT.INPUT.DESCRIPTION", RuleGuid, RuleInterfaceDirection.Input, 1, 1);

            factory.CreateRuleInterfaceTemplate(RuleOutput, "O", "LOGIC.NOT.OUTPUT.DESCRIPTION", RuleGuid, RuleInterfaceDirection.Output, 0, 1, RuleInterfaceType.Status);
        }

        public override IRule CreateRuleInstance(IRuleContext context)
        {
            return new NotRule(context);
        }
    }
}
