using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Rule;
using RuleInterfaceDirection = Automatica.Core.Base.Templates.RuleInterfaceDirection;

namespace P3.Rule.Math.BasicOperations.Random
{
    public class RandomRuleFactory : RuleFactory
    {
        public static readonly Guid RuleInputTrigger = new Guid("40e41d2c-b7d7-4d85-906d-8abf2e5ed4c7");
        public static readonly Guid RuleInputDisabled = new Guid("37bca3d8-e865-46f6-9803-7ca23431f890");

        public static readonly Guid RuleParamMin= new Guid("d091fcf2-4561-4cba-b880-b6d8aeecd2b0");
        public static readonly Guid RuleParamMax = new Guid("1a36e087-3b98-4e44-a40b-da698087da6f");

        public static readonly Guid RuleOutput = new Guid("1c7d9aff-0b29-40b1-9502-17b534763c08");

        public override string RuleName => "Math.Random";
        public override Version RuleVersion => new Version(1, 0, 0, 2);
        public override Guid RuleGuid => new Guid("3e08d671-610f-42f4-a676-a626ba42e83d");

        public override void InitTemplates(IRuleTemplateFactory factory)
        {
            factory.CreateRuleTemplate(RuleGuid, "MATH.RANDOM.NAME", "MATH.RANDOM.DESCRIPTION",
                "math.random", "MATH.NAME", 100, 100);

            factory.CreateRuleInterfaceTemplate(RuleInputTrigger, "Tr", "MATH.RANDOM.TRIGGER.DESCRIPTION", RuleGuid, RuleInterfaceDirection.Input, 1, 1);
            factory.CreateRuleInterfaceTemplate(RuleInputDisabled, "Dis", "MATH.RANDOM.DISABLED.DESCRIPTION", RuleGuid, RuleInterfaceDirection.Input, 1, 2);

            factory.CreateParameterRuleInterfaceTemplate(RuleParamMin, "Min", "MATH.RANDOM.MIN.DESCRIPTION", RuleGuid,  1, RuleInterfaceParameterDataType.Integer, 0);
            factory.CreateParameterRuleInterfaceTemplate(RuleParamMax, "Max", "MATH.RANDOM.MAX.DESCRIPTION", RuleGuid, 2, RuleInterfaceParameterDataType.Integer, 100);

            factory.CreateRuleInterfaceTemplate(RuleOutput, "O", "MATH.RANDOM.OUTPUT.DESCRIPTION", RuleGuid, RuleInterfaceDirection.Output, 0, 1, RuleInterfaceType.Status);
        }

        public override IRule CreateRuleInstance(IRuleContext context)
        {
            return new RandomRule(context);
        }
    }
}
