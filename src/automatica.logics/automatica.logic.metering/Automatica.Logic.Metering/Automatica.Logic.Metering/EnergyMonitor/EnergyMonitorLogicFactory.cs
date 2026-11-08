using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;

namespace Automatica.Logic.Metering.EnergyMonitor
{
    /// <summary>
    /// Energy monitor (like Loxone's energy flow monitor): grid, PV, battery and up to three extra consumers in,
    /// house consumption, self-consumption, autarky and today's energy out. Brings its own visu tile (energy flow).
    /// </summary>
    public class EnergyMonitorLogicFactory : LogicFactory
    {
        internal const double DefaultPeakPower = 10;

        // inputs (kW, kWh for the meters)
        public static readonly Guid InputGrid = new Guid("3b540e08-ce52-443d-a040-99ff6367cc24");
        public static readonly Guid InputGridImport = new Guid("3e7bf9bc-a0d0-4915-ac78-37e8ff5dd483");
        public static readonly Guid InputGridExport = new Guid("5c8784bf-4b24-4cac-a4b8-4026b3db9acd");
        public static readonly Guid InputPv = new Guid("202039d8-ee0d-49d3-ac8b-6b8110d6ae84");
        public static readonly Guid InputBattery = new Guid("0457147c-2dbb-45fe-8555-dc2b79ea4c8b");
        public static readonly Guid InputSoc = new Guid("45163372-bf08-4cca-accd-32ae9992d929");
        public static readonly Guid[] InputConsumers =
        {
            new Guid("38443425-7feb-4931-a731-bebb737f375b"),
            new Guid("7322bfe1-3f09-4884-95f4-958a4cb35c11"),
            new Guid("9d0c6855-3d20-4522-b36f-6a77b29cd8e1")
        };
        public static readonly Guid InputPvMeter = new Guid("139f01aa-6a80-4108-b4e3-7b6dd5b58368");
        public static readonly Guid InputImportMeter = new Guid("a9002abc-35e1-42ab-84ff-74303b10e055");
        public static readonly Guid InputExportMeter = new Guid("759447cb-d93c-4503-9ace-8f23d5ac66c6");
        public static readonly Guid InputChargeMeter = new Guid("0f7debe2-5cfc-4584-ba99-1a1f1e3ba6db");
        public static readonly Guid InputDischargeMeter = new Guid("6ef54a18-5182-4b66-836b-970d5dcd8026");

        // outputs
        public static readonly Guid OutputGrid = new Guid("3ac4576e-ac4b-45e0-9097-1bfcf9deca2b");
        public static readonly Guid OutputPv = new Guid("3d3ab024-2483-46ef-bccd-22ee51a7e112");
        public static readonly Guid OutputBattery = new Guid("ab0a3a9c-7195-4177-8810-f75737a3c87a");
        public static readonly Guid OutputSoc = new Guid("bf45bee2-1a95-4fdd-8739-88dfbac298b2");
        public static readonly Guid[] OutputConsumers =
        {
            new Guid("01ce23af-f2a7-4589-b8c2-2c4add4f5d40"),
            new Guid("9582934c-fb92-4ee3-90a4-a2f4952c4c79"),
            new Guid("b11212c4-1c5e-4426-87a1-ebf960e43249")
        };
        public static readonly Guid OutputHouse = new Guid("627349df-f31b-4ac1-ba6f-66e5a1472227");
        public static readonly Guid OutputConsumption = new Guid("0e8d3441-de9d-4bec-8012-8d6f312bad50");
        public static readonly Guid OutputSelfConsumption = new Guid("8d85185d-9e84-4d19-8922-9bf31b502700");
        public static readonly Guid OutputAutarky = new Guid("e2caa25e-7687-4fc5-8c6c-c62a1a253a2d");
        public static readonly Guid OutputPvToday = new Guid("87a55e45-54ea-4690-8131-551246f954d1");
        public static readonly Guid OutputImportToday = new Guid("d32b4a01-8e5b-4907-b19b-107dec1cb879");
        public static readonly Guid OutputExportToday = new Guid("9e963a22-a0d9-4df2-9688-23dc0af59c16");
        public static readonly Guid OutputChargeToday = new Guid("49bd374f-d7a1-40f3-b9f3-f9b2aa722200");
        public static readonly Guid OutputDischargeToday = new Guid("9710eb7f-07f7-4b59-970e-26ad0c642c11");
        public static readonly Guid OutputConsumptionToday = new Guid("8a1bbe59-13e4-4a9f-b297-4269d6b1a093");
        public static readonly Guid OutputSelfConsumptionToday = new Guid("b240f5ca-0590-40c5-99dd-a30ece6deb4a");
        public static readonly Guid OutputAutarkyToday = new Guid("43919875-4af2-4345-94e3-838bcf972e35");

