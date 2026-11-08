using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;

namespace P3.Logic.Operations.Garage
{
    /// <summary>
    /// Garage / Gate: Motor control of a garage door or gate with one or two buttons, end switches and a light barrier (modelled on the Loxone function block of the same name)
    /// </summary>
    public class GarageGateLogicFactory : LogicFactory
    {
        public static readonly Guid InputToggle = new Guid("6b0304cf-6968-52f6-8f7b-a95c60a3fc98");
        public static readonly Guid InputOpen = new Guid("234a72d0-5713-564b-b8ce-d4afff323c42");
        public static readonly Guid InputClose = new Guid("b34d8e64-a9b0-5753-a2b8-b645951bda2d");
        public static readonly Guid InputCompleteOpen = new Guid("e5e3aa26-be94-50eb-bca0-5892ae025226");
        public static readonly Guid InputCompleteClose = new Guid("bc6ce183-bc63-510c-816a-e294969fc704");
        public static readonly Guid InputPartialOpen = new Guid("d4f54d4c-179f-5f25-8bb3-6acb183f4a1a");
        public static readonly Guid InputStop = new Guid("3414565f-569e-5b51-a109-47d66fe42351");
        public static readonly Guid InputLock = new Guid("26b68967-d960-5bec-9cfd-0bbf9c8bd37a");
        public static readonly Guid InputDisablePeriphery = new Guid("8f764f8b-e137-502c-ad67-8e1522e9aa4c");
        public static readonly Guid InputEndOpen = new Guid("c9e4ad49-ca71-5ba8-b796-75bdd92f0fd7");
        public static readonly Guid InputEndClosed = new Guid("67a610bc-6bed-523c-8dd5-c62a0b0c7ffb");
        public static readonly Guid InputObstacle = new Guid("d80cdc90-8957-582d-b519-cf6830825842");
        public static readonly Guid InputCentral = new Guid("ed3e62dd-8c3c-589d-9362-635d436db334");
        public static readonly Guid OutputOpen = new Guid("fef2d3af-67cb-5182-8c5e-3b5e6cdcfa7d");
        public static readonly Guid OutputClose = new Guid("ad20b2c0-5fa2-520a-b953-00ee0bcd218b");
        public static readonly Guid OutputPulse = new Guid("a5a46d13-e476-584d-90b2-f2fd7d0813ce");
        public static readonly Guid OutputPosition = new Guid("d03eceac-eece-5387-82c9-e74471d1a8db");
        public static readonly Guid OutputState = new Guid("d1af760b-128d-5578-ad77-cff5073e5555");
        public static readonly Guid OutputIsOpen = new Guid("0ab861ea-5e82-5515-9fcc-d4d8f53dbbcc");
        public static readonly Guid OutputLocked = new Guid("af3bf2c6-5588-58d4-a4b3-ad011180ab02");
        public static readonly Guid ParamType = new Guid("f3df34a6-e846-58ea-8ebb-1f3ef322ddac");
        public static readonly Guid ParamMotorMode = new Guid("4cc35024-45df-56d0-923b-a459cd045e71");
        public static readonly Guid ParamOpenTime = new Guid("d72c6ca5-8f77-5354-9ca4-ab91eacbb4a7");
        public static readonly Guid ParamCloseTime = new Guid("15b18c3e-852a-5ae5-97ba-da69e1a026b2");
        public static readonly Guid ParamPartialPosition = new Guid("22526cb6-93f6-5c2e-a45e-97fdae6def6e");
        public static readonly Guid ParamPulseLength = new Guid("f411b545-2754-582f-81fa-11c2664998b4");
        public static readonly Guid ParamAutoClose = new Guid("363c8633-1937-5b48-8091-890866995f3f");


        public override string LogicName => "P3.Logic.Operations.GarageGate";
        public override Guid LogicGuid => new Guid("a5d24738-aae4-59b1-83b3-f29565c5a5c6");
        public override Version LogicVersion => new Version(1, 0, 0, 0);

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, "OPERATIONS.GARAGE_GATE.NAME", "OPERATIONS.GARAGE_GATE.DESCRIPTION", "operations-garage-gate", "OPERATIONS.NAME", 100, 100);

