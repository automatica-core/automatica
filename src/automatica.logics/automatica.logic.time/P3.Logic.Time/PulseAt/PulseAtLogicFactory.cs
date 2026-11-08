using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;

namespace P3.Logic.Time.PulseAt
{
    public enum PulseAtType
    {
        [EnumName("TIME.PULSE_AT.TYPE.TIME")]
        Time = 0,
        [EnumName("TIME.PULSE_AT.TYPE.SUNRISE")]
        Sunrise = 1,
        [EnumName("TIME.PULSE_AT.TYPE.SUNSET")]
        Sunset = 2,
        [EnumName("TIME.PULSE_AT.TYPE.DAWN")]
        Dawn = 3,
        [EnumName("TIME.PULSE_AT.TYPE.DUSK")]
        Dusk = 4,
        [EnumName("TIME.PULSE_AT.TYPE.MINUTE")]
        Minute = 5,
        [EnumName("TIME.PULSE_AT.TYPE.HOUR")]
        Hour = 6,
        [EnumName("TIME.PULSE_AT.TYPE.DAY")]
        Day = 7,
        [EnumName("TIME.PULSE_AT.TYPE.WEEK")]
        Week = 8,
        [EnumName("TIME.PULSE_AT.TYPE.MONTH")]
        Month = 9,
        [EnumName("TIME.PULSE_AT.TYPE.YEAR")]
        Year = 10
    }

    /// <summary>
    /// Pulse at (Loxone "Pulse at"): a pulse at a time of day, at a sun event or every minute/hour/day/week/month/year
    /// </summary>
    public class PulseAtLogicFactory : LogicFactory
    {
        internal const double DefaultDuration = 1;
        internal const string DefaultTime = "07:00:00";

        public static readonly Guid InputOff = new Guid("7f835bd2-1980-4a0a-a4d7-119735edde58");
        public static readonly Guid Output = new Guid("abc67d10-f1a5-402c-a641-63d620fe966c");
        public static readonly Guid OutputNext = new Guid("6cffba8b-ae41-408b-b602-9d7eeb8f3f58");

        public static readonly Guid ParamType = new Guid("ac525139-5811-4a0b-9367-d10fe893a70e");
        public static readonly Guid ParamTime = new Guid("cd2d04aa-955e-4815-820d-60b42a37d28a");
        public static readonly Guid ParamOffset = new Guid("c89a31be-4e7b-4154-bf75-9c436194b51c");
        public static readonly Guid ParamOneTime = new Guid("c486d32f-613d-4f52-8c94-fa854d04932b");
        public static readonly Guid ParamDate = new Guid("69a12dde-eb6b-4f93-bf1f-166a3755d18b");
        public static readonly Guid ParamDuration = new Guid("0db56766-fd8b-44ed-a46d-b6a8f3d205c1");

        internal const string TimeKey = "time";

        public override string LogicName => "Time.PulseAt";
        public override Version LogicVersion => new Version(1, 0, 0, 0);
        public override Guid LogicGuid => new Guid("5a8a909c-6ab2-4e06-9224-f1da282486d7");

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, "TIME.PULSE_AT.NAME", "TIME.PULSE_AT.DESCRIPTION", "time.pulse_at", "TIME.NAME", 100, 100);

            factory.CreateLogicInterfaceTemplate(InputOff, "Off", "TIME.PULSE_AT.OFF.DESCRIPTION", "off", LogicGuid, LogicInterfaceDirection.Input, 0, 1);

            factory.CreateLogicInterfaceTemplate(Output, "O", "TIME.PULSE_AT.OUTPUT.DESCRIPTION", "output", LogicGuid, LogicInterfaceDirection.Output, 0, 1, RuleInterfaceType.Status);
            factory.CreateLogicInterfaceTemplate(OutputNext, "Tn", "TIME.PULSE_AT.NEXT.DESCRIPTION", "next", LogicGuid, LogicInterfaceDirection.Output, 0, 2);

            factory.CreateParameterLogicInterfaceTemplate(ParamType, "TIME.PULSE_AT.TYPE.NAME", "TIME.PULSE_AT.TYPE.DESCRIPTION", "type", LogicGuid, 1,
                RuleInterfaceParameterDataType.Enum, $"{(int)PulseAtType.Time}", false, PropertyHelper.CreateEnumMetaString(typeof(PulseAtType)));
            factory.CreateParameterLogicInterfaceTemplate(ParamTime, "TIME.PULSE_AT.TIME.NAME", "TIME.PULSE_AT.TIME.DESCRIPTION", TimeKey, LogicGuid, 2, RuleInterfaceParameterDataType.Text, DefaultTime, false);
            factory.CreateParameterLogicInterfaceTemplate(ParamOffset, "TIME.PULSE_AT.OFFSET.NAME", "TIME.PULSE_AT.OFFSET.DESCRIPTION", "offset", LogicGuid, 3, RuleInterfaceParameterDataType.Double, 0, false);
            factory.CreateParameterLogicInterfaceTemplate(ParamOneTime, "TIME.PULSE_AT.ONE_TIME.NAME", "TIME.PULSE_AT.ONE_TIME.DESCRIPTION", "one_time", LogicGuid, 4, RuleInterfaceParameterDataType.Bool, false, false);
            factory.CreateParameterLogicInterfaceTemplate(ParamDate, "TIME.PULSE_AT.DATE.NAME", "TIME.PULSE_AT.DATE.DESCRIPTION", "date", LogicGuid, 5, RuleInterfaceParameterDataType.Text, "", false);
            factory.CreateParameterLogicInterfaceTemplate(ParamDuration, "TIME.PULSE_AT.DURATION.NAME", "TIME.PULSE_AT.DURATION.DESCRIPTION", "duration", LogicGuid, 6, RuleInterfaceParameterDataType.Double, DefaultDuration, false);
        }

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new PulseAtRule(context);
        }
    }
}
