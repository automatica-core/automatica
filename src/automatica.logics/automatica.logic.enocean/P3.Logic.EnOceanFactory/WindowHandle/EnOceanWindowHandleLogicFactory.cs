using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.Rule;

namespace P3.Logic.EnOceanFactory.WindowHandle
{
    public class EnOceanWindowHandleLogicFactory : RuleFactory
    {
        public override string RuleName => "EnOcean.WindowHandle";

        public override Guid RuleGuid => new Guid("5ef3001b-0fbf-40e2-86b9-c6e672daef1c");
        public static readonly Guid RuleInput = new Guid("0395f5da-e4fe-49da-b3b2-570db24b50b5");
        public static readonly Guid RuleOutput = new Guid("98418c84-881a-4e22-ad51-374769a2fb4b");

        public override Version RuleVersion => new Version(0, 0, 0, 1);

        public override bool InDevelopmentMode => true;

        public override IRule CreateRuleInstance(IRuleContext context)
        {
            return new EnOceanWindowHandleLogic(context);
        }

        public override void InitTemplates(IRuleTemplateFactory factory)
        {
            factory.CreateRuleTemplate(RuleGuid, "ENOCEAN_LOGIC.WINDOW_HANDLE.NAME", "ENOCEAN_LOGIC.WINDOW_HANDLE.DESCRIPTION",
               "enocean-window-handle", "ENOCEAN_LOGIC.NAME", 100, 100);

            factory.CreateRuleInterfaceTemplate(RuleInput, "I", "ENOCEAN_LOGIC.WINDOW_HANDLE.INPUT.DESCRIPTION", RuleGuid, RuleInterfaceDirection.Input, 1, 1);
            factory.CreateRuleInterfaceTemplate(RuleOutput, "O", "ENOCEAN_LOGIC.WINDOW_HANDLE.OUTPUT.DESCRIPTION", RuleGuid, RuleInterfaceDirection.Output, 0, 1);
        }
    }
}
