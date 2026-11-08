using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using LogicInterfaceDirection = Automatica.Core.Base.Templates.LogicInterfaceDirection;

namespace P3.Logic.Messenger
{
    public class MessengerFactory : LogicFactory
    {
        public override string LogicName => "Messenger";

        public override Guid LogicGuid => new Guid("215efbd7-95e6-4451-9837-612010f493a6");

        // the core rewrites the templates only when this goes up. 0.2.0.0: cloud email body template parameter
        public override Version LogicVersion => new Version(0, 2, 0, 0);

        public override bool InDevelopmentMode => true;



        public static Guid ToProperty = new Guid("836178a4-b8b5-472c-a515-5e3e7a730a89");
        public static Guid SubjectProperty = new Guid("ea67a0cd-1765-4c24-b645-3298147b584a");
        public static Guid BodyProperty = new Guid("5b0d7a3e-8f61-4c2b-9d47-2e8a1f6c3b90");

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new MessengerLogic(context);
        }

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, "MESSENGER.CLOUD_EMAIL.NAME", "MESSENGER.CLOUD_EMAIL.DESCRIPTION",
                "messenger-cloud-email", "MESSENGER.NAME", 100, 100);

            factory.CreateLogicInterfaceTemplate(new Guid("7ac55e7b-3837-4efb-8995-70ef4c4dfef2"), "T", "MESSENGER.CLOUD_EMAIL.TRIGGER.DESCRIPTION",
                LogicGuid, LogicInterfaceDirection.Input, 0, 1);

            factory.CreateParameterLogicInterfaceTemplate(ToProperty, "MESSENGER.CLOUD_EMAIL.TO.NAME",
                "MESSENGER.CLOUD_EMAIL.TO.DESCRIPTION", LogicGuid, 0, RuleInterfaceParameterDataType.Text, "");
            factory.CreateParameterLogicInterfaceTemplate(SubjectProperty, "MESSENGER.CLOUD_EMAIL.SUBJECT.NAME",
                "MESSENGER.CLOUD_EMAIL.SUBJECT.DESCRIPTION", LogicGuid, 0, RuleInterfaceParameterDataType.Text, "");
            factory.CreateParameterLogicInterfaceTemplate(BodyProperty, "MESSENGER.CLOUD_EMAIL.BODY_TEMPLATE.NAME",
                "MESSENGER.CLOUD_EMAIL.BODY_TEMPLATE.DESCRIPTION", LogicGuid, 0, RuleInterfaceParameterDataType.Text, "");
        }
    }
}
