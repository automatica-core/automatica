using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using LogicInterfaceDirection = Automatica.Core.Base.Templates.LogicInterfaceDirection;

namespace P3.Logic.Logic.BaseOperations.PushSwitch
{
    public class PushSwitchLogicFactory : LogicFactory
    {
        public static readonly Guid RuleToggle = new Guid("42aa6f1a-8f5e-40ce-bdc0-3627776022f3");
        public static readonly Guid RuleSet = new Guid("4b526de9-6b02-4060-a446-a8f7ba0f2f6e");
        public static readonly Guid RuleReset = new Guid("03c8a9c4-c7d5-4e52-8740-2aed710efdc9");
        public static readonly Guid RuleOutput = new Guid("0b292a0c-4667-45f5-a04f-dd6eb7f43532");

        public override string LogicName => "Logic.PushSwitch";
        public override Version LogicVersion => new Version(1, 0, 0, 0);
        public override Guid LogicGuid => new Guid("b94c6ce7-6f3e-4bf0-8e8d-84c583d47a82");

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, "LOGIC.PUSH_SWITCH.NAME", "LOGIC.PUSH_SWITCH.DESCRIPTION", "logic.push_switch", "LOGIC.NAME", 100, 100);

            factory.CreateLogicInterfaceTemplate(RuleToggle, "T", "LOGIC.PUSH_SWITCH.TOGGLE.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 0, 1);
            factory.CreateLogicInterfaceTemplate(RuleSet, "S", "LOGIC.PUSH_SWITCH.SET.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 0, 2);
            factory.CreateLogicInterfaceTemplate(RuleReset, "R", "LOGIC.PUSH_SWITCH.RESET.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 0, 3);

            factory.CreateLogicInterfaceTemplate(RuleOutput, "Q", "LOGIC.PUSH_SWITCH.OUTPUT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Output, 0, 1, RuleInterfaceType.Status);
        }

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new PushSwitchRule(context);
        }
    }
}
