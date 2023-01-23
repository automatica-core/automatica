using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using NodeDataType = Automatica.Core.Base.Templates.NodeDataType;

namespace P3.Driver.Pixoo64
{
    public class Pixoo64DriverFactory : DriverFactory
    {
        public override string DriverName => "pixoo64";
        public override Guid DriverGuid => new Guid("235b8a64-853a-498b-a0f9-077bec05eb1f");


        public override Version DriverVersion => new Version(0, 10, 0, 2);

        public override string ImageName => "automaticacore/plugin-p3.driver.pixoo64";
        
        public override string Tag => "latest-develop";

        public override void InitNodeTemplates(INodeTemplateFactory factory)
        {
            factory.CreateInterfaceType(DriverGuid, "PIXOO64.NAME", "PIXOO64.DESCRIPTION", int.MaxValue, 1, true);

            factory.CreateNodeTemplate(DriverGuid, "PIXOO64.NAME", "PIXOO64.DESCRIPTION", "consts", GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Ethernet),
                DriverGuid, false, true, true, false, true, NodeDataType.NoAttribute, Int32.MaxValue, false);
            factory.CreatePropertyTemplate(new Guid("ac051d8d-57dd-4acb-93f9-e101e3cf8bfd"), "COMMON.PROPERTY.IP.NAME", "COMMON.PROPERTY.IP.DESCRIPTION",
                "pixoo64-ip", PropertyTemplateType.Ip, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, "", null, 1, 1);
            factory.CreatePropertyTemplate(new Guid("608154e5-c005-4d59-9aab-07e123a0ad46"), "PIXOO64.PROPERTY.SIZE.NAME", "PIXOO64.PROPERTY.SIZE.DESCRIPTION",
                "pixoo64-size", PropertyTemplateType.Numeric, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, "", 64, 1, 1);



            CreateBatteryScreen(factory);
            CreateCryptoScreen(factory);
            CreateInfoScreen(factory);
            CreateMeterScreen(factory);


        }

