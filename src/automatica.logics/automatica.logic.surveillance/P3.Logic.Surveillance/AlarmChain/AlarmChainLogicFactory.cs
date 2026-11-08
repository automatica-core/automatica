using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using P3.Logic.Surveillance.Security;
using LogicInterfaceDirection = Automatica.Core.Base.Templates.LogicInterfaceDirection;

namespace P3.Logic.Surveillance.AlarmChain
{
    /// <summary>
    /// Alarm chain (Loxone "Alarm Chain"): up to 10 escalation stages with text outputs, a reaction time per stage,
    /// repetitions and finally the emergency services output.
    /// </summary>
    public class AlarmChainLogicFactory : LogicFactory
    {
        public const int MaxStages = 10;

        public static readonly Guid InAlarm = new Guid("0759afdf-8bf1-491b-a740-fd9cf1651af3");
        public static readonly Guid InUrgent = new Guid("eac5eca6-a4b1-41e4-afb7-ddfb8aa957c5");
        public static readonly Guid InEmergency = new Guid("1904698b-8008-44ee-816c-bdb2422f2164");
        public static readonly Guid InText1 = new Guid("e718f1ad-835b-40bb-a4b9-b71031675710");
        public static readonly Guid InText2 = new Guid("68beea72-54e1-4d42-a714-b64bb9be2e5d");
        public static readonly Guid InText3 = new Guid("ece4df78-681f-4c1c-990b-4e351b0a90a1");
        public static readonly Guid InConfirm = new Guid("4b6ebecb-74a1-4ee1-8630-672c377543a1");
        public static readonly Guid InOff = new Guid("a1af3a81-e3d1-440e-851b-f016c00a3310");

        public static readonly Guid[] OutStages =
        {
            new Guid("65082b2b-fa7f-4f97-ae97-2c8306ed644c"), new Guid("665a2eaa-e5a1-4ec4-a928-7acb42959cff"), new Guid("02d764c1-0d36-4679-aec0-d2a9493d7867"), new Guid("1527fe94-9228-4c16-b5c7-fa7d1cadcbfd"), new Guid("64c930df-2433-4ce6-8f9b-d554c3c16c6c"),
            new Guid("82fc60b4-3d19-4093-8f89-c5419d4579b5"), new Guid("f43e04a3-b4bd-436f-ac2d-ce1b244ddad0"), new Guid("26ffe3d3-bf7d-4b7b-9638-801e3aa05bb0"), new Guid("21f1c684-f488-4fee-a16c-e74891d610ac"), new Guid("7478f5e1-7af3-4d5f-ba04-55b7b6bdd405")
        };
        public static readonly Guid OutEmergency = new Guid("2ace4996-1667-4cb4-a7c8-1a53ce5a299f");
        public static readonly Guid OutStage = new Guid("abc55cce-e82f-4ac5-ae58-80aa062a97b4");
        public static readonly Guid OutStartTime = new Guid("6c6975fd-a7b6-4d48-8576-f7816df5bf36");
        public static readonly Guid OutStopTime = new Guid("58c05caf-72f4-4076-af49-55097a4fef29");
        public static readonly Guid OutConfirmCause = new Guid("6c5ba3b2-be1f-4d58-ab01-31eb64ad6478");
        public static readonly Guid OutConfirmTime = new Guid("99c58ec6-c00e-44c6-bdfa-691b551e9c8c");

        public static readonly Guid ParamReactionTime = new Guid("fd31ac36-7929-4853-ab29-b693ec58c632");
        public static readonly Guid ParamMaxRepetitions = new Guid("8d230cf7-4a56-4a44-bc7f-2252e313d42c");
        public static readonly Guid ParamStages = new Guid("3ab92136-fb68-4625-9986-19b548100995");
        public static readonly Guid ParamAlarmText = new Guid("8ebb29a4-3832-4f34-a45e-146fba6182a7");
        public static readonly Guid ParamEmergencyText = new Guid("9e171eae-cb3c-45a5-b2a3-b997888fc8ea");
        public static readonly Guid ParamCustomerName = new Guid("2abd263b-8251-490f-b270-a0e2cbe178f9");
        public static readonly Guid ParamCustomerAddress = new Guid("3f4d8a43-a1ba-4d34-8e0d-2660ef52e2b3");

        public override string LogicName => "Surveillance.AlarmChain";
        public override Guid LogicGuid => new Guid("ce8417fe-4f3d-4a76-8e1c-4239c8c6c8c1");
        public override Version LogicVersion => new Version(1, 0, 0, 0);

        private const string P = "SURVEILLANCE.ALARM_CHAIN.";

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, $"{P}NAME", $"{P}DESCRIPTION", "surveillance.alarm_chain", "SURVEILLANCE.NAME", 100, 100);

            var sort = 1;
            void Input(Guid id, string name, string key) =>
                factory.CreateLogicInterfaceTemplate(id, name, P + key + ".DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, int.MaxValue, sort++);

            Input(InAlarm, "A", "ALARM");
            Input(InUrgent, "Au", "URGENT");
            Input(InEmergency, "AEs", "EMERGENCY_IN");
            Input(InText1, "T1", "TEXT");
            Input(InText2, "T2", "TEXT");
            Input(InText3, "T3", "TEXT");
            Input(InConfirm, "Ca", "CONFIRM");
            Input(InOff, "Off", "OFF");

            sort = 1;
            void Output(Guid id, string name, string key) =>
                factory.CreateLogicInterfaceTemplate(id, name, P + key + ".DESCRIPTION", LogicGuid, LogicInterfaceDirection.Output, 0, sort++, RuleInterfaceType.Status);

            for (var i = 0; i < MaxStages; i++)
            {
                Output(OutStages[i], $"A{i + 1}", "STAGE_OUT");
            }
            Output(OutEmergency, "AEs", "EMERGENCY_OUT");
            Output(OutStage, "As", "STAGE");
            Output(OutStartTime, "Ton", "START_TIME");
            Output(OutStopTime, "Toff", "STOP_TIME");
            Output(OutConfirmCause, "Cc", "CONFIRM_CAUSE");
            Output(OutConfirmTime, "Tc", "CONFIRM_TIME");

            sort = 1;
            void Param(Guid id, string key, RuleInterfaceParameterDataType type, object value) =>
                factory.CreateParameterLogicInterfaceTemplate(id, P + key + ".NAME", P + key + ".DESCRIPTION", LogicGuid, sort++, type, value);

            Param(ParamReactionTime, "REACTION_TIME", RuleInterfaceParameterDataType.Integer, 60);
            Param(ParamMaxRepetitions, "MAX_REPETITIONS", RuleInterfaceParameterDataType.Integer, 4);
            Param(ParamStages, "STAGES", RuleInterfaceParameterDataType.Integer, 3);
            Param(ParamAlarmText, "ALARM_TEXT", RuleInterfaceParameterDataType.Text, AlarmChainLogic.DefaultAlarmText);
            Param(ParamEmergencyText, "EMERGENCY_TEXT", RuleInterfaceParameterDataType.Text, AlarmChainLogic.DefaultEmergencyText);
            Param(ParamCustomerName, "CUSTOMER_NAME", RuleInterfaceParameterDataType.Text, "");
            Param(ParamCustomerAddress, "CUSTOMER_ADDRESS", RuleInterfaceParameterDataType.Text, "");

            factory.ChangeDefaultVisuTemplate(LogicGuid, VisuMobileObjectTemplateTypes.Security);
        }

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new AlarmChainLogic(context, FileSecurityStateStore.Default);
        }
    }
}
