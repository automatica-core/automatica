using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Rule;
using RuleInterfaceDirection = Automatica.Core.Base.Templates.RuleInterfaceDirection;

namespace P3.Rule.DigitalToAnalog.StateToImpuls
{
    public class StateToImpulsRuleFactory : RuleFactory
    {
        public static readonly Guid RuleInput = new Guid("6a2dd6e5-3d36-41ff-bb2e-ad04060519dc");
        public static readonly Guid RuleOutput = new Guid("32d1bc7d-14e4-4033-a5bb-40da0db6adad");

        public static readonly Guid DelayParameter = new Guid("fe035850-de9a-4648-ac7e-57a6ef20cd89");

        public override Version RuleVersion => new Version(1, 0, 0, 3);

        public override string RuleName => "StatusToImpuls";
        public override Guid RuleGuid => new Guid("088dfa08-6487-4f5e-8d77-2ce4402ba661");

        public override void InitTemplates(IRuleTemplateFactory factory)
        {
            factory.CreateRuleTemplate(RuleGuid, "DIGITAL_TO_ANALOG.STATE_TO_IMPULS.NAME", "DIGITAL_TO_ANALOG.STATE_TO_IMPULS.DESCRIPTION", "digital_to_analog.state_to_imopuls", "DIGITAL_TO_ANALOG.NAME", 100, 100);

            factory.CreateRuleInterfaceTemplate(RuleInput, "I", "DIGITAL_TO_ANALOG.STATE_TO_IMPULS.INPUT.DESCRIPTION", RuleGuid, RuleInterfaceDirection.Input, 1, 1);
            factory.CreateRuleInterfaceTemplate(RuleOutput, "O", "DIGITAL_TO_ANALOG.STATE_TO_IMPULS.OUTPUT.DESCRIPTION", RuleGuid, RuleInterfaceDirection.Output, 0, 1);

            factory.CreateParameterRuleInterfaceTemplate(DelayParameter, "DIGITAL_TO_ANALOG.STATE_TO_IMPULS.DELAY.NAME", "DIGITAL_TO_ANALOG.STATE_TO_IMPULS.DELAY.DESCRIPTION", RuleGuid, 1, RuleInterfaceParameterDataType.Integer, 1000);
        }

        public override IRule CreateRuleInstance(IRuleContext context)
        {
            return new StateToImpulsRule(context);
        }
    }
}
