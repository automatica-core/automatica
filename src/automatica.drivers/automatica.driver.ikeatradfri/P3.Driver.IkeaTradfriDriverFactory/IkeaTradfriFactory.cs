using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using NodeDataType = Automatica.Core.Base.Templates.NodeDataType;

namespace P3.Driver.IkeaTradfriDriverFactory
{
    public class IkeaTradfriFactory : DriverFactory
    {
        public static Guid GatewayContainerGuid = new Guid("e4335f3f-721a-40f1-bb61-2c1aec9e936c");
        public static Guid GatewayGuid = new Guid("c357e6fd-5cfc-4064-9936-1ae356d2a226");

        public static Guid RelayContainerGuid = new Guid("d0b074df-a658-4041-909e-53d4e3d4aeb5");
        public static Guid RelayGuid = new Guid("31c6cce2-9afe-4546-8df9-7f48f8b1a7db");
        public static Guid RelayInterfaceGuid = new Guid("f85a9509-34e7-4ecc-bdc2-59f0911ec627");

        public static Guid LightContainerGuid = new Guid("c328d4d8-781a-45cf-893b-013dda818039");
        public static Guid LightInterfaceGuid = new Guid("a38b3006-640b-40ce-a1b7-2c24d110e38d");
        public static Guid LightGuid = new Guid("e78cdfad-c9a1-4592-9801-849c999fb870");
        public static Guid LightDimmerGuid = new Guid("fa88f555-27bb-4080-bd61-b953b0ba4d08");
        public static Guid LightColorGuid = new Guid("a0974672-5227-486a-90bf-f954dfd1c528");

        public const string IdAddressPropertyKey = "gateway-id";
        public const string ConnectionPropertyKey = "connection-key";
        public const string SecretPropertyKey = "gateway-secret";

        public const string DeviceIdPropertyKey = "id";

        public override string DriverName => "Ikea.Tradfri";

        public override Guid DriverGuid => GatewayContainerGuid;


        public override Version DriverVersion => new Version(0, 7, 0, 15);

        public override bool InDevelopmentMode => false;

        public override IDriver CreateDriver(IDriverContext config)
        {
            return new IkeaTradfri(config);
        }

        public override void InitNodeTemplates(INodeTemplateFactory factory)
        {
            factory.CreateInterfaceType(DriverGuid, "IKEA.TRADFRI.NAME", "IKEA.TRADFRI.DESCRIPTION", int.MaxValue, 1, true);
            factory.CreateNodeTemplate(DriverGuid, "IKEA.TRADFRI.NAME", "IKEA.TRADFRI.DESCRIPTION", "ikea.tradfri", GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Ethernet),
                DriverGuid, false, false, true, false, true, NodeDataType.NoAttribute, Int32.MaxValue, false);

            factory.CreatePropertyTemplate(new Guid("b6b5e4ff-8511-44f4-aceb-bd80d01a141b"), "IKEA.TRADFRI.SCAN.NAME", "IKEA.TRADFRI.SCAN.DESCRIPTION",
                "scan", PropertyTemplateType.Scan, DriverGuid, "COMMON.CATEGORY.DISCOVERY", true, false, "", null, 0,
                0);

            var gwInterface = new Guid("f15cca2e-7fe5-43f6-ad8a-ea4b60fc7b56");
            factory.CreateInterfaceType(gwInterface, "IKEA.TRADFRI.GATEWAY.NAME", "IKEA.TRADFRI.GATEWAY.DESCRIPTION", int.MaxValue, int.MaxValue, true);
            factory.CreateNodeTemplate(GatewayGuid, "IKEA.TRADFRI.GATEWAY.NAME", "IKEA.TRADFRI.GATEWAY.NAME", "ikea-tradfri-gateway", DriverGuid, gwInterface, false, false, true, false, true, NodeDataType.NoAttribute, int.MaxValue, false);

            factory.CreatePropertyTemplate(new Guid("594be0ff-63c4-4e46-9cc8-37ff7ccf80a2"), "IKEA.TRADFRI.GATEWAY.ID.NAME",
                "IKEA.TRADFRI.GATEWAY.ID.DESCRIPTION", IdAddressPropertyKey, PropertyTemplateType.Text, GatewayGuid,
                "COMMON.CATEGORY.ADDRESS", true, false, "", null, 1, 0);

            factory.CreatePropertyTemplate(new Guid("330b668e-2f46-42c3-b44e-b42a149a2a0d"), "IKEA.TRADFRI.GATEWAY.SECRET.NAME",
                "IKEA.TRADFRI.GATEWAY.SECRET.DESCRIPTION", SecretPropertyKey, PropertyTemplateType.Text,
                GatewayGuid, "COMMON.CATEGORY.ADDRESS", true, false, "", null, 1, 1);

