using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Rule;
using RuleInterfaceDirection = Automatica.Core.Base.Templates.RuleInterfaceDirection;

namespace P3.Logic.Operations.Dimmer
{
    public class DimmerLogicFactory : RuleFactory
    {
        public static Guid RuleInputState = new Guid("1753a8a7-18ed-4636-952e-66cdabd73698");
        public static Guid RuleInputValue = new Guid("1d1f873b-9d56-4c28-8373-7024320b1b89");
        public static Guid RuleInputReset = new Guid("b0368661-51fb-46e5-9151-3c7901ffbffe");

        public static Guid RuleState = new Guid("3082aac1-544c-46b0-b9fd-05a54ab2a67f");
        public static Guid RuleOutput = new Guid("59223284-c80f-441f-a647-fc2786e998a4");

        public override string RuleName => "P3.Rule.Operations.Dimmer";
        public override Guid RuleGuid => new Guid("b1010e11-0e8d-4d8c-97df-a7829cd7be96");
        public override Version RuleVersion => new Version(1, 0, 0, 0);
        public override bool InDevelopmentMode => true;

        public override void InitTemplates(IRuleTemplateFactory factory)
        {
            factory.CreateRuleTemplate(RuleGuid, "OPERATIONS.DIMMER.NAME", "OPERATIONS.DIMMER.DESCRIPTION",
                "operations-DIMMER", "OPERATIONS.NAME", 100, 100);

            factory.CreateRuleInterfaceTemplate(RuleInputState, "OPERATIONS.DIMMER.INPUT.STATE.NAME", "OPERATIONS.DIMMER.INPUT.STATE.DESCRIPTION", RuleGuid, RuleInterfaceDirection.Input, 0, 1, RuleInterfaceType.Input);
            factory.CreateRuleInterfaceTemplate(RuleInputValue, "OPERATIONS.DIMMER.INPUT.VALUE.NAME", "OPERATIONS.DIMMER.INPUT.VALUE.DESCRIPTION", RuleGuid, RuleInterfaceDirection.Input, 0, 1, RuleInterfaceType.Status);
            factory.CreateRuleInterfaceTemplate(RuleInputReset, "OPERATIONS.DIMMER.INPUT.RESET.NAME", "OPERATIONS.DIMMER.INPUT.RESET.DESCRIPTION", RuleGuid, RuleInterfaceDirection.Input, 0, 1, RuleInterfaceType.Unknown);

            factory.CreateRuleInterfaceTemplate(RuleOutput, "OPERATIONS.DIMMER.OUTPUT.NAME", "OPERATIONS.DIMMER.OUTPUT.DESCRIPTION", RuleGuid, RuleInterfaceDirection.Output, 0, 1, RuleInterfaceType.Output);
            
            factory.ChangeDefaultVisuTemplate(RuleGuid, VisuMobileObjectTemplateTypes.Dimmer);
        }

        public override IRule CreateRuleInstance(IRuleContext context)
        {
            return new DimmerLogic(context);
        }

    }
}
