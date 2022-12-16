using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using P3.Driver.MBus.Frames.VariableData;
using NodeDataType = Automatica.Core.Base.Templates.NodeDataType;

namespace P3.Driver.MBusDriverFactory
{
    public class MBusUdpFactory : DriverFactory
    {
        private readonly Guid _mbusDeviceInterfaceType = new Guid("611b3f76-ccbb-4a7b-b14d-8c68a3e9ac9b");
        private readonly Guid _mbusAttributeInterfaceType = new Guid("aa4b2bfd-e0f4-4cca-86de-80adc3115892");

        public static readonly Guid MbusSerialNode = new Guid("57beac65-5b6f-430b-b593-bfe3202b9384");
        private readonly Guid _mbusUdpNode = new Guid("de985f68-45e5-4ec8-be4e-e042367c1567");
        private readonly Guid _mbusDevice = new Guid("d64c75fb-b8be-44ab-8b91-4fc676cdeda5");

        public static readonly Guid MbusEnergyW = new Guid("83ceb5d1-bf2f-4ee4-a1e3-f52e32a2ee52");
        public static readonly Guid MbusEnergyJ = new Guid("43444159-ad98-4094-9e79-d6d3297c8566");
        public static readonly Guid MbusVolume = new Guid("aae49b53-d9db-485a-82af-da39f75cde7c");
        public static readonly Guid MbusMass = new Guid("6e4d2c30-0e67-4525-92d8-0a8be093bd6b");
        public static readonly Guid MbusOnTime = new Guid("e75c5272-a21f-46d8-b40f-f5bfe1d3af72");
        public static readonly Guid MbusOperatingTime = new Guid("4b80eb5a-5dee-48e2-8bb8-a81b6fc7f323");
        public static readonly Guid MbusPowerW = new Guid("9d900c2a-c0e4-471d-b758-cf2be712f650");
        public static readonly Guid MbusPowerJh = new Guid("436dd6d0-dd41-47ea-b384-d0ef40421113");
        public static readonly Guid MbusVolumeFlowh = new Guid("6f58f647-1808-4766-9bfc-cc666335f914");
        public static readonly Guid MbusVolumeFlowmin = new Guid("5963882c-9b27-48fa-8ee9-c6dd2f699130");
        public static readonly Guid MbusVolumeFlows = new Guid("348626b5-0ada-4573-a898-95bffaa33d3e");
        public static readonly Guid MbusMassFlow = new Guid("a12a3395-da3e-4066-80e4-f0e97fdd6a27");
        public static readonly Guid MbusFlowTemperature = new Guid("c790814a-d003-4d7f-a635-5180143eeee9");
        public static readonly Guid MbusReturnTemperature = new Guid("695ba86e-e3f1-45aa-bf04-3f1d5a8260bc");
        public static readonly Guid MbusTemperatureDifference = new Guid("f7ac12f3-b211-4da0-9fa2-a17503554b1f");
        public static readonly Guid MbusExternalTemperature = new Guid("4ecc9928-ef62-44d4-8781-8898d1a0c7b0");
        public static readonly Guid MbusPressure = new Guid("24cf8dd8-7673-4730-b0e6-26b4d4e87c53");
        public static readonly Guid MbusTimePoint = new Guid("da08df0a-52fd-4215-b74a-c1f18129f1a8");
        public static readonly Guid MbusUnitsForHca = new Guid("47918102-010e-47a4-a470-4830c2f0f9c4");
        public static readonly Guid MbusAveragingDuration = new Guid("e146f194-be10-4f01-894d-df882b4b4a84");
        public static readonly Guid MbusActualityDuration = new Guid("3f1c1055-d107-45c3-b484-9c1be6da64a0");
        public static readonly Guid MbusEnhancedId = new Guid("9e6f763e-2363-4ad6-a514-92d6e2e82e39");
        public static readonly Guid MbusBusAddress = new Guid("75ccf901-6b2f-4e8d-b248-eecb9df4ccaa");
        public static readonly Guid MbusFabricationNumber = new Guid("34e6d638-dce1-4fbb-bf91-79c07c901937");
        public static readonly Guid MbusManufacturerSpecific = new Guid("0c473f60-8597-4161-b9e6-25e378ec8a1b");

