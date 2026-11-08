using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;

namespace P3.Logic.Time.DelayedPulse
{
    public class DelayedPulseLogicFactory : LogicFactory
    {
        internal const double DefaultDelay = 5;
        internal const double DefaultPulse = 0.5;

        public static readonly Guid RuleTrigger = new Guid("9b3e4d3f-62c0-4d2b-9509-06bf74c6c371");
        public static readonly Guid RuleOff = new Guid("10583515-75db-43cd-849c-52d79ed14667");
        public static readonly Guid RuleOutput = new Guid("2018671f-5439-4918-b053-e36e76a0a377");
        public static readonly Guid RuleParamDelay = new Guid("04f5af2e-6629-409b-bcca-9ff50723eacb");
        public static readonly Guid RuleParamPulse = new Guid("231c1e2a-936c-4691-ac80-af710eb9bb0b");

        public override string LogicName => "Time.DelayedPulse";
        public override Version LogicVersion => new Version(1, 0, 0, 0);
        public override Guid LogicGuid => new Guid("0ab66d7d-cf89-440e-abce-774a34ca754f");

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, "TIME.DELAYED_PULSE.NAME", "TIME.DELAYED_PULSE.DESCRIPTION", "time.delayed_pulse", "TIME.NAME", 100, 100);

            factory.CreateLogicInterfaceTemplate(RuleTrigger, "P", "TIME.DELAYED_PULSE.TRIGGER.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, 1);
            factory.CreateLogicInterfaceTemplate(RuleOff, "Off", "TIME.LOXONE.OFF.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, 2);

            factory.CreateLogicInterfaceTemplate(RuleOutput, "P", "TIME.DELAYED_PULSE.OUTPUT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Output, 0, 1);

            factory.CreateParameterLogicInterfaceTemplate(RuleParamDelay, "TIME.DELAYED_PULSE.DELAY.NAME", "TIME.DELAYED_PULSE.DELAY.DESCRIPTION", "dd", LogicGuid, 1, RuleInterfaceParameterDataType.Double, DefaultDelay);
            factory.CreateParameterLogicInterfaceTemplate(RuleParamPulse, "TIME.DELAYED_PULSE.PULSE.NAME", "TIME.DELAYED_PULSE.PULSE.DESCRIPTION", "dp", LogicGuid, 2, RuleInterfaceParameterDataType.Double, DefaultPulse);
        }

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new DelayedPulseRule(context);
        }
    }
}
