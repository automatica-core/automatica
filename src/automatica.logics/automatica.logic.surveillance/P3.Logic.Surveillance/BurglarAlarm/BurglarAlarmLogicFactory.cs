using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using P3.Logic.Surveillance.Security;
using LogicInterfaceDirection = Automatica.Core.Base.Templates.LogicInterfaceDirection;

namespace P3.Logic.Surveillance.BurglarAlarm
{
    /// <summary>
    /// Burglar alarm (Loxone "Burglar Alarm"): armed with or without presence sensors, immediately or delayed, six alarm
    /// levels staggered in time, acknowledge, maximum alarm duration, test mode.
    /// </summary>
    public class BurglarAlarmLogicFactory : LogicFactory
    {
        // sensors
        public static readonly Guid InPresence = new Guid("134d1aa5-19e5-4039-9ed9-0a93b2439f47");
        public static readonly Guid InGlassBreakage = new Guid("148c0f04-0438-463e-bd94-524ebdf1f019");
        public static readonly Guid InWindow = new Guid("602b8224-942b-49a0-8381-cfa8a5e0bf32");
        public static readonly Guid InDoor = new Guid("c889f410-9bee-4b4e-9db4-a9ec1411c6df");
        public static readonly Guid InOther = new Guid("ca696b75-9a6d-4344-b4d1-6c6abf0f2fe2");
        // commands
        public static readonly Guid InToggle = new Guid("3028fb8d-b9f4-4bdd-a8d2-dadee7ce5a14");
        public static readonly Guid InToggleNoPresence = new Guid("56a0331b-127b-4f5b-b8cd-55453fc6ac61");
        public static readonly Guid InArm = new Guid("127c25c7-24fe-4e5b-8f32-5d9bfd1646c9");
        public static readonly Guid InArmNoPresence = new Guid("291a1035-4f53-4ddf-a3d9-4038d10a4d84");
        public static readonly Guid InArmDelayed = new Guid("4e932807-aca7-4060-aee1-574e8455ad7c");
        public static readonly Guid InArmDelayedNoPresence = new Guid("4d2ccb6b-3289-420d-9d91-1d18a6f78be1");
        public static readonly Guid InOff = new Guid("bc6d97e6-f46e-4a29-808c-64aa0141fcf5");
        public static readonly Guid InConfirm = new Guid("d8ae55e8-e7d4-4c94-8135-3ed91d99c32c");
        public static readonly Guid InDisablePeriphery = new Guid("9e2fe545-eae9-4cb7-8910-3583757f6fd9");

        public static readonly Guid OutStatus = new Guid("ee3b8af4-34ce-4a0a-8e44-b4cb47025df6");
        public static readonly Guid OutAlarm = new Guid("08cb447a-90c0-4ef1-92aa-e3bf0e2c6f39");
        public static readonly Guid OutSilent = new Guid("e5db79f0-cd83-4605-8be5-8ae0bdea335a");
        public static readonly Guid OutAudible = new Guid("b09fd8d0-d837-4f80-bdb6-422e02b568cb");
        public static readonly Guid OutVisual = new Guid("93b5a54e-4ff3-4d3c-900b-3312f57e363b");
        public static readonly Guid OutInternal = new Guid("737c5474-7e09-4d6f-ae57-6d7e21af8e51");
        public static readonly Guid OutExternal = new Guid("a7d85115-9e36-4d63-bef9-7d7c9748585b");
        public static readonly Guid OutRemote = new Guid("ad07bb52-72fb-4350-81f4-54e12a6b95b4");
        public static readonly Guid OutActiveSensors = new Guid("fefb6118-0761-4c3d-a343-e500a953da52");
        public static readonly Guid OutTest = new Guid("3790b947-14f2-4a00-9f3c-06243b231217");
        public static readonly Guid OutArmingRemaining = new Guid("3428a5dd-f34d-4efd-ad77-baf4bdc9e685");
        public static readonly Guid OutCause = new Guid("d1663dda-3717-4f02-9b16-dc05ef38790a");
        public static readonly Guid OutTime = new Guid("cecee06a-cc18-45e0-a757-24731245f7ca");
        public static readonly Guid OutOpenContacts = new Guid("ce5bb524-182e-4b21-a2c4-606b07a5a445");
        public static readonly Guid OutOpenContactsText = new Guid("15035107-d1fe-47f1-8321-31f32e9be946");