        public override string ImageName => "automaticacore/plugin-p3.driver.mbus";
        public override string DriverName => "MBus";
        public override Version DriverVersion => new Version(0, 1, 0, 1);
        public override Guid DriverGuid => _mbusUdpNode;

        public override void InitNodeTemplates(INodeTemplateFactory factory)
        {
            factory.CreateInterfaceType(_mbusDeviceInterfaceType, "MBUS.DEVICE.NAME", "MBUS.DEVICE.DESCRIPTION", 100, int.MaxValue, true);
            factory.CreateInterfaceType(_mbusAttributeInterfaceType, "MBUS.ATTRIBUTE.NAME", "MBUS.ATTRIBUTE.DESCRIPTION", int.MaxValue, int.MaxValue, false);

            factory.CreateNodeTemplate(MbusSerialNode, "MBUS.SERIAL.NAME", "MBUS.SERIAL.DESCRIPTION", "mbus-s",
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Rs232), _mbusDeviceInterfaceType, false, false, true, false, true,
                NodeDataType.NoAttribute, int.MaxValue, false);

            factory.CreatePropertyTemplate(GenerateNewGuid(MbusSerialNode, 1), "COMMON.PROPERTY.BAUDRATE.NAME", "COMMON.PROPERTY.BAUDRATE.DESCRIPTION",
                "mbus-baudrate", PropertyTemplateType.Baudrate, MbusSerialNode, "COMMON.CATEGORY.ADDRESS", true, false, "", "9600", 1,
                1);
            factory.CreatePropertyTemplate(GenerateNewGuid(MbusSerialNode, 2), "MBUS.PROPERTY.TIMEOUT.NAME", ".PROPERTY.TIMEOUT.DESCRIPTION",
                "mbus-timeout", PropertyTemplateType.Integer, MbusSerialNode, "COMMON.CATEGORY.ADDRESS", true, false, "", "5000", 1,
                1);

            factory.CreateNodeTemplate(_mbusUdpNode, "MBUS.UDP.NAME", "MBUS.UDP.DESCRIPTION", "mbus-UDP",
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Ethernet), _mbusDeviceInterfaceType, false, false, true, false, true,
                NodeDataType.NoAttribute, int.MaxValue, false);

            factory.CreatePropertyTemplate(GenerateNewGuid(_mbusUdpNode, 1), "COMMON.PROPERTY.IP.NAME", "COMMON.PROPERTY.IP.DESCRIPTION",
                "mbus-ip", PropertyTemplateType.Ip, _mbusUdpNode, "COMMON.CATEGORY.ADDRESS", true, false, "", "", 1,
                1);
            factory.CreatePropertyTemplate(GenerateNewGuid(_mbusUdpNode, 2), "COMMON.PROPERTY.IPPORT.NAME", "COMMON.PROPERTY.IPPORT.DESCRIPTION",
                "mbus-port", PropertyTemplateType.Range, _mbusUdpNode, "COMMON.CATEGORY.ADDRESS", true, false, PropertyHelper.CreateRangeMetaString(1, ushort.MaxValue), "44818", 1,
                1);

            factory.CreatePropertyTemplate(GenerateNewGuid(_mbusUdpNode, 3), "MBUS.PROPERTY.TIMEOUT.NAME", "MBUS.PROPERTY.TIMEOUT.DESCRIPTION",
                "mbus-timeout", PropertyTemplateType.Integer, _mbusUdpNode, "COMMON.CATEGORY.ADDRESS", true, false, "", "5000", 1,
                1);

