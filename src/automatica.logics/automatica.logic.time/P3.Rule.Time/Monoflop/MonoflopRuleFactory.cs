using System;
using Automatica.Core.Rule;
using Automatica.Core.Base.Templates;

namespace P3.Rule.Time.Monoflop
{
    public class MonoflopRuleFactory : RuleFactory
    {
        public static readonly Guid RuleTrigger = new Guid("8f018851-dac0-4392-9b3a-547eeeef6a1f");
        public static readonly Guid RuleParamDelay = new Guid("52cfb566-5f13-4e58-98db-01568fddce6f");


        public static readonly Guid RuleOutput = new Guid("3DBE05BF-BD15-4460-BB1D-9622797FD4AA");

        public override string RuleName => "Time.Monoflop";
        public override Version RuleVersion => new Version(0, 0, 0, 2);
        public override Guid RuleGuid => new Guid("32C3A4C9-9A84-4581-AA25-72B5D25091EA");

        public override void InitTemplates(IRuleTemplateFactory factory)
        {
            factory.CreateRuleTemplate(RuleGuid, "TIME.MONOFLOP.NAME", "TIME.MONOFLOP.DESCRIPTION",
                "time.monoflop", "TIME.NAME", 100, 100);

            factory.CreateRuleInterfaceTemplate(RuleTrigger, "T", "TIME.MONOFLOP.TRIGGER.DESCRIPTION", RuleGuid, RuleInterfaceDirection.Input, 0, 1);

            factory.CreateRuleInterfaceTemplate(RuleOutput, "O", "TIME.MONOFLOP.OUTPUT.DESCRIPTION", RuleGuid, RuleInterfaceDirection.Output, 0, 1);

            factory.CreateParameterRuleInterfaceTemplate(RuleParamDelay, "TIME.DELAY.NAME", "TIME.DELAY.DESCRIPTION", RuleGuid, 1, Automatica.Core.EF.Models.RuleInterfaceParameterDataType.Integer, 5);
        }

        public override IRule CreateRuleInstance(IRuleContext context)
        {
            return new MonoflopRule(context);
        }
    }
}
