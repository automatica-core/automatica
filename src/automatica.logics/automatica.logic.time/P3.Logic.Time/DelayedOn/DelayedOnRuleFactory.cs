using System;
using Automatica.Core.Logic;
using Automatica.Core.Base.Templates;

namespace P3.Logic.Time.DelayedOn
{
    public class DelayedOnLogicFactory : LogicFactory
    {
        public static readonly Guid RuleTrigger = new Guid("3d9cf92c-2070-4b00-97b8-e1c3bf1ef720");
        public static readonly Guid RuleParamDelay = new Guid("dbe52a58-a78c-4103-8e17-6856934c08eb");


        public static readonly Guid RuleOutput = new Guid("d716c3aa-cc83-410c-b62f-7597e5ba6c3d");

        public override string LogicName => "Time.DelayedOn";
        public override Version LogicVersion => new Version(1, 1, 0, 2);
        public override Guid LogicGuid => new Guid("84607422-507d-4156-b5aa-559474d5080e");

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, "TIME.DELAYON.NAME", "TIME.DELAYON.DESCRIPTION",
                "time.delayed-off", "TIME.NAME", 100, 100);

            factory.CreateLogicInterfaceTemplate(RuleTrigger, "T", "TIME.DELAYON.TRIGGER.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 0, 1);

            factory.CreateLogicInterfaceTemplate(RuleOutput, "O", "TIME.DELAYON.OUTPUT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Output, 0, 1);

            factory.CreateParameterLogicInterfaceTemplate(RuleParamDelay, "TIME.DELAY.NAME", "TIME.DELAY.DESCRIPTION", "delay", LogicGuid, 1, Automatica.Core.EF.Models.RuleInterfaceParameterDataType.Integer, 5, true);
        }

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new DelayedOnRule(context);
        }
    }
}
