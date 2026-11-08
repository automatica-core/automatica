using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;

namespace P3.Logic.Operations.Garage
{
    /// <summary>
    /// Garage / Gate Central: Controls several Garage / Gate blocks together and counts the open and closed doors (modelled on the Loxone function block of the same name)
    /// </summary>
    public class GarageCentralLogicFactory : LogicFactory
    {
        public static readonly Guid InputToggle = new Guid("2504cede-b70a-5215-8f28-aec123ba2f02");
        public static readonly Guid InputCompleteOpen = new Guid("924c9abd-6ef8-5d5d-bf33-3cb694eaba35");
        public static readonly Guid InputCompleteClose = new Guid("72eff664-2ddf-53af-bdf6-e856fa0e22c2");
        public static readonly Guid InputPartialOpen = new Guid("6662a950-3619-50a8-a72e-852436de5bed");
        public static readonly Guid InputStop = new Guid("12394eef-57bb-5a4d-b757-1e6cc88a3ab5");
        public static readonly Guid InputLock = new Guid("8b50b9ed-12fc-5f54-a988-a25d7d0f4cd8");
        public static readonly Guid InputDisablePeriphery = new Guid("e2ca2f6e-f53d-5606-ade3-e0fbccc0ccc3");
        public static readonly Guid InputState1 = new Guid("38c67b50-fad6-534e-9c50-480cc6a995e7");
        public static readonly Guid InputState2 = new Guid("d142dddb-6003-5420-90d3-e00129a3a057");
        public static readonly Guid InputState3 = new Guid("18c4fe6d-57d1-57e5-b269-66340f46c347");
        public static readonly Guid InputState4 = new Guid("a2c1a3d3-d145-5adc-b8c1-caa66d00a4b3");
        public static readonly Guid InputState5 = new Guid("4fd9b0e2-8652-57ba-8f82-50091b974dd5");
        public static readonly Guid InputState6 = new Guid("4ed6ad44-229a-578f-af75-4470da2d3d53");
        public static readonly Guid InputState7 = new Guid("f4c98876-c34b-5489-a2ee-c4e80b9a7b4e");
        public static readonly Guid InputState8 = new Guid("87df4ad3-31bd-521f-a260-c4e8d91eda0b");
        public static readonly Guid OutputCommand = new Guid("fc565464-efd8-581e-8dbf-c60e7408ed73");
        public static readonly Guid OutputOpenCount = new Guid("6eb4754d-8c22-5528-a1b5-743b8966dd1f");
        public static readonly Guid OutputClosedCount = new Guid("d1e922bd-6416-5009-8959-7048790ee6e1");

        public static readonly Guid[] InputStates = { InputState1, InputState2, InputState3, InputState4, InputState5, InputState6, InputState7, InputState8 };

        public override string LogicName => "P3.Logic.Operations.GarageCentral";
        public override Guid LogicGuid => new Guid("c3f5bffa-388c-5fbb-9182-4ad8f6367d6b");
        public override Version LogicVersion => new Version(1, 0, 0, 0);

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, "OPERATIONS.GARAGE_CENTRAL.NAME", "OPERATIONS.GARAGE_CENTRAL.DESCRIPTION", "operations-garage-central", "OPERATIONS.NAME", 100, 100);

