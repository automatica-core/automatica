using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using P3.Logic.Surveillance.Security;
using LogicInterfaceDirection = Automatica.Core.Base.Templates.LogicInterfaceDirection;

namespace P3.Logic.Surveillance.EmergencyAlarm
{
    /// <summary>
    /// Emergency alarm (Loxone "Emergency Alarm"): a call for help from buttons (push and hold) or the visualization,
    /// acknowledged by a button or the visualization, with an optional escalation output.
    /// </summary>
    public class EmergencyAlarmLogicFactory : LogicFactory
    {
        public static readonly Guid InToggleHold = new Guid("b95cb321-4ab7-4f0e-8057-b7262251d04b");
        public static readonly Guid InActivate = new Guid("2771dc3a-b11b-4e7e-b490-ffb9f6806bf4");
        public static readonly Guid InConfirm = new Guid("5e5a9615-2850-48a8-ab03-91b9a6fb9dba");
        public static readonly Guid InOff = new Guid("da301303-1a14-4a78-ace1-d73f7e0100f5");

        public static readonly Guid OutAlarm = new Guid("baba8b12-7aca-45a2-8b81-014ef18e3005");
        public static readonly Guid OutAlarmOn = new Guid("1089d008-68ca-4e51-af87-eca116aed4f3");
        public static readonly Guid OutAlarmOff = new Guid("89773efc-9766-4b27-b5fb-23aea1bf6937");
        public static readonly Guid OutCause = new Guid("855f544c-968a-4dc4-88a0-4562bdca43ed");
        public static readonly Guid OutConfirmCause = new Guid("fe0444af-b6e6-431d-abfa-3abeda51a202");
        public static readonly Guid OutEscalated = new Guid("7f9f0c77-3fbe-4f34-8bfe-78e9c333d3d6");

        public static readonly Guid ParamHoldAlarm = new Guid("f3266a41-4b60-442a-9926-de7c74037013");
        public static readonly Guid ParamHoldConfirm = new Guid("bfc410ad-b38f-4c59-86ec-70d9815d61e9");
        public static readonly Guid ParamEscalation = new Guid("6657c185-1c17-46e1-b436-a55551530a19");

        public override string LogicName => "Surveillance.EmergencyAlarm";
        public override Guid LogicGuid => new Guid("5d1ae70a-b429-4d89-a568-db2a827b0372");
        public override Version LogicVersion => new Version(1, 0, 0, 0);

        private const string P = "SURVEILLANCE.EMERGENCY_ALARM.";

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, $"{P}NAME", $"{P}DESCRIPTION", "surveillance.emergency_alarm", "SURVEILLANCE.NAME", 100, 100);

            factory.CreateLogicInterfaceTemplate(InToggleHold, "Tg", $"{P}TOGGLE_HOLD.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, int.MaxValue, 1);
            factory.CreateLogicInterfaceTemplate(InActivate, "A", $"{P}ACTIVATE.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, int.MaxValue, 2);
            factory.CreateLogicInterfaceTemplate(InConfirm, "Ca", $"{P}CONFIRM.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, int.MaxValue, 3);
            factory.CreateLogicInterfaceTemplate(InOff, "Off", $"{P}OFF.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, int.MaxValue, 4);

            factory.CreateLogicInterfaceTemplate(OutAlarm, "A", $"{P}ALARM.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Output, 0, 1, RuleInterfaceType.Status);
            factory.CreateLogicInterfaceTemplate(OutAlarmOn, "Aon", $"{P}ALARM_ON.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Output, 0, 2, RuleInterfaceType.Status);
            factory.CreateLogicInterfaceTemplate(OutAlarmOff, "Aoff", $"{P}ALARM_OFF.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Output, 0, 3, RuleInterfaceType.Status);
            factory.CreateLogicInterfaceTemplate(OutCause, "Coa", $"{P}CAUSE.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Output, 0, 4, RuleInterfaceType.Status);
            factory.CreateLogicInterfaceTemplate(OutConfirmCause, "Cc", $"{P}CONFIRM_CAUSE.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Output, 0, 5, RuleInterfaceType.Status);
            factory.CreateLogicInterfaceTemplate(OutEscalated, "Esc", $"{P}ESCALATED.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Output, 0, 6, RuleInterfaceType.Status);

            factory.CreateParameterLogicInterfaceTemplate(ParamHoldAlarm, $"{P}HOLD_ALARM.NAME", $"{P}HOLD_ALARM.DESCRIPTION", LogicGuid, 1, RuleInterfaceParameterDataType.Integer, 4);
            factory.CreateParameterLogicInterfaceTemplate(ParamHoldConfirm, $"{P}HOLD_CONFIRM.NAME", $"{P}HOLD_CONFIRM.DESCRIPTION", LogicGuid, 2, RuleInterfaceParameterDataType.Integer, 2);
            factory.CreateParameterLogicInterfaceTemplate(ParamEscalation, $"{P}ESCALATION.NAME", $"{P}ESCALATION.DESCRIPTION", LogicGuid, 3, RuleInterfaceParameterDataType.Integer, 0);

            factory.ChangeDefaultVisuTemplate(LogicGuid, VisuMobileObjectTemplateTypes.Security);
        }

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new EmergencyAlarmLogic(context, FileSecurityStateStore.Default);
        }
    }
}
