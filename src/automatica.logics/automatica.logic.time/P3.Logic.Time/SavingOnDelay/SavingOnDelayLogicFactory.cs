using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;

namespace P3.Logic.Time.SavingOnDelay
{
    public class SavingOnDelayLogicFactory : LogicFactory
    {
        internal const double DefaultDelay = 1;

        public static readonly Guid RuleTrigger = new Guid("2bed8e63-3d4f-436e-bd62-43a6f44a656b");
        public static readonly Guid RuleOff = new Guid("d1c3d1a3-1b78-468e-a309-fb7a932743a8");
        public static readonly Guid RuleOutput = new Guid("34d44599-e3c5-4743-8895-052542ad3596");
        public static readonly Guid RuleParamDelay = new Guid("68b3774a-a36c-42f7-899d-46c957d357d5");

        public override string LogicName => "Time.SavingOnDelay";
        public override Version LogicVersion => new Version(1, 0, 0, 0);
        public override Guid LogicGuid => new Guid("0f063981-de7d-4fa9-b796-ce3fc5f44522");

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, "TIME.SAVING_ON_DELAY.NAME", "TIME.SAVING_ON_DELAY.DESCRIPTION", "time.saving_on_delay", "TIME.NAME", 100, 100);

            factory.CreateLogicInterfaceTemplate(RuleTrigger, "Tr", "TIME.SAVING_ON_DELAY.TRIGGER.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, 1);
            factory.CreateLogicInterfaceTemplate(RuleOff, "Off", "TIME.LOXONE.OFF.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, 2);

            factory.CreateLogicInterfaceTemplate(RuleOutput, "O", "TIME.SAVING_ON_DELAY.OUTPUT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Output, 0, 1);

            factory.CreateParameterLogicInterfaceTemplate(RuleParamDelay, "TIME.SAVING_ON_DELAY.DELAY.NAME", "TIME.SAVING_ON_DELAY.DELAY.DESCRIPTION", "don", LogicGuid, 1, RuleInterfaceParameterDataType.Double, DefaultDelay);
        }

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new SavingOnDelayRule(context);
        }
    }
}
