using System;
using System.Globalization;
using Automatica.Core.Base.Templates;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using NodeDataType = Automatica.Core.Base.Templates.NodeDataType;

namespace P3.Driver.FroniusSolarFactory
{
    public class FroniusSolarFactory : DriverFactory
    {
        public override string DriverName => "FroniusSolar";

        public override Guid DriverGuid => new Guid("d0f5650d-f786-4a08-8360-03fada6d4b29");
        
        public override Version DriverVersion => new Version(0, 2, 0, 2);

        public override bool InDevelopmentMode => false;

        public override string ImageName => "automaticacore/plugin-p3.driver.froniussolar";

        public Guid CommonInverterData => new("ba559505-4d40-4676-b8f9-fba0923ab405");
        public Guid P3InverterData => new("e4017c27-0f30-4437-988c-a56aea52d60f");
        public Guid PowerFlowRealtimeData => new("71f4d1d2-8472-4694-b03f-87fff220d7d3");

        public override IDriver CreateDriver(IDriverContext config)
        {
            return new FroniusSolarDriver(config);
        }

        public override void InitNodeTemplates(INodeTemplateFactory factory)
        {
            factory.CreateInterfaceType(DriverGuid, "FRONIUS_SOLAR.NAME", "FRONIUS_SOLAR.DESCRIPTION", int.MaxValue, 1, true);
            factory.CreateNodeTemplate(DriverGuid, "FRONIUS_SOLAR.NAME", "FRONIUS_SOLAR.DESCRIPTION", "fronius-solar", GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Ethernet),
                DriverGuid, false, true, true, false, true, NodeDataType.NoAttribute, Int32.MaxValue, false);


            var deviceGuid = new Guid("b41759b5-99b7-4844-85aa-176dfee82682");
            factory.CreateInterfaceType(deviceGuid, "FRONIUS_SOLAR.DEVICE.NAME", "FRONIUS_SOLAR.DEVICE.DESCRIPTION", int.MaxValue, 1, false);
            factory.CreateNodeTemplate(deviceGuid, "FRONIUS_SOLAR.DEVICE.NAME", "FRONIUS_SOLAR.DEVICE.DESCRIPTION", "fronius-solar-device", DriverGuid,
                deviceGuid, false, true, true, false, true, NodeDataType.NoAttribute, Int32.MaxValue, false);

            factory.CreatePropertyTemplate(new Guid("531fe2ea-4f8b-4e9a-b6fe-ecf129ec6b16"), "COMMON.PROPERTY.IP.NAME",
                "COMMON.PROPERTY.IP.DESCRIPTION", "fronius-ip", PropertyTemplateType.Ip, deviceGuid,
                "COMMON.CATEGORY.ADDRESS", true, false, null, null, 1, 1);

            factory.CreatePropertyTemplate(new Guid("2f3a8957-e852-4dbc-af3d-5a95845fb8ab"), "FRONIUS_SOLAR.PROPERTY.DEVICE_ID.NAME",
                "FRONIUS_SOLAR.PROPERTY.DEVICE_ID.DESCRIPTION", "fronius-device-id", PropertyTemplateType.Integer, deviceGuid,
                "COMMON.CATEGORY.ADDRESS", true, false, PropertyHelper.CreateRangeMetaString(1, 65535), 1, 1, 1);

            factory.CreatePropertyTemplate(new Guid("eb985bb2-2f5e-407a-8fd3-7a387eb8dd2f"),
                "FRONIUS_SOLAR.PROPERTY.POLL_INTERVAL.NAME", "FRONIUS_SOLAR.PROPERTY.POLL_INTERVAL.DESCRIPTION", "fronius-poll-interval",
                PropertyTemplateType.Integer, deviceGuid, "COMMON.CATEGORY.DEVICE", true, false,
                PropertyHelper.CreateRangeMetaString(30000, 65535), 30000, 1, 1);


            factory.CreatePropertyTemplate(new Guid("79ab381b-b9e3-48b6-8a06-d93be3d39ea0"),
                "FRONIUS_SOLAR.PROPERTY.DISABLED.NAME", "FRONIUS_SOLAR.PROPERTY.DISABLED.DESCRIPTION", "fronius-disabled",
                PropertyTemplateType.Bool, deviceGuid, "COMMON.CATEGORY.DEVICE", true, false,
                "", false, 1, 1);