        private void CreateBatteryScreen(INodeTemplateFactory factory)
        {
            var batteryGuid = new Guid("81f2daaf-0bb9-4ea5-9c93-1b476f6ee952");
            factory.CreateInterfaceType(batteryGuid, "PIXOO64.BATTERYSCREEN.NAME", "PIXOO64.BATTERYSCREEN.DESCRIPTION",
                int.MaxValue, int.MaxValue, false);
            factory.CreateNodeTemplate(batteryGuid, "PIXOO64.BATTERYSCREEN.NAME", "PIXOO64.BATTERYSCREEN.DESCRIPTION",
                "pixoo64-batteryscreen", DriverGuid, batteryGuid, false, false, true, false, true,
                NodeDataType.NoAttribute, 1, false);
            factory.CreatePropertyTemplate(new Guid("3dc74cfe-2ccf-4866-adee-8cfd705990c5"), "PIXOO64.PROPERTY.SCREENTIME.NAME",
                "PIXOO64.PROPERTY.SCREENTIME.DESCRIPTION", "screen-time", PropertyTemplateType.Integer, batteryGuid,
                "COMMON.CATEGORY.ADDRESS", true, false, PropertyHelper.CreateRangeMetaString(1, 60), 5, 1, 1);

            factory.CreateNodeTemplate(new Guid("9b383768-daae-4a50-ad7a-de6dc40cba6d"), "PIXOO64.BATTERYSCREEN.V1.NAME",
                "PIXOO64.BATTERYSCREEN.V1.DESCRIPTION", "battery-v1", batteryGuid,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, false, true, true, true,
                NodeDataType.Double, 1, false);
            factory.CreateNodeTemplate(new Guid("54c4e99f-a1d7-4a57-b84c-805d03dc090c"), "PIXOO64.BATTERYSCREEN.V2.NAME",
                "PIXOO64.BATTERYSCREEN.V2.DESCRIPTION", "battery-v2", batteryGuid,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, false, true, true, true,
                NodeDataType.Double, 1, false);
            factory.CreateNodeTemplate(new Guid("1f7867f3-fb25-4ff7-8f7e-1df5dcd12652"), "PIXOO64.BATTERYSCREEN.V3.NAME",
                "PIXOO64.BATTERYSCREEN.V3.DESCRIPTION", "battery-v3", batteryGuid,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, false, true, true, true,
                NodeDataType.Double, 1, false);
            factory.CreateNodeTemplate(new Guid("e0f78da6-b3ad-41df-b05d-ece07e1b381c"), "PIXOO64.BATTERYSCREEN.V4.NAME",
                "PIXOO64.BATTERYSCREEN.V4.DESCRIPTION", "battery-v4", batteryGuid,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, false, true, true, true,
                NodeDataType.Double, 1, false);

            factory.CreateNodeTemplate(new Guid("dd3bcb87-ede0-4334-a3b8-2721fc6cd2fe"), "PIXOO64.BATTERYSCREEN.A1.NAME",
                "PIXOO64.BATTERYSCREEN.A1.DESCRIPTION", "battery-a1", batteryGuid,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, false, true, true, true,
                NodeDataType.Double, 1, false);
            factory.CreateNodeTemplate(new Guid("01afbfe2-3827-4cb2-9efd-47ead0408861"), "PIXOO64.BATTERYSCREEN.A2.NAME",
                "PIXOO64.BATTERYSCREEN.A2.DESCRIPTION", "battery-a2", batteryGuid,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, false, true, true, true,
                NodeDataType.Double, 1, false);
            factory.CreateNodeTemplate(new Guid("ad32cd57-b690-44dd-a76d-a41d98f293c3"), "PIXOO64.BATTERYSCREEN.A3.NAME",
                "PIXOO64.BATTERYSCREEN.A3.DESCRIPTION", "battery-a3", batteryGuid,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, false, true, true, true,
                NodeDataType.Double, 1, false);
            factory.CreateNodeTemplate(new Guid("ffd3c202-a68c-4527-8917-2dd1ea7c20aa"), "PIXOO64.BATTERYSCREEN.A4.NAME",
                "PIXOO64.BATTERYSCREEN.A4.DESCRIPTION", "battery-a4", batteryGuid,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, false, true, true, true,
                NodeDataType.Double, 1, false);

            factory.CreateNodeTemplate(new Guid("11f60460-20c2-4ab9-b607-5ef2eb9a595d"), "PIXOO64.BATTERYSCREEN.SOC1.NAME",
                "PIXOO64.BATTERYSCREEN.SOC1.DESCRIPTION", "battery-soc1", batteryGuid,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, false, true, true, true,
                NodeDataType.Double, 1, false);
            factory.CreateNodeTemplate(new Guid("a3c67701-d202-41c7-81e0-ff5413eae511"), "PIXOO64.BATTERYSCREEN.SOC2.NAME",
                "PIXOO64.BATTERYSCREEN.SOC2.DESCRIPTION", "battery-soc2", batteryGuid,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, false, true, true, true,
                NodeDataType.Double, 1, false);
            factory.CreateNodeTemplate(new Guid("0ade57f8-6e64-4490-ae01-d9e609f99a85"), "PIXOO64.BATTERYSCREEN.SOC3.NAME",
                "PIXOO64.BATTERYSCREEN.SOC3.DESCRIPTION", "battery-soc3", batteryGuid,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, false, true, true, true,
                NodeDataType.Double, 1, false);
            factory.CreateNodeTemplate(new Guid("6dae9de8-fff2-4774-8b10-8b6c35f5e342"), "PIXOO64.BATTERYSCREEN.SOC4.NAME",
                "PIXOO64.BATTERYSCREEN.SOC4.DESCRIPTION", "battery-soc4", batteryGuid,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, false, true, true, true,
                NodeDataType.Double, 1, false);
        }

        private void CreateCryptoScreen(INodeTemplateFactory factory)
        {
            var cryptoGuid = new Guid("9655afb5-127d-4fa1-9583-ef1fb56efe51");
            factory.CreateInterfaceType(cryptoGuid, "PIXOO64.CRYPTOSCREEN.NAME", "PIXOO64.CRYPTOSCREEN.DESCRIPTION",
                int.MaxValue, 1, false);
            factory.CreateNodeTemplate(cryptoGuid, "PIXOO64.CRYPTOSCREEN.NAME", "PIXOO64.CRYPTOSCREEN.DESCRIPTION",
                "pixoo64-crytoscreen", DriverGuid, cryptoGuid, false, false, true, false, true,
                NodeDataType.NoAttribute, 1, false);
            factory.CreatePropertyTemplate(new Guid("09919d84-5aad-435f-8bd8-d124f8d917a0"), "PIXOO64.PROPERTY.SCREENTIME.NAME",
                "PIXOO64.PROPERTY.SCREENTIME.DESCRIPTION", "screen-time", PropertyTemplateType.Integer, cryptoGuid,
                "COMMON.CATEGORY.ADDRESS", true, false, PropertyHelper.CreateRangeMetaString(1, 60), 5, 1, 1);



            factory.CreateNodeTemplate(new Guid("9430940a-49ce-4ada-b59b-6d59cb477ac5"), "PIXOO64.CRYPTOSCREEN.BITCOIN.NAME",
                "PIXOO64.CRYPTOSCREEN.BITCOIN.DESCRIPTION", "crypto-bitcoin", cryptoGuid,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, false, true, true, true,
                NodeDataType.Double, 1, false);
            factory.CreateNodeTemplate(new Guid("cf7ba198-2290-40a8-913e-0ec6a482ff9d"), "PIXOO64.CRYPTOSCREEN.ETHEREUM.NAME",
                "PIXOO64.CRYPTOSCREEN.ETHEREUM.DESCRIPTION", "crypto-ethereum", cryptoGuid,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, false, true, true, true,
                NodeDataType.Double, 1, false);
            factory.CreateNodeTemplate(new Guid("6f1e7f93-4e84-4626-81d0-6473f49c6726"), "PIXOO64.CRYPTOSCREEN.CARDANO.NAME",
                "PIXOO64.CRYPTOSCREEN.CARDANO.DESCRIPTION", "crypto-cardano", cryptoGuid,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, false, true, true, true,
                NodeDataType.Double, 1, false);
        }