            factory.CreateNodeTemplate(_mbusDevice, "MBUS.DEVICE.NAME", "MBUS.DEVICE.DESCRIPTION", "mbus-device",
                _mbusDeviceInterfaceType, _mbusAttributeInterfaceType, false, false, true, false, true,
                NodeDataType.NoAttribute, 249, false);

            factory.CreatePropertyTemplate(GenerateNewGuid(_mbusDevice, 1), "MBUS.PROPERTY.DEVICE_ID.NAME", "MBUS.PROPERTY.DEVICE_ID.DESCRIPTION",
                "mbus-deviceId", PropertyTemplateType.Range, _mbusDevice, "MBUS.CATEGORY.DEVICE", true, false, PropertyHelper.CreateRangeMetaString(0, 249), "1", 1, 1);
            //TODO:  1, PropertyConstraint.Unique, PropertyConstraintLevel.Error
            factory.CreatePropertyTemplate(GenerateNewGuid(_mbusDevice, 2), "MBUS.PROPERTY.POLL_INTERVAL.NAME", "MBUS.PROPERTY.POLL_INTERVAL.DESCRIPTION",
                "mbus-pollInterval", PropertyTemplateType.Integer, _mbusDevice, "MBUS.CATEGORY.DEVICE", true, false, null, "900", 1,
                1);
            factory.CreatePropertyTemplate(GenerateNewGuid(_mbusDevice, 3), "MBUS.PROPERTY.RESET_BEFORE_POLL.NAME", "MBUS.PROPERTY.RESET_BEFORE_POLL.DESCRIPTION",
                "mbus-resetBeforePoll", PropertyTemplateType.Bool, _mbusDevice, "MBUS.CATEGORY.DEVICE", true, false, null, "false", 1,
                1);
            factory.CreatePropertyTemplate(GenerateNewGuid(_mbusDevice, 4), "MBUS.PROPERTY.TIMEOUT.NAME", "MBUS.PROPERTY.TIMEOUT.DESCRIPTION",
                "mbus-device-timeout", PropertyTemplateType.Integer, _mbusDevice, "MBUS.CATEGORY.DEVICE", true, false, "", "300", 1,
                1);

            factory.CreatePropertyTemplate(GenerateNewGuid(_mbusDevice, 5), "Scan Device", "Scans the mbus device",
                "mbus-scan", PropertyTemplateType.Scan, _mbusDevice, "Data", true, true, "OBJECT_SAVED", null, 0, 0);

