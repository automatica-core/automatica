using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using Automatica.Core.Base.Localization;
using Automatica.Core.Base.Templates;
using Automatica.Core.Rule;

namespace P3.Rule.Math.BasicOperations.Multiply
{
    public class MultiplyRuleFactory : RuleFactory
    {
        public static readonly Guid RuleInput1 = new Guid("d1b17504-897d-4707-87e5-3d29d1bf75f5");
        public static readonly Guid RuleInput2 = new Guid("383facb9-fe8c-4ffc-a3d7-593feddb4de9");


        public static readonly Guid RuleOutput = new Guid("1b8dac1d-e812-4938-9640-db0a6e29d225");

        public override string RuleName => "Math.Multiply";
        public override Version RuleVersion => new Version(1, 0, 0, 0);
        public override Guid RuleGuid => new Guid("4856b924-459f-4190-8ff3-8db9b7cd992e");

        public override void InitTemplates(IRuleTemplateFactory factory)
        {
            factory.CreateRuleTemplate(RuleGuid, "MATH.MULTIPLY.NAME", "MATH.MULTIPLY.DESCRIPTION",
                "math.multiply", "MATH.NAME", 100, 100);

            factory.CreateRuleInterfaceTemplate(RuleInput1, "I1", "MATH.MULTIPLY.INPUT.DESCRIPTION", RuleGuid, RuleInterfaceDirection.Input, 1, 1);
            factory.CreateRuleInterfaceTemplate(RuleInput2, "I2", "MATH.MULTIPLY.INPUT.DESCRIPTION", RuleGuid, RuleInterfaceDirection.Input, 1, 2);

            factory.CreateRuleInterfaceTemplate(RuleOutput, "O", "MATH.MULTIPLY.OUTPUT.DESCRIPTION", RuleGuid, RuleInterfaceDirection.Output, 0, 1);
        }

        public override IRule CreateRuleInstance(IRuleContext context)
        {
            return new MultiplyRule(context);
        }
    }
}
