using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using P3.Driver.ModBusDriver;
using P3.Driver.ModBusDriverFactory.Slave;
using NodeDataType = Automatica.Core.Base.Templates.NodeDataType;

namespace P3.Driver.ModBusDriverFactory
{
    public class ModBusRtuSlaveDriverFactory : ModBusDriverFactory
    {
        public override string DriverName => "ModBus.Rtu.Slave";
        public override Guid DriverGuid => new Guid("7a284855-4435-4c38-8144-bb7908d4ccf6");
        public override Version DriverVersion => new Version(1, 6, 0, 2);
        public override string ImageName => "automaticacore/plugin-p3.driver.modbus";

        public override void InitTemplates(INodeTemplateFactory factory)
        {
            factory.CreateNodeTemplate(DriverGuid, "MODBUS.SLAVE.RTU.NAME", "MODBUS.SLAVE.RTU.DESCRIPTION", "modbus-slave-rtu", GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Rs485),
                DeviceInterface, false, false, true, false, true, NodeDataType.NoAttribute, int.MaxValue, false);
            factory.CreatePropertyTemplate(new Guid("0e84243a-a320-4f33-82ab-d108660322d7"), "COMMON.PROPERTY.PORT.NAME", "COMMON.PROPERTY.PORT.DESCRIPTION", "modbus-port",
                PropertyTemplateType.Interface, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, "", "", 0, 0);

            factory.CreatePropertyTemplate(new Guid("54fc2c02-42a8-4409-bb85-55dd6d0352b4"), "COMMON.PROPERTY.BAUDRATE.NAME", "COMMON.PROPERTY.BAUDRATE.DESCRIPTION", "modbus-baudrate",
                PropertyTemplateType.Baudrate, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, "", 9600, 0, 1);

            factory.CreatePropertyTemplate(new Guid("3ddc6040-d66e-46e9-8ab2-a749397e1b82"), "COMMON.PROPERTY.DATABITS.NAME", "COMMON.PROPERTY.DATABITS.DESCRIPTION", "modbus-databits",
                PropertyTemplateType.Baudrate, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, "", 8, 0, 2);

            factory.CreatePropertyTemplate(new Guid("2046dab3-bd27-4c77-becf-92fc7b7fab9e"), "COMMON.PROPERTY.PARITY.NAME", "COMMON.PROPERTY.PARITY.DESCRIPTION", "modbus-parity",
                PropertyTemplateType.Parity, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, "", "even", 0, 3);

            factory.CreatePropertyTemplate(new Guid("912806e1-81dd-47da-8369-3d13e8c0580f"), "COMMON.PROPERTY.STOPBITS.NAME", "COMMON.PROPERTY.STOPBITS.DESCRIPTION", "modbus-stopbits",
                PropertyTemplateType.Baudrate, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, "", 1, 0, 4);

            factory.CreatePropertyTemplate(new Guid("091a44cd-9f58-4769-9341-bb84329f51ba"), "type", "type", "modbus-type", PropertyTemplateType.Text,
                DriverGuid, "", false, true, null, "MODBUS-SLAVE", 0, 0);


        }

        public override IDriver CreateDriver(IDriverContext config)
        {
            ModBus.Logger = config.Logger;
            return new ModBusSlaveDriver(config, config.Logger, false);
        }
    }
}