        public static readonly Guid ParamArmingDelay = new Guid("ca8c4473-cb82-4f29-9af6-266e0a4593fb");
        public static readonly Guid ParamSilentDelay = new Guid("e8ca9a05-4b43-4069-8425-f0bbb313b327");
        public static readonly Guid ParamAudibleDelay = new Guid("4e5d19cf-f64a-444c-a54c-731897a5f820");
        public static readonly Guid ParamVisualDelay = new Guid("8a1d0ad3-679d-4414-b221-14b02e8a4da4");
        public static readonly Guid ParamInternalDelay = new Guid("11177c79-e6ed-4915-862b-f4328f5df160");
        public static readonly Guid ParamExternalDelay = new Guid("6f136ef8-e371-4e2f-bff7-a22e20782037");
        public static readonly Guid ParamRemoteDelay = new Guid("7fbe67c1-d84f-4a2e-8b66-ba3dadb52c75");
        public static readonly Guid ParamPulseExtension = new Guid("14211af1-8c6d-455a-aa64-1b308c170353");
        public static readonly Guid ParamSecondPresence = new Guid("40443e6f-b654-4bc2-8d5f-6674732fe147");
        public static readonly Guid ParamTestMode = new Guid("bd91a9d2-5a02-469d-9b78-a16088ba6193");
        public static readonly Guid ParamMaxAlarm = new Guid("e47694ba-57cd-4d4f-bf0f-711fcc841978");
        public static readonly Guid ParamSilentConfirm = new Guid("38faa33a-3a20-4d66-b00b-147bf48f42b6");
        public static readonly Guid ParamArmOpenContacts = new Guid("498fcfeb-8080-4272-bdbc-fa09395d21f5");
        public static readonly Guid ParamStartDelay = new Guid("6f62d0b5-b263-479b-a116-e7efb515da76");
        public static readonly Guid ParamLights = new Guid("c8a78def-49eb-445c-baef-3a8c7f1860e5");
        public static readonly Guid ParamBlinds = new Guid("e28e7199-ba81-4482-aa3d-6019d5fd1bc5");

        public override string LogicName => "Surveillance.BurglarAlarm";
        public override Guid LogicGuid => new Guid("1d9af4b3-5ae4-4da4-befa-f4eaf33c0e89");
        public override Version LogicVersion => new Version(1, 0, 0, 0);

        private const string P = "SURVEILLANCE.BURGLAR_ALARM.";

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, $"{P}NAME", $"{P}DESCRIPTION", "surveillance.burglar_alarm", "SURVEILLANCE.NAME", 100, 100);

