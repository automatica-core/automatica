using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using P3.Logic.Operations.Switch;
using LogicInterfaceDirection = Automatica.Core.Base.Templates.LogicInterfaceDirection;

namespace P3.Logic.Operations.Push
{
    public class PushLogicFactory : LogicFactory
    {
        public static Guid RuleInput = new Guid("d87fd650-b8e8-4ef7-ba4b-cdee01970cef");
        public static Guid RuleOutput = new Guid("9f8f9a68-c2a5-4000-9763-7952a75d7062");
        public static Guid Duration = new Guid("e9b13289-bcdc-4094-beab-bd8c591f1a85");

        public override string LogicName => "P3.Logic.Operations.Push";
        public override Guid LogicGuid => new Guid("26e85c17-57bb-4f49-bd08-eb745a6f0a44");
        public override Version LogicVersion => new Version(1, 0, 0, 0);
        public override bool InDevelopmentMode => true;

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, "OPERATIONS.PUSH.NAME", "OPERATIONS.PUSH.DESCRIPTION",
                "operations-push", "OPERATIONS.NAME", 100, 100);

            factory.CreateLogicInterfaceTemplate(RuleInput, "OPERATIONS.PUSH.INPUT.NAME", "OPERATIONS.PUSH.INPUT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 0, 1, RuleInterfaceType.Input);
            
            factory.CreateLogicInterfaceTemplate(RuleOutput, "OPERATIONS.PUSH.OUTPUT.NAME", "OPERATIONS.PUSH.OUTPUT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Output, 0, 1, RuleInterfaceType.Output);
            factory.CreateParameterLogicInterfaceTemplate(Duration, "OPERATIONS.PUSH.DURATION.NAME",
                "OPERATIONS.PUSH.DURATION.DESCRIPTION", LogicGuid, 1, RuleInterfaceParameterDataType.Integer, "300",
                false);

            factory.ChangeDefaultVisuTemplate(LogicGuid, VisuMobileObjectTemplateTypes.PushButton);
        }

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new PushLogic(context);
        }

    }
}