            factory.CreateLogicInterfaceTemplate(InputToggle, "OPERATIONS.GARAGE_GATE.INPUT.TOGGLE.NAME", "OPERATIONS.GARAGE_GATE.INPUT.TOGGLE.DESCRIPTION", "toggle", LogicGuid, LogicInterfaceDirection.Input, 0, 1);
            factory.CreateLogicInterfaceTemplate(InputOpen, "OPERATIONS.GARAGE_GATE.INPUT.OPEN.NAME", "OPERATIONS.GARAGE_GATE.INPUT.OPEN.DESCRIPTION", "open", LogicGuid, LogicInterfaceDirection.Input, 0, 2);
            factory.CreateLogicInterfaceTemplate(InputClose, "OPERATIONS.GARAGE_GATE.INPUT.CLOSE.NAME", "OPERATIONS.GARAGE_GATE.INPUT.CLOSE.DESCRIPTION", "close", LogicGuid, LogicInterfaceDirection.Input, 0, 3);
            factory.CreateLogicInterfaceTemplate(InputCompleteOpen, "OPERATIONS.GARAGE_GATE.INPUT.COMPLETE_OPEN.NAME", "OPERATIONS.GARAGE_GATE.INPUT.COMPLETE_OPEN.DESCRIPTION", "completeOpen", LogicGuid, LogicInterfaceDirection.Input, 0, 4);
            factory.CreateLogicInterfaceTemplate(InputCompleteClose, "OPERATIONS.GARAGE_GATE.INPUT.COMPLETE_CLOSE.NAME", "OPERATIONS.GARAGE_GATE.INPUT.COMPLETE_CLOSE.DESCRIPTION", "completeClose", LogicGuid, LogicInterfaceDirection.Input, 0, 5);
            factory.CreateLogicInterfaceTemplate(InputPartialOpen, "OPERATIONS.GARAGE_GATE.INPUT.PARTIAL_OPEN.NAME", "OPERATIONS.GARAGE_GATE.INPUT.PARTIAL_OPEN.DESCRIPTION", "partialOpen", LogicGuid, LogicInterfaceDirection.Input, 0, 6);
            factory.CreateLogicInterfaceTemplate(InputStop, "OPERATIONS.GARAGE_GATE.INPUT.STOP.NAME", "OPERATIONS.GARAGE_GATE.INPUT.STOP.DESCRIPTION", "stop", LogicGuid, LogicInterfaceDirection.Input, 0, 7);
            factory.CreateLogicInterfaceTemplate(InputLock, "OPERATIONS.GARAGE_GATE.INPUT.LOCK.NAME", "OPERATIONS.GARAGE_GATE.INPUT.LOCK.DESCRIPTION", "lock", LogicGuid, LogicInterfaceDirection.Input, 0, 8);
            factory.CreateLogicInterfaceTemplate(InputDisablePeriphery, "OPERATIONS.GARAGE_GATE.INPUT.DISABLE_PERIPHERY.NAME", "OPERATIONS.GARAGE_GATE.INPUT.DISABLE_PERIPHERY.DESCRIPTION", "disablePeriphery", LogicGuid, LogicInterfaceDirection.Input, 0, 9);
            factory.CreateLogicInterfaceTemplate(InputEndOpen, "OPERATIONS.GARAGE_GATE.INPUT.END_OPEN.NAME", "OPERATIONS.GARAGE_GATE.INPUT.END_OPEN.DESCRIPTION", "endOpen", LogicGuid, LogicInterfaceDirection.Input, 0, 10);
            factory.CreateLogicInterfaceTemplate(InputEndClosed, "OPERATIONS.GARAGE_GATE.INPUT.END_CLOSED.NAME", "OPERATIONS.GARAGE_GATE.INPUT.END_CLOSED.DESCRIPTION", "endClosed", LogicGuid, LogicInterfaceDirection.Input, 0, 11);
            factory.CreateLogicInterfaceTemplate(InputObstacle, "OPERATIONS.GARAGE_GATE.INPUT.OBSTACLE.NAME", "OPERATIONS.GARAGE_GATE.INPUT.OBSTACLE.DESCRIPTION", "obstacle", LogicGuid, LogicInterfaceDirection.Input, 0, 12);
            factory.CreateLogicInterfaceTemplate(InputCentral, "OPERATIONS.GARAGE_GATE.INPUT.CENTRAL.NAME", "OPERATIONS.GARAGE_GATE.INPUT.CENTRAL.DESCRIPTION", "central", LogicGuid, LogicInterfaceDirection.Input, 0, 13);
            factory.CreateLogicInterfaceTemplate(OutputOpen, "OPERATIONS.GARAGE_GATE.OUTPUT.OPEN.NAME", "OPERATIONS.GARAGE_GATE.OUTPUT.OPEN.DESCRIPTION", "openOutput", LogicGuid, LogicInterfaceDirection.Output, 0, 1);
            factory.CreateLogicInterfaceTemplate(OutputClose, "OPERATIONS.GARAGE_GATE.OUTPUT.CLOSE.NAME", "OPERATIONS.GARAGE_GATE.OUTPUT.CLOSE.DESCRIPTION", "closeOutput", LogicGuid, LogicInterfaceDirection.Output, 0, 2);
            factory.CreateLogicInterfaceTemplate(OutputPulse, "OPERATIONS.GARAGE_GATE.OUTPUT.PULSE.NAME", "OPERATIONS.GARAGE_GATE.OUTPUT.PULSE.DESCRIPTION", "pulseOutput", LogicGuid, LogicInterfaceDirection.Output, 0, 3);
            factory.CreateLogicInterfaceTemplate(OutputPosition, "OPERATIONS.GARAGE_GATE.OUTPUT.POSITION.NAME", "OPERATIONS.GARAGE_GATE.OUTPUT.POSITION.DESCRIPTION", "position", LogicGuid, LogicInterfaceDirection.Output, 0, 4);
            factory.CreateLogicInterfaceTemplate(OutputState, "OPERATIONS.GARAGE_GATE.OUTPUT.STATE.NAME", "OPERATIONS.GARAGE_GATE.OUTPUT.STATE.DESCRIPTION", "state", LogicGuid, LogicInterfaceDirection.Output, 0, 5);
            factory.CreateLogicInterfaceTemplate(OutputIsOpen, "OPERATIONS.GARAGE_GATE.OUTPUT.IS_OPEN.NAME", "OPERATIONS.GARAGE_GATE.OUTPUT.IS_OPEN.DESCRIPTION", "isOpen", LogicGuid, LogicInterfaceDirection.Output, 0, 6);
            factory.CreateLogicInterfaceTemplate(OutputLocked, "OPERATIONS.GARAGE_GATE.OUTPUT.LOCKED.NAME", "OPERATIONS.GARAGE_GATE.OUTPUT.LOCKED.DESCRIPTION", "locked", LogicGuid, LogicInterfaceDirection.Output, 0, 7);
            factory.CreateParameterLogicInterfaceTemplate(ParamType, "OPERATIONS.GARAGE_GATE.PARAM.TYPE.NAME", "OPERATIONS.GARAGE_GATE.PARAM.TYPE.DESCRIPTION", "type", LogicGuid, 1, RuleInterfaceParameterDataType.Enum, "0", false, "ENUM(0,OPERATIONS.GARAGE_GATE.TYPE.GARAGE;1,OPERATIONS.GARAGE_GATE.TYPE.SLIDING_GATE;2,OPERATIONS.GARAGE_GATE.TYPE.SWING_GATE)");
            factory.CreateParameterLogicInterfaceTemplate(ParamMotorMode, "OPERATIONS.GARAGE_GATE.PARAM.MOTOR_MODE.NAME", "OPERATIONS.GARAGE_GATE.PARAM.MOTOR_MODE.DESCRIPTION", "motorMode", LogicGuid, 2, RuleInterfaceParameterDataType.Enum, "0", false, "ENUM(0,OPERATIONS.GARAGE_GATE.MOTOR.TWO_OUTPUTS;1,OPERATIONS.GARAGE_GATE.MOTOR.SINGLE_BUTTON)");
            factory.CreateParameterLogicInterfaceTemplate(ParamOpenTime, "OPERATIONS.GARAGE_GATE.PARAM.OPEN_TIME.NAME", "OPERATIONS.GARAGE_GATE.PARAM.OPEN_TIME.DESCRIPTION", "openTime", LogicGuid, 3, RuleInterfaceParameterDataType.Double, 20.0, false);
            factory.CreateParameterLogicInterfaceTemplate(ParamCloseTime, "OPERATIONS.GARAGE_GATE.PARAM.CLOSE_TIME.NAME", "OPERATIONS.GARAGE_GATE.PARAM.CLOSE_TIME.DESCRIPTION", "closeTime", LogicGuid, 4, RuleInterfaceParameterDataType.Double, 20.0, false);
            factory.CreateParameterLogicInterfaceTemplate(ParamPartialPosition, "OPERATIONS.GARAGE_GATE.PARAM.PARTIAL_POSITION.NAME", "OPERATIONS.GARAGE_GATE.PARAM.PARTIAL_POSITION.DESCRIPTION", "partialPosition", LogicGuid, 5, RuleInterfaceParameterDataType.Integer, 20, false);
            factory.CreateParameterLogicInterfaceTemplate(ParamPulseLength, "OPERATIONS.GARAGE_GATE.PARAM.PULSE_LENGTH.NAME", "OPERATIONS.GARAGE_GATE.PARAM.PULSE_LENGTH.DESCRIPTION", "pulseLength", LogicGuid, 6, RuleInterfaceParameterDataType.Integer, 500, false);
            factory.CreateParameterLogicInterfaceTemplate(ParamAutoClose, "OPERATIONS.GARAGE_GATE.PARAM.AUTO_CLOSE.NAME", "OPERATIONS.GARAGE_GATE.PARAM.AUTO_CLOSE.DESCRIPTION", "autoClose", LogicGuid, 7, RuleInterfaceParameterDataType.Integer, 0, true);

            // the tile of this block is a core visu template (enum value 20); older cores without it keep the default logic tile,
            // so the plugin still builds and runs against the published core NuGet
            if (Enum.GetName(typeof(VisuMobileObjectTemplateTypes), 20) == "Garage")
            {
                factory.ChangeDefaultVisuTemplate(LogicGuid, (VisuMobileObjectTemplateTypes)20);
            }
        }

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new GarageGateLogic(context);
        }
    }
}
