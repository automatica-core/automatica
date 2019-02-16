using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using Automatica.Core.Base.Localization;
using Automatica.Core.Base.Templates;
using Automatica.Core.Rule;

namespace P3.Rule.Compare.BaseOperations.Smaller
{
    public class SmallerRuleFactory : RuleFactory
    {
        public static readonly Guid RuleInput1 = new Guid("e558ee37-2fdd-47d9-a923-674e72947f38");
        public static readonly Guid RuleInput2 = new Guid("cca4474a-d9c3-44d3-86c3-4d20c8d33fe1");
        public static readonly Guid RuleOutput = new Guid("bbaf45f0-44ac-4d5f-9572-9bd20aeabac5");

        public override string RuleName => "Compare.Smaller";
        public override Version RuleVersion => new Version(1, 0, 0, 0);
        public override Guid RuleGuid => new Guid("09c0af42-153c-45e1-b8d7-292f682a2eff");

        public override void InitTemplates(IRuleTemplateFactory factory)
        {
            factory.CreateRuleTemplate(RuleGuid, "COMPARE.SMALLER.NAME", "COMPARE.SMALLER.DESCRIPTION",
                "compare.smaller", "COMPARE.NAME", 100, 100);

            factory.CreateRuleInterfaceTemplate(RuleInput1, "I1", "COMPARE.SMALLER.INPUT1.DESCRIPTION", RuleGuid, RuleInterfaceDirection.Input, 1, 1);
            factory.CreateRuleInterfaceTemplate(RuleInput2, "I2", "COMPARE.SMALLER.INPUT2.DESCRIPTION", RuleGuid, RuleInterfaceDirection.Input, 1, 1);

            factory.CreateRuleInterfaceTemplate(RuleOutput, "O", "COMPARE.SMALLER.OUTPUT.DESCRIPTION", RuleGuid, RuleInterfaceDirection.Output, 0, 1);
        }

        public override IRule CreateRuleInstance(IRuleContext context)
        {
            return new SmallerRule(context);
        }
    }
}