            factory.CreateLogicInterfaceTemplate(InputToggle, "OPERATIONS.GARAGE_CENTRAL.INPUT.TOGGLE.NAME", "OPERATIONS.GARAGE_CENTRAL.INPUT.TOGGLE.DESCRIPTION", "toggle", LogicGuid, LogicInterfaceDirection.Input, 0, 1);
            factory.CreateLogicInterfaceTemplate(InputCompleteOpen, "OPERATIONS.GARAGE_CENTRAL.INPUT.COMPLETE_OPEN.NAME", "OPERATIONS.GARAGE_CENTRAL.INPUT.COMPLETE_OPEN.DESCRIPTION", "completeOpen", LogicGuid, LogicInterfaceDirection.Input, 0, 2);
            factory.CreateLogicInterfaceTemplate(InputCompleteClose, "OPERATIONS.GARAGE_CENTRAL.INPUT.COMPLETE_CLOSE.NAME", "OPERATIONS.GARAGE_CENTRAL.INPUT.COMPLETE_CLOSE.DESCRIPTION", "completeClose", LogicGuid, LogicInterfaceDirection.Input, 0, 3);
            factory.CreateLogicInterfaceTemplate(InputPartialOpen, "OPERATIONS.GARAGE_CENTRAL.INPUT.PARTIAL_OPEN.NAME", "OPERATIONS.GARAGE_CENTRAL.INPUT.PARTIAL_OPEN.DESCRIPTION", "partialOpen", LogicGuid, LogicInterfaceDirection.Input, 0, 4);
            factory.CreateLogicInterfaceTemplate(InputStop, "OPERATIONS.GARAGE_CENTRAL.INPUT.STOP.NAME", "OPERATIONS.GARAGE_CENTRAL.INPUT.STOP.DESCRIPTION", "stop", LogicGuid, LogicInterfaceDirection.Input, 0, 5);
            factory.CreateLogicInterfaceTemplate(InputLock, "OPERATIONS.GARAGE_CENTRAL.INPUT.LOCK.NAME", "OPERATIONS.GARAGE_CENTRAL.INPUT.LOCK.DESCRIPTION", "lock", LogicGuid, LogicInterfaceDirection.Input, 0, 6);
            factory.CreateLogicInterfaceTemplate(InputDisablePeriphery, "OPERATIONS.GARAGE_CENTRAL.INPUT.DISABLE_PERIPHERY.NAME", "OPERATIONS.GARAGE_CENTRAL.INPUT.DISABLE_PERIPHERY.DESCRIPTION", "disablePeriphery", LogicGuid, LogicInterfaceDirection.Input, 0, 7);
            factory.CreateLogicInterfaceTemplate(InputState1, "OPERATIONS.GARAGE_CENTRAL.INPUT.STATE1.NAME", "OPERATIONS.GARAGE_CENTRAL.INPUT.STATE1.DESCRIPTION", "state1", LogicGuid, LogicInterfaceDirection.Input, 0, 8);
            factory.CreateLogicInterfaceTemplate(InputState2, "OPERATIONS.GARAGE_CENTRAL.INPUT.STATE2.NAME", "OPERATIONS.GARAGE_CENTRAL.INPUT.STATE2.DESCRIPTION", "state2", LogicGuid, LogicInterfaceDirection.Input, 0, 9);
            factory.CreateLogicInterfaceTemplate(InputState3, "OPERATIONS.GARAGE_CENTRAL.INPUT.STATE3.NAME", "OPERATIONS.GARAGE_CENTRAL.INPUT.STATE3.DESCRIPTION", "state3", LogicGuid, LogicInterfaceDirection.Input, 0, 10);
            factory.CreateLogicInterfaceTemplate(InputState4, "OPERATIONS.GARAGE_CENTRAL.INPUT.STATE4.NAME", "OPERATIONS.GARAGE_CENTRAL.INPUT.STATE4.DESCRIPTION", "state4", LogicGuid, LogicInterfaceDirection.Input, 0, 11);
            factory.CreateLogicInterfaceTemplate(InputState5, "OPERATIONS.GARAGE_CENTRAL.INPUT.STATE5.NAME", "OPERATIONS.GARAGE_CENTRAL.INPUT.STATE5.DESCRIPTION", "state5", LogicGuid, LogicInterfaceDirection.Input, 0, 12);
            factory.CreateLogicInterfaceTemplate(InputState6, "OPERATIONS.GARAGE_CENTRAL.INPUT.STATE6.NAME", "OPERATIONS.GARAGE_CENTRAL.INPUT.STATE6.DESCRIPTION", "state6", LogicGuid, LogicInterfaceDirection.Input, 0, 13);
            factory.CreateLogicInterfaceTemplate(InputState7, "OPERATIONS.GARAGE_CENTRAL.INPUT.STATE7.NAME", "OPERATIONS.GARAGE_CENTRAL.INPUT.STATE7.DESCRIPTION", "state7", LogicGuid, LogicInterfaceDirection.Input, 0, 14);
            factory.CreateLogicInterfaceTemplate(InputState8, "OPERATIONS.GARAGE_CENTRAL.INPUT.STATE8.NAME", "OPERATIONS.GARAGE_CENTRAL.INPUT.STATE8.DESCRIPTION", "state8", LogicGuid, LogicInterfaceDirection.Input, 0, 15);
            factory.CreateLogicInterfaceTemplate(OutputCommand, "OPERATIONS.GARAGE_CENTRAL.OUTPUT.COMMAND.NAME", "OPERATIONS.GARAGE_CENTRAL.OUTPUT.COMMAND.DESCRIPTION", "command", LogicGuid, LogicInterfaceDirection.Output, 0, 1);
            factory.CreateLogicInterfaceTemplate(OutputOpenCount, "OPERATIONS.GARAGE_CENTRAL.OUTPUT.OPEN_COUNT.NAME", "OPERATIONS.GARAGE_CENTRAL.OUTPUT.OPEN_COUNT.DESCRIPTION", "openCount", LogicGuid, LogicInterfaceDirection.Output, 0, 2);
            factory.CreateLogicInterfaceTemplate(OutputClosedCount, "OPERATIONS.GARAGE_CENTRAL.OUTPUT.CLOSED_COUNT.NAME", "OPERATIONS.GARAGE_CENTRAL.OUTPUT.CLOSED_COUNT.DESCRIPTION", "closedCount", LogicGuid, LogicInterfaceDirection.Output, 0, 3);
        }

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new GarageCentralLogic(context);
        }
    }
}
