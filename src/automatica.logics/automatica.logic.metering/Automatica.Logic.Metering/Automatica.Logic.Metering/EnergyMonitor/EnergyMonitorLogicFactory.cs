using System;
using System.Linq;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;

namespace Automatica.Logic.Metering.EnergyMonitor
{
    /// <summary>
    /// Energy monitor (like Loxone's energy flow monitor): grid, PV, battery and up to six extra consumers in,
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
            new Guid("9d0c6855-3d20-4522-b36f-6a77b29cd8e1"),
            new Guid("d497c9fe-3f59-4eb5-8feb-78b0b856dbc9"),
            new Guid("c800f1f7-528b-431e-b031-7c1d7272c6a8"),
            new Guid("f6dde8a9-e791-4985-bd30-7c10a8e62f16")
        };
        /// <summary>power of the measured sub-circuits (kW), see <see cref="EnergyCircuitInputs"/></summary>
        public static readonly Guid[] InputCircuits =
        {
            new Guid("a4404911-d7f4-49a9-a08b-75e21bdffffa"),
            new Guid("f67fb28d-b0ae-4423-bdda-4a94814abb99"),
            new Guid("975c583f-da2a-46dd-9ee5-39ae981def01"),
            new Guid("e0440a9b-d5f9-417d-8d95-68a21664c9cd"),
            new Guid("0a8290b6-094d-4dfa-9d55-b09cc490fede"),
            new Guid("34caa145-b562-400c-bad8-3722f3735631"),
            new Guid("f5d4f074-835c-42b5-a7fe-f77f10743dea"),
            new Guid("910fc7c5-b3f8-48e9-a072-91aa57d0769c"),
            new Guid("730709b8-2f87-404d-8ef2-d3bf182262a6"),
            new Guid("6c4c29ee-dcc0-4c2f-b640-3e439f51c639"),
            new Guid("ec99ae87-50d9-43ca-a300-d689939fc2e9"),
            new Guid("e99e08ef-77f5-4be1-8a5c-9715162a07d1"),
            new Guid("8a9834ad-19f8-48f6-9db0-8df56058da18"),
            new Guid("5eeeba73-65fe-471f-ac45-e363cebf9a9c"),
            new Guid("0dfbfac3-a937-48dc-afd0-574501f2ea0a"),
            new Guid("6e7809f2-e413-4899-b6a5-885c84deac95"),
            new Guid("e2e90ad9-0210-4572-8f60-caf6db02efbe"),
            new Guid("8cded5fe-9279-404b-9485-3f8c02f35071")
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
            new Guid("b11212c4-1c5e-4426-87a1-ebf960e43249"),
            new Guid("145f99a8-01ea-4748-908f-7be38e3029c1"),
            new Guid("798ca2b2-4601-4489-ba7d-e66c8b797102"),
            new Guid("ea992104-882f-4aaf-8899-767f6a6cdd92")
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
        /// <summary>not measured part (kW) of the house (index 0) and of consumers 1-6, sent once the group has circuits</summary>
        public static readonly Guid[] OutputRests =
        {
            new Guid("59023f37-43ce-4568-97bc-5157545d6739"),
            new Guid("e775507d-39f1-4beb-ae29-564c214bdc84"),
            new Guid("d4edee0a-9ca6-4e33-b15b-7ec7d706d671"),
            new Guid("41ab06d9-fded-48bb-a4e0-e11591039e27"),
            new Guid("933dc025-2158-4f69-bf9c-5d462858cf72"),
            new Guid("36a68084-0bd6-4839-8bfe-a3550a976c37"),
            new Guid("199c2654-d70e-433d-8209-d04dee3e457f")
        };

        // parameters
        public static readonly Guid ParamInvertGrid = new Guid("932f6f30-5d85-47d3-8df1-67b0041fa1f8");
        public static readonly Guid ParamInvertBattery = new Guid("585e9891-08ce-4d60-b583-c70be64bb804");
        public static readonly Guid ParamPeakPower = new Guid("1e88b300-07a1-4c4e-ab9c-5958909b0d2f");
        public static readonly Guid[] ParamConsumerNames =
        {
            new Guid("c84e1ab6-010f-4dab-8fc2-d8f7646df031"),
            new Guid("56aa2d0a-ea2d-43f9-b821-be4ae8f6eeb1"),
            new Guid("39847236-d148-434f-a1f0-7ae438649253"),
            new Guid("4b74931c-0033-4869-98a4-1fb75a1f858c"),
            new Guid("0879d7f9-4a46-49b2-a74a-ccfe74a876cb"),
            new Guid("c3d29b2d-b980-42f7-b044-64038e0fa3d8")
        };
        /// <summary>which group a circuit belongs to: 0 = house, 1-6 = consumer</summary>
        public static readonly Guid[] ParamCircuitGroups =
        {
            new Guid("2aac0b7e-b61e-40a2-b364-d5cebcf3c883"),
            new Guid("63329d83-60f2-4f7f-b37b-a0af5bd83f51"),
            new Guid("46b6c5f1-b4fd-4a66-a750-dc5ca672a5fe"),
            new Guid("e5ef385e-e83d-4c43-8452-3259d0ca05e1"),
            new Guid("ce57d88c-be97-49b7-9400-6cbf1ce155fb"),
            new Guid("aaf90c89-70c5-4f01-adab-147be7cb7237"),
            new Guid("ec8cd9bb-3512-48af-a895-6f8e96980c9c"),
            new Guid("709bf30e-624f-42a6-9496-e5eb618575a2"),
            new Guid("46bc7583-c263-4e55-b5e1-8fec8b0b2fb8"),
            new Guid("ad92c3d5-e159-437a-8483-5db7e93fb512"),
            new Guid("1e6f1ca6-83ae-417d-aec3-f008c54ee8e2"),
            new Guid("041e0462-753c-4425-8060-3b1eb16c0c77"),
            new Guid("d6d4e94a-e84f-4cc8-901d-7b4e1aa91471"),
            new Guid("d717a16e-1c19-4110-9eff-03104e8ec2f3"),
            new Guid("68feb00c-26e4-4cac-a71c-aad6d2fd11dd"),
            new Guid("4b06af09-9ae3-426f-a5e7-56bc57abf4e8"),
            new Guid("0d353f11-4351-4673-83fa-87302669e499"),
            new Guid("e8822674-1aff-4d55-b148-b34495422cdf")
        };
        public static readonly Guid[] ParamCircuitNames =
        {
            new Guid("450627b7-9489-443e-ba36-029ddd3493bd"),
            new Guid("bf961157-f73d-4be6-b99a-828ff31209b7"),
            new Guid("89fcd395-c157-42a5-8198-5092659af1c1"),
            new Guid("1db3dcad-fd72-45bc-9bf1-09386a8dc9ee"),
            new Guid("77cc8a0e-bdc1-474a-b7f4-0af86cf40f88"),
            new Guid("fb7b4c81-ddf1-44af-8d12-f817bed01ea3"),
            new Guid("56017b15-c830-4f66-bab0-23dbfc6443df"),
            new Guid("d07d3179-f5b9-4ee3-88ee-db24f860d9a5"),
            new Guid("dee34965-6040-4324-8d15-d01f273b6079"),
            new Guid("71f24aed-200e-493a-ba2d-66b1130f6933"),
            new Guid("8911747e-cae6-45e6-a659-d147953ff127"),
            new Guid("a738dc20-b158-4f96-b9a5-86cd0c0a950b"),
            new Guid("e8ab9f75-8b5b-4858-8741-ce5103c01922"),
            new Guid("bc35a647-e832-44db-960a-984a9b967911"),
            new Guid("16388133-b0cc-49b7-8bb7-2728ff5455cd"),
            new Guid("129ef886-b049-4d3d-9921-2b36326da860"),
            new Guid("8e909517-2c62-45aa-8621-9cfb085dddd0"),
            new Guid("e54899e8-c824-4f2e-8bec-dd452bd1de7b")
        };

        private const string CircuitGroupMetaGroups = "ENUM(0,METERING.ENERGY_MONITOR.GROUP.HOUSE;1,METERING.ENERGY_MONITOR.GROUP.CONSUMER1;"
            + "2,METERING.ENERGY_MONITOR.GROUP.CONSUMER2;3,METERING.ENERGY_MONITOR.GROUP.CONSUMER3;4,METERING.ENERGY_MONITOR.GROUP.CONSUMER4;"
            + "5,METERING.ENERGY_MONITOR.GROUP.CONSUMER5;6,METERING.ENERGY_MONITOR.GROUP.CONSUMER6)";

        /// <summary>
        /// what a circuit can belong to: the house, a consumer or any other circuit (101-118); the circuit itself is left out
        /// </summary>
        internal static string CircuitParentMeta(int circuit) =>
            CircuitGroupMetaGroups.TrimEnd(')')
            + string.Concat(Enumerable.Range(0, EnergyCircuitInputs.CircuitCount)
                .Where(other => other != circuit)
                .Select(other => $";{EnergyCircuitInputs.CircuitParent(other)},METERING.ENERGY_MONITOR.GROUP.CIRCUIT{other + 1}"))
            + ")";

        public override string LogicName => "Metering.EnergyMonitor";
        // 1.1: consumers 4-6 (inputs, outputs, names); 1.2: sub-circuits (inputs, group + name, rest outputs);
        // 1.3: a circuit can belong to another circuit (choices of the group parameter).
        // The core adds new interfaces to existing instances only on a version bump.
        public override Version LogicVersion => new Version(1, 3, 0, 0);
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
            // after the existing inputs, so they keep their place on the block
            for (var i = 0; i < InputCircuits.Length; i++)
            {
                MonitorInput(factory, InputCircuits[i], $"CIRCUIT{i + 1}", $"circuit{i + 1}", ++sort);
            }

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
            MonitorOutput(factory, OutputRests[0], "HOUSE_REST", "houseRest", ++sort);
            for (var i = 1; i < OutputRests.Length; i++)
            {
                MonitorOutput(factory, OutputRests[i], $"CONSUMER{i}_REST", $"consumer{i}Rest", ++sort);
            }

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
            for (var i = 0; i < ParamCircuitNames.Length; i++)
            {
                factory.CreateParameterLogicInterfaceTemplate(ParamCircuitNames[i], $"METERING.ENERGY_MONITOR.PARAM.CIRCUIT{i + 1}_NAME.NAME",
                    "METERING.ENERGY_MONITOR.PARAM.CIRCUIT_NAME.DESCRIPTION", $"circuit{i + 1}Name", LogicGuid, ++sort, RuleInterfaceParameterDataType.Text, "", false);
                factory.CreateParameterLogicInterfaceTemplate(ParamCircuitGroups[i], $"METERING.ENERGY_MONITOR.PARAM.CIRCUIT{i + 1}_GROUP.NAME",
                    "METERING.ENERGY_MONITOR.PARAM.CIRCUIT_GROUP.DESCRIPTION", $"circuit{i + 1}Group", LogicGuid, ++sort, RuleInterfaceParameterDataType.Enum,
                    EnergyCircuitInputs.HouseGroup, false, CircuitParentMeta(i));
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
