using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using Microsoft.VisualStudio.Web.CodeGeneration.EntityFrameworkCore;
using P3.Driver.HomeKit;
using NodeDataType = Automatica.Core.Base.Templates.NodeDataType;

namespace P3.Driver.HomeKitFactory
{
    public class HomeKitFactory : DriverFactory
    {
        public HomeKitFactory()
        {
            HomeKitServer.Init();
        }
        public override string DriverName => "AppleHomeKitServer";

        public override Guid DriverGuid => new Guid("c0491f87-83e4-4510-bad2-e21ebbc490d1");
        
        public override Version DriverVersion => new Version(0, 0, 0, 3);

        public override bool InDevelopmentMode => true;

        public override IDriver CreateDriver(IDriverContext config)
        {
            return new HomeKitDriver(config);
        }

        public override void InitNodeTemplates(INodeTemplateFactory factory)
        {
            factory.CreateInterfaceType(DriverGuid, "APPLE_HOMEKIT_SERVER.NAME", "APPLE_HOMEKIT_SERVER.DESCRIPTION", int.MaxValue, int.MaxValue, true);
            factory.CreateNodeTemplate(DriverGuid, "APPLE_HOMEKIT_SERVER.NAME", "APPLE_HOMEKIT_SERVER.DESCRIPTION", "apple-homekit-server", GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Virtual),
                DriverGuid, false, true, true, false, true, NodeDataType.NoAttribute, Int32.MaxValue, false);

            factory.CreateNodeTemplate(new Guid("13d10902-a07c-4abf-a331-70ad2f95a184"), "APPLE_HOMEKIT_SERVER.PAIRING_KEY.NAME", "APPLE_HOMEKIT_SERVER.PAIRING_KEY.DESCRIPTION", "pairing-key", DriverGuid,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.String, 1, false, false);

            factory.CreatePropertyTemplate(new Guid("4085d4eb-11c0-48ff-b005-045020729d85"), "LTSK_PRIVATE", "LTSK_PRIVATE", "ltsk-private",
                PropertyTemplateType.Text, DriverGuid, "", false, true, null, null, 0, 0);
            factory.CreatePropertyTemplate(new Guid("4bd1b665-61ec-4f43-b80d-ec236983d365"), "LTPK_PRIVATE", "LTPK_PRIVATE", "ltpk-private",
                PropertyTemplateType.Text, DriverGuid, "", false, true, null, null, 0, 0);

            factory.CreatePropertyTemplate(new Guid("c3759cf5-6a9b-4afb-b0e4-32941db684ca"), "CONFIG_VERSION", "CONFIG_VERSION", "config-version",
                PropertyTemplateType.Integer, DriverGuid, "", false, true, null, 1, 0, 0);


            factory.CreatePropertyTemplate(new Guid("25497b6e-8059-4dfc-8e9f-917d31c624cf"), "COMMON.PROPERTY.IPPORT.NAME", "COMMON.PROPERTY.IPPORT.DESCRIPTION", "port",
                PropertyTemplateType.Integer, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, PropertyHelper.CreateRangeMetaString(1, short.MaxValue), 52634, 0, 0);

            CreateLight(factory);
            CreatePowerOutlet(factory);
            CreateContactSensor(factory);
            CreateSwitch(factory);
            CreateTemperatureSensor(factory);
        }

        private void CreateLight(INodeTemplateFactory factory)
        {
            var interfaceGuid = new Guid("6d07b0a9-4635-4404-8254-76079ea7f6ce");
            factory.CreateInterfaceType(interfaceGuid, "APPLE_HOMEKIT_SERVER.BULB.NAME",
                "APPLE_HOMEKIT_SERVER.BULB.DESCRIPTION", 3, int.MaxValue, false);

            factory.CreateNodeTemplate(interfaceGuid, "APPLE_HOMEKIT_SERVER.BULB.NAME",
                "APPLE_HOMEKIT_SERVER.BULB.DESCRIPTION", "light-bulb-folder", DriverGuid, interfaceGuid, false, false,
                true, false, true, NodeDataType.NoAttribute, int.MaxValue, false);

            factory.CreateNodeTemplate(new Guid("37685cd8-0494-4ce4-8c59-1fddcf669aa3"), "APPLE_HOMEKIT_SERVER.BULB.SWITCH.NAME",
                "APPLE_HOMEKIT_SERVER.BULB.SWITCH.DESCRIPTION", "light-bulb-switch", interfaceGuid, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true,
                true, true, false, NodeDataType.Boolean, 1, false);

            factory.CreateNodeTemplate(new Guid("79e7c522-3eaa-49af-8985-44b345e30473"), "APPLE_HOMEKIT_SERVER.BULB.BRIGHTNESS.NAME",
                "APPLE_HOMEKIT_SERVER.BULB.BRIGHTNESS.DESCRIPTION", "light-bulb-brightness", interfaceGuid, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), false, true,
                true, true, false, NodeDataType.Integer, 1, false);

