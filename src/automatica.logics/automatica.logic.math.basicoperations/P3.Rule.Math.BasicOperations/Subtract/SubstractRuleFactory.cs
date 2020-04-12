using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Rule;
using RuleInterfaceDirection = Automatica.Core.Base.Templates.RuleInterfaceDirection;

namespace P3.Rule.Math.BasicOperations.Subtract
{
    public class SubtractRuleFactory : RuleFactory
    {
        public static readonly Guid RuleInput1 = new Guid("0c81581a-fb6f-4f05-8a5d-65284c030685");
        public static readonly Guid RuleInput2 = new Guid("1f93eeea-6483-46e3-a7da-fca6ed9f7922");


        public static readonly Guid RuleOutput = new Guid("85044f07-165a-428a-84bb-764f1aa37575");

        public override string RuleName => "Math.Subtraction";

        public override Version RuleVersion => new Version(2, 0, 0, 0);

        public override Guid RuleGuid => new Guid("d9899bcb-a4e6-4dcc-93d7-e5dc0237844f");

        public override void InitTemplates(IRuleTemplateFactory factory)
        {
            factory.CreateRuleTemplate(RuleGuid, "MATH.SUBTRACT.NAME", "MATH.SUBTRACT.DESCRIPTION",
                "math.subtraction", "MATH.NAME", 100, 100);

            factory.CreateRuleInterfaceTemplate(RuleInput1, "I1", "MATH.SUBTRACT.INPUT.DESCRIPTION", RuleGuid, RuleInterfaceDirection.Input, 1, 1);
            factory.CreateRuleInterfaceTemplate(RuleInput2, "I2", "MATH.SUBTRACT.INPUT.DESCRIPTION", RuleGuid, RuleInterfaceDirection.Input, 1, 2);

            factory.CreateRuleInterfaceTemplate(RuleOutput, "O", "MATH.SUBTRACT.OUTPUT.DESCRIPTION", RuleGuid, RuleInterfaceDirection.Output, 0, 1, RuleInterfaceType.Status);
        }

        public override IRule CreateRuleInstance(IRuleContext context)
        {
            return new SubtractRule(context);
        }
    }
}
