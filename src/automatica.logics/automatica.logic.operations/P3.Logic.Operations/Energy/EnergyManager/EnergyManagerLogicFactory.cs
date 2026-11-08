using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using LogicInterfaceDirection = Automatica.Core.Base.Templates.LogicInterfaceDirection;

namespace P3.Logic.Operations.Energy.EnergyManager
{
    /// <summary>
    /// Energy manager (Loxone "Energy Manager"): switches up to 12 loads on and off by priority to use the PV surplus
    /// </summary>
    public class EnergyManagerLogicFactory : LogicFactory
    {
        internal const string I18N = "OPERATIONS.ENERGY_MANAGER";
        internal const string Group = "OPERATIONS.ENERGY.NAME";

        internal const double DefaultReserve = 0.1;
        internal const double DefaultHysteresis = 0.2;
        internal const double DefaultOnDelay = 60;
        internal const double DefaultOffDelay = 30;
        internal const double DefaultModulationInterval = 10;
        internal const double DefaultMinOn = 60;
        internal const double DefaultMinOff = 60;

        public static readonly Guid InputGrid = new("0a6b3f62-7c41-4d0e-9b8a-2f1c5d7e3a01");
        public static readonly Guid InputSurplus = new("0a6b3f62-7c41-4d0e-9b8a-2f1c5d7e3a02");
        public static readonly Guid InputBattery = new("0a6b3f62-7c41-4d0e-9b8a-2f1c5d7e3a03");
        public static readonly Guid InputSoc = new("0a6b3f62-7c41-4d0e-9b8a-2f1c5d7e3a04");
        public static readonly Guid InputEnable = new("0a6b3f62-7c41-4d0e-9b8a-2f1c5d7e3a05");

        public static readonly Guid OutputAvailable = new("0a6b3f62-7c41-4d0e-9b8a-2f1c5d7e3a11");
        public static readonly Guid OutputAllocated = new("0a6b3f62-7c41-4d0e-9b8a-2f1c5d7e3a12");

        public static readonly Guid ParamInvertGrid = new("0a6b3f62-7c41-4d0e-9b8a-2f1c5d7e3a21");
        public static readonly Guid ParamReserve = new("0a6b3f62-7c41-4d0e-9b8a-2f1c5d7e3a22");
        public static readonly Guid ParamHysteresis = new("0a6b3f62-7c41-4d0e-9b8a-2f1c5d7e3a23");
        public static readonly Guid ParamOnDelay = new("0a6b3f62-7c41-4d0e-9b8a-2f1c5d7e3a24");
        public static readonly Guid ParamOffDelay = new("0a6b3f62-7c41-4d0e-9b8a-2f1c5d7e3a25");
        public static readonly Guid ParamModulationInterval = new("0a6b3f62-7c41-4d0e-9b8a-2f1c5d7e3a26");
        public static readonly Guid ParamMinSoc = new("0a6b3f62-7c41-4d0e-9b8a-2f1c5d7e3a27");

        private const string LoadBase = "6f1d2c8e4b7a4e9d8c3f5a1b2e7d";

        internal const int FieldManual = 1;
        internal const int FieldLock = 2;
        internal const int FieldSwitch = 3;
        internal const int FieldSetpoint = 4;
        internal const int FieldPriority = 5;
        internal const int FieldOnPower = 6;
        internal const int FieldMaxPower = 7;
        internal const int FieldType = 8;
        internal const int FieldMinOn = 9;
        internal const int FieldMinOff = 10;

        /// <summary>
        /// Interface id of a load field, load counts from 1
        /// </summary>
        public static Guid LoadInterface(int load, int field) => EnergyConvert.LoadGuid(LoadBase, load, field);

        public override string LogicName => "P3.Logic.Operations.EnergyManager";
        public override Guid LogicGuid => new("5b8e2d41-93c7-4f6a-a0d5-7e1b4c9f2a63");
        public override Version LogicVersion => new(1, 0, 0, 0);

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, $"{I18N}.NAME", $"{I18N}.DESCRIPTION", "operations.energy_manager", Group, 100, 100);

            var sort = 0;
            Input(factory, InputGrid, "Pgrid", "GRID", "grid", ++sort);
            Input(factory, InputSurplus, "Ps", "SURPLUS", "surplus", ++sort);
            Input(factory, InputBattery, "Pbat", "BATTERY", "battery", ++sort);
            Input(factory, InputSoc, "SoC", "SOC", "soc", ++sort);
            Input(factory, InputEnable, "EN", "ENABLE", "enable", ++sort);
            for (var load = 1; load <= EnergyManagerEngine.LoadCount; load++)
            {
                Input(factory, LoadInterface(load, FieldManual), $"M{load}", "LOAD.MANUAL", $"manual{load}", ++sort);
                Input(factory, LoadInterface(load, FieldLock), $"Lk{load}", "LOAD.LOCK", $"lock{load}", ++sort);
            }

