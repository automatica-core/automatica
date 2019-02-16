using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.Rule;

namespace P3.Rule.Logic.BaseOperations.If
{
    public class IfRuleFactory : RuleFactory
    {
        public static readonly Guid RuleInput1 = new Guid("ad42fdfd-cac1-418d-9ab8-326352485b86");
        public static readonly Guid RuleInput2 = new Guid("098455a7-0337-4e6b-bbb9-e4e379aa5885");


        public static readonly Guid RuleParamTrue = new Guid("b644de1d-35dd-4f1e-aef4-06b2a566bae7");
        public static readonly Guid RuleParamFalse = new Guid("eead24da-f1c8-479f-b643-755efc240125");


        public static readonly Guid RuleOutput = new Guid("76b8d507-8237-4af8-a320-ccebfdd7007d");

        public override string RuleName => "Logic.If";
        public override Version RuleVersion => new Version(0, 0, 0, 1);
        public override Guid RuleGuid => new Guid("8e3666b9-3455-441e-afe2-b96c012929c5");

        public override void InitTemplates(IRuleTemplateFactory factory)
        {
            factory.CreateRuleTemplate(RuleGuid, "LOGIC.IF.NAME", "LOGIC.IF.DESCRIPTION", "logic.if", "LOGIC.NAME", 100, 100);

            factory.CreateRuleInterfaceTemplate(RuleInput1, "I1", "LOGIC.IF.INPUT.DESCRIPTION", RuleGuid, RuleInterfaceDirection.Input, 1, 1);
            factory.CreateRuleInterfaceTemplate(RuleInput2, "I2", "LOGIC.IF.INPUT.DESCRIPTION", RuleGuid, RuleInterfaceDirection.Input, 1, 2);

            factory.CreateRuleInterfaceTemplate(RuleOutput, "O", "LOGIC.IF.OUTPUT.DESCRIPTION", RuleGuid, RuleInterfaceDirection.Output, 0, 1);

            factory.CreateParameterRuleInterfaceTemplate(RuleParamTrue, "LOGIC.IF.PARAM.TRUE.NAME", "LOGIC.IF.PARAM.TRUE.DESCRIPTION", RuleGuid, 1, Automatica.Core.EF.Models.RuleInterfaceParameterDataType.Integer, 1);
            factory.CreateParameterRuleInterfaceTemplate(RuleParamFalse, "LOGIC.IF.PARAM.FALSE.NAME", "LOGIC.IF.PARAM.FALSE.DESCRIPTION", RuleGuid, 2, Automatica.Core.EF.Models.RuleInterfaceParameterDataType.Integer, 0);
        }

        public override IRule CreateRuleInstance(IRuleContext context)
        {
            return new IfRule(context);
        }
    }
}
