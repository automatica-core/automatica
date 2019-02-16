using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using Automatica.Core.Base.Localization;
using Automatica.Core.Base.Templates;
using Automatica.Core.Rule;

namespace P3.Rule.Compare.BaseOperations.BiggerOrEqual
{
    public class BiggerOrEqualRuleFactory : RuleFactory
    {
        public static readonly Guid RuleInput1 = new Guid("bcb9b541-b51c-468a-9906-c6f31c9c77aa");
        public static readonly Guid RuleInput2 = new Guid("faeb38a1-bdc8-440a-a1c3-8fd75db9119c");
        public static readonly Guid RuleOutput = new Guid("f1002b54-506e-461d-9490-854ff8fc6042");

        public override string RuleName => "Compare.BiggerOrEqual";
        public override Version RuleVersion => new Version(1, 0, 0, 0);
        public override Guid RuleGuid => new Guid("f3dda9bc-5f7c-4100-8187-ef5b4ea46bd7");

        public override void InitTemplates(IRuleTemplateFactory factory)
        {
            factory.CreateRuleTemplate(RuleGuid, "COMPARE.BIGGER_OR_EQUAL.NAME", "COMPARE.BIGGER_OR_EQUAL.DESCRIPTION",
                "compare.bigger_or_equal", "COMPARE.NAME", 100, 100);

            factory.CreateRuleInterfaceTemplate(RuleInput1, "I1", "COMPARE.BIGGER_OR_EQUAL.INPUT1.DESCRIPTION", RuleGuid, RuleInterfaceDirection.Input, 1, 1);
            factory.CreateRuleInterfaceTemplate(RuleInput2, "I2", "COMPARE.BIGGER_OR_EQUAL.INPUT2.DESCRIPTION", RuleGuid, RuleInterfaceDirection.Input, 1, 1);

            factory.CreateRuleInterfaceTemplate(RuleOutput, "O", "COMPARE.BIGGER_OR_EQUAL.OUTPUT.DESCRIPTION", RuleGuid, RuleInterfaceDirection.Output, 0, 1);
        }

        public override IRule CreateRuleInstance(IRuleContext context)
        {
            return new BiggerOrEqualRule(context);
        }
    }
}
