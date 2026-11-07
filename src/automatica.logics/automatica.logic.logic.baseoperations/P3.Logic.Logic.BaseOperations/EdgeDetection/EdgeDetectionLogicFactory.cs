using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.Logic;
using LogicInterfaceDirection = Automatica.Core.Base.Templates.LogicInterfaceDirection;

namespace P3.Logic.Logic.BaseOperations.EdgeDetection
{
    public class EdgeDetectionLogicFactory : LogicFactory
    {
        public static readonly Guid RuleInput = new Guid("16bab6bb-e7d0-4d9c-b451-880d4edfb98d");
        public static readonly Guid RuleRising = new Guid("7363f61a-90d2-4a5d-a7f1-3d4ae4c1f63f");
        public static readonly Guid RuleFalling = new Guid("803151cb-8627-4d9f-8c43-fa4cc4635b37");

        public override string LogicName => "Logic.EdgeDetection";
        public override Version LogicVersion => new Version(1, 0, 0, 0);
        public override Guid LogicGuid => new Guid("c675447c-56d5-4dd4-bf80-59066a17c4e2");

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, "LOGIC.EDGE_DETECTION.NAME", "LOGIC.EDGE_DETECTION.DESCRIPTION", "logic.edge_detection", "LOGIC.NAME", 100, 100);

            factory.CreateLogicInterfaceTemplate(RuleInput, "I", "LOGIC.EDGE_DETECTION.INPUT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, 1, 1);

            factory.CreateLogicInterfaceTemplate(RuleRising, "R", "LOGIC.EDGE_DETECTION.RISING.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Output, 0, 1);
            factory.CreateLogicInterfaceTemplate(RuleFalling, "F", "LOGIC.EDGE_DETECTION.FALLING.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Output, 0, 2);
        }

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new EdgeDetectionRule(context);
        }
    }
}
