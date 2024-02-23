using Automatica.Core.Base.Templates;
using Automatica.Core.Logic;
using System;

namespace P3.Logic.Time.DelayedOff
{
    public class DelayedOffLogicFactory : LogicFactory
    {
        public static readonly Guid RuleTrigger = new Guid("50789839-fa2a-4869-b373-58fa73021356");
        public static readonly Guid RuleReset = new Guid("d8c08ce2-08d1-4cf8-8f9d-8a4395247fc3");
        public static readonly Guid RuleParamDelay = new Guid("cb89b73b-b62f-4a38-88fe-78b9bd09c00d");
        public static readonly Guid TriggerOnlyIfTrue = new Guid("c2aae4d4-76d8-47f3-9751-44630688ae6b");


        public static readonly Guid RuleOutput = new Guid("b3261338-09ee-40d2-b051-efc4d6b577ce");

        public override string LogicName => "Time.DelayedOff";
        public override Version LogicVersion => new Version(1, 2, 0, 2);
        public override Guid LogicGuid => new Guid("61182a90-6dd7-4677-ac06-a8bc38ba46a9");

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, "TIME.DELAYOFF.NAME", "TIME.DELAYOFF.DESCRIPTION",
                "time.delayed-off", "TIME.NAME", 100, 100);

            factory.CreateLogicInterfaceTemplate(RuleTrigger, "T", "TIME.DELAYOFF.TRIGGER.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, 1);
            factory.CreateLogicInterfaceTemplate(RuleReset, "R", "TIME.DELAYOFF.RESET.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 0, 2);

            factory.CreateLogicInterfaceTemplate(RuleOutput, "O", "TIME.DELAYOFF.OUTPUT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Output, 0, 1);

            factory.CreateParameterLogicInterfaceTemplate(RuleParamDelay, "TIME.DELAY.NAME", "TIME.DELAY.DESCRIPTION", "delay", LogicGuid, 1, Automatica.Core.EF.Models.RuleInterfaceParameterDataType.Integer, 5, true);
            factory.CreateParameterLogicInterfaceTemplate(TriggerOnlyIfTrue, "TIME.TRIGGER_ONLY_IF_TRUE.NAME", "TIME.TRIGGER_ONLY_IF_TRUE.DESCRIPTION", "trigger-only-if-true", LogicGuid, 2, Automatica.Core.EF.Models.RuleInterfaceParameterDataType.Bool, false, true);
        }

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new DelayedOffRule(context);
        }
    }
}
