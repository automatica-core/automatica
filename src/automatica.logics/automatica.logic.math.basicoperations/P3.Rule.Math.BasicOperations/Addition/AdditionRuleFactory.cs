using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using Automatica.Core.Base.Localization;
using Automatica.Core.Base.Templates;
using Automatica.Core.Rule;

namespace P3.Rule.Math.BasicOperations.Addition
{
    public class AdditionRuleFactory : RuleFactory
    {
        public static readonly Guid RuleInput1 = new Guid("e697bef4-ddca-45fb-ae83-6b3ad1c840e4");
        public static readonly Guid RuleInput2 = new Guid("fda9e783-252e-469d-8065-bb2e99aa5fe5");
        public static readonly Guid RuleInput3 = new Guid("cd594053-8c6b-444e-ac2f-593ba5d50ca5");
        public static readonly Guid RuleInput4 = new Guid("536e2e6b-59f9-483b-93ca-be1b365ecbc6");


        public static readonly Guid RuleOutput = new Guid("7a9d63b2-050f-4532-8ba9-188a308b4a9a");

        public override string RuleName => "Math.Add";
        public override Version RuleVersion => new Version(1, 0, 0, 0);
        public override Guid RuleGuid => new Guid("010669c3-11db-4e99-86a9-ce678779d3a0");
        public override void InitTemplates(IRuleTemplateFactory factory)
        {
            factory.CreateRuleTemplate(RuleGuid, "MATH.ADD.NAME", "MATH.ADD.DESCRIPTION",
                "math.add", "MATH.NAME", 100, 100);

            factory.CreateRuleInterfaceTemplate(RuleInput1, "I1", "MATH.ADD.INPUT.DESCRIPTION", RuleGuid, RuleInterfaceDirection.Input, 1, 1);
            factory.CreateRuleInterfaceTemplate(RuleInput2, "I2", "MATH.ADD.INPUT.DESCRIPTION", RuleGuid, RuleInterfaceDirection.Input, 1, 2);
            factory.CreateRuleInterfaceTemplate(RuleInput3, "I3", "MATH.ADD.INPUT.DESCRIPTION", RuleGuid, RuleInterfaceDirection.Input, 1, 3);
            factory.CreateRuleInterfaceTemplate(RuleInput4, "I4", "MATH.ADD.INPUT.DESCRIPTION", RuleGuid, RuleInterfaceDirection.Input, 1, 4);

            factory.CreateRuleInterfaceTemplate(RuleOutput, "O", "MATH.ADD.OUTPUT.DESCRIPTION", RuleGuid, RuleInterfaceDirection.Output, 0, 1);
        }

        public override IRule CreateRuleInstance(IRuleContext context)
        {
            return new AdditionRule(context);
        }
    }
}
