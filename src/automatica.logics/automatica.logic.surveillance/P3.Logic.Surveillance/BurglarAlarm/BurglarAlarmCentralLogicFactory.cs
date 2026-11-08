using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using P3.Logic.Surveillance.Security;
using LogicInterfaceDirection = Automatica.Core.Base.Templates.LogicInterfaceDirection;

namespace P3.Logic.Surveillance.BurglarAlarm
{
    /// <summary>
    /// Burglar alarm central (Loxone "Burglar Alarm Central"): arms, disarms and acknowledges several burglar alarms at once.
    /// </summary>
    public class BurglarAlarmCentralLogicFactory : LogicFactory
    {
        public static readonly Guid InToggle = new Guid("7b81e5ac-05e6-40ed-98a2-3396e52ba309");
        public static readonly Guid InToggleNoPresence = new Guid("3eac36f9-a567-4c0a-8466-24b94e9df26b");
        public static readonly Guid InArm = new Guid("7171673a-84c4-4d09-88f1-194eef7445a4");
        public static readonly Guid InArmNoPresence = new Guid("6a7a7810-5074-46f9-967c-32b0a06fd901");
        public static readonly Guid InArmDelayed = new Guid("5b3fa193-51cf-46da-b54e-81839741d542");
        public static readonly Guid InArmDelayedNoPresence = new Guid("abbf3350-cf01-418a-abf7-874b094be3a5");
        public static readonly Guid InOff = new Guid("3082c12a-03bd-4359-9e8f-4215608f4c55");
        public static readonly Guid InConfirm = new Guid("e7827ad6-0c2f-487e-af12-d1eee50fabb4");
        public static readonly Guid InDisablePeriphery = new Guid("4ee1ab67-9d27-4dd5-8c89-6ac972b5e998");

        public static readonly Guid OutArmedCount = new Guid("624e10f9-ac85-45d7-bd0e-2ba92c9768e4");
        public static readonly Guid OutAlarm = new Guid("3edad036-5b47-435d-a56d-692b08f2782a");

        public static readonly Guid ParamAlarms = new Guid("acea9149-1ce5-4946-b4aa-5194150bcc43");

        public override string LogicName => "Surveillance.BurglarAlarmCentral";
        public override Guid LogicGuid => new Guid("b1879ee4-f405-4f80-9608-974e5315c041");
        public override Version LogicVersion => new Version(1, 0, 0, 0);

        private const string P = "SURVEILLANCE.BURGLAR_ALARM_CENTRAL.";

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, $"{P}NAME", $"{P}DESCRIPTION", "surveillance.burglar_alarm_central", "SURVEILLANCE.NAME", 100, 100);

            var sort = 1;
            void Input(Guid id, string name, string key) =>
                factory.CreateLogicInterfaceTemplate(id, name, "SURVEILLANCE.BURGLAR_ALARM." + key + ".DESCRIPTION", LogicGuid, LogicInterfaceDirection.Input, int.MaxValue, sort++);

            Input(InToggle, "Tg", "TOGGLE");
            Input(InToggleNoPresence, "Tgnp", "TOGGLE_NP");
            Input(InArm, "A", "ARM");
            Input(InArmNoPresence, "Anp", "ARM_NP");
            Input(InArmDelayed, "Ad", "ARM_DELAYED");
            Input(InArmDelayedNoPresence, "Adnp", "ARM_DELAYED_NP");
            Input(InOff, "Off", "OFF");
            Input(InConfirm, "Ca", "CONFIRM");
            Input(InDisablePeriphery, "DisPc", "DISABLE_PERIPHERY");

            factory.CreateLogicInterfaceTemplate(OutArmedCount, "Na", $"{P}ARMED_COUNT.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Output, 0, 1, RuleInterfaceType.Status);
            factory.CreateLogicInterfaceTemplate(OutAlarm, "Al", $"{P}ALARM.DESCRIPTION", LogicGuid, LogicInterfaceDirection.Output, 0, 2, RuleInterfaceType.Status);

            factory.CreateParameterLogicInterfaceTemplate(ParamAlarms, $"{P}ALARMS.NAME", $"{P}ALARMS.DESCRIPTION", LogicGuid, 1,
                RuleInterfaceParameterDataType.Controls, null);

            factory.ChangeDefaultVisuTemplate(LogicGuid, VisuMobileObjectTemplateTypes.Security);
        }

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new BurglarAlarmCentralLogic(context, FileSecurityStateStore.Default);
        }
    }
}