        private void CreateInfoScreen(INodeTemplateFactory factory)
        {
            var infoScreen = new Guid("628cae5c-da5b-457a-be86-c215d998549b");
            factory.CreateInterfaceType(infoScreen, "PIXOO64.INFOSCREEN.NAME", "PIXOO64.INFOSCREEN.DESCRIPTION",
                int.MaxValue, 1, false);
            factory.CreateNodeTemplate(infoScreen, "PIXOO64.INFOSCREEN.NAME", "PIXOO64.INFOSCREEN.DESCRIPTION",
                "pixoo64-infoscreen", DriverGuid, infoScreen, false, false, true, false, true,
                NodeDataType.NoAttribute, 1, false);
            factory.CreatePropertyTemplate(new Guid("85da291f-fc6c-49f1-8a7e-6096f3f1eb14"), "PIXOO64.PROPERTY.SCREENTIME.NAME",
                "PIXOO64.PROPERTY.SCREENTIME.DESCRIPTION", "screen-time", PropertyTemplateType.Integer, infoScreen,
                "COMMON.CATEGORY.ADDRESS", true, false, PropertyHelper.CreateRangeMetaString(1, 60), 5, 1, 1);



            factory.CreateNodeTemplate(new Guid("c2cae9d7-3ce9-41e4-8398-fd327ced88bd"), "PIXOO64.INFOSCREEN.OUTSIDE.NAME",
                "PIXOO64.INFOSCREEN.OUTSIDE.DESCRIPTION", "info-outside", infoScreen,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, false, true, true, true,
                NodeDataType.Double, 1, false);
            factory.CreateNodeTemplate(new Guid("5aa2976d-aab5-41f8-abcf-de9fc8bf4d09"), "PIXOO64.INFOSCREEN.INSIDE.NAME",
                "PIXOO64.INFOSCREEN.INSIDE.DESCRIPTION", "info-inside", infoScreen,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, false, true, true, true,
                NodeDataType.Double, 1, false);
        }