            var sort = 1;
            void Input(Guid id, string name, string key) =>
                factory.CreateLogicInterfaceTemplate(id, name, P + key + ".DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, int.MaxValue, sort++);

            Input(InPresence, "P", "PRESENCE");
            Input(InGlassBreakage, "Gb", "GLASS");
            Input(InWindow, "Wc", "WINDOW");
            Input(InDoor, "Dc", "DOOR");
            Input(InOther, "Ot", "OTHER");
            Input(InToggle, "Tg", "TOGGLE");
            Input(InToggleNoPresence, "Tgnp", "TOGGLE_NP");
            Input(InArm, "A", "ARM");
            Input(InArmNoPresence, "Anp", "ARM_NP");
            Input(InArmDelayed, "Ad", "ARM_DELAYED");
            Input(InArmDelayedNoPresence, "Adnp", "ARM_DELAYED_NP");
            Input(InOff, "Off", "OFF");
            Input(InConfirm, "Ca", "CONFIRM");
            Input(InDisablePeriphery, "DisPc", "DISABLE_PERIPHERY");

            sort = 1;
            void Output(Guid id, string name, string key) =>
                factory.CreateLogicInterfaceTemplate(id, name, P + key + ".DESCRIPTION", LogicGuid, LogicInterfaceDirection.Output, 0, sort++, RuleInterfaceType.Status);

            Output(OutStatus, "S", "STATUS");
            Output(OutAlarm, "Al", "ALARM");
            Output(OutSilent, "Sa", "SILENT");
            Output(OutAudible, "Aa", "AUDIBLE");
            Output(OutVisual, "Va", "VISUAL");
            Output(OutInternal, "Ia", "INTERNAL");
            Output(OutExternal, "Ea", "EXTERNAL");
            Output(OutRemote, "Ra", "REMOTE");
            Output(OutActiveSensors, "N", "ACTIVE_SENSORS");
            Output(OutTest, "At", "TEST");
            Output(OutArmingRemaining, "Rtad", "ARMING_REMAINING");
            Output(OutCause, "Coa", "CAUSE");
            Output(OutTime, "Ta", "TIME");
            Output(OutOpenContacts, "WDs", "OPEN_CONTACTS");
            Output(OutOpenContactsText, "WDot", "OPEN_CONTACTS_TEXT");

            sort = 1;
            void Param(Guid id, string key, RuleInterfaceParameterDataType type, object value) =>
                factory.CreateParameterLogicInterfaceTemplate(id, P + key + ".NAME", P + key + ".DESCRIPTION", LogicGuid, sort++, type, value);

            Param(ParamArmingDelay, "ARMING_DELAY", RuleInterfaceParameterDataType.Integer, BurglarAlarmLogic.DefaultArmingDelay);
            Param(ParamSilentDelay, "SILENT_DELAY", RuleInterfaceParameterDataType.Integer, 0);
            Param(ParamAudibleDelay, "AUDIBLE_DELAY", RuleInterfaceParameterDataType.Integer, 20);
            Param(ParamVisualDelay, "VISUAL_DELAY", RuleInterfaceParameterDataType.Integer, 40);
            Param(ParamInternalDelay, "INTERNAL_DELAY", RuleInterfaceParameterDataType.Integer, 90);
            Param(ParamExternalDelay, "EXTERNAL_DELAY", RuleInterfaceParameterDataType.Integer, 150);
            Param(ParamRemoteDelay, "REMOTE_DELAY", RuleInterfaceParameterDataType.Integer, 300);
            Param(ParamMaxAlarm, "MAX_ALARM", RuleInterfaceParameterDataType.Integer, 900);
            Param(ParamSilentConfirm, "SILENT_CONFIRM", RuleInterfaceParameterDataType.Bool, false);
            Param(ParamPulseExtension, "PULSE_EXTENSION", RuleInterfaceParameterDataType.Integer, 0);
            Param(ParamSecondPresence, "SECOND_PRESENCE", RuleInterfaceParameterDataType.Integer, 900);
            Param(ParamArmOpenContacts, "ARM_OPEN_CONTACTS", RuleInterfaceParameterDataType.Bool, false);
            Param(ParamTestMode, "TEST_MODE", RuleInterfaceParameterDataType.Bool, false);
            Param(ParamStartDelay, "START_DELAY", RuleInterfaceParameterDataType.Integer, 10);
            Param(ParamLights, "LIGHTS", RuleInterfaceParameterDataType.Controls, null);
            Param(ParamBlinds, "BLINDS", RuleInterfaceParameterDataType.Controls, null);

            factory.ChangeDefaultVisuTemplate(LogicGuid, VisuMobileObjectTemplateTypes.Security);
        }

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new BurglarAlarmLogic(context, FileSecurityStateStore.Default);
        }
    }
}
