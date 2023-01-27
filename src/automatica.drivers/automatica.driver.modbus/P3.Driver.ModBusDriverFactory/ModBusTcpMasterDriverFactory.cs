using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using P3.Driver.ModBusDriver;
using P3.Driver.ModBusDriverFactory.Master;
using NodeDataType = Automatica.Core.Base.Templates.NodeDataType;

namespace P3.Driver.ModBusDriverFactory
{
    public class ModBusTcpMasterDriverFactory : ModBusDriverFactory
    {
        public override string DriverName => "ModBus.Tcp.Master";

        public static readonly Guid ModBusMasterTcpGuid = new Guid("c7854e79-4483-4131-8c26-99ed227f2163");
        public override Guid DriverGuid => ModBusMasterTcpGuid;
        public override string ImageName => "automaticacore/plugin-p3.driver.modbus";

        public override Version DriverVersion => new Version(1, 1, 0, 2);

        public override void InitTemplates(INodeTemplateFactory factory)
        {
            factory.CreateNodeTemplate(DriverGuid, "MODBUS.MASTER.TCP.NAME", "MODBUS.MASTER.TCP.DESCRIPTION", "modbus-master-tcp", GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Ethernet),
                DeviceInterface, false, false, true, false, true, NodeDataType.NoAttribute, int.MaxValue, false);

            factory.CreatePropertyTemplate(new Guid("a461409d-fc55-4efc-a82f-bca85c446735"), "COMMON.PROPERTY.IP.NAME", "COMMON.PROPERTY.IP.DESCRIPTION",
                "modbus-master-ip", PropertyTemplateType.Ip, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, "", null, 1, 1);
            factory.CreatePropertyTemplate(new Guid("d5bf4e69-23a3-4c31-abf1-3b3d077929b2"), "COMMON.PROPERTY.IPPORT.NAME", "COMMON.PROPERTY.IPPORT.DESCRIPTION",
                "modbus-master-port", PropertyTemplateType.Range, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, PropertyHelper.CreateRangeMetaString(1, ushort.MaxValue), 502, 1, 1);

            factory.CreatePropertyTemplate(new Guid("b1496d3c-b9d9-47f1-a54b-f9b51ff1e3db"), "type", "type", "modbus-type", PropertyTemplateType.Text,
                DriverGuid, "ae868066-c472-4a9b-ac3a-dd9f2ff563db", false, true, null, "MODBUS-MASTER", 0, 0);
        }

        public override IDriver CreateDriver(IDriverContext config)
        {
            ModBus.Logger = config.Logger;
            return new ModBusMasterDriver(config, true);
        }
    }
}