            factory.CreateNodeTemplate(new Guid("2d1f4463-e881-4214-a8f7-eeed4b7df24b"), "FRONIUS_SOLAR.LAST_POLL_TIMESTAMP.NAME", "FRONIUS_SOLAR.LAST_POLL_TIMESTAMP.DESCRIPTION",
                "last-poll-timestamp", deviceGuid, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, false, true, false, true,
                NodeDataType.DateTime, 1, false);

            factory.CreateNodeTemplate(new Guid("61be4df1-b5ca-4961-b833-99a2a96c044c"), "FRONIUS_SOLAR.DEVICE_STATE.NAME", "FRONIUS_SOLAR.DEVICE_STATE.DESCRIPTION",
                "device-state", deviceGuid, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, false, true, false, true,
                NodeDataType.DateTime, 1, false);

            factory.CreateInterfaceType(CommonInverterData, "FRONIUS_SOLAR.COMMON_INVERTER_DATA.NAME", "FRONIUS_SOLAR.COMMON_INVERTER_DATA.DESCRIPTION", 7, 1,
                true);
            factory.CreateInterfaceType(P3InverterData, "FRONIUS_SOLAR.P3_INVERTER_DATA.NAME", "FRONIUS_SOLAR.P3_INVERTER_DATA.DESCRIPTION", int.MaxValue, 1,
                true);
            factory.CreateInterfaceType(PowerFlowRealtimeData, "FRONIUS_SOLAR.POWER_FLOW_REALTIME_DATA.NAME", "FRONIUS_SOLAR.POWER_FLOW_REALTIME_DATA.DESCRIPTION", int.MaxValue, 1,
                true);

            factory.CreateNodeTemplate(CommonInverterData, "FRONIUS_SOLAR.COMMON_INVERTER_DATA.NAME", "FRONIUS_SOLAR.COMMON_INVERTER_DATA.DESCRIPTION",
                "fronius-solar-common-data", deviceGuid, CommonInverterData, true, false, true, false, true,
                NodeDataType.NoAttribute, 1, false);
            factory.CreateNodeTemplate(P3InverterData, "FRONIUS_SOLAR.P3_INVERTER_DATA.NAME", "FRONIUS_SOLAR.P3_INVERTER_DATA.DESCRIPTION",
                "fronius-solar-p3-inverter-data", deviceGuid, P3InverterData, true, false, true, false, true,
                NodeDataType.NoAttribute, 1, false);
            factory.CreateNodeTemplate(PowerFlowRealtimeData, "FRONIUS_SOLAR.POWER_FLOW_REALTIME_DATA.NAME", "FRONIUS_SOLAR.POWER_FLOW_REALTIME_DATA.DESCRIPTION",
                "fronius-solar-power-flow-realtime-data", deviceGuid, PowerFlowRealtimeData, true, false, true, false, true,
                NodeDataType.NoAttribute, 1, false);


            CreateValueTemplate(factory, CommonInverterData, new Guid("c4c8ffee-7633-49fe-8a54-d91c62bf7f77"), "ac-power", NodeDataType.Double);
            CreateValueTemplate(factory, CommonInverterData, new Guid("845badf4-10f8-42de-b160-6389fde8c50f"), "ac-current", NodeDataType.Double);
            CreateValueTemplate(factory, CommonInverterData, new Guid("c29a29a4-f258-4bb9-88a7-7a20d9485673"), "ac-voltage", NodeDataType.Double);
            CreateValueTemplate(factory, CommonInverterData, new Guid("463902e9-ffac-4842-9d8a-d9ae7e8e07f3"), "ac-frequency", NodeDataType.Double);
            CreateValueTemplate(factory, CommonInverterData, new Guid("38b57640-d0fe-4a46-a49c-d0cf42778a78"), "dc-current", NodeDataType.Double);
            CreateValueTemplate(factory, CommonInverterData, new Guid("d7120013-b520-4c92-b70a-2940f1495c8b"), "dc-voltage", NodeDataType.Double);
            CreateValueTemplate(factory, CommonInverterData, new Guid("8d367af6-04e3-4fd0-bc2a-a565d62a6266"), "current-day-energy", NodeDataType.Double);
            CreateValueTemplate(factory, CommonInverterData, new Guid("b9253ba4-acca-451c-b6eb-22afc01776fa"), "current-year-energy", NodeDataType.Double);
            CreateValueTemplate(factory, CommonInverterData, new Guid("18dcb8aa-7d2c-4875-9689-04049c150c01"), "total-energy", NodeDataType.Double);
            CreateValueTemplate(factory, CommonInverterData, new Guid("71be07e9-8d6e-44a0-8b8d-46fe8cb90fee"), "device-status", NodeDataType.Integer);
            CreateValueTemplate(factory, CommonInverterData, new Guid("9c64ca64-168b-4d6e-b114-567ec2f505f0"), "error-code", NodeDataType.Integer);
            CreateValueTemplate(factory, CommonInverterData, new Guid("fc4eb50f-e788-4af2-927c-87b7f0efbb96"), "mgm-timer-remaining-time", NodeDataType.Integer);

