using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;

namespace P3.Logic.Operations.WindowCentral
{
    /// <summary>
    /// Window Central: Central control of several motorised windows (window actuators or shutter blocks), weather protection and counters (modelled on the Loxone function block of the same name)
    /// </summary>
    public class WindowCentralLogicFactory : LogicFactory
    {
        public static readonly Guid InputCompleteOpen = new Guid("9857a533-b31d-5a80-8c28-4e90c5fef332");
        public static readonly Guid InputCompleteClose = new Guid("0c2cefcb-5ca1-53f7-864a-15497ade3a14");
        public static readonly Guid InputToggle = new Guid("218b4bbb-3626-572e-90ab-8485de4a4128");
        public static readonly Guid InputPosition = new Guid("4e0e3b1e-d54c-5ff6-96be-b142ca7fd643");
        public static readonly Guid InputPartialOpen = new Guid("7bb9df00-6d05-5be9-89b5-101e2a9e9548");
        public static readonly Guid InputPartialClose = new Guid("a53251cc-d450-5f19-be07-565924ddfd0c");
        public static readonly Guid InputSlightlyOpen = new Guid("f9165f5c-a7af-5b32-9217-ace1f1ad25ce");
        public static readonly Guid InputWeatherProtection = new Guid("14ed4674-5402-512c-83f1-4becc1a24053");
        public static readonly Guid InputLock = new Guid("5212ad31-6c69-5aed-86d5-c5394d68af82");
        public static readonly Guid InputWindowPosition1 = new Guid("640339e5-f847-5ce8-99d8-17129e995199");
        public static readonly Guid InputWindowPosition2 = new Guid("4a6c4892-754f-52da-9146-a523575b81c0");
        public static readonly Guid InputWindowPosition3 = new Guid("b31ec215-f78f-5378-b72e-b1dfd446ad6f");
        public static readonly Guid InputWindowPosition4 = new Guid("2737b032-b261-51d0-a815-c5487fa54cf8");
        public static readonly Guid InputWindowPosition5 = new Guid("40adc3f0-f4ab-52f6-9be1-b0f0a39188c7");
        public static readonly Guid InputWindowPosition6 = new Guid("b7f76cbe-7616-58b8-aa9a-48a259d724d5");
        public static readonly Guid InputWindowPosition7 = new Guid("8cd4d050-e224-5ce0-9e37-dcc09e365b79");
        public static readonly Guid InputWindowPosition8 = new Guid("94f5251d-2ad6-5497-9915-e674f84f96ff");
        public static readonly Guid OutputOpen = new Guid("e4307e68-ffb7-574d-9a80-ccb44bd72e71");
        public static readonly Guid OutputClose = new Guid("283d08d8-64ab-587d-9ae4-1dbba22b7c95");
        public static readonly Guid OutputStop = new Guid("c41a5d7e-ae97-5b43-ad26-92c2967dfc53");
        public static readonly Guid OutputPosition = new Guid("0d5329c5-f226-56db-91f5-d20cec5423af");
        public static readonly Guid OutputLocked = new Guid("82af7c2d-3b0d-5562-8877-a13bc0a0328d");
        public static readonly Guid OutputOpenCount = new Guid("64eb4bf0-f62c-556d-98de-39c00ae8f9bf");
        public static readonly Guid OutputClosedCount = new Guid("c3987add-d90d-56ba-a635-fb8cadff3f27");
        public static readonly Guid ParamSlightlyOpen = new Guid("4b37428e-ec0d-578c-9ffc-f549ff81a747");

        public static readonly Guid[] InputWindowPositions = { InputWindowPosition1, InputWindowPosition2, InputWindowPosition3, InputWindowPosition4, InputWindowPosition5, InputWindowPosition6, InputWindowPosition7, InputWindowPosition8 };

        public override string LogicName => "P3.Logic.Operations.WindowCentral";
        public override Guid LogicGuid => new Guid("43a77ba6-18c2-5580-918e-4904e00400ef");
        public override Version LogicVersion => new Version(1, 0, 0, 0);

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, "OPERATIONS.WINDOW_CENTRAL.NAME", "OPERATIONS.WINDOW_CENTRAL.DESCRIPTION", "operations-window-central", "OPERATIONS.NAME", 100, 100);

