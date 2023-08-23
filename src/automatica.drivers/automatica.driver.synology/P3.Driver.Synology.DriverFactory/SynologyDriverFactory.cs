using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using NodeDataType = Automatica.Core.Base.Templates.NodeDataType;

namespace P3.Driver.Synology.DriverFactory
{
    public class SynologyDriverFactory : Automatica.Core.Driver.DriverFactory
    {
        public static Guid DriverId = new Guid("49a0ee4a-4b01-4560-983f-a1043c2e7de6");
        public static Guid SynologyDevice = new Guid("9f0c88f7-62ec-44e0-840a-966d0aa3aa9e");
        public override string DriverName => "Synology";

        public override Guid DriverGuid => DriverId;

        public override string ImageName => "automaticacore/plugin-p3.driver.synology";


        public override Version DriverVersion => new Version(0, 0, 1, 3);

        public override bool InDevelopmentMode => false;

        public override IDriver CreateDriver(IDriverContext config)
        {
            return new SynologyDriver(config);
        }

        public override void InitNodeTemplates(INodeTemplateFactory factory)
        {
            factory.CreateInterfaceType(DriverGuid, "SYNOLOGY.NAME", "SYNOLOGY.DESCRIPTION", int.MaxValue, 1, true);
            factory.CreateInterfaceType(SynologyDevice, "SYNOLOGY.DEVICE.NAME", "SYNOLOGY.DEVICE.DESCRIPTION", int.MaxValue, 1, false);

            factory.CreateNodeTemplate(DriverGuid, "SYNOLOGY.NAME", "SYNOLOGY.DESCRIPTION", "SYNOLOGY", GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Ethernet),
                DriverGuid, false, true, true, false, true, NodeDataType.NoAttribute, Int32.MaxValue, false);

            factory.CreateNodeTemplate(SynologyDevice, "SYNOLOGY.DEVICE.NAME", "SYNOLOGY.DEVICE.DESCRIPTION", "synology-device",
                DriverGuid, SynologyDevice, false, true,
                true, false, true, NodeDataType.NoAttribute, int.MaxValue, false, true);

            factory.CreatePropertyTemplate(new Guid("59c74166-5556-43d5-bafb-eb0b6c8dff08"), "SYNOLOGY.IP.NAME", "SYNOLOGY.IP.DESCRIPTION", "ip",
                PropertyTemplateType.Ip, SynologyDevice, "COMMON.CATEGORY.ADDRESS", true, false, null, null, 0, 1);
            factory.CreatePropertyTemplate(new Guid("7877c5ef-c6c2-4fb9-92c5-e4248fd83217"), "SYNOLOGY.PORT.NAME", "SYNOLOGY.PORT.DESCRIPTION", "port",
                PropertyTemplateType.Integer, SynologyDevice, "COMMON.CATEGORY.ADDRESS", true, false, null, null, 0, 2);
            factory.CreatePropertyTemplate(new Guid("8b3edfb5-6380-42c1-8233-0620f9571854"), "SYNOLOGY.USE_HTTPS.NAME", "SYNOLOGY.USE_HTTPS.DESCRIPTION", "use_https",
                PropertyTemplateType.Bool, SynologyDevice, "COMMON.CATEGORY.ADDRESS", true, false, null, null, 0, 3);
            factory.CreatePropertyTemplate(new Guid("ce4d5678-649c-46bf-ba0f-6855f4124291"), "SYNOLOGY.USER.NAME", "SYNOLOGY.USER.DESCRIPTION", "user",
                PropertyTemplateType.Text, SynologyDevice, "COMMON.CATEGORY.ADDRESS", true, false, null, null, 0, 4);
            factory.CreatePropertyTemplate(new Guid("9e5ea4d2-fe43-426d-b68d-53abd2788de8"), "SYNOLOGY.PASSWORD.NAME", "SYNOLOGY.PASSWORD.DESCRIPTION", "password",
                PropertyTemplateType.Password, SynologyDevice, "COMMON.CATEGORY.ADDRESS", true, false, null, null, 0, 5);
            factory.CreatePropertyTemplate(new Guid("f24a2aa3-722c-42b6-a9c5-b07f48aaea3c"), "SYNOLOGY.IGNORE_SSL_ERRORS.NAME", "SYNOLOGY.IGNORE_SSL_ERRORS.DESCRIPTION", "ignore_ssl_errors",
                PropertyTemplateType.Bool, SynologyDevice, "COMMON.CATEGORY.ADDRESS", true, false, null, true, 0, 6);