            CreateValueTemplate(factory, P3InverterData, new Guid("b31f4edc-c867-4768-93da-848d40d7b151"), "l1-ac-current", NodeDataType.Double);
            CreateValueTemplate(factory, P3InverterData, new Guid("b99e2c3e-d55f-4348-a28b-42e64452b12b"), "l2-ac-current", NodeDataType.Double);
            CreateValueTemplate(factory, P3InverterData, new Guid("965cefbb-dc13-46b2-a53c-0805c857de36"), "l3-ac-current", NodeDataType.Double);
            CreateValueTemplate(factory, P3InverterData, new Guid("0fd13198-9dd4-4184-b32d-398d8219659c"), "l1-ac-voltage", NodeDataType.Double);
            CreateValueTemplate(factory, P3InverterData, new Guid("15f8298a-10a3-4b86-a222-90a8eab07f9b"), "l2-ac-voltage", NodeDataType.Double);
            CreateValueTemplate(factory, P3InverterData, new Guid("74e4603d-9641-46ec-baf7-5e725a01d333"), "l3-ac-voltage", NodeDataType.Double);
            CreateValueTemplate(factory, P3InverterData, new Guid("f0195b05-2e64-4aaa-9788-561325765ab0"), "ambient-temperature", NodeDataType.Double);
            CreateValueTemplate(factory, P3InverterData, new Guid("eb48c2be-4459-4344-989e-6e12108d99a7"), "fan-front-left-speed", NodeDataType.Double);
            CreateValueTemplate(factory, P3InverterData, new Guid("12f8d880-c53d-497a-aa34-91cd2a89cc24"), "fan-front-right-speed", NodeDataType.Double);
            CreateValueTemplate(factory, P3InverterData, new Guid("7001f45f-89b5-4e2e-8cbf-a9661e6dcb32"), "fan-back-left-speed", NodeDataType.Double);
            CreateValueTemplate(factory, P3InverterData, new Guid("21ca6a18-5ca7-4c40-857d-b947c053d70c"), "fan-back-right-speed", NodeDataType.Double);

            CreateValueTemplate(factory, PowerFlowRealtimeData, new Guid("54cf9d87-2c6e-4ae3-bfb3-f55655cd2dd6"), "device-type", NodeDataType.Double);
            CreateValueTemplate(factory, PowerFlowRealtimeData, new Guid("1a867ef3-4676-457c-9d1b-aafd256dd15c"), "current-power", NodeDataType.Double);
            CreateValueTemplate(factory, PowerFlowRealtimeData, new Guid("5740baf8-15ca-44db-9b30-1ceb472c5739"), "ac-energy-today", NodeDataType.Double);
            CreateValueTemplate(factory, PowerFlowRealtimeData, new Guid("acc8d6c6-d7d4-4bfd-bd96-a2d637a33ee1"), "ac-energy-year", NodeDataType.Double);
            CreateValueTemplate(factory, PowerFlowRealtimeData, new Guid("8c4b1a53-6de0-4a94-adf1-14a625613ecd"), "ac-energy-total", NodeDataType.Double);

        }
        private void CreateValueTemplate(INodeTemplateFactory factory, Guid needsInterfaceGuid, Guid guid, string name, NodeDataType nodeDataType)
        {
            factory.CreateNodeTemplate(guid, $"FRONIUS_SOLAR.DEVICE.{name.ToUpperInvariant()}.NAME", $"FRONIUS_SOLAR.DEVICE.{name.ToUpperInvariant()}.DESCRIPTION", $"{name.ToLowerInvariant()}", needsInterfaceGuid,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, nodeDataType, 1, false, false);
        }
    }
}
