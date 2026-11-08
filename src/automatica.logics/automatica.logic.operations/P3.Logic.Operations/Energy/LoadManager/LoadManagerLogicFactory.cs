using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using LogicInterfaceDirection = Automatica.Core.Base.Templates.LogicInterfaceDirection;

namespace P3.Logic.Operations.Energy.LoadManager
{
    /// <summary>
    /// Load manager (Loxone "Load Manager"): locks loads by priority while the total power is above max power
    /// </summary>
    public class LoadManagerLogicFactory : LogicFactory
    {
        internal const string I18N = "OPERATIONS.LOAD_MANAGER";

        internal const double DefaultMaxPower = 11;
        internal const double DefaultHysteresis = 0.5;
        internal const double DefaultShedDelay = 2;
        internal const double DefaultReleaseDelay = 60;

        public static readonly Guid InputPower = new("d3a7c915-2e6b-4f80-8c1d-94b5e0f7a201");

        public static readonly Guid OutputOverload = new("d3a7c915-2e6b-4f80-8c1d-94b5e0f7a211");
        public static readonly Guid OutputReserve = new("d3a7c915-2e6b-4f80-8c1d-94b5e0f7a212");

        public static readonly Guid ParamMaxPower = new("d3a7c915-2e6b-4f80-8c1d-94b5e0f7a221");
        public static readonly Guid ParamHysteresis = new("d3a7c915-2e6b-4f80-8c1d-94b5e0f7a222");
        public static readonly Guid ParamShedDelay = new("d3a7c915-2e6b-4f80-8c1d-94b5e0f7a223");
        public static readonly Guid ParamReleaseDelay = new("d3a7c915-2e6b-4f80-8c1d-94b5e0f7a224");

        private const string LoadBase = "8b2e61f04c9d4a73b5e8d0c17f3a";

        internal const int FieldStatus = 1;
        internal const int FieldLock = 2;
        internal const int FieldPriority = 3;
        internal const int FieldPower = 4;

        /// <summary>
        /// Interface id of a load field, load counts from 1
        /// </summary>
        public static Guid LoadInterface(int load, int field) => EnergyConvert.LoadGuid(LoadBase, load, field);

        public override string LogicName => "P3.Logic.Operations.LoadManager";
        public override Guid LogicGuid => new("a41f7e2c-6d93-4b58-9e07-c2d815b4f6e9");
        public override Version LogicVersion => new(1, 0, 0, 0);

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, $"{I18N}.NAME", $"{I18N}.DESCRIPTION", "operations.load_manager", EnergyManager.EnergyManagerLogicFactory.Group, 100, 100);

            var sort = 0;
            factory.CreateLogicInterfaceTemplate(InputPower, "P", $"{I18N}.INPUT.POWER.DESCRIPTION", "power", LogicGuid, LogicInterfaceDirection.Input, 1, ++sort);
            for (var load = 1; load <= LoadManagerEngine.LoadCount; load++)
            {
                factory.CreateLogicInterfaceTemplate(LoadInterface(load, FieldStatus), $"S{load}", $"{I18N}.INPUT.STATUS.DESCRIPTION", $"status{load}", LogicGuid,
                    LogicInterfaceDirection.Input, 1, ++sort);
            }

            sort = 0;
            factory.CreateLogicInterfaceTemplate(OutputOverload, "Ovl", $"{I18N}.OUTPUT.OVERLOAD.DESCRIPTION", "overload", LogicGuid, LogicInterfaceDirection.Output, 0, ++sort);
            factory.CreateLogicInterfaceTemplate(OutputReserve, "Pr", $"{I18N}.OUTPUT.RESERVE.DESCRIPTION", "reserve", LogicGuid, LogicInterfaceDirection.Output, 0, ++sort);
            for (var load = 1; load <= LoadManagerEngine.LoadCount; load++)
            {
                factory.CreateLogicInterfaceTemplate(LoadInterface(load, FieldLock), $"Lk{load}", $"{I18N}.OUTPUT.LOCK.DESCRIPTION", $"lock{load}", LogicGuid,
                    LogicInterfaceDirection.Output, 0, ++sort);
            }

            sort = 0;
            // max power can be linked, e.g. to a tariff or a time program
            factory.CreateParameterLogicInterfaceTemplate(ParamMaxPower, $"{I18N}.MAX_POWER.NAME", $"{I18N}.MAX_POWER.DESCRIPTION", "maxPower", LogicGuid, ++sort,
                RuleInterfaceParameterDataType.Double, DefaultMaxPower, true);
            Param(factory, ParamHysteresis, "HYSTERESIS", "hysteresis", ++sort, DefaultHysteresis);
            Param(factory, ParamShedDelay, "SHED_DELAY", "shedDelay", ++sort, DefaultShedDelay);
            Param(factory, ParamReleaseDelay, "RELEASE_DELAY", "releaseDelay", ++sort, DefaultReleaseDelay);

            for (var load = 1; load <= LoadManagerEngine.LoadCount; load++)
            {
                factory.CreateParameterLogicInterfaceTemplate(LoadInterface(load, FieldPriority), $"{I18N}.LOAD_{load}.PRIORITY.NAME", $"{I18N}.LOAD.PRIORITY.DESCRIPTION",
                    $"priority{load}", LogicGuid, ++sort, RuleInterfaceParameterDataType.Integer, load);
                factory.CreateParameterLogicInterfaceTemplate(LoadInterface(load, FieldPower), $"{I18N}.LOAD_{load}.POWER.NAME", $"{I18N}.LOAD.POWER.DESCRIPTION",
                    $"power{load}", LogicGuid, ++sort, RuleInterfaceParameterDataType.Double, 0d);
            }
        }

        private void Param(ILogicTemplateFactory factory, Guid id, string i18n, string key, int sort, double defaultValue)
        {
            factory.CreateParameterLogicInterfaceTemplate(id, $"{I18N}.{i18n}.NAME", $"{I18N}.{i18n}.DESCRIPTION", key, LogicGuid, sort, RuleInterfaceParameterDataType.Double, defaultValue);
        }

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new LoadManagerRule(context);
        }
    }
}
