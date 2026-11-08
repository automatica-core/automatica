using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;

namespace P3.Logic.Time.RuntimeCounter
{
    public enum RuntimeUnit
    {
        [EnumName("TIME.RUNTIME_COUNTER.UNIT.SECONDS")]
        Seconds = 0,
        [EnumName("TIME.RUNTIME_COUNTER.UNIT.MINUTES")]
        Minutes = 1,
        [EnumName("TIME.RUNTIME_COUNTER.UNIT.HOURS")]
        Hours = 2,
        [EnumName("TIME.RUNTIME_COUNTER.UNIT.DAYS")]
        Days = 3
    }

    /// <summary>
    /// Runtime counter (Loxone "Runtime Counter"): operating hours and a maintenance interval, kept across restarts
    /// </summary>
    public class RuntimeCounterLogicFactory : LogicFactory
    {
        public static readonly Guid InputEnable = new Guid("2c17cef1-a34a-4505-af28-ab862a534109");
        public static readonly Guid InputResetMaintenance = new Guid("4c74bb1e-a085-4a4b-8798-0193a62eec3e");
        public static readonly Guid InputResetTotal = new Guid("00a5b8f8-6840-497d-a3cd-320ff35f48e7");
        public static readonly Guid InputOff = new Guid("95f6e459-1f65-49e8-873e-ad5641edbd8d");

        public static readonly Guid OutputMaintenance = new Guid("3f5dbe58-4c95-4989-af67-9de945bf013d");
        public static readonly Guid OutputTotal = new Guid("cf53b5af-08b8-466b-92c8-7aa314f88171");
        public static readonly Guid OutputLastStart = new Guid("f2c7019e-4fb4-4896-b61b-c290fc701f2f");
        public static readonly Guid OutputRemaining = new Guid("a0ac9b96-4da1-43b9-80d5-e8500236d192");

        public static readonly Guid ParamUnit = new Guid("4e9fe60f-47a6-413f-a1ea-0216b8152f50");
        public static readonly Guid ParamInterval = new Guid("f93dbc73-c90c-4b6f-8199-70a5c5388ff7");

        public override string LogicName => "Time.RuntimeCounter";
        public override Version LogicVersion => new Version(1, 0, 0, 0);
        public override Guid LogicGuid => new Guid("e7a36b1a-946b-4f83-80ef-e1723dbf91ed");

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, "TIME.RUNTIME_COUNTER.NAME", "TIME.RUNTIME_COUNTER.DESCRIPTION", "time.runtime_counter", "TIME.NAME", 100, 100);

            factory.CreateLogicInterfaceTemplate(InputEnable, "En", "TIME.RUNTIME_COUNTER.ENABLE.DESCRIPTION", "enable", LogicGuid, LogicInterfaceDirection.Input, 0, 1);
            factory.CreateLogicInterfaceTemplate(InputResetMaintenance, "Rmc", "TIME.RUNTIME_COUNTER.RESET_MAINTENANCE.DESCRIPTION", "reset_maintenance", LogicGuid, LogicInterfaceDirection.Input, 0, 2);
            factory.CreateLogicInterfaceTemplate(InputResetTotal, "Rto", "TIME.RUNTIME_COUNTER.RESET_TOTAL.DESCRIPTION", "reset_total", LogicGuid, LogicInterfaceDirection.Input, 0, 3);
            factory.CreateLogicInterfaceTemplate(InputOff, "Off", "TIME.RUNTIME_COUNTER.OFF.DESCRIPTION", "off", LogicGuid, LogicInterfaceDirection.Input, 0, 4);

            factory.CreateLogicInterfaceTemplate(OutputMaintenance, "Me", "TIME.RUNTIME_COUNTER.MAINTENANCE.DESCRIPTION", "maintenance", LogicGuid, LogicInterfaceDirection.Output, 0, 1);
            factory.CreateLogicInterfaceTemplate(OutputTotal, "To", "TIME.RUNTIME_COUNTER.TOTAL.DESCRIPTION", "total", LogicGuid, LogicInterfaceDirection.Output, 0, 2, RuleInterfaceType.Status);
            factory.CreateLogicInterfaceTemplate(OutputLastStart, "Lst", "TIME.RUNTIME_COUNTER.LAST_START.DESCRIPTION", "last_start", LogicGuid, LogicInterfaceDirection.Output, 0, 3);
            factory.CreateLogicInterfaceTemplate(OutputRemaining, "Rtm", "TIME.RUNTIME_COUNTER.REMAINING.DESCRIPTION", "remaining", LogicGuid, LogicInterfaceDirection.Output, 0, 4);

            factory.CreateParameterLogicInterfaceTemplate(ParamUnit, "TIME.RUNTIME_COUNTER.UNIT.NAME", "TIME.RUNTIME_COUNTER.UNIT.DESCRIPTION", "unit", LogicGuid, 1,
                RuleInterfaceParameterDataType.Enum, $"{(int)RuntimeUnit.Hours}", false, PropertyHelper.CreateEnumMetaString(typeof(RuntimeUnit)));
            factory.CreateParameterLogicInterfaceTemplate(ParamInterval, "TIME.RUNTIME_COUNTER.INTERVAL.NAME", "TIME.RUNTIME_COUNTER.INTERVAL.DESCRIPTION", "interval", LogicGuid, 2, RuleInterfaceParameterDataType.Double, 0, false);
        }

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new RuntimeCounterRule(context);
        }
    }
}
