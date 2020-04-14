using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Rule;
using RuleInterfaceDirection = Automatica.Core.Base.Templates.RuleInterfaceDirection;

namespace P3.Rule.Math.BasicOperations.Division
{
    public class DivisionRuleFactory : RuleFactory
    {
        public static readonly Guid RuleInput1 = new Guid("f1facd80-cdab-4487-97c4-e2477444191c");
        public static readonly Guid RuleInput2 = new Guid("75b5ee1e-8f3c-492d-b196-325bbaa88022");


        public static readonly Guid RuleOutput = new Guid("538c6157-2c0a-4f90-ba62-57038abac838");

        public override string RuleName => "Math.Divison";
        public override Version RuleVersion => new Version(2, 0, 0, 0);
        public override Guid RuleGuid => new Guid("151ee36d-680a-4978-b51f-a099e8f94895");

        public override void InitTemplates(IRuleTemplateFactory factory)
        {
            factory.CreateRuleTemplate(RuleGuid, "MATH.DIVISION.NAME", "MATH.DIVIDE.DESCRIPTION",
                "math.division", "MATH.NAME", 100, 100);

            factory.CreateRuleInterfaceTemplate(RuleInput1, "I1", "MATH.DIVISION.INPUT.DESCRIPTION", RuleGuid, RuleInterfaceDirection.Input, 1, 1);
            factory.CreateRuleInterfaceTemplate(RuleInput2, "I2", "MATH.DIVISION.INPUT.DESCRIPTION", RuleGuid, RuleInterfaceDirection.Input, 1, 2);

            factory.CreateRuleInterfaceTemplate(RuleOutput, "O", "MATH.DIVISION.OUTPUT.DESCRIPTION", RuleGuid, RuleInterfaceDirection.Output, 0, 1, RuleInterfaceType.Status);
        }

        public override IRule CreateRuleInstance(IRuleContext context)
        {
            return new DivisionRule(context);
        }
    }
}