            factory.CreatePropertyTemplate(new Guid("a7758436-1415-4564-a5fe-eb74836939dc"), "SYNOLOGY.USE_REMOTE_CONNECT.NAME", "SYNOLOGY.USE_REMOTE_CONNECT.DESCRIPTION", "use_remote_connect", 
                PropertyTemplateType.Bool, SynologyDevice, "COMMON.CATEGORY.REMOTE", true, false, null, null, 0, 5);
            factory.CreatePropertyTemplate(new Guid("8879d8e6-4e69-4ee4-b54f-27db4db7a1c6"), "SYNOLOGY.REMOTE_CONNECT_DOMAIN.NAME", "SYNOLOGY.REMOTE_CONNECT_DOMAIN.DESCRIPTION", "remote_connect_domain",
                PropertyTemplateType.Text, SynologyDevice, "COMMON.CATEGORY.REMOTE", true, false, null, null, 0, 6);


            factory.CreateNodeTemplate(new Guid("fbdc5300-ba67-406e-a22c-c11019de42af"), "SYNOLOGY.DEVICE.CONNECTED.NAME", "SYNOLOGY.DEVICE.CONNECTED.DESCRIPTION", "synology-device-connected",
                SynologyDevice, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true,
                true, false, true, NodeDataType.Boolean, 1, false, true);

            AddWebStationNodes(factory);

        }

        private void AddWebStationNodes(INodeTemplateFactory factory)
        {
            var dsmWebStationGuid = new Guid("25a06729-049f-409f-9e0e-f731f444db07");
            factory.CreateInterfaceType(dsmWebStationGuid, "SYNOLOGY.DSM.WEBSTATION.NAME", "SYNOLOGY..WEBSTATION.DESCRIPTION", int.MaxValue, 1, false);

            factory.CreateNodeTemplate(dsmWebStationGuid, "SYNOLOGY.DSM.WEBSTATION.NAME", "SYNOLOGY.DSM.WEBSTATION.DESCRIPTION", "synology-dsm-webstation",
                SynologyDevice, dsmWebStationGuid, false, true,
                true, false, true, NodeDataType.NoAttribute, 1, false, true);


            var dsmWebStationConnectedGuid = new Guid("59761e22-17c1-434d-8146-1ea548cde077");
            factory.CreateNodeTemplate(dsmWebStationConnectedGuid, "SYNOLOGY.DSM.WEBSTATION.CONNECTED.NAME", "SYNOLOGY.DSM.WEBSTATION.CONNECTED.DESCRIPTION", "synology-dsm-webstation-connected",
                dsmWebStationGuid, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), false, true,
                true, false, true, NodeDataType.Boolean, 1, false, true);

            factory.CreatePropertyTemplate(new Guid("e31ab036-88dc-4f29-a20b-d906c30bdabe"), "SYNOLOGY.USE_REMOTE_CONNECT.NAME", "SYNOLOGY.USE_REMOTE_CONNECT.DESCRIPTION", "use_remote_connect",
                PropertyTemplateType.Bool, dsmWebStationConnectedGuid, "COMMON.CATEGORY.REMOTE", true, false, null, null, 0, 5);
            factory.CreatePropertyTemplate(new Guid("71b77f1d-0ea9-4448-b432-8884d010aaff"), "SYNOLOGY.REMOTE_CONNECT_DOMAIN.NAME", "SYNOLOGY.REMOTE_CONNECT_DOMAIN.DESCRIPTION", "remote_connect_domain",
                PropertyTemplateType.Text, dsmWebStationConnectedGuid, "COMMON.CATEGORY.REMOTE", true, false, null, null, 0, 6);


        }
    }
}
