using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using Automatica.Core.Base.Localization;
using Automatica.Core.Base.Templates;
using Automatica.Core.Rule;

namespace P3.Rule.Compare.BaseOperations.Equal
{
    public class EqualRuleFactory : RuleFactory
    {
        public static readonly Guid RuleInput1 = new Guid("67443626-60fe-4aa1-b696-af5e41857508");
        public static readonly Guid RuleInput2 = new Guid("53376d1f-1684-4653-8c03-6497968cc142");
        public static readonly Guid RuleOutput = new Guid("ece069c5-7363-48a3-ab31-1f3202bfa2ad");

        public override string RuleName => "Compare.Equal";
        public override Version RuleVersion => new Version(1, 0, 0, 0);
        public override Guid RuleGuid => new Guid("c79e65da-41ed-4bfe-8a7e-ac5fec54539a");
        public override void InitTemplates(IRuleTemplateFactory factory)
        {
            factory.CreateRuleTemplate(RuleGuid, "COMPARE.EQUAL.NAME", "COMPARE.EQUAL.DESCRIPTION",
                "compare.equal", "COMPARE.NAME", 100, 100);

            factory.CreateRuleInterfaceTemplate(RuleInput1, "I1", "COMPARE.EQUAL.INPUT1.DESCRIPTION", RuleGuid, RuleInterfaceDirection.Input, 1, 1);
            factory.CreateRuleInterfaceTemplate(RuleInput2, "I2", "COMPARE.EQUAL.INPUT2.DESCRIPTION", RuleGuid, RuleInterfaceDirection.Input, 1, 1);

            factory.CreateRuleInterfaceTemplate(RuleOutput, "O", "COMPARE.EQUAL.OUTPUT.DESCRIPTION", RuleGuid, RuleInterfaceDirection.Output, 0, 1);
        }

        public override IRule CreateRuleInstance(IRuleContext context)
        {
            return new EqualRule(context);
        }
    }
}
