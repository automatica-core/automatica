using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;

namespace P3.Logic.Time.OnOffDelay
{
    public class OnOffDelayLogicFactory : LogicFactory
    {
        internal const double DefaultOn = 1;
        internal const double DefaultOff = 1;

        public static readonly Guid RuleTrigger = new Guid("faa74bb6-80e7-4e86-b99d-2a12cf2f9836");
        public static readonly Guid RuleOff = new Guid("82b9f256-ca50-48ab-bbbb-5dbf92a8f905");
        public static readonly Guid RuleOutput = new Guid("0cfc13ad-e8f2-468a-9a30-cb9efd84f02a");
        public static readonly Guid RuleParamOn = new Guid("fea91853-2a3c-4f26-b956-a6874fafb14e");
        public static readonly Guid RuleParamOff = new Guid("f3ea12b8-5403-4334-9f7e-41463d618347");

        public override string LogicName => "Time.OnOffDelay";
        public override Version LogicVersion => new Version(1, 0, 0, 0);
        public override Guid LogicGuid => new Guid("747b5922-d7f8-471f-b650-ebbc98fd029e");

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, "TIME.ON_OFF_DELAY.NAME", "TIME.ON_OFF_DELAY.DESCRIPTION", "time.on_off_delay", "TIME.NAME", 100, 100);

            factory.CreateLogicInterfaceTemplate(RuleTrigger, "Tr", "TIME.ON_OFF_DELAY.TRIGGER.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, 1);
            factory.CreateLogicInterfaceTemplate(RuleOff, "Off", "TIME.LOXONE.OFF.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, 2);

            factory.CreateLogicInterfaceTemplate(RuleOutput, "O", "TIME.ON_OFF_DELAY.OUTPUT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Output, 0, 1);

            factory.CreateParameterLogicInterfaceTemplate(RuleParamOn, "TIME.ON_OFF_DELAY.ON.NAME", "TIME.ON_OFF_DELAY.ON.DESCRIPTION", "don", LogicGuid, 1, RuleInterfaceParameterDataType.Double, DefaultOn);
            factory.CreateParameterLogicInterfaceTemplate(RuleParamOff, "TIME.ON_OFF_DELAY.OFF.NAME", "TIME.ON_OFF_DELAY.OFF.DESCRIPTION", "doff", LogicGuid, 2, RuleInterfaceParameterDataType.Double, DefaultOff);
        }

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new OnOffDelayRule(context);
        }
    }
}
