using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Rule;
using RuleInterfaceDirection = Automatica.Core.Base.Templates.RuleInterfaceDirection;

namespace P3.Rule.DigitalToAnalog.Trigger
{
    public class TriggerRuleFactory : RuleFactory
    {
        public static readonly Guid RuleInput = new Guid("5e8b998d-6e89-4ae9-bed2-a2045ae2ca9d");
        public static readonly Guid RuleValueInput = new Guid("a3068c7e-ca1e-4de9-b25e-ceaaba33e37b");

        public static readonly Guid RuleOutput = new Guid("48f1a2d7-2a9d-40a5-ae0d-064538014f37");

        public static readonly Guid DelayParameter = new Guid("62e4a6c7-9748-4339-b085-59b7c8969ca2");
        public static readonly Guid ValueParameter = new Guid("95af86b8-cff1-4039-8849-bed814fdf468");

        public override Version RuleVersion => new Version(1, 0, 0, 3);

        public override string RuleName => "Trigger";
        public override Guid RuleGuid => new Guid("94a9071e-f0b1-4e83-86d2-ea0a0b565e22");

        public override void InitTemplates(IRuleTemplateFactory factory)
        {
            factory.CreateRuleTemplate(RuleGuid, "DIGITAL_TO_ANALOG.TRIGGER.NAME", "DIGITAL_TO_ANALOG.TRIGGER.DESCRIPTION", "digital_to_analog.trigger", "DIGITAL_TO_ANALOG.NAME", 100, 100);

            factory.CreateRuleInterfaceTemplate(RuleInput, "I", "DIGITAL_TO_ANALOG.TRIGGER.INPUT.DESCRIPTION", RuleGuid, RuleInterfaceDirection.Input, 1, 1);
            factory.CreateRuleInterfaceTemplate(RuleValueInput, "V", "DIGITAL_TO_ANALOG.TRIGGER.VALUE.DESCRIPTION", RuleGuid, RuleInterfaceDirection.Input, 1, 2);

            factory.CreateRuleInterfaceTemplate(RuleOutput, "O", "DIGITAL_TO_ANALOG.TRIGGER.OUTPUT.DESCRIPTION", RuleGuid, RuleInterfaceDirection.Output, 0, 1);

            factory.CreateParameterRuleInterfaceTemplate(DelayParameter, "DIGITAL_TO_ANALOG.TRIGGER.DELAY.NAME", "DIGITAL_TO_ANALOG.TRIGGER.DELAY.DESCRIPTION", RuleGuid, 1, RuleInterfaceParameterDataType.Integer, 0);
            factory.CreateParameterRuleInterfaceTemplate(ValueParameter, "DIGITAL_TO_ANALOG.TRIGGER.VALUE_PARAM.NAME", "DIGITAL_TO_ANALOG.TRIGGER.VALUE_PARAM.DESCRIPTION", RuleGuid, 1, RuleInterfaceParameterDataType.Text, "");
        }

        public override IRule CreateRuleInstance(IRuleContext context)
        {
            return new TriggerRule(context);
        }
    }
}