        // parameters
        public static readonly Guid ParamInvertGrid = new Guid("932f6f30-5d85-47d3-8df1-67b0041fa1f8");
        public static readonly Guid ParamInvertBattery = new Guid("585e9891-08ce-4d60-b583-c70be64bb804");
        public static readonly Guid ParamPeakPower = new Guid("1e88b300-07a1-4c4e-ab9c-5958909b0d2f");
        public static readonly Guid[] ParamConsumerNames =
        {
            new Guid("c84e1ab6-010f-4dab-8fc2-d8f7646df031"),
            new Guid("56aa2d0a-ea2d-43f9-b821-be4ae8f6eeb1"),
            new Guid("39847236-d148-434f-a1f0-7ae438649253")
        };

        public override string LogicName => "Metering.EnergyMonitor";
        public override Version LogicVersion => new Version(1, 0, 0, 0);
        public override Guid LogicGuid => new Guid("270e43f0-bda0-4e87-a1ad-691dbe745adb");

        /// <summary>
        /// Name of the energy flow visu template in <see cref="VisuMobileObjectTemplateTypes"/>. Looked up by name, so the
        /// plugin also builds against core packages without it; on such a core the logic keeps the default (label) tile.
        /// </summary>
        internal const string EnergyFlowVisuTemplate = "EnergyFlow";

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, "METERING.ENERGY_MONITOR.NAME", "METERING.ENERGY_MONITOR.DESCRIPTION",
                "metering.energy_monitor", "METERING.NAME", 100, 100);

            var sort = 0;
            MonitorInput(factory, InputGrid, "GRID", "grid", ++sort);
            MonitorInput(factory, InputGridImport, "GRID_IMPORT", "gridImport", ++sort);
            MonitorInput(factory, InputGridExport, "GRID_EXPORT", "gridExport", ++sort);
            MonitorInput(factory, InputPv, "PV", "pv", ++sort);
            MonitorInput(factory, InputBattery, "BATTERY", "battery", ++sort);
            MonitorInput(factory, InputSoc, "SOC", "soc", ++sort);
            for (var i = 0; i < InputConsumers.Length; i++)
            {
                MonitorInput(factory, InputConsumers[i], $"CONSUMER{i + 1}", $"consumer{i + 1}", ++sort);
            }
            MonitorInput(factory, InputPvMeter, "PV_METER", "pvMeter", ++sort);
            MonitorInput(factory, InputImportMeter, "IMPORT_METER", "importMeter", ++sort);
            MonitorInput(factory, InputExportMeter, "EXPORT_METER", "exportMeter", ++sort);
            MonitorInput(factory, InputChargeMeter, "CHARGE_METER", "chargeMeter", ++sort);
            MonitorInput(factory, InputDischargeMeter, "DISCHARGE_METER", "dischargeMeter", ++sort);

            sort = 0;
            MonitorOutput(factory, OutputGrid, "GRID", "grid", ++sort);
            MonitorOutput(factory, OutputPv, "PV", "pv", ++sort);
            MonitorOutput(factory, OutputBattery, "BATTERY", "battery", ++sort);
            MonitorOutput(factory, OutputSoc, "SOC", "soc", ++sort);
            for (var i = 0; i < OutputConsumers.Length; i++)
            {
                MonitorOutput(factory, OutputConsumers[i], $"CONSUMER{i + 1}", $"consumer{i + 1}", ++sort);
            }
            // status: the label tile of cores without the energy flow tile shows the house consumption
            MonitorOutput(factory, OutputHouse, "HOUSE", "house", ++sort, RuleInterfaceType.Status);
            MonitorOutput(factory, OutputConsumption, "CONSUMPTION", "consumption", ++sort);
            MonitorOutput(factory, OutputSelfConsumption, "SELF_CONSUMPTION", "selfConsumption", ++sort);
            MonitorOutput(factory, OutputAutarky, "AUTARKY", "autarky", ++sort);
            MonitorOutput(factory, OutputPvToday, "PV_TODAY", "pvToday", ++sort);
            MonitorOutput(factory, OutputImportToday, "IMPORT_TODAY", "importToday", ++sort);
            MonitorOutput(factory, OutputExportToday, "EXPORT_TODAY", "exportToday", ++sort);
            MonitorOutput(factory, OutputChargeToday, "CHARGE_TODAY", "chargeToday", ++sort);
            MonitorOutput(factory, OutputDischargeToday, "DISCHARGE_TODAY", "dischargeToday", ++sort);
            MonitorOutput(factory, OutputConsumptionToday, "CONSUMPTION_TODAY", "consumptionToday", ++sort);
            MonitorOutput(factory, OutputSelfConsumptionToday, "SELF_CONSUMPTION_TODAY", "selfConsumptionToday", ++sort);
            MonitorOutput(factory, OutputAutarkyToday, "AUTARKY_TODAY", "autarkyToday", ++sort);

