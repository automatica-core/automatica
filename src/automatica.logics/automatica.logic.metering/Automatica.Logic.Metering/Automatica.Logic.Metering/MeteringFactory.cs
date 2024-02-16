using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.Logic;


namespace Automatica.Logic.Metering
{
    public class MeteringFactory : LogicFactory
    {
        public override string LogicName => "Metering";

        public override Guid LogicGuid => new Guid("6b395c46-ee29-4f06-b416-c9367c41fc85");

        public override Version LogicVersion => new Version(0, 3, 1, 1);

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new MeteringLogic(context);
        }

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, "METERING.NAME", "METERING.DESCRIPTION",
                "metering", "METERING.NAME", 100, 100);



        }
    }
}
