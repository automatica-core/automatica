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
        public override Version DriverVersion => new Version(1, 3, 0, 0);
        public override void InitTemplates(INodeTemplateFactory factory)
        {
            factory.CreateNodeTemplate(DriverGuid, "MODBUS.SLAVE.TCP.NAME", "MODBUS.SLAVE.TCP.DESCRIPTION", "modbus-slave-tcp", GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Ethernet),
                DeviceInterface, false, false, true, false, true, NodeDataType.NoAttribute, int.MaxValue, false);

            factory.CreatePropertyTemplate(new Guid("8c7eb99d-93db-4544-9ed3-2178a8ccd1e7"), "COMMON.PROPERTY.IPPORT.NAME", "COMMON.PROPERTY.IPPORT.DESCRIPTION", "modbus-port", PropertyTemplateType.Range,
                DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, PropertyHelper.CreateRangeMetaString(1, ushort.MaxValue), 502, 1, 1);

            factory.CreatePropertyTemplate(new Guid("d04ab318-dc1b-46ee-ab88-bcc4b4d9d438"), "MODBUS.PROPERTY.IGNORE_DEVICE_ID.NAME", "MODBUS.PROPERTY.IGNORE_DEVICE_ID.DESCRIPTION", "modbus-ignoreDeviceId", PropertyTemplateType.Bool,
                DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, "", true, 1, 1);

            factory.CreatePropertyTemplate(new Guid("3a9c5e1d-7b2f-4d8a-9e6c-0f1b2a3c4d61"), "COMMON.PROPERTY.IP.NAME", "MODBUS.PROPERTY.BIND_ADDRESS.DESCRIPTION", "modbus-bind-address", PropertyTemplateType.Ip,
                DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, null, "0.0.0.0", 1, 0);

            factory.CreatePropertyTemplate(new Guid("5b0d6f2e-8c3a-4e9b-af7d-1a2c3b4d5e62"), "MODBUS.PROPERTY.ALLOWED_CLIENTS.NAME", "MODBUS.PROPERTY.ALLOWED_CLIENTS.DESCRIPTION", "modbus-allowed-clients", PropertyTemplateType.Text,
                DriverGuid, "MODBUS.CATEGORY.SECURITY", true, false, null, "", 2, 0);

            factory.CreatePropertyTemplate(new Guid("7c1e7a3f-9d4b-4fac-b08e-2b3d4c5e6f63"), "MODBUS.PROPERTY.MAX_CONNECTIONS.NAME", "MODBUS.PROPERTY.MAX_CONNECTIONS.DESCRIPTION", "modbus-max-connections", PropertyTemplateType.Range,
                DriverGuid, "MODBUS.CATEGORY.SECURITY", true, false, PropertyHelper.CreateRangeMetaString(1, 256), 16, 2, 1);

            factory.CreatePropertyTemplate(new Guid("9d2f8b4a-ae5c-4b0d-c19f-3c4e5d6f7a64"), "MODBUS.PROPERTY.IDLE_TIMEOUT.NAME", "MODBUS.PROPERTY.IDLE_TIMEOUT.DESCRIPTION", "modbus-idle-timeout", PropertyTemplateType.Range,
                DriverGuid, "MODBUS.CATEGORY.SECURITY", true, false, PropertyHelper.CreateRangeMetaString(5, 3600), 120, 2, 2);

            factory.CreatePropertyTemplate(new Guid("be3a9c5b-bf6d-4c1e-d2a0-4d5f6e7a8b65"), "MODBUS.PROPERTY.READ_ONLY.NAME", "MODBUS.PROPERTY.READ_ONLY.DESCRIPTION", "modbus-read-only", PropertyTemplateType.Bool,
                DriverGuid, "MODBUS.CATEGORY.SECURITY", true, false, "", false, 2, 3);

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