            sort = 0;
            Output(factory, OutputAvailable, "Pav", "AVAILABLE", "available", ++sort);
            Output(factory, OutputAllocated, "Pal", "ALLOCATED", "allocated", ++sort);
            for (var load = 1; load <= EnergyManagerEngine.LoadCount; load++)
            {
                Output(factory, LoadInterface(load, FieldSwitch), $"Q{load}", "LOAD.SWITCH", $"switch{load}", ++sort);
                Output(factory, LoadInterface(load, FieldSetpoint), $"P{load}", "LOAD.SETPOINT", $"setpoint{load}", ++sort);
            }

            sort = 0;
            Param(factory, ParamInvertGrid, "INVERT_GRID", "invertGrid", ++sort, RuleInterfaceParameterDataType.Bool, false);
            Param(factory, ParamReserve, "RESERVE", "reserve", ++sort, RuleInterfaceParameterDataType.Double, DefaultReserve);
            Param(factory, ParamHysteresis, "HYSTERESIS", "hysteresis", ++sort, RuleInterfaceParameterDataType.Double, DefaultHysteresis);
            Param(factory, ParamOnDelay, "ON_DELAY", "onDelay", ++sort, RuleInterfaceParameterDataType.Double, DefaultOnDelay);
            Param(factory, ParamOffDelay, "OFF_DELAY", "offDelay", ++sort, RuleInterfaceParameterDataType.Double, DefaultOffDelay);
            Param(factory, ParamModulationInterval, "MODULATION_INTERVAL", "modulationInterval", ++sort, RuleInterfaceParameterDataType.Double, DefaultModulationInterval);
            Param(factory, ParamMinSoc, "MIN_SOC", "minSoc", ++sort, RuleInterfaceParameterDataType.Double, 0d);

            for (var load = 1; load <= EnergyManagerEngine.LoadCount; load++)
            {
                LoadParam(factory, load, FieldPriority, "PRIORITY", "priority", ++sort, RuleInterfaceParameterDataType.Integer, load);
                LoadParam(factory, load, FieldOnPower, "ON_POWER", "onPower", ++sort, RuleInterfaceParameterDataType.Double, 0d);
                LoadParam(factory, load, FieldMaxPower, "MAX_POWER", "maxPower", ++sort, RuleInterfaceParameterDataType.Double, 0d);
                factory.CreateParameterLogicInterfaceTemplate(LoadInterface(load, FieldType), $"{I18N}.LOAD_{load}.TYPE.NAME", $"{I18N}.LOAD.TYPE.DESCRIPTION",
                    $"type{load}", LogicGuid, ++sort, RuleInterfaceParameterDataType.Enum, $"{(int)EnergyLoadType.Digital}", false,
                    PropertyHelper.CreateEnumMetaString(typeof(EnergyLoadType)));
                LoadParam(factory, load, FieldMinOn, "MIN_ON", "minOn", ++sort, RuleInterfaceParameterDataType.Double, DefaultMinOn);
                LoadParam(factory, load, FieldMinOff, "MIN_OFF", "minOff", ++sort, RuleInterfaceParameterDataType.Double, DefaultMinOff);
            }
        }

        private void Input(ILogicTemplateFactory factory, Guid id, string name, string i18n, string key, int sort)
        {
            factory.CreateLogicInterfaceTemplate(id, name, $"{I18N}.INPUT.{i18n}.DESCRIPTION", key, LogicGuid, LogicInterfaceDirection.Input, 1, sort);
        }

        private void Output(ILogicTemplateFactory factory, Guid id, string name, string i18n, string key, int sort)
        {
            factory.CreateLogicInterfaceTemplate(id, name, $"{I18N}.OUTPUT.{i18n}.DESCRIPTION", key, LogicGuid, LogicInterfaceDirection.Output, 0, sort);
        }

        private void Param(ILogicTemplateFactory factory, Guid id, string i18n, string key, int sort, RuleInterfaceParameterDataType type, object defaultValue)
        {
            factory.CreateParameterLogicInterfaceTemplate(id, $"{I18N}.{i18n}.NAME", $"{I18N}.{i18n}.DESCRIPTION", key, LogicGuid, sort, type, defaultValue);
        }

        private void LoadParam(ILogicTemplateFactory factory, int load, int field, string i18n, string key, int sort, RuleInterfaceParameterDataType type, object defaultValue)
        {
            factory.CreateParameterLogicInterfaceTemplate(LoadInterface(load, field), $"{I18N}.LOAD_{load}.{i18n}.NAME", $"{I18N}.LOAD.{i18n}.DESCRIPTION",
                $"{key}{load}", LogicGuid, sort, type, defaultValue);
        }

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new EnergyManagerRule(context);
        }
    }
}
