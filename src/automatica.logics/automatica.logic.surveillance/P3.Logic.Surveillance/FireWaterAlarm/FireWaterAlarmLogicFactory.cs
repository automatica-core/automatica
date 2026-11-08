using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using P3.Logic.Surveillance.Security;
using LogicInterfaceDirection = Automatica.Core.Base.Templates.LogicInterfaceDirection;

namespace P3.Logic.Surveillance.FireWaterAlarm
{
    /// <summary>
    /// Fire and water alarm (Loxone "Fire and Water Alarm"): smoke, water, arc fault and temperature sensors, a pre-alarm and
    /// a main alarm after a delay, silencing the signals, acknowledge, service (test) mode, reactions on lights and blinds.
    /// </summary>
    public class FireWaterAlarmLogicFactory : LogicFactory
    {
        public static readonly Guid InConfirm = new Guid("2acc204d-f901-45ac-a49c-6b7aae3d1a0a");
        public static readonly Guid InConfirmSignals = new Guid("76c49831-bcfb-4dbd-a1c1-4cef7e19d459");
        public static readonly Guid InSmoke = new Guid("f5d55b8f-130f-457d-a36a-169df3877b18");
        public static readonly Guid InWater = new Guid("3eb60efd-118e-4ec5-a0e3-4680db5b0bad");
        public static readonly Guid InArcFault = new Guid("68072851-457a-47c7-8cd9-c79adae642cf");
        public static readonly Guid InTemperature = new Guid("f1920ee8-87a4-4785-9361-76e99f4a267c");
        public static readonly Guid InOff = new Guid("d6213ad7-3117-41ff-8fb9-b23287b8e01d");

        public static readonly Guid OutPreAlarm = new Guid("fa33e2d5-a9e8-4ca7-928e-ea8a917c1763");
        public static readonly Guid OutMainAlarm = new Guid("572c6a52-6857-4c3a-85d1-ec0cc89ac1d6");
        public static readonly Guid OutPreAlarmSignal = new Guid("949e1b78-7287-4b9c-893b-4853636891cb");
        public static readonly Guid OutMainAlarmSignal = new Guid("399131e5-4976-48e9-a029-3eeaaca3f9f0");
        public static readonly Guid OutActiveSensors = new Guid("5825ba89-b562-404d-ac8c-8f399624783a");
        public static readonly Guid OutTest = new Guid("6d0f7ecb-4884-409a-8522-0c4da2f06b63");
        public static readonly Guid OutCause = new Guid("72143e49-c416-4f7a-b711-abf269aaa621");
        public static readonly Guid OutTime = new Guid("4ea4f688-6828-44d2-92d9-d0b5d62b9517");

        public static readonly Guid ParamMainDelay = new Guid("cfe31811-ce7d-4976-8c8e-47f8803e71cb");
        public static readonly Guid ParamMaxTemperature = new Guid("d84d7aaf-a3eb-4682-9288-6a85a1f8f485");
        public static readonly Guid ParamMaxAlarm = new Guid("161e094c-b6aa-41a5-ae69-302788f719ea");
        public static readonly Guid ParamPreAlarmConfirm = new Guid("59d04566-4d76-4581-aae9-b3794e2e4062");
        public static readonly Guid ParamServiceMode = new Guid("4db3fc0e-8aa3-47aa-96ef-2f67e4a5904a");
        public static readonly Guid ParamServiceDuration = new Guid("31002799-d9ca-494d-b279-26eb8fd2c020");
        public static readonly Guid ParamLights = new Guid("14f46c2f-1bb3-46dd-8108-7114a0764944");
        public static readonly Guid ParamBlinds = new Guid("e2f1eff9-1be8-4a73-8fde-35d05d989683");

        public override string LogicName => "Surveillance.FireWaterAlarm";
        public override Guid LogicGuid => new Guid("a14e9338-c9f9-435d-b528-5e1bf4bfba24");
        public override Version LogicVersion => new Version(1, 0, 0, 0);

        private const string P = "SURVEILLANCE.FIRE_WATER_ALARM.";

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, $"{P}NAME", $"{P}DESCRIPTION", "surveillance.fire_water_alarm", "SURVEILLANCE.NAME", 100, 100);

            var sort = 1;
            void Input(Guid id, string name, string key) =>
                factory.CreateLogicInterfaceTemplate(id, name, P + key + ".DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, int.MaxValue, sort++);

            Input(InSmoke, "S", "SMOKE");
            Input(InWater, "W", "WATER");
            Input(InArcFault, "F", "ARC_FAULT");
            Input(InTemperature, "T", "TEMPERATURE");
            Input(InConfirm, "Ca", "CONFIRM");
            Input(InConfirmSignals, "Cs", "CONFIRM_SIGNALS");
            Input(InOff, "Off", "OFF");

            sort = 1;
            void Output(Guid id, string name, string key) =>
                factory.CreateLogicInterfaceTemplate(id, name, P + key + ".DESCRIPTION", LogicGuid, LogicInterfaceDirection.Output, 0, sort++, RuleInterfaceType.Status);

            Output(OutPreAlarm, "Pa", "PRE_ALARM");
            Output(OutMainAlarm, "Ma", "MAIN_ALARM");
            Output(OutPreAlarmSignal, "Pas", "PRE_ALARM_SIGNAL");
            Output(OutMainAlarmSignal, "Mas", "MAIN_ALARM_SIGNAL");
            Output(OutActiveSensors, "N", "ACTIVE_SENSORS");
            Output(OutTest, "At", "TEST");
            Output(OutCause, "Coa", "CAUSE");
            Output(OutTime, "Ta", "TIME");

            sort = 1;
            void Param(Guid id, string key, RuleInterfaceParameterDataType type, object value) =>
                factory.CreateParameterLogicInterfaceTemplate(id, P + key + ".NAME", P + key + ".DESCRIPTION", LogicGuid, sort++, type, value);

            Param(ParamMainDelay, "MAIN_DELAY", RuleInterfaceParameterDataType.Integer, 120);
            Param(ParamMaxTemperature, "MAX_TEMPERATURE", RuleInterfaceParameterDataType.Double, 43d);
            Param(ParamMaxAlarm, "MAX_ALARM", RuleInterfaceParameterDataType.Integer, 300);
            Param(ParamPreAlarmConfirm, "PRE_ALARM_CONFIRM", RuleInterfaceParameterDataType.Bool, false);
            Param(ParamServiceMode, "SERVICE_MODE", RuleInterfaceParameterDataType.Integer, 0);
            Param(ParamServiceDuration, "SERVICE_DURATION", RuleInterfaceParameterDataType.Integer, 1800);
            Param(ParamLights, "LIGHTS", RuleInterfaceParameterDataType.Controls, null);
            Param(ParamBlinds, "BLINDS", RuleInterfaceParameterDataType.Controls, null);

            factory.ChangeDefaultVisuTemplate(LogicGuid, VisuMobileObjectTemplateTypes.Security);
        }

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new FireWaterAlarmLogic(context, FileSecurityStateStore.Default);
        }
    }
}
