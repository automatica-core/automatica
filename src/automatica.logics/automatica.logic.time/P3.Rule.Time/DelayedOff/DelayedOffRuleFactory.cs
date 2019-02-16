using Automatica.Core.Base.Templates;
using Automatica.Core.Rule;
using System;

namespace P3.Rule.Time.DelayedOff
{
    public class DelayedOffRuleFactory : RuleFactory
    {
        public static readonly Guid RuleTrigger = new Guid("50789839-fa2a-4869-b373-58fa73021356");
        public static readonly Guid RuleReset = new Guid("d8c08ce2-08d1-4cf8-8f9d-8a4395247fc3");
        public static readonly Guid RuleParamDelay = new Guid("cb89b73b-b62f-4a38-88fe-78b9bd09c00d");


        public static readonly Guid RuleOutput = new Guid("b3261338-09ee-40d2-b051-efc4d6b577ce");

        public override string RuleName => "Time.DelayedOff";
        public override Version RuleVersion => new Version(0, 1, 0, 1);
        public override Guid RuleGuid => new Guid("61182a90-6dd7-4677-ac06-a8bc38ba46a9");

        public override void InitTemplates(IRuleTemplateFactory factory)
        {
            factory.CreateRuleTemplate(RuleGuid, "TIME.DELAYOFF.NAME", "TIME.DELAYOFF.DESCRIPTION",
                "time.delayed-off", "TIME.NAME", 100, 100);

            factory.CreateRuleInterfaceTemplate(RuleTrigger, "T", "TIME.DELAYOFF.TRIGGER.DESCRIPTION", RuleGuid, RuleInterfaceDirection.Input, 1, 1);
            factory.CreateRuleInterfaceTemplate(RuleReset, "R", "TIME.DELAYOFF.RESET.DESCRIPTION", RuleGuid, RuleInterfaceDirection.Input, 0, 2);

            factory.CreateRuleInterfaceTemplate(RuleOutput, "O", "TIME.DELAYOFF.OUTPUT.DESCRIPTION", RuleGuid, RuleInterfaceDirection.Output, 0, 1);

            factory.CreateParameterRuleInterfaceTemplate(RuleParamDelay, "TIME.DELAY.NAME", "TIME.DELAY.DESCRIPTION", RuleGuid, 1, Automatica.Core.EF.Models.RuleInterfaceParameterDataType.Integer, 5, true);
        }

        public override IRule CreateRuleInstance(IRuleContext context)
        {
            return new DelayedOffRule(context);
        }
    }
}