            factory.CreatePropertyTemplate(new Guid("22438ff2-4739-4fd0-a2a0-abe691dfe1b4"), "IKEA.TRADFRI.GATEWAY.CONNECTION_KEY.NAME",
                "IKEA.TRADFRI.GATEWAY.CONNECTION_KEY.DESCRIPTION", ConnectionPropertyKey, PropertyTemplateType.Text,
                GatewayGuid, "COMMON.CATEGORY.ADDRESS", false, true, "", null, 1, 2);

            factory.CreatePropertyTemplate(new Guid("252d18ed-af14-447c-8472-2d3d0d15ff0d"), "IKEA.TRADFRI.GATEWAY.SCAN.NAME", "IKEA.TRADFRI.GATEWAY.SCAN.DESCRIPTION",
                "scan", PropertyTemplateType.Scan, GatewayGuid, "COMMON.CATEGORY.DISCOVERY", true, false, "", null, 2,
                0);

            CreateSwitch(factory, gwInterface);
            CreateLight(factory, gwInterface);
        }

        private void CreateSwitch(INodeTemplateFactory factory, Guid gwInterface)
        {
            factory.CreateInterfaceType(RelayInterfaceGuid, "IKEA.TRADFRI.RELAY.NAME", "IKEA.TRADFRI.RELAY.DESCRIPTION", 0, 0, false);

            factory.CreateNodeTemplate(RelayContainerGuid, "IKEA.TRADFRI.RELAY.NAME",
                "IKEA.TRADFRI.RELAY.DESCRIPTION", "container", gwInterface, RelayInterfaceGuid, false, false, true,
                false, true, NodeDataType.NoAttribute, 0, false);

            factory.CreatePropertyTemplate(new Guid("7612c5bb-69f5-46ac-a421-ce199dd0a0c3"), "IKEA.TRADFRI.DEVICE.ID.NAME", "IKEA.TRADFRI.DEVICE.ID.DESCRIPTION",
                DeviceIdPropertyKey, PropertyTemplateType.Numeric, RelayContainerGuid, "COMMON.CATEGORY.ADDRESS", true, false, null, null, 0,
                0);

            factory.CreateNodeTemplate(RelayGuid, "IKEA.TRADFRI.RELAY.VALUE.NAME", "IKEA.TRADFRI.RELAY.VALUE.DESCRIPTION",
                "ikea-relay", RelayInterfaceGuid, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true,
                false, true, false, NodeDataType.Boolean, 1, false);
            factory.ChangeDefaultVisuTemplate(RelayGuid, VisuMobileObjectTemplateTypes.ToggleButton);
        }

        private void CreateLight(INodeTemplateFactory factory, Guid gwInterface)
        {
            factory.CreateInterfaceType(LightInterfaceGuid, "IKEA.TRADFRI.LIGHT.NAME", "IKEA.TRADFRI.LIGHT.DESCRIPTION", 0, 0, false);

            factory.CreateNodeTemplate(LightContainerGuid, "IKEA.TRADFRI.LIGHT.NAME",
                "IKEA.TRADFRI.LIGHT.DESCRIPTION", "container", gwInterface, LightInterfaceGuid, false, false, true,
                false, true, NodeDataType.NoAttribute, 0, false);

            factory.CreatePropertyTemplate(new Guid("ae3582db-310e-4a10-9af9-27ef561e475a"), "IKEA.TRADFRI.DEVICE.ID.NAME", "IKEA.TRADFRI.DEVICE.ID.DESCRIPTION",
                DeviceIdPropertyKey, PropertyTemplateType.Numeric, LightContainerGuid, "COMMON.CATEGORY.ADDRESS", true, false, null, null, 0,
                0);

            factory.CreateNodeTemplate(LightGuid, "IKEA.TRADFRI.LIGHT.SWITCH.NAME", "IKEA.TRADFRI.LIGHT.SWITCH.DESCRIPTION",
                "ikea-light-switch", LightInterfaceGuid, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true,
                false, true, false, NodeDataType.Boolean, 1, false);
            factory.ChangeDefaultVisuTemplate(LightGuid, VisuMobileObjectTemplateTypes.ToggleButton);

            factory.CreateNodeTemplate(LightDimmerGuid, "IKEA.TRADFRI.LIGHT.DIMMER.NAME", "IKEA.TRADFRI.LIGHT.DIMMER.DESCRIPTION",
                "ikea-light-dimmer", LightInterfaceGuid, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true,
                false, true, false, NodeDataType.Integer, 1, false);

            factory.CreateNodeTemplate(LightColorGuid, "IKEA.TRADFRI.LIGHT.COLOR.NAME", "IKEA.TRADFRI.LIGHT.COLOR.DESCRIPTION",
                "ikea-light-dimmer", LightInterfaceGuid, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true,
                false, true, false, NodeDataType.String, 1, false);
        }
    }
}
