using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Rule;
using RuleInterfaceDirection = Automatica.Core.Base.Templates.RuleInterfaceDirection;

namespace P3.Logic.Lightning.FlashingLights
{
    internal class FlashingLightsRuleFactory : RuleFactory
    {
        public override string RuleName => "FlashingLights";
        public override Guid RuleGuid => new Guid("2c55738a-221a-456b-8923-a6eead3de823");
        public override Version RuleVersion => new Version(0, 3, 0, 0);

        public static readonly Guid Trigger = new Guid("a5fd09a9-64f6-4eec-98a3-52740e30c9ac");
        public static readonly Guid State = new Guid("03fc863e-e657-4f83-afe1-fe29920f6615");

        public static readonly Guid Output = new Guid("57157462-715d-4d3d-92c2-7e373bdadcc7");

        public static readonly Guid Delay = new Guid("a320d9bc-0c8c-4e5b-93a3-b96546c426c5");

        public override void InitTemplates(IRuleTemplateFactory factory)
        {
            factory.CreateRuleTemplate(RuleGuid, "LIGHTNING.FLASHING_LIGHTS.NAME", "LIGHTNING.FLASHING_LIGHTS.DESCRIPTION",
                "lightning.flashing_lights", "LIGHTNING.NAME", 100, 100);

            factory.CreateRuleInterfaceTemplate(Trigger, "LIGHTNING.FLASHING_LIGHTS.TRIGGER.NAME", "LIGHTNING.FLASHING_LIGHTS.TRIGGER.DESCRIPTION", RuleGuid, RuleInterfaceDirection.Input, 1, 1);
            factory.CreateRuleInterfaceTemplate(State, "LIGHTNING.FLASHING_LIGHTS.STATE.NAME", "LIGHTNING.FLASHING_LIGHTS.STATE.DESCRIPTION", RuleGuid, RuleInterfaceDirection.Input, 1, 2);

            factory.CreateRuleInterfaceTemplate(Output, "LIGHTNING.FLASHING_LIGHTS.OUTPUT.NAME", "LIGHTNING.FLASHING_LIGHTS.OUTPUT.DESCRIPTION", RuleGuid, RuleInterfaceDirection.Output, 1, 3);

            factory.CreateParameterRuleInterfaceTemplate(Delay, "LIGHTNING.FLASHING_LIGHTS.DELAY.NAME",
                "LIGHTNING.FLASHING_LIGHTS.DELAY.DESCRIPTION", RuleGuid, 1, RuleInterfaceParameterDataType.Integer,
                1000, false);

        }

        public override IRule CreateRuleInstance(IRuleContext context)
        {
            return new FlashingLightsRule(context);
        }

    }
}