            CreateAttributeNodeTemplate(MbusEnergyW, "ENERGY_WATT", Unit.EnergyW, factory);
            CreateAttributeNodeTemplate(MbusEnergyJ, "ENERGY_JOULE", Unit.EnergyJ, factory);
            CreateAttributeNodeTemplate(MbusVolume, "VOLUME", Unit.Volume, factory);
            CreateAttributeNodeTemplate(MbusMass, "MASS", Unit.Mass, factory);
            CreateAttributeNodeTemplate(MbusOnTime, "ON_TIME", Unit.OnTime, factory);
            CreateAttributeNodeTemplate(MbusOperatingTime, "OPERATING_TIME", Unit.OperatingTime, factory);
            CreateAttributeNodeTemplate(MbusPowerW, "POWER_WATT", Unit.PowerW, factory);
            CreateAttributeNodeTemplate(MbusPowerJh, "POWER_JOULE_H", Unit.PowerJh, factory);
            CreateAttributeNodeTemplate(MbusVolumeFlowh, "VOLUME_FLOW_HOURS", Unit.VolumeFlowh, factory);
            CreateAttributeNodeTemplate(MbusVolumeFlowmin, "VOLUME_FLOW_MINUTES", Unit.VolumeFlowmin, factory);
            CreateAttributeNodeTemplate(MbusVolumeFlows, "VOLUME_FLOW_SECONDS", Unit.VolumeFlows, factory);
            CreateAttributeNodeTemplate(MbusMassFlow, "MASS_FLOW", Unit.MassFlow, factory);
            CreateAttributeNodeTemplate(MbusFlowTemperature, "FLOW_TEMP", Unit.FlowTemperature, factory);
            CreateAttributeNodeTemplate(MbusReturnTemperature, "RETURN_TEMP", Unit.ReturnTemperature, factory);
            CreateAttributeNodeTemplate(MbusTemperatureDifference, "TEMP_DIFFERENCE", Unit.TemperatureDifference, factory);
            CreateAttributeNodeTemplate(MbusExternalTemperature, "EXTENERAL_TEMP", Unit.ExternalTemperature, factory);
            CreateAttributeNodeTemplate(MbusPressure, "PRESSURE", Unit.Pressure, factory);
            CreateAttributeNodeTemplate(MbusTimePoint, "TIME_POINT", Unit.TimePoint, factory);
            CreateAttributeNodeTemplate(MbusUnitsForHca, "UNITS_FOR_HCA", Unit.UnitsForHca, factory);
            CreateAttributeNodeTemplate(MbusAveragingDuration, "AVERAGING_DURATION", Unit.AveragingDuration, factory);
            CreateAttributeNodeTemplate(MbusActualityDuration, "ACTUALITY_DURATION", Unit.ActualityDuration, factory);
            CreateAttributeNodeTemplate(MbusEnhancedId, "ENHANCED_ID", Unit.EnhancedId, factory);
            CreateAttributeNodeTemplate(MbusBusAddress, "BUS_ADDRESS", Unit.BusAddress, factory);
            CreateAttributeNodeTemplate(MbusFabricationNumber, "FABRICATION_NUMBER", Unit.FabricationNumber, factory);
            CreateAttributeNodeTemplate(MbusManufacturerSpecific, "MANUFACTURER_SPECIFIC", Unit.ManufacturerSpecific, factory);
        }

        private void CreateAttributeNodeTemplate(Guid guid, string name, Unit key, INodeTemplateFactory factory)
        {
            factory.CreateNodeTemplate(guid, $"MBUS.ATTRIBUTES.{name}.NAME", $"MBUS.ATTRIBUTES{name}.DESCRIPTION", "mbus-"+ key,
                _mbusAttributeInterfaceType, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), false, true, true, false, true,
                NodeDataType.Double, 100, false);

            factory.CreatePropertyTemplate(GenerateNewGuid(guid,1), "MBUS.PROPERTY.STORAGE_NUMBER.NAME", "MBUS.PROPERTY.STORAGE_NUMBER.DESCRIPTION", "mbus-storageNumber",
                PropertyTemplateType.Integer, guid, "MBUS.CATEGORY.ATTRIBUTE", true, false, null, "0", 0, 0);
            factory.CreatePropertyTemplate(GenerateNewGuid(guid, 2), "MBUS.PROPERTY.TARIFF.NAME", "MBUS.PROPERTY.TARIFF.DESCRIPTION", "mbus-tariff",
                PropertyTemplateType.Integer, guid, "MBUS.CATEGORY.ATTRIBUTE", true, false, null, "0", 0, 0);
            factory.CreatePropertyTemplate(GenerateNewGuid(guid, 3), "Type", "Type", "mbus-type",
                PropertyTemplateType.Integer, guid, "MBUS.CATEGORY.ATTRIBUTE", false, true, null, (int)key, 0, 0);
        }

        private Guid GenerateNewGuid(Guid guid, int c)
        {
            byte[] gu = guid.ToByteArray();

            gu[gu.Length - 1] = (byte) (Convert.ToInt32(gu[gu.Length - 1]) + c);

            return new Guid(gu);
        }

        public override IDriver CreateDriver(IDriverContext config)
        {
            return new Driver(config, MBusType.Udp);
        }
    }
}
