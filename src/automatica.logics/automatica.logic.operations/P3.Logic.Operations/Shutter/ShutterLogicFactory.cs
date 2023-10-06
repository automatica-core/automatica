using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.Logic;
using P3.Logic.Operations.Dimmer;
using LogicInterfaceDirection = Automatica.Core.Base.Templates.LogicInterfaceDirection;

namespace P3.Logic.Operations.Shutter
{
    public class ShutterLogicFactory : LogicFactory
    {
        public static Guid RuleInputMove = new Guid("d5005f24-245e-413d-b28d-f62aba4e835c");
        public static Guid RuleInputDirection = new Guid("63d1fa21-35cb-4881-a82c-386aa89cfdd9");
        public static Guid RuleInputUp = new Guid("1fb1bedf-9141-4369-acde-4c5f26e4a961");
        public static Guid RuleInputDown = new Guid("6e0ccfe5-a55e-4ed3-bc0c-659c1307233e");
        public static Guid RuleInputStop = new Guid("454ca126-775c-42f4-9cfd-da65b098568e");
        public static Guid RuleInputLocked = new Guid("ec8a1979-e15d-4fc7-8d92-ed6569449c9a");

        public static Guid RuleInputAbsolutePercentage = new Guid("1f66b971-a80f-4001-bc1e-670bd23630b7");

        public static Guid RuleOutputAbsolutePercentage = new Guid("32837da4-a619-41d3-8390-aa0221bf2cba");
        public static Guid RuleOutputShutterMove = new Guid("1e59f6a8-136f-4d3c-9b30-4ec2f8396932");
        public static Guid RuleOutputDirection = new Guid("9f909f2e-68c6-4e0a-8e76-fdcb7f408111");
        public static Guid RuleOutputStop = new Guid("90b89646-bc3c-4ec5-9f6e-6b4e67b43666");
        public static Guid RuleOutputMoveState = new Guid("66703a94-f9c6-4c01-bee9-96286fe148b3");

        public override string LogicName => "P3.Logic.Operations.Shutter";
        public override Guid LogicGuid => new Guid("2f96f82f-2dd1-4fcb-9960-3e3f016975b8");
        public override Version LogicVersion => new Version(1, 0, 0, 2);
        public override bool InDevelopmentMode => true;

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, "OPERATIONS.SHUTTER.NAME", "OPERATIONS.SHUTTER.DESCRIPTION",
                "operations-blinds", "OPERATIONS.NAME", 100, 100);

            factory.CreateLogicInterfaceTemplate(RuleInputMove, "OPERATIONS.SHUTTER.INPUT.MOVE.NAME", "OPERATIONS.SHUTTER.INPUT.MOVE.DESCRIPTION", "ruleInputMove", LogicGuid, LogicInterfaceDirection.Input, 0, 1);
            factory.CreateLogicInterfaceTemplate(RuleInputDirection, "OPERATIONS.SHUTTER.INPUT.DIRECTION.NAME", "OPERATIONS.SHUTTER.INPUT.DIRECTION.DESCRIPTION", "direction", LogicGuid, LogicInterfaceDirection.Input, 0, 2);
            factory.CreateLogicInterfaceTemplate(RuleInputUp, "OPERATIONS.SHUTTER.INPUT.UP.NAME", "OPERATIONS.SHUTTER.INPUT.UP.DESCRIPTION", "up", LogicGuid, LogicInterfaceDirection.Input, 0, 3);
            factory.CreateLogicInterfaceTemplate(RuleInputDown, "OPERATIONS.SHUTTER.INPUT.DOWN.NAME", "OPERATIONS.SHUTTER.INPUT.DOWN.DESCRIPTION", "down", LogicGuid, LogicInterfaceDirection.Input, 0, 4);
            factory.CreateLogicInterfaceTemplate(RuleInputStop, "OPERATIONS.SHUTTER.INPUT.STOP.NAME", "OPERATIONS.SHUTTER.INPUT.STOP.DESCRIPTION", "stop", LogicGuid, LogicInterfaceDirection.Input, 0, 5);
            factory.CreateLogicInterfaceTemplate(RuleInputAbsolutePercentage, "OPERATIONS.SHUTTER.INPUT.ABSOLUTE.NAME", "OPERATIONS.SHUTTER.INPUT.ABSOLUTE.DESCRIPTION", "absolutePercentage", LogicGuid, LogicInterfaceDirection.Input, 0, 6);
            factory.CreateLogicInterfaceTemplate(RuleInputLocked, "OPERATIONS.SHUTTER.INPUT.LOCKED.NAME", "OPERATIONS.SHUTTER.INPUT.LOCKED.DESCRIPTION", "locked", LogicGuid, LogicInterfaceDirection.Input, 0, 6);

            factory.CreateLogicInterfaceTemplate(RuleOutputAbsolutePercentage, "OPERATIONS.SHUTTER.OUTPUT.ABSOLUTE.NAME", "OPERATIONS.SHUTTER.OUTPUT.ABSOLUTE.DESCRIPTION", "absolutePercentageOutput", LogicGuid, LogicInterfaceDirection.Output, 0, 1);
            factory.CreateLogicInterfaceTemplate(RuleOutputShutterMove, "OPERATIONS.SHUTTER.OUTPUT.MOVE.NAME", "OPERATIONS.SHUTTER.OUTPUT.MOVE.DESCRIPTION", "moveOutput", LogicGuid, LogicInterfaceDirection.Output, 0, 1);
            factory.CreateLogicInterfaceTemplate(RuleOutputDirection, "OPERATIONS.SHUTTER.OUTPUT.DIRECTION.NAME", "OPERATIONS.SHUTTER.OUTPUT.DIRECTION.DESCRIPTION", "directionOutput", LogicGuid, LogicInterfaceDirection.Output, 0, 2);
            factory.CreateLogicInterfaceTemplate(RuleOutputStop, "OPERATIONS.SHUTTER.OUTPUT.STOP.NAME", "OPERATIONS.SHUTTER.OUTPUT.STOP.DESCRIPTION", "stopOutput", LogicGuid, LogicInterfaceDirection.Output, 0, 5);

            factory.CreateLogicInterfaceTemplate(RuleOutputMoveState, "OPERATIONS.SHUTTER.OUTPUT.IS_MOVING.NAME", "OPERATIONS.SHUTTER.OUTPUT.IS_MOVING.DESCRIPTION", "ruleIsMoving", LogicGuid, LogicInterfaceDirection.Output, 0, 1);

            factory.ChangeDefaultVisuTemplate(LogicGuid, VisuMobileObjectTemplateTypes.Shutter);
        }

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new ShutterLogic(context);
        }

    }
}
