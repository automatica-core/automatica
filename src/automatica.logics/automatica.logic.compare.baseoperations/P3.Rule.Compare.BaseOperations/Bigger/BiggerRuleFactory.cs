using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using Automatica.Core.Base.Localization;
using Automatica.Core.Base.Templates;
using Automatica.Core.Rule;

namespace P3.Rule.Compare.BaseOperations.Bigger
{
    public class BiggerRuleFactory : RuleFactory
    {
        public static readonly Guid RuleInput1 = new Guid("967ec11f-e1aa-48a8-bd5d-19cf403d0e86");
        public static readonly Guid RuleInput2 = new Guid("3531c209-4c5d-459c-a418-264691cbe5b2");
        public static readonly Guid RuleOutput = new Guid("ffdee98d-9f8c-4a9d-bd29-53814959a3f2");

        public override string RuleName => "Compare.Bigger";
        public override Version RuleVersion => new Version(1, 0, 0, 0);
        public override Guid RuleGuid => new Guid("255e4e8a-7f69-49d7-87d9-731917e7a6c0");

        public override void InitTemplates(IRuleTemplateFactory factory)
        {
            factory.CreateRuleTemplate(RuleGuid, "COMPARE.BIGGER.NAME", "COMPARE.BIGGER.DESCRIPTION",
                "compare.bigger", "COMPARE.NAME", 100, 100);

            factory.CreateRuleInterfaceTemplate(RuleInput1, "I1", "COMPARE.BIGGER.INPUT1.DESCRIPTION", RuleGuid, RuleInterfaceDirection.Input, 1, 1);
            factory.CreateRuleInterfaceTemplate(RuleInput2, "I2", "COMPARE.BIGGER.INPUT2.DESCRIPTION", RuleGuid, RuleInterfaceDirection.Input, 1, 1);

            factory.CreateRuleInterfaceTemplate(RuleOutput, "O", "COMPARE.BIGGER.OUTPUT.DESCRIPTION", RuleGuid, RuleInterfaceDirection.Output, 0, 1);
        }

        public override IRule CreateRuleInstance(IRuleContext context)
        {
            return new BiggerRule(context);
        }
    }
}
