using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using P3.Driver.ModBusDriver;
using P3.Driver.ModBusDriverFactory.Slave;
using NodeDataType = Automatica.Core.Base.Templates.NodeDataType;

namespace P3.Driver.ModBusDriverFactory
{
    public class ModBusTcpSlaveDriverFactory : ModBusDriverFactory
    {
        public override string DriverName => "ModBus.Tcp.Slave";
        public override Guid DriverGuid => new Guid("d80ebf9d-c6a9-4554-8ff5-cbeb2cef7acd");
        public override string ImageName => "automaticacore/plugin-p3.driver.modbus";
        public override Version DriverVersion => new Version(1, 2, 0, 3);
        public override void InitTemplates(INodeTemplateFactory factory)
        {
            factory.CreateNodeTemplate(DriverGuid, "MODBUS.SLAVE.TCP.NAME", "MODBUS.SLAVE.TCP.DESCRIPTION", "modbus-slave-tcp", GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Ethernet),
                DeviceInterface, false, false, true, false, true, NodeDataType.NoAttribute, int.MaxValue, false);

            factory.CreatePropertyTemplate(new Guid("8c7eb99d-93db-4544-9ed3-2178a8ccd1e7"), "COMMON.PROPERTY.IPPORT.NAME", "COMMON.PROPERTY.IPPORT.DESCRIPTION", "modbus-port", PropertyTemplateType.Range,
                DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, PropertyHelper.CreateRangeMetaString(1, ushort.MaxValue), 502, 1, 1);

            factory.CreatePropertyTemplate(new Guid("d04ab318-dc1b-46ee-ab88-bcc4b4d9d438"), "MODBUS.PROPERTY.IGNORE_DEVICE_ID.NAME", "MODBUS.PROPERTY.IGNORE_DEVICE_ID.DESCRIPTION", "modbus-ignoreDeviceId", PropertyTemplateType.Bool,
                DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, "", true, 1, 1);

            factory.CreatePropertyTemplate(new Guid("ee6faef8-65a7-4c77-b653-28db70a6152d"), "type", "type", "modbus-type", PropertyTemplateType.Text,
                DriverGuid, "405e97a3-c039-426f-80f6-4be1554f154b", false, true, null, "MODBUS-SLAVE", 0, 0);
        }

        public override IDriver CreateDriver(IDriverContext config)
        {
            ModBus.Logger = config.Logger;
            return new ModBusSlaveDriver(config, config.Logger);
        }
    }
}
