using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using P3.Driver.ModBusDriver;
using P3.Driver.ModBusDriverFactory.Master;
using NodeDataType = Automatica.Core.Base.Templates.NodeDataType;

namespace P3.Driver.ModBusDriverFactory
{
    public class ModBusRtuMasterDriverFactory : ModBusDriverFactory
    {
        public override string DriverName => "ModBus.Rtu.Master";
        public override Guid DriverGuid => new Guid("3e967c47-dad2-4ce5-9a42-7a346facd2fb");

        public override string ImageName => "automaticacore/plugin-p3.driver.modbus";
        public override Version DriverVersion => new Version(1, 6, 0, 2);

        public override void InitTemplates(INodeTemplateFactory factory)
        {
            factory.CreateNodeTemplate(DriverGuid, "MODBUS.MASTER.RTU.NAME", "MODBUS.MASTER.RTU.DESCRIPTION", "modbus-master-rtu", GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Rs485),
                DeviceInterface, false, false, true, false, true, NodeDataType.NoAttribute, int.MaxValue, false);

            factory.CreatePropertyTemplate(new Guid("f94b50da-c1f1-463f-a3ec-bd61a6ecc514"), "COMMON.PROPERTY.BAUDRATE.NAME", "COMMON.PROPERTY.BAUDRATE.DESCRIPTION", "modbus-baudrate",
                PropertyTemplateType.Baudrate, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, "", 9600, 0, 0);

            factory.CreatePropertyTemplate(new Guid("65ce8f30-a769-43bb-88d8-edbe5e4fa267"), "COMMON.PROPERTY.PARITY.NAME", "COMMON.PROPERTY.PARITY.DESCRIPTION", "modbus-parity",
                PropertyTemplateType.Parity, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, "", "even", 0, 0);

            factory.CreatePropertyTemplate(new Guid("208cd361-7b8a-46b8-93b3-4f0245443264"), "COMMON.PROPERTY.DATABITS.NAME", "COMMON.PROPERTY.DATABITS.DESCRIPTION", "modbus-databits",
                PropertyTemplateType.Baudrate, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, "", 8, 0, 0);

            factory.CreatePropertyTemplate(new Guid("4ff96f51-c06b-496e-998b-1c0fb411dc79"), "COMMON.PROPERTY.STOPBITS.NAME", "COMMON.PROPERTY.STOPBITS.DESCRIPTION", "modbus-stopbits",
                PropertyTemplateType.Stopbits, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, "", 1, 0, 0);


            factory.CreatePropertyTemplate(new Guid("79a827b1-e175-4ad6-9151-1b9c402237cd"), "type", "type", "modbus-type", PropertyTemplateType.Text,
                DriverGuid, "bca58c9f-55c0-4713-971e-42b776b05d4e", false, true, null, "MODBUS-MASTER", 0, 0);


            factory.CreatePropertyTemplate(new Guid("ee02ffdd-26ee-4544-8090-517a468f67aa"), "COMMON.PROPERTY.ADDRESS.NAME", "COMMON.PROPERTY.ADDRESS.DESCRIPTION", "modbus-port",
                PropertyTemplateType.Interface, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, "", 1, 0, 0);

        }

        public override IDriver CreateDriver(IDriverContext config)
        {
            ModBus.Logger = config.Logger;
            return new ModBusMasterDriver(config, false);
        }
    }
}