            factory.CreateNodeTemplate(new Guid("515b3a15-9bc5-4121-9f89-36abbb7108c8"), "APPLE_HOMEKIT_SERVER.BULB.HUE.NAME",
                "APPLE_HOMEKIT_SERVER.HUE.BRIGHTNESS.DESCRIPTION", "light-bulb-hue", interfaceGuid, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), false, true,
                true, true, false, NodeDataType.String, 1, false);
        }

        private void CreatePowerOutlet(INodeTemplateFactory factory)
        {
            var interfaceGuid = new Guid("a0809786-de41-40b9-ae7f-076862068464");
            factory.CreateInterfaceType(interfaceGuid, "APPLE_HOMEKIT_SERVER.OUTLET.NAME",
                "APPLE_HOMEKIT_SERVER.OUTLET.DESCRIPTION", 1, int.MaxValue, false);

            factory.CreateNodeTemplate(interfaceGuid, "APPLE_HOMEKIT_SERVER.OUTLET.NAME",
                "APPLE_HOMEKIT_SERVER.OUTLET.DESCRIPTION", "power-outlet-folder", DriverGuid, interfaceGuid, false, false,
                true, false, true, NodeDataType.NoAttribute, int.MaxValue, false);

            factory.CreateNodeTemplate(new Guid("123fb63d-943e-4c8c-8c59-1586eb79b961"), "APPLE_HOMEKIT_SERVER.OUTLET.SWITCH.NAME",
                "APPLE_HOMEKIT_SERVER.OUTLET.SWITCH.DESCRIPTION", "power-outlet-switch", interfaceGuid, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true,
                true, true, false, NodeDataType.Boolean, 1, false);
        }
        private void CreateContactSensor(INodeTemplateFactory factory)
        {
            var interfaceGuid = new Guid("57303efc-252d-4a2f-b5ec-4a6651e9ef0e");
            factory.CreateInterfaceType(interfaceGuid, "APPLE_HOMEKIT_SERVER.CONTACT_SENSOR.NAME",
                "APPLE_HOMEKIT_SERVER.CONTACT_SENSOR.DESCRIPTION", 1, int.MaxValue, false);

            factory.CreateNodeTemplate(interfaceGuid, "APPLE_HOMEKIT_SERVER.CONTACT_SENSOR.NAME",
                "APPLE_HOMEKIT_SERVER.CONTACT_SENSOR.DESCRIPTION", "contact-sensor-folder", DriverGuid, interfaceGuid, false, false,
                true, false, true, NodeDataType.NoAttribute, int.MaxValue, false);

            factory.CreateNodeTemplate(new Guid("e3d05974-6c44-4ac8-9eea-a29e036de343"), "APPLE_HOMEKIT_SERVER.CONTACT_SENSOR.CONTACT.NAME",
                "APPLE_HOMEKIT_SERVER.CONTACT_SENSOR.CONTACT.DESCRIPTION", "contact-sensor", interfaceGuid, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true,
                true, true, false, NodeDataType.Boolean, 1, false);
        }

        private void CreateSwitch(INodeTemplateFactory factory)
        {
            var interfaceGuid = new Guid("f107b325-e7c4-46dc-ae56-f2b99ecc5dbe");
            factory.CreateInterfaceType(interfaceGuid, "APPLE_HOMEKIT_SERVER.SWITCH.NAME",
                "APPLE_HOMEKIT_SERVER.SWITCH.DESCRIPTION", 1, int.MaxValue, false);

            factory.CreateNodeTemplate(interfaceGuid, "APPLE_HOMEKIT_SERVER.SWITCH.NAME",
                "APPLE_HOMEKIT_SERVER.SWITCH.DESCRIPTION", "switch-folder", DriverGuid, interfaceGuid, false, false,
                true, false, true, NodeDataType.NoAttribute, int.MaxValue, false);

            factory.CreateNodeTemplate(new Guid("6e6cd234-b1b6-466f-8bea-2bffba36592f"), "APPLE_HOMEKIT_SERVER.SWITCH.SWITCH.NAME",
                "APPLE_HOMEKIT_SERVER.SWITCH.SWITCH.DESCRIPTION", "switch", interfaceGuid, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true,
                true, true, false, NodeDataType.Boolean, 1, false);
        }
        private void CreateTemperatureSensor(INodeTemplateFactory factory)
        {
            var interfaceGuid = new Guid("98b69750-964f-487b-9195-175f6d976099");
            factory.CreateInterfaceType(interfaceGuid, "APPLE_HOMEKIT_SERVER.TEMPERATURE_SENSOR.NAME",
                "APPLE_HOMEKIT_SERVER.TEMPERATURE_SENSOR.DESCRIPTION", 1, int.MaxValue, false);

            factory.CreateNodeTemplate(interfaceGuid, "APPLE_HOMEKIT_SERVER.TEMPERATURE_SENSOR.NAME",
                "APPLE_HOMEKIT_SERVER.TEMPERATURE_SENSOR.DESCRIPTION", "temperature-sensor-folder", DriverGuid, interfaceGuid, false, false,
                true, false, true, NodeDataType.NoAttribute, int.MaxValue, false);

            factory.CreateNodeTemplate(new Guid("693e8ded-b488-4991-971a-c2b46c2b1dcd"), "APPLE_HOMEKIT_SERVER.TEMPERATURE_SENSOR.VALUE.NAME",
                "APPLE_HOMEKIT_SERVER.TEMPERATURE_SENSOR.VALUE.DESCRIPTION", "temperature-sensor", interfaceGuid, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true,
                true, true, false, NodeDataType.Double, 1, false);
        }
    }
}