            factory.CreateLogicInterfaceTemplate(InputCompleteOpen, "OPERATIONS.WINDOW_CENTRAL.INPUT.COMPLETE_OPEN.NAME", "OPERATIONS.WINDOW_CENTRAL.INPUT.COMPLETE_OPEN.DESCRIPTION", "completeOpen", LogicGuid, LogicInterfaceDirection.Input, 0, 1);
            factory.CreateLogicInterfaceTemplate(InputCompleteClose, "OPERATIONS.WINDOW_CENTRAL.INPUT.COMPLETE_CLOSE.NAME", "OPERATIONS.WINDOW_CENTRAL.INPUT.COMPLETE_CLOSE.DESCRIPTION", "completeClose", LogicGuid, LogicInterfaceDirection.Input, 0, 2);
            factory.CreateLogicInterfaceTemplate(InputToggle, "OPERATIONS.WINDOW_CENTRAL.INPUT.TOGGLE.NAME", "OPERATIONS.WINDOW_CENTRAL.INPUT.TOGGLE.DESCRIPTION", "toggle", LogicGuid, LogicInterfaceDirection.Input, 0, 3);
            factory.CreateLogicInterfaceTemplate(InputPosition, "OPERATIONS.WINDOW_CENTRAL.INPUT.POSITION.NAME", "OPERATIONS.WINDOW_CENTRAL.INPUT.POSITION.DESCRIPTION", "position", LogicGuid, LogicInterfaceDirection.Input, 0, 4);
            factory.CreateLogicInterfaceTemplate(InputPartialOpen, "OPERATIONS.WINDOW_CENTRAL.INPUT.PARTIAL_OPEN.NAME", "OPERATIONS.WINDOW_CENTRAL.INPUT.PARTIAL_OPEN.DESCRIPTION", "partialOpen", LogicGuid, LogicInterfaceDirection.Input, 0, 5);
            factory.CreateLogicInterfaceTemplate(InputPartialClose, "OPERATIONS.WINDOW_CENTRAL.INPUT.PARTIAL_CLOSE.NAME", "OPERATIONS.WINDOW_CENTRAL.INPUT.PARTIAL_CLOSE.DESCRIPTION", "partialClose", LogicGuid, LogicInterfaceDirection.Input, 0, 6);
            factory.CreateLogicInterfaceTemplate(InputSlightlyOpen, "OPERATIONS.WINDOW_CENTRAL.INPUT.SLIGHTLY_OPEN.NAME", "OPERATIONS.WINDOW_CENTRAL.INPUT.SLIGHTLY_OPEN.DESCRIPTION", "slightlyOpen", LogicGuid, LogicInterfaceDirection.Input, 0, 7);
            factory.CreateLogicInterfaceTemplate(InputWeatherProtection, "OPERATIONS.WINDOW_CENTRAL.INPUT.WEATHER_PROTECTION.NAME", "OPERATIONS.WINDOW_CENTRAL.INPUT.WEATHER_PROTECTION.DESCRIPTION", "weatherProtection", LogicGuid, LogicInterfaceDirection.Input, 0, 8);
            factory.CreateLogicInterfaceTemplate(InputLock, "OPERATIONS.WINDOW_CENTRAL.INPUT.LOCK.NAME", "OPERATIONS.WINDOW_CENTRAL.INPUT.LOCK.DESCRIPTION", "lock", LogicGuid, LogicInterfaceDirection.Input, 0, 9);
            factory.CreateLogicInterfaceTemplate(InputWindowPosition1, "OPERATIONS.WINDOW_CENTRAL.INPUT.WINDOW_POSITION1.NAME", "OPERATIONS.WINDOW_CENTRAL.INPUT.WINDOW_POSITION1.DESCRIPTION", "windowPosition1", LogicGuid, LogicInterfaceDirection.Input, 0, 10);
            factory.CreateLogicInterfaceTemplate(InputWindowPosition2, "OPERATIONS.WINDOW_CENTRAL.INPUT.WINDOW_POSITION2.NAME", "OPERATIONS.WINDOW_CENTRAL.INPUT.WINDOW_POSITION2.DESCRIPTION", "windowPosition2", LogicGuid, LogicInterfaceDirection.Input, 0, 11);
            factory.CreateLogicInterfaceTemplate(InputWindowPosition3, "OPERATIONS.WINDOW_CENTRAL.INPUT.WINDOW_POSITION3.NAME", "OPERATIONS.WINDOW_CENTRAL.INPUT.WINDOW_POSITION3.DESCRIPTION", "windowPosition3", LogicGuid, LogicInterfaceDirection.Input, 0, 12);
            factory.CreateLogicInterfaceTemplate(InputWindowPosition4, "OPERATIONS.WINDOW_CENTRAL.INPUT.WINDOW_POSITION4.NAME", "OPERATIONS.WINDOW_CENTRAL.INPUT.WINDOW_POSITION4.DESCRIPTION", "windowPosition4", LogicGuid, LogicInterfaceDirection.Input, 0, 13);
            factory.CreateLogicInterfaceTemplate(InputWindowPosition5, "OPERATIONS.WINDOW_CENTRAL.INPUT.WINDOW_POSITION5.NAME", "OPERATIONS.WINDOW_CENTRAL.INPUT.WINDOW_POSITION5.DESCRIPTION", "windowPosition5", LogicGuid, LogicInterfaceDirection.Input, 0, 14);
            factory.CreateLogicInterfaceTemplate(InputWindowPosition6, "OPERATIONS.WINDOW_CENTRAL.INPUT.WINDOW_POSITION6.NAME", "OPERATIONS.WINDOW_CENTRAL.INPUT.WINDOW_POSITION6.DESCRIPTION", "windowPosition6", LogicGuid, LogicInterfaceDirection.Input, 0, 15);
            factory.CreateLogicInterfaceTemplate(InputWindowPosition7, "OPERATIONS.WINDOW_CENTRAL.INPUT.WINDOW_POSITION7.NAME", "OPERATIONS.WINDOW_CENTRAL.INPUT.WINDOW_POSITION7.DESCRIPTION", "windowPosition7", LogicGuid, LogicInterfaceDirection.Input, 0, 16);
            factory.CreateLogicInterfaceTemplate(InputWindowPosition8, "OPERATIONS.WINDOW_CENTRAL.INPUT.WINDOW_POSITION8.NAME", "OPERATIONS.WINDOW_CENTRAL.INPUT.WINDOW_POSITION8.DESCRIPTION", "windowPosition8", LogicGuid, LogicInterfaceDirection.Input, 0, 17);
            factory.CreateLogicInterfaceTemplate(OutputOpen, "OPERATIONS.WINDOW_CENTRAL.OUTPUT.OPEN.NAME", "OPERATIONS.WINDOW_CENTRAL.OUTPUT.OPEN.DESCRIPTION", "openOutput", LogicGuid, LogicInterfaceDirection.Output, 0, 1);
            factory.CreateLogicInterfaceTemplate(OutputClose, "OPERATIONS.WINDOW_CENTRAL.OUTPUT.CLOSE.NAME", "OPERATIONS.WINDOW_CENTRAL.OUTPUT.CLOSE.DESCRIPTION", "closeOutput", LogicGuid, LogicInterfaceDirection.Output, 0, 2);
            factory.CreateLogicInterfaceTemplate(OutputStop, "OPERATIONS.WINDOW_CENTRAL.OUTPUT.STOP.NAME", "OPERATIONS.WINDOW_CENTRAL.OUTPUT.STOP.DESCRIPTION", "stopOutput", LogicGuid, LogicInterfaceDirection.Output, 0, 3);
            factory.CreateLogicInterfaceTemplate(OutputPosition, "OPERATIONS.WINDOW_CENTRAL.OUTPUT.POSITION.NAME", "OPERATIONS.WINDOW_CENTRAL.OUTPUT.POSITION.DESCRIPTION", "positionOutput", LogicGuid, LogicInterfaceDirection.Output, 0, 4);
            factory.CreateLogicInterfaceTemplate(OutputLocked, "OPERATIONS.WINDOW_CENTRAL.OUTPUT.LOCKED.NAME", "OPERATIONS.WINDOW_CENTRAL.OUTPUT.LOCKED.DESCRIPTION", "locked", LogicGuid, LogicInterfaceDirection.Output, 0, 5);
            factory.CreateLogicInterfaceTemplate(OutputOpenCount, "OPERATIONS.WINDOW_CENTRAL.OUTPUT.OPEN_COUNT.NAME", "OPERATIONS.WINDOW_CENTRAL.OUTPUT.OPEN_COUNT.DESCRIPTION", "openCount", LogicGuid, LogicInterfaceDirection.Output, 0, 6);
            factory.CreateLogicInterfaceTemplate(OutputClosedCount, "OPERATIONS.WINDOW_CENTRAL.OUTPUT.CLOSED_COUNT.NAME", "OPERATIONS.WINDOW_CENTRAL.OUTPUT.CLOSED_COUNT.DESCRIPTION", "closedCount", LogicGuid, LogicInterfaceDirection.Output, 0, 7);
            factory.CreateParameterLogicInterfaceTemplate(ParamSlightlyOpen, "OPERATIONS.WINDOW_CENTRAL.PARAM.SLIGHTLY_OPEN.NAME", "OPERATIONS.WINDOW_CENTRAL.PARAM.SLIGHTLY_OPEN.DESCRIPTION", "slightlyOpenPosition", LogicGuid, 1, RuleInterfaceParameterDataType.Integer, 10, false);
        }

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new WindowCentralLogic(context);
        }
    }
}
