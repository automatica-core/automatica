using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;

namespace P3.Logic.Time.AlarmClock
{
    /// <summary>
    /// Alarm clock (Loxone "Alarm Clock"): alarm times per weekday and per operating mode, snooze, pre-alarm, editable in the visu
    /// </summary>
    public class AlarmClockLogicFactory : LogicFactory
    {
        internal const double DefaultMaxDuration = 120;
        internal const double DefaultPreAlarm = 180;
        internal const double DefaultSnooze = 300;
        internal const string DefaultSuppressModes = "public-holiday,vacation";

        public static readonly Guid InputConfirm = new Guid("173a9c57-70b3-44cd-9449-7544a5bca189");
        public static readonly Guid InputSnooze = new Guid("a62bda8a-b9ab-4174-938f-351e9e12606d");
        public static readonly Guid InputDisable = new Guid("84edd9fc-d732-42fa-8c01-7eddfa6e5ea3");
        public static readonly Guid InputToggle = new Guid("f32ca51c-45d7-4ada-96a2-9aaea2daba40");
        public static readonly Guid InputSet = new Guid("e12f8c68-1d70-457b-9428-26b43e0fe817");
        public static readonly Guid InputOff = new Guid("6450f5ed-28ef-46f7-b44f-324017bbae14");

        public static readonly Guid OutputAlarmStart = new Guid("bcbe4663-eeea-4284-af6c-5925cbaa0e41");
        public static readonly Guid OutputAlarmEnd = new Guid("e01bc170-de76-4dd6-b0bb-8ec12a79f995");
        public static readonly Guid OutputBuzzer = new Guid("c135d22d-8722-4c9d-9780-dbcf7577fec9");
        public static readonly Guid OutputAlarm = new Guid("266c08af-fc42-4e73-b6ff-1c998a00accc");
        public static readonly Guid OutputPreAlarm = new Guid("50b2e7ba-994b-4533-b3eb-2832e1be5f66");
        public static readonly Guid OutputSnoozeRemaining = new Guid("9a5d18a4-8ccd-422e-a7e9-d3624bf07eed");
        public static readonly Guid OutputDefaultEnabled = new Guid("c4beca55-12d8-472a-9a75-5f021b266ba8");
        public static readonly Guid OutputNextAlarm = new Guid("63e19c1f-a87c-416f-b2f2-bf293d25593e");
        public static readonly Guid OutputEntries = new Guid("86861dbe-0995-4c9d-ad24-6a3682f356c9");

        public static readonly Guid ParamMaxDuration = new Guid("76d73f75-1e9a-4469-9ee6-7a48ad526189");
        public static readonly Guid ParamPreAlarm = new Guid("0b8efe16-f55d-4a19-978b-28cf0cfd9b66");
        public static readonly Guid ParamSnooze = new Guid("a01b42a6-e340-451d-b5ba-92eba897c23a");
        public static readonly Guid ParamConfirmRequired = new Guid("6eab9562-103a-4c96-b349-355b9804a3f7");
        public static readonly Guid ParamAlarms = new Guid("af739c87-a809-417f-8285-0b7a0ccdc48f");
        public static readonly Guid ParamSuppressModes = new Guid("a9389504-8af5-40fb-8c6f-05622f352fde");

        internal const string AlarmsKey = "alarms";

        public override string LogicName => "Time.AlarmClock";
        public override Version LogicVersion => new Version(1, 0, 0, 0);
        public override Guid LogicGuid => new Guid("3b9bcc74-6577-40ed-964b-029979fa0187");

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, "TIME.ALARM_CLOCK.NAME", "TIME.ALARM_CLOCK.DESCRIPTION", "time.alarm_clock", "TIME.NAME", 100, 100);

            factory.CreateLogicInterfaceTemplate(InputConfirm, "Ca", "TIME.ALARM_CLOCK.CONFIRM.DESCRIPTION", "confirm", LogicGuid, LogicInterfaceDirection.Input, 0, 1);
            factory.CreateLogicInterfaceTemplate(InputSnooze, "S", "TIME.ALARM_CLOCK.SNOOZE.DESCRIPTION", "snooze", LogicGuid, LogicInterfaceDirection.Input, 0, 2);
            factory.CreateLogicInterfaceTemplate(InputDisable, "DisA", "TIME.ALARM_CLOCK.DISABLE.DESCRIPTION", "disable", LogicGuid, LogicInterfaceDirection.Input, 0, 3);
            factory.CreateLogicInterfaceTemplate(InputToggle, "Tg", "TIME.ALARM_CLOCK.TOGGLE.DESCRIPTION", "toggle", LogicGuid, LogicInterfaceDirection.Input, 0, 4);
            factory.CreateLogicInterfaceTemplate(InputSet, "Set", "TIME.ALARM_CLOCK.SET.DESCRIPTION", "set", LogicGuid, LogicInterfaceDirection.Input, 0, 5);
            factory.CreateLogicInterfaceTemplate(InputOff, "Off", "TIME.ALARM_CLOCK.OFF.DESCRIPTION", "off", LogicGuid, LogicInterfaceDirection.Input, 0, 6);

