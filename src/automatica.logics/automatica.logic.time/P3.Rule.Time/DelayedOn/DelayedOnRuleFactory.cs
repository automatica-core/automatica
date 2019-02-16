using System;
using Automatica.Core.Rule;
using Automatica.Core.Base.Templates;

namespace P3.Rule.Time.DelayedOn
{
    public class DelayedOnRuleFactory : RuleFactory
    {
        public static readonly Guid RuleTrigger = new Guid("3d9cf92c-2070-4b00-97b8-e1c3bf1ef720");
        public static readonly Guid RuleParamDelay = new Guid("dbe52a58-a78c-4103-8e17-6856934c08eb");


        public static readonly Guid RuleOutput = new Guid("d716c3aa-cc83-410c-b62f-7597e5ba6c3d");

        public override string RuleName => "Time.DelayedOn";
        public override Version RuleVersion => new Version(0, 1, 0, 2);
        public override Guid RuleGuid => new Guid("84607422-507d-4156-b5aa-559474d5080e");

        public override void InitTemplates(IRuleTemplateFactory factory)
        {
            factory.CreateRuleTemplate(RuleGuid, "TIME.DELAYON.NAME", "TIME.DELAYON.DESCRIPTION",
                "time.delayed-off", "TIME.NAME", 100, 100);

            factory.CreateRuleInterfaceTemplate(RuleTrigger, "T", "TIME.DELAYON.TRIGGER.DESCRIPTION", RuleGuid, RuleInterfaceDirection.Input, 0, 1);

            factory.CreateRuleInterfaceTemplate(RuleOutput, "O", "TIME.DELAYON.OUTPUT.DESCRIPTION", RuleGuid, RuleInterfaceDirection.Output, 0, 1);

            factory.CreateParameterRuleInterfaceTemplate(RuleParamDelay, "TIME.DELAY.NAME", "TIME.DELAY.DESCRIPTION", RuleGuid, 1, Automatica.Core.EF.Models.RuleInterfaceParameterDataType.Integer, 5, true);
        }

        public override IRule CreateRuleInstance(IRuleContext context)
        {
            return new DelayedOnRule(context);
        }
    }
}
