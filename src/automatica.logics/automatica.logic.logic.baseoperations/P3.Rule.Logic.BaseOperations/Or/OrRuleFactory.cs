using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Rule;
using RuleInterfaceDirection = Automatica.Core.Base.Templates.RuleInterfaceDirection;

namespace P3.Rule.Logic.BaseOperations.Or
{
    public class OrRuleFactory : RuleFactory
    {
        public static readonly Guid RuleInput1 = new Guid("9d00f9d0-34f8-4f44-9d1f-b4a264d10c72");
        public static readonly Guid RuleInput2 = new Guid("a008582a-2f64-4212-94ee-9165dd39cfad");


        public static readonly Guid RuleOutput = new Guid("6b7874eb-f0c2-421f-a55f-4f1e0bfcd137");

        public override string RuleName => "Logic.Or";
        public override Version RuleVersion => new Version(2, 0, 0, 0);
        public override Guid RuleGuid => new Guid("aa37f47b-8d47-4d43-9121-1992ca89e2f5");

        public override void InitTemplates(IRuleTemplateFactory factory)
        {
            factory.CreateRuleTemplate(RuleGuid, "LOGIC.OR.NAME", "LOGIC.OR.DESCRIPTION", "logic.or", "LOGIC.NAME", 100, 100);

            factory.CreateRuleInterfaceTemplate(RuleInput1, "I1", "LOGIC.OR.INPUT.DESCRIPTION", RuleGuid, RuleInterfaceDirection.Input, 1, 1);
            factory.CreateRuleInterfaceTemplate(RuleInput2, "I2", "LOGIC.OR.INPUT.DESCRIPTION", RuleGuid, RuleInterfaceDirection.Input, 1, 2);

            factory.CreateRuleInterfaceTemplate(RuleOutput, "O", "LOGIC.OR.OUTPUT.DESCRIPTION", RuleGuid, RuleInterfaceDirection.Output, 0, 1, RuleInterfaceType.Status);
        }

        public override IRule CreateRuleInstance(IRuleContext context)
        {
            return new OrRule(context);
        }
    }
}
