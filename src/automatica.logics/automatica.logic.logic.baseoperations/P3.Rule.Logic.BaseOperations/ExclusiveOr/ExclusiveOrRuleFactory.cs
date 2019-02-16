using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.Rule;

namespace P3.Rule.Logic.BaseOperations.ExclusiveOr
{
    public class ExclusiveOrRuleFactory : RuleFactory
    {
        public static readonly Guid RuleInput1 = new Guid("c362dd5d-6a33-4fc5-9d97-12ab77d03ce8");
        public static readonly Guid RuleInput2 = new Guid("f91adad2-11a0-415f-9a9f-6712f5a0b361");


        public static readonly Guid RuleOutput = new Guid("85248065-c7c0-458e-af4b-488a287bed9a");

        public override string RuleName => "Logic.ExclusiveOr";
        public override Version RuleVersion => new Version(1, 0, 0, 0);
        public override Guid RuleGuid => new Guid("0f6ddef4-0522-4181-ab0b-32b4e68ea390");

        public override void InitTemplates(IRuleTemplateFactory factory)
        {
            factory.CreateRuleTemplate(RuleGuid, "LOGIC.EXCLUSIVE_OR.NAME", "LOGIC.EXCLUSIVE_OR.DESCRIPTION", "logic.exclusive_or", "LOGIC.NAME", 100, 100);

            factory.CreateRuleInterfaceTemplate(RuleInput1, "I1", "LOGIC.EXCLUSIVE_OR.INPUT.DESCRIPTION", RuleGuid, RuleInterfaceDirection.Input, 1, 1);
            factory.CreateRuleInterfaceTemplate(RuleInput2, "I2", "LOGIC.EXCLUSIVE_OR.INPUT.DESCRIPTION", RuleGuid, RuleInterfaceDirection.Input, 1, 2);

            factory.CreateRuleInterfaceTemplate(RuleOutput, "O", "LOGIC.EXCLUSIVE_OR.OUTPUT.DESCRIPTION", RuleGuid, RuleInterfaceDirection.Output, 0, 1);
        }

        public override IRule CreateRuleInstance(IRuleContext context)
        {
            return new ExclusiveOrRule(context);
        }
    }
}
