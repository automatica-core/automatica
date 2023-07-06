using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.Logic;

namespace Automatica.Core.WebApi.Tests.Logic
{
    public class TestLogicFactory : LogicFactory
    {
        public override string LogicName => nameof(TestLogicFactory);
        public override Guid LogicGuid => new Guid("997dd7fd-9509-4497-9bcc-63b834db293f");
        public override System.Version LogicVersion => new System.Version(0, 0, 0, 1);


        public static readonly Guid RuleInput1 = new Guid("eb1eda08-a66a-4d94-8a55-73c88f87b367");
        public static readonly Guid RuleOutput = new Guid("3ff22fc2-7608-4752-b943-02a6a1728c79");

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, "TEST_RULE", "TEST_RULE",
                "TEST_RULE", "TEST_RULE", 100, 100);

            factory.CreateLogicInterfaceTemplate(RuleInput1, "I1", "TEST_INPUT", LogicGuid, LogicInterfaceDirection.Input, 1, 1);
            factory.CreateLogicInterfaceTemplate(RuleOutput, "O", "TEST_OUTPUT", LogicGuid, LogicInterfaceDirection.Output, 0, 1);
        }

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new TestLogic(context);
        }
    }
}
