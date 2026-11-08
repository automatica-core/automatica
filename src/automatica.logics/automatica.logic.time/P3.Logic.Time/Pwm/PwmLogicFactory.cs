using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;

namespace P3.Logic.Time.Pwm
{
    public class PwmLogicFactory : LogicFactory
    {
        internal const double DefaultPeriod = 1;

        public static readonly Guid RuleValue = new Guid("4ceae3a8-2873-4c81-800d-f3c3c587db2b");
        public static readonly Guid RuleOff = new Guid("e8c4629c-c214-4cc2-a724-71150a8c700d");
        public static readonly Guid RuleOutput = new Guid("637d1462-c73f-4865-bc6c-5a20783faa02");
        public static readonly Guid RuleParamPeriod = new Guid("07346029-7bdc-4b23-b52a-e3cb54267fe4");

        public override string LogicName => "Time.Pwm";
        public override Version LogicVersion => new Version(1, 0, 0, 0);
        public override Guid LogicGuid => new Guid("f0df14f7-949d-4369-9a6c-bf148a46f5d4");

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, "TIME.PWM.NAME", "TIME.PWM.DESCRIPTION", "time.pwm", "TIME.NAME", 100, 100);

            factory.CreateLogicInterfaceTemplate(RuleValue, "V", "TIME.PWM.VALUE.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, 1);
            factory.CreateLogicInterfaceTemplate(RuleOff, "Off", "TIME.LOXONE.OFF.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, 2);

            factory.CreateLogicInterfaceTemplate(RuleOutput, "PWM", "TIME.PWM.OUTPUT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Output, 0, 1);

            factory.CreateParameterLogicInterfaceTemplate(RuleParamPeriod, "TIME.PWM.PERIOD.NAME", "TIME.PWM.PERIOD.DESCRIPTION", "p", LogicGuid, 1, RuleInterfaceParameterDataType.Double, DefaultPeriod);
        }

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new PwmRule(context);
        }
    }
}
