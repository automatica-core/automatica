using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Rule;
using RuleInterfaceDirection = Automatica.Core.Base.Templates.RuleInterfaceDirection;

namespace P3.Rule.Logic.BaseOperations.BinaryDecoder
{
    public class BinaryDecoderRuleFactory : RuleFactory
    {
        public static readonly Guid RuleInput1 = new Guid("fe4e6b12-1968-4d61-85ea-88b7b4c44e88");


        public static readonly Guid RuleOutput1 = new Guid("38bc32c3-107d-4766-80e9-4c3ac2af3851");
        public static readonly Guid RuleOutput2 = new Guid("c70b402c-fcda-4cf4-b819-883c7b81a941");
        public static readonly Guid RuleOutput3 = new Guid("1809f422-e576-485b-88cc-3f20c7a07314");
        public static readonly Guid RuleOutput4 = new Guid("71ae7779-8f11-4060-a58e-016f491237d1");
        public static readonly Guid RuleOutput5 = new Guid("a807aa1b-579b-48c2-9735-160a55a403a5");
        public static readonly Guid RuleOutput6 = new Guid("3e88cfaf-619c-48f5-a0a6-6e87fc3aef23");
        public static readonly Guid RuleOutput7 = new Guid("d64aba76-9799-4f05-a6b3-8c1ed4f8662c");
        public static readonly Guid RuleOutput8 = new Guid("1e11f30b-df35-4daa-8804-3e4b36125795");

        public override string RuleName => "Logic.BinaryDecoder";
        public override Version RuleVersion => new Version(2, 0, 0, 0);
        public override Guid RuleGuid => new Guid("bb300e2a-dcec-4753-a37b-2b23ea937387");

        public override void InitTemplates(IRuleTemplateFactory factory)
        {
            factory.CreateRuleTemplate(RuleGuid, "LOGIC.BINARY_DECODER.NAME", "LOGIC.BINARY_DECODER.DESCRIPTION", "logic.binary_decoder", "LOGIC.NAME", 100, 100);

            factory.CreateRuleInterfaceTemplate(RuleInput1, "AI", "LOGIC.BINARY_DECODER.INPUT.DESCRIPTION", RuleGuid, RuleInterfaceDirection.Input, 1, 1, RuleInterfaceType.Status);

            factory.CreateRuleInterfaceTemplate(RuleOutput1, "Q1", "LOGIC.BINARY_DECODER.OUTPUT.DESCRIPTION", RuleGuid, RuleInterfaceDirection.Output, 0, 1);
            factory.CreateRuleInterfaceTemplate(RuleOutput2, "Q2", "LOGIC.BINARY_DECODER.OUTPUT.DESCRIPTION", RuleGuid, RuleInterfaceDirection.Output, 0, 2);
            factory.CreateRuleInterfaceTemplate(RuleOutput3, "Q3", "LOGIC.BINARY_DECODER.OUTPUT.DESCRIPTION", RuleGuid, RuleInterfaceDirection.Output, 0, 3);
            factory.CreateRuleInterfaceTemplate(RuleOutput4, "Q4", "LOGIC.BINARY_DECODER.OUTPUT.DESCRIPTION", RuleGuid, RuleInterfaceDirection.Output, 0, 4);
            factory.CreateRuleInterfaceTemplate(RuleOutput5, "Q5", "LOGIC.BINARY_DECODER.OUTPUT.DESCRIPTION", RuleGuid, RuleInterfaceDirection.Output, 0, 5);
            factory.CreateRuleInterfaceTemplate(RuleOutput6, "Q6", "LOGIC.BINARY_DECODER.OUTPUT.DESCRIPTION", RuleGuid, RuleInterfaceDirection.Output, 0, 6);
            factory.CreateRuleInterfaceTemplate(RuleOutput7, "Q7", "LOGIC.BINARY_DECODER.OUTPUT.DESCRIPTION", RuleGuid, RuleInterfaceDirection.Output, 0, 7);
            factory.CreateRuleInterfaceTemplate(RuleOutput8, "Q8", "LOGIC.BINARY_DECODER.OUTPUT.DESCRIPTION", RuleGuid, RuleInterfaceDirection.Output, 0, 8);
        }

        public override IRule CreateRuleInstance(IRuleContext context)
        {
            return new BinaryDecoderRule(context);
        }
    }
}
