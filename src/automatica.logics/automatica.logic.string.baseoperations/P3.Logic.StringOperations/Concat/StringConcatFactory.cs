using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.Rule;

namespace P3.Logic.StringOperations.Concat
{
    public class StringConcatFactory : RuleFactory
    {
        public override string RuleName => "StringOperations";

        public override Guid RuleGuid => new Guid("c7a58432-159f-449b-88fc-8542d9c5dde9");

        public override Version RuleVersion => new Version(0, 0, 0, 1);

        public override bool InDevelopmentMode => true;

        public override IRule CreateRuleInstance(IRuleContext context)
        {
            return new StringConcatLogic(context);
        }

        public override void InitTemplates(IRuleTemplateFactory factory)
        {
            factory.CreateRuleTemplate(RuleGuid, "STRING_OPERATIONS.NAME", "STRING_OPERATIONS.DESCRIPTION",
               "string_operations", "STRING_OPERATIONS.NAME", 100, 100);
        }
    }
}
