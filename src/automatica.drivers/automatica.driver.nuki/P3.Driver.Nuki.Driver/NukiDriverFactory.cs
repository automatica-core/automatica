using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using NodeDataType = Automatica.Core.Base.Templates.NodeDataType;

namespace P3.Driver.Nuki.Driver
{
    public class NukiDriverFactory : DriverFactory
    {
        public override string DriverName => "Nuki";

        public override Guid DriverGuid => new Guid("da739d07-56a6-4cd3-a3dc-ac7c5d2d4fe5");

        public override Version DriverVersion => new Version(1, 0, 0, 0);

        public override string ImageName => "automaticacore/plugin-p3.driver.nuki";

        public override bool InDevelopmentMode => false;
        public override IDriver CreateDriver(IDriverContext config)
        {
            return new NukiDriver(config);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="factory"></param>9
        public override void InitNodeTemplates(INodeTemplateFactory factory)
        {
            factory.CreateInterfaceType(DriverGuid, "NUKI.NAME", "NUKI.DESCRIPTION", int.MaxValue, 1, true);

            factory.CreateNodeTemplate(DriverGuid, "NUKI.NAME", "NUKI.DESCRIPTION", "NUKI", 
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Virtual), DriverGuid, false, false, true, false, true, NodeDataType.NoAttribute, 1, false);

            factory.CreatePropertyTemplate(new Guid("243b2e63-21b4-40e0-8e4d-295bb0a4894c"), "NUKI.POLL_INTERVAL.NAME", "NUKI.POLL_INTERVAL.DESCRIPTION", "poll",
                PropertyTemplateType.Range, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, PropertyHelper.CreateRangeMetaString(TimeSpan.FromSeconds(1).TotalSeconds, TimeSpan.FromHours(2).TotalSeconds), TimeSpan.FromMinutes(2).TotalSeconds, 1, 1);

            factory.CreatePropertyTemplate(new Guid("d6d2f651-70e5-4bbc-9a7a-baf5576cfbae"), "NUKI.IP_ADDRESS.NAME", "NUKI.IP_ADDRESS.DESCRIPTION", "ip",
                PropertyTemplateType.Ip, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, null, null, 1, 2);

            factory.CreatePropertyTemplate(new Guid("d24e0269-305d-47c0-b10c-1d13397e4e94"), "NUKI.PORT.NAME", "NUKI.PORT.DESCRIPTION", "port",
                PropertyTemplateType.Integer, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, null, 8080, 1, 3);

            factory.CreatePropertyTemplate(new Guid("2260ec26-9f88-4c90-91f8-072a852c92a3"), "NUKI.ACCESS_TOKEN.NAME", "NUKI.ACCESS_TOKEN.DESCRIPTION", "token",
                PropertyTemplateType.Password, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, null, null, 1, 4);

        }

    }
}