            factory.CreateLogicInterfaceTemplate(OutputAlarmStart, "Aon", "TIME.ALARM_CLOCK.ALARM_START.DESCRIPTION", "alarm_start", LogicGuid, LogicInterfaceDirection.Output, 0, 1);
            factory.CreateLogicInterfaceTemplate(OutputAlarmEnd, "Aoff", "TIME.ALARM_CLOCK.ALARM_END.DESCRIPTION", "alarm_end", LogicGuid, LogicInterfaceDirection.Output, 0, 2);
            factory.CreateLogicInterfaceTemplate(OutputBuzzer, "Buzzer", "TIME.ALARM_CLOCK.BUZZER.DESCRIPTION", "buzzer", LogicGuid, LogicInterfaceDirection.Output, 0, 3);
            factory.CreateLogicInterfaceTemplate(OutputAlarm, "A", "TIME.ALARM_CLOCK.ALARM.DESCRIPTION", "alarm", LogicGuid, LogicInterfaceDirection.Output, 0, 4, RuleInterfaceType.Status);
            factory.CreateLogicInterfaceTemplate(OutputPreAlarm, "Pa", "TIME.ALARM_CLOCK.PRE_ALARM.DESCRIPTION", "pre_alarm", LogicGuid, LogicInterfaceDirection.Output, 0, 5);
            factory.CreateLogicInterfaceTemplate(OutputSnoozeRemaining, "Rst", "TIME.ALARM_CLOCK.SNOOZE_REMAINING.DESCRIPTION", "snooze_remaining", LogicGuid, LogicInterfaceDirection.Output, 0, 6);
            factory.CreateLogicInterfaceTemplate(OutputDefaultEnabled, "Da", "TIME.ALARM_CLOCK.DEFAULT_ENABLED.DESCRIPTION", "default_enabled", LogicGuid, LogicInterfaceDirection.Output, 0, 7);
            factory.CreateLogicInterfaceTemplate(OutputNextAlarm, "Tna", "TIME.ALARM_CLOCK.NEXT_ALARM.DESCRIPTION", "next_alarm", LogicGuid, LogicInterfaceDirection.Output, 0, 8);
            factory.CreateLogicInterfaceTemplate(OutputEntries, "E", "TIME.ALARM_CLOCK.ENTRIES.DESCRIPTION", "entries", LogicGuid, LogicInterfaceDirection.Output, 0, 9);

            factory.CreateParameterLogicInterfaceTemplate(ParamMaxDuration, "TIME.ALARM_CLOCK.MAX_DURATION.NAME", "TIME.ALARM_CLOCK.MAX_DURATION.DESCRIPTION", "max_duration", LogicGuid, 1, RuleInterfaceParameterDataType.Double, DefaultMaxDuration, false);
            factory.CreateParameterLogicInterfaceTemplate(ParamPreAlarm, "TIME.ALARM_CLOCK.PRE_ALARM_TIME.NAME", "TIME.ALARM_CLOCK.PRE_ALARM_TIME.DESCRIPTION", "pre_alarm_time", LogicGuid, 2, RuleInterfaceParameterDataType.Double, DefaultPreAlarm, false);
            factory.CreateParameterLogicInterfaceTemplate(ParamSnooze, "TIME.ALARM_CLOCK.SNOOZE_DURATION.NAME", "TIME.ALARM_CLOCK.SNOOZE_DURATION.DESCRIPTION", "snooze_duration", LogicGuid, 3, RuleInterfaceParameterDataType.Double, DefaultSnooze, false);
            factory.CreateParameterLogicInterfaceTemplate(ParamConfirmRequired, "TIME.ALARM_CLOCK.CONFIRM_REQUIRED.NAME", "TIME.ALARM_CLOCK.CONFIRM_REQUIRED.DESCRIPTION", "confirm_required", LogicGuid, 4, RuleInterfaceParameterDataType.Bool, false, false);
            factory.CreateParameterLogicInterfaceTemplate(ParamSuppressModes, "TIME.ALARM_CLOCK.SUPPRESS_MODES.NAME", "TIME.ALARM_CLOCK.SUPPRESS_MODES.DESCRIPTION", "suppress_modes", LogicGuid, 5, RuleInterfaceParameterDataType.Text, DefaultSuppressModes, false);
            factory.CreateParameterLogicInterfaceTemplate(ParamAlarms, "TIME.ALARM_CLOCK.ALARMS.NAME", "TIME.ALARM_CLOCK.ALARMS.DESCRIPTION", AlarmsKey, LogicGuid, 6, RuleInterfaceParameterDataType.Text, AlarmEntries.DefaultJson, false);
        }

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new AlarmClockRule(context);
        }
    }
}
