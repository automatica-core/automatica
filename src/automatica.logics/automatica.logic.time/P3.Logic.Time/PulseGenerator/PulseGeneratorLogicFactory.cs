using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;

namespace P3.Logic.Time.PulseGenerator
{
    public class PulseGeneratorLogicFactory : LogicFactory
    {
        internal const double DefaultOn = 1;
        internal const double DefaultOff = 1;

        public static readonly Guid RuleEnable = new Guid("b99cc889-e41b-48e4-b9cf-fd502e3cc225");
        public static readonly Guid RuleParamOn = new Guid("e5adc4a1-ce4e-49b5-9c66-09e123c359bb");
        public static readonly Guid RuleParamOff = new Guid("3a3ef8e1-08e9-42a5-b98b-9cee496c0862");
        public static readonly Guid RuleOutput = new Guid("43dd5dcb-4c8d-44c6-8240-99ee7fd1f232");

        public override string LogicName => "Time.PulseGenerator";
        public override Version LogicVersion => new Version(1, 0, 0, 0);
        public override Guid LogicGuid => new Guid("1ced42d6-eaa3-462c-92a4-918a8991e9a5");

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, "TIME.PULSE_GENERATOR.NAME", "TIME.PULSE_GENERATOR.DESCRIPTION", "time.pulse_generator", "TIME.NAME", 100, 100);

            factory.CreateLogicInterfaceTemplate(RuleEnable, "EN", "TIME.PULSE_GENERATOR.ENABLE.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, 1);

            factory.CreateLogicInterfaceTemplate(RuleOutput, "O", "TIME.PULSE_GENERATOR.OUTPUT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Output, 0, 1);

            factory.CreateParameterLogicInterfaceTemplate(RuleParamOn, "TIME.PULSE_GENERATOR.ON.NAME", "TIME.PULSE_GENERATOR.ON.DESCRIPTION", "on", LogicGuid, 1, RuleInterfaceParameterDataType.Double, DefaultOn);
            factory.CreateParameterLogicInterfaceTemplate(RuleParamOff, "TIME.PULSE_GENERATOR.OFF.NAME", "TIME.PULSE_GENERATOR.OFF.DESCRIPTION", "off", LogicGuid, 2, RuleInterfaceParameterDataType.Double, DefaultOff);
        }

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new PulseGeneratorRule(context);
        }
    }
}
