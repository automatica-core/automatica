using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Rule;
using RuleInterfaceDirection = Automatica.Core.Base.Templates.RuleInterfaceDirection;

namespace P3.Logic.Messenger
{
    public class MessengerFactory : RuleFactory
    {
        public override string RuleName => "Messenger";

        public override Guid RuleGuid => new Guid("215efbd7-95e6-4451-9837-612010f493a6");

        public override Version RuleVersion => new Version(0, 1, 0, 1);

        public override bool InDevelopmentMode => true;



        public static Guid ToProperty = new Guid("836178a4-b8b5-472c-a515-5e3e7a730a89");
        public static Guid SubjectProperty = new Guid("ea67a0cd-1765-4c24-b645-3298147b584a");

        public override IRule CreateRuleInstance(IRuleContext context)
        {
            return new MessengerLogic(context);
        }

        public override void InitTemplates(IRuleTemplateFactory factory)
        {
            factory.CreateRuleTemplate(RuleGuid, "MESSENGER.CLOUD_EMAIL.NAME", "MESSENGER.CLOUD_EMAIL.DESCRIPTION",
                "messenger-cloud-email", "MESSENGER.NAME", 100, 100);

            factory.CreateRuleInterfaceTemplate(new Guid("7ac55e7b-3837-4efb-8995-70ef4c4dfef2"), "T", "MESSENGER.CLOUD_EMAIL.TRIGGER.DESCRIPTION",
                RuleGuid, RuleInterfaceDirection.Input, 0, 1);

            factory.CreateParameterRuleInterfaceTemplate(ToProperty, "MESSENGER.CLOUD_EMAIL.TO.NAME",
                "MESSENGER.CLOUD_EMAIL.TO.DESCRIPTION", RuleGuid, 0, RuleInterfaceParameterDataType.Text, "");
            factory.CreateParameterRuleInterfaceTemplate(SubjectProperty, "MESSENGER.CLOUD_EMAIL.SUBJECT.NAME",
                "MESSENGER.CLOUD_EMAIL.SUBJECT.DESCRIPTION", RuleGuid, 0, RuleInterfaceParameterDataType.Text, "");
        }
    }
}
