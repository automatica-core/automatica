using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using NodeDataType = Automatica.Core.Base.Templates.NodeDataType;

namespace P3.Driver.PingFactory
{
    public class PingFactory : DriverFactory
    {
        public static Guid DriverId = new Guid("c69ed1ad-4805-4c0d-be8e-a112c332e4ec");
        public static Guid PingDevice = new Guid("840b2245-81a1-4d86-838d-1bfd1b57addc");
        public override string DriverName => "Ping";

        public override Guid DriverGuid => DriverId;

        public override string ImageName => "automaticacore/plugin-p3.driver.ping";


        public override Version DriverVersion => new Version(0, 0, 0, 2);

        public override bool InDevelopmentMode => false;

        public override IDriver CreateDriver(IDriverContext config)
        {
            return new PingDriver(config);
        }

        public override void InitNodeTemplates(INodeTemplateFactory factory)
        {
            factory.CreateInterfaceType(DriverGuid, "PING.NAME", "PING.DESCRIPTION", int.MaxValue, 1, true);
            factory.CreateNodeTemplate(DriverGuid, "PING.NAME", "PING.DESCRIPTION", "PING", GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Ethernet),
                DriverGuid, false, true, true, false, true, NodeDataType.NoAttribute, Int32.MaxValue, false);

            
            factory.CreateNodeTemplate(PingDevice, "PING.DEVICE.NAME", "PING.DEVICE.DESCRIPTION", "ping-device",
                DriverGuid, GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), false, true,
                true, false, true, NodeDataType.Boolean, int.MaxValue, false, true);

            factory.CreatePropertyTemplate(new Guid("33f1648e-0886-4664-a698-9018f1bf3d5a"), "PING.MIN_SUCCESS.NAME", "PING.MIN_SUCCESS.DESCRIPTION", "min_success",
                PropertyTemplateType.Range, PingDevice, "COMMON.CATEGORY.ADDRESS", true, false, PropertyHelper.CreateRangeMetaString(1, 5000), 3, 0, 0);

            factory.CreatePropertyTemplate(new Guid("ed76bce9-de87-4cba-9a07-9e3c8c80dc9f"), "PING.IP.NAME", "PING.IP.DESCRIPTION", "ip",
                PropertyTemplateType.Text, PingDevice, "COMMON.CATEGORY.ADDRESS", true, false, null, null, 0, 1);

            factory.CreatePropertyTemplate(new Guid("e28fa8fb-2bf5-49f1-9f70-ad96d863d90c"), "PING.INTERVAL.NAME", "PING.INTERVAL.DESCRIPTION", "interval",
                PropertyTemplateType.Range, PingDevice, "COMMON.CATEGORY.ADDRESS", true, false, PropertyHelper.CreateRangeMetaString(5, 300), 5, 0, 2);

            factory.CreatePropertyTemplate(new Guid("ac3676e5-11cf-432c-871b-ab9a1ac6815d"), "PING.TIMEOUT.NAME", "PING.TIMEOUT.DESCRIPTION", "timeout",
                PropertyTemplateType.Range, PingDevice, "COMMON.CATEGORY.ADDRESS", true, false, PropertyHelper.CreateRangeMetaString(1024, 5000), 1024, 0, 3);
       
        }
    }
}
