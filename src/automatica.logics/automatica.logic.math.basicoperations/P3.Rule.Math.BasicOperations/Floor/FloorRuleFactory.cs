using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Rule;
using RuleInterfaceDirection = Automatica.Core.Base.Templates.RuleInterfaceDirection;

namespace P3.Rule.Math.BasicOperations.Floor
{
    public class FloorRuleFactory : RuleFactory
    {
        public static readonly Guid RuleInput1 = new Guid("746e1068-232f-4ec1-b019-7e6bae074a44");
        public static readonly Guid RuleOutput = new Guid("a44cb7b6-13ae-4394-b9dd-4b4abd595766");

        public override string RuleName => "Math.Floor";
        public override Version RuleVersion => new Version(2, 0, 0, 0);
        public override Guid RuleGuid => new Guid("aa351d39-443e-425b-8a50-dde82ce4ba58");

        public override void InitTemplates(IRuleTemplateFactory factory)
        {
            factory.CreateRuleTemplate(RuleGuid, "MATH.FLOOR.NAME", "MATH.FLOOR.DESCRIPTION",
                "math.floor", "MATH.NAME", 100, 100);

            factory.CreateRuleInterfaceTemplate(RuleInput1, "I1", "MATH.FLOOR.INPUT.DESCRIPTION", RuleGuid, RuleInterfaceDirection.Input, 1, 1);

            factory.CreateRuleInterfaceTemplate(RuleOutput, "O", "MATH.FLOOR.OUTPUT.DESCRIPTION", RuleGuid, RuleInterfaceDirection.Output, 0, 1, RuleInterfaceType.Status);
        }

        public override IRule CreateRuleInstance(IRuleContext context)
        {
            return new FloorRule(context);
        }
    }
}
