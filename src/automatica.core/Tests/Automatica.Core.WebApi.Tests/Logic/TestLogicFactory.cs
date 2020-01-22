using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.Rule;

namespace Automatica.Core.WebApi.Tests.Logic
{
    public class TestLogicFactory : RuleFactory
    {
        public override string RuleName => nameof(TestLogicFactory);
        public override Guid RuleGuid => new Guid("997dd7fd-9509-4497-9bcc-63b834db293f");
        public override Version RuleVersion => new Version(0, 0, 0, 1);


        public static readonly Guid RuleInput1 = new Guid("eb1eda08-a66a-4d94-8a55-73c88f87b367");
        public static readonly Guid RuleOutput = new Guid("3ff22fc2-7608-4752-b943-02a6a1728c79");

        public override void InitTemplates(IRuleTemplateFactory factory)
        {
            factory.CreateRuleTemplate(RuleGuid, "TEST_RULE", "TEST_RULE",
                "TEST_RULE", "TEST_RULE", 100, 100);

            factory.CreateRuleInterfaceTemplate(RuleInput1, "I1", "TEST_INPUT", RuleGuid, RuleInterfaceDirection.Input, 1, 1);
            factory.CreateRuleInterfaceTemplate(RuleOutput, "O", "TEST_OUTPUT", RuleGuid, RuleInterfaceDirection.Output, 0, 1);
        }

        public override IRule CreateRuleInstance(IRuleContext context)
        {
            return new TestLogic(context);
        }
    }
}