        private void CreateMeterScreen(INodeTemplateFactory factory)
        {
            var meterScreen = new Guid("f511f977-f238-4f41-8e4c-e2dbdcaa9256");
            factory.CreateInterfaceType(meterScreen, "PIXOO64.METERSCREEN.NAME", "PIXOO64.METERSCREEN.DESCRIPTION",
                int.MaxValue, int.MaxValue, false);
            factory.CreateNodeTemplate(meterScreen, "PIXOO64.METERSCREEN.NAME", "PIXOO64.METERSCREEN.DESCRIPTION",
                "pixoo64-meterscreen", DriverGuid, meterScreen, false, false, true, false, true,
                NodeDataType.NoAttribute, 1, false);
            factory.CreatePropertyTemplate(new Guid("f3e70c6e-b7a5-4ed1-aef1-0f05acbc9c3a"), "PIXOO64.PROPERTY.SCREENTIME.NAME",
                "PIXOO64.PROPERTY.SCREENTIME.DESCRIPTION", "screen-time", PropertyTemplateType.Integer, meterScreen,
                "COMMON.CATEGORY.ADDRESS", true, false, PropertyHelper.CreateRangeMetaString(1, 60), 5, 1, 1);


            var meter1 = new Guid("d00b83d3-c739-4349-99a5-93df79d2627b");
            factory.CreateNodeTemplate(meter1, "PIXOO64.METERSCREEN.METER1.NAME",
                "PIXOO64.METERSCREEN.METER1.DESCRIPTION", "meterscreen-meter1", meterScreen,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, false, true, true, true,
                NodeDataType.Double, 1, false);
            factory.CreatePropertyTemplate(new Guid("884d3e79-1b83-46cb-afc0-538be561ebee"), "PIXOO64.METERSCREEN.METER.NAME",
                "PIXOO64.METERSCREEN.METER.DESCRIPTION", "meter-name", PropertyTemplateType.Text, meter1,
                "COMMON.CATEGORY.ADDRESS", true, false, "", "Meter1", 1, 1);

            var meter2 = new Guid("810333a3-bc65-4c97-babf-3759b5402ae4");
            factory.CreateNodeTemplate(meter2, "PIXOO64.METERSCREEN.METER2.NAME",
                "PIXOO64.METERSCREEN.METER2.DESCRIPTION", "meterscreen-meter2", meterScreen,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, false, true, true, true,
                NodeDataType.Double, 1, false);
            factory.CreatePropertyTemplate(new Guid("a8497945-493f-4bb6-99a3-c2f16e960d3a"), "PIXOO64.METERSCREEN.METER.NAME",
                "PIXOO64.METERSCREEN.METER.DESCRIPTION", "meter-name", PropertyTemplateType.Text, meter2,
                "COMMON.CATEGORY.ADDRESS", true, false, "", "Meter2", 1, 1);

            var meter3 = new Guid("44200deb-e0ab-4086-ae7f-8eb237b69589");
            factory.CreateNodeTemplate(meter3, "PIXOO64.METERSCREEN.METER3.NAME",
                "PIXOO64.METERSCREEN.METER3.DESCRIPTION", "meterscreen-meter3", meterScreen,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, false, true, true, true,
                NodeDataType.Double, 1, false);
            factory.CreatePropertyTemplate(new Guid("4aefd5bc-4830-4cd7-a91b-d02ef127b6a9"), "PIXOO64.METERSCREEN.METER.NAME",
                "PIXOO64.METERSCREEN.METER.DESCRIPTION", "meter-name", PropertyTemplateType.Text, meter3,
                "COMMON.CATEGORY.ADDRESS", true, false, "", "Meter3", 1, 1);

            var meter4 = new Guid("3eb1b910-7978-419f-ad5b-8fb51510e7b8");
            factory.CreateNodeTemplate(meter4, "PIXOO64.METERSCREEN.METER4.NAME",
                "PIXOO64.METERSCREEN.METER4.DESCRIPTION", "meterscreen-meter4", meterScreen,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, false, true, true, true,
                NodeDataType.Double, 1, false);
            factory.CreatePropertyTemplate(new Guid("4a3afd16-7a69-4f5d-a0ed-2e48134fed01"), "PIXOO64.METERSCREEN.METER.NAME",
                "PIXOO64.METERSCREEN.METER.DESCRIPTION", "meter-name", PropertyTemplateType.Text, meter4,
                "COMMON.CATEGORY.ADDRESS", true, false, "", "Meter4", 1, 1);

            var meter5 = new Guid("78e88cdd-187a-4665-a8b5-9e3eac72a3cb");
            factory.CreateNodeTemplate(meter5, "PIXOO64.METERSCREEN.METER5.NAME",
                "PIXOO64.METERSCREEN.METER5.DESCRIPTION", "meterscreen-meter5", meterScreen,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, false, true, true, true,
                NodeDataType.Double, 1, false);
            factory.CreatePropertyTemplate(new Guid("290d21fc-a941-4942-8bea-6be9f9414e91"), "PIXOO64.METERSCREEN.METER.NAME",
                "PIXOO64.METERSCREEN.METER.DESCRIPTION", "meter-name", PropertyTemplateType.Text, meter5,
                "COMMON.CATEGORY.ADDRESS", true, false, "", "Meter5", 1, 1);


            var meter6 = new Guid("d4e21c29-5316-496c-a7d8-5aaf3a223698");
            factory.CreateNodeTemplate(meter6, "PIXOO64.METERSCREEN.METER6.NAME",
                "PIXOO64.METERSCREEN.METER6.DESCRIPTION", "meterscreen-meter6", meterScreen,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, false, true, true, true,
                NodeDataType.Double, 1, false);
            factory.CreatePropertyTemplate(new Guid("e2c52b88-7f3b-4de6-97eb-bace20e85b11"), "PIXOO64.METERSCREEN.METER.NAME",
                "PIXOO64.METERSCREEN.METER.DESCRIPTION", "meter-name", PropertyTemplateType.Text, meter6,
                "COMMON.CATEGORY.ADDRESS", true, false, "", "Meter6", 1, 1);
        }

        public override IDriver CreateDriver(IDriverContext config)
        {
            return new Pixoo64Driver(config);
        }

        public override void AfterSave(NodeInstance instance)
        {
            // do nothing
        }
    }
}
