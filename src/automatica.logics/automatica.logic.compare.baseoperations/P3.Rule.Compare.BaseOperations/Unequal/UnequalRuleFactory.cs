using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using Automatica.Core.Base.Localization;
using Automatica.Core.Base.Templates;
using Automatica.Core.Rule;

namespace P3.Rule.Compare.BaseOperations.Unequal
{
    public class UnequalRuleFactory : RuleFactory
    {
        public static readonly Guid RuleInput1 = new Guid("0e37bbee-8d82-4322-8b15-fc7139500c3e");
        public static readonly Guid RuleInput2 = new Guid("421cc8f0-4057-4a05-aa28-a5d208ac6ebd");
        public static readonly Guid RuleOutput = new Guid("8c8ee302-4da8-4f43-956b-52d5ca96eb4f");

        public override string RuleName => "Compare.Unequal";
        public override Version RuleVersion => new Version(1, 0, 0, 0);
        public override Guid RuleGuid => new Guid("22d83387-a7db-425a-82fc-28cde7effd25");

        public override void InitTemplates(IRuleTemplateFactory factory)
        {
            factory.CreateRuleTemplate(RuleGuid, "COMPARE.UNEQUAL.NAME", "COMPARE.UNEQUAL.DESCRIPTION",
                "compare.unequal", "COMPARE.NAME", 100, 100);

            factory.CreateRuleInterfaceTemplate(RuleInput1, "I1", "COMPARE.UNEQUAL.INPUT1.DESCRIPTION", RuleGuid, RuleInterfaceDirection.Input, 1, 1);
            factory.CreateRuleInterfaceTemplate(RuleInput2, "I2", "COMPARE.UNEQUAL.INPUT2.DESCRIPTION", RuleGuid, RuleInterfaceDirection.Input, 1, 1);

            factory.CreateRuleInterfaceTemplate(RuleOutput, "O", "COMPARE.UNEQUAL.OUTPUT.DESCRIPTION", RuleGuid, RuleInterfaceDirection.Output, 0, 1);
        }

        public override IRule CreateRuleInstance(IRuleContext context)
        {
            return new UnequalRule(context);
        }
    }
}