            sort = 0;
            // the overloads with "linkable": the one without it stores the name as key on current cores, and the tile looks the
            // parameters up by key (peakPower, consumerNName)
            factory.CreateParameterLogicInterfaceTemplate(ParamInvertGrid, "METERING.ENERGY_MONITOR.PARAM.INVERT_GRID.NAME",
                "METERING.ENERGY_MONITOR.PARAM.INVERT_GRID.DESCRIPTION", "invertGrid", LogicGuid, ++sort, RuleInterfaceParameterDataType.Bool, false, false);
            factory.CreateParameterLogicInterfaceTemplate(ParamInvertBattery, "METERING.ENERGY_MONITOR.PARAM.INVERT_BATTERY.NAME",
                "METERING.ENERGY_MONITOR.PARAM.INVERT_BATTERY.DESCRIPTION", "invertBattery", LogicGuid, ++sort, RuleInterfaceParameterDataType.Bool, false, false);
            factory.CreateParameterLogicInterfaceTemplate(ParamPeakPower, "METERING.ENERGY_MONITOR.PARAM.PEAK_POWER.NAME",
                "METERING.ENERGY_MONITOR.PARAM.PEAK_POWER.DESCRIPTION", "peakPower", LogicGuid, ++sort, RuleInterfaceParameterDataType.Double, DefaultPeakPower, false);
            for (var i = 0; i < ParamConsumerNames.Length; i++)
            {
                factory.CreateParameterLogicInterfaceTemplate(ParamConsumerNames[i], $"METERING.ENERGY_MONITOR.PARAM.CONSUMER{i + 1}_NAME.NAME",
                    "METERING.ENERGY_MONITOR.PARAM.CONSUMER_NAME.DESCRIPTION", $"consumer{i + 1}Name", LogicGuid, ++sort, RuleInterfaceParameterDataType.Text, "", false);
            }

            UseEnergyFlowVisu(factory, LogicGuid);
        }

        /// <summary>sets the energy flow tile as default visu template, false when the core does not know it yet</summary>
        internal static bool UseEnergyFlowVisu(ILogicTemplateFactory factory, Guid logicGuid)
        {
            if (!Enum.TryParse(EnergyFlowVisuTemplate, false, out VisuMobileObjectTemplateTypes template)
                || !Enum.IsDefined(typeof(VisuMobileObjectTemplateTypes), template))
            {
                return false;
            }
            factory.ChangeDefaultVisuTemplate(logicGuid, template);
            return true;
        }

        private void MonitorInput(ILogicTemplateFactory factory, Guid id, string name, string key, int sort)
        {
            factory.CreateLogicInterfaceTemplate(id, $"METERING.ENERGY_MONITOR.INPUT.{name}.NAME", $"METERING.ENERGY_MONITOR.INPUT.{name}.DESCRIPTION",
                key + "In", LogicGuid, LogicInterfaceDirection.Input, 0, sort);
        }

        private void MonitorOutput(ILogicTemplateFactory factory, Guid id, string name, string key, int sort, RuleInterfaceType type = RuleInterfaceType.Output)
        {
            factory.CreateLogicInterfaceTemplate(id, $"METERING.ENERGY_MONITOR.OUTPUT.{name}.NAME", $"METERING.ENERGY_MONITOR.OUTPUT.{name}.DESCRIPTION",
                key, LogicGuid, LogicInterfaceDirection.Output, 0, sort, type);
        }

        public override ILogic CreateLogicInstance(ILogicContext context)
        {
            return new EnergyMonitorRule(context);
        }
    }
}
