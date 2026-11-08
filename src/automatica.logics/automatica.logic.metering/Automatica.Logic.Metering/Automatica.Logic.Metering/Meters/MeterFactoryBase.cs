using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;

namespace Automatica.Logic.Metering.Meters
{
    /// <summary>
    /// Template helpers of the meters. Texts are METERING.{section}.INPUT|OUTPUT|PARAM.{name}.NAME|DESCRIPTION, where the
    /// section is the meter's own (e.g. METER_STORAGE) or COMMON for interfaces all meters share (power, today, ...).
    /// </summary>
    public abstract class MeterFactoryBase : LogicFactory
    {
        public const string Common = "COMMON";

        /// <summary>text section of this meter, e.g. METER_STORAGE</summary>
        protected abstract string Section { get; }

        /// <summary>template key, e.g. metering.meter_storage</summary>
        protected abstract string Key { get; }

        protected void CreateTemplate(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, $"METERING.{Section}.NAME", $"METERING.{Section}.DESCRIPTION", Key, "METERING.NAME", 100, 100);
        }

        protected void Input(ILogicTemplateFactory factory, Guid id, string name, string key, int sort, string section = null)
        {
            var s = section ?? Section;
            factory.CreateLogicInterfaceTemplate(id, $"METERING.{s}.INPUT.{name}.NAME", $"METERING.{s}.INPUT.{name}.DESCRIPTION",
                key + "In", LogicGuid, LogicInterfaceDirection.Input, 0, sort);
        }

        protected void Output(ILogicTemplateFactory factory, Guid id, string name, string key, int sort, string section = null,
            RuleInterfaceType type = RuleInterfaceType.Output)
        {
            var s = section ?? Section;
            factory.CreateLogicInterfaceTemplate(id, $"METERING.{s}.OUTPUT.{name}.NAME", $"METERING.{s}.OUTPUT.{name}.DESCRIPTION",
                key, LogicGuid, LogicInterfaceDirection.Output, 0, sort, type);
        }

        /// <summary>
        /// Always the overload with "linkable": the one without it stores the name as key on current cores
        /// (see EnergyMonitorLogicFactoryTests).
        /// </summary>
        protected void Param(ILogicTemplateFactory factory, Guid id, string name, string key, int sort, RuleInterfaceParameterDataType type,
            object defaultValue, string section = null)
        {
            var s = section ?? Section;
            factory.CreateParameterLogicInterfaceTemplate(id, $"METERING.{s}.PARAM.{name}.NAME", $"METERING.{s}.PARAM.{name}.DESCRIPTION",
                key, LogicGuid, sort, type, defaultValue, false);
        }

        /// <summary>today, this month and total of a register, keys {prefix}Today / Month / Total</summary>
        protected void Counters(ILogicTemplateFactory factory, Guid[] ids, string namePrefix, string keyPrefix, ref int sort, string section = null)
        {
            Output(factory, ids[0], namePrefix + "TODAY", Lower(keyPrefix, "Today"), ++sort, section);
            Output(factory, ids[1], namePrefix + "MONTH", Lower(keyPrefix, "Month"), ++sort, section);
            Output(factory, ids[2], namePrefix + "TOTAL", Lower(keyPrefix, "Total"), ++sort, section);
        }

        private static string Lower(string prefix, string name) => string.IsNullOrEmpty(prefix) ? char.ToLowerInvariant(name[0]) + name[1..] : prefix + name;
    }
}
