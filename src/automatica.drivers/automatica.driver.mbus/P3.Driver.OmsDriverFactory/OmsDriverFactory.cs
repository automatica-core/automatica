using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using NodeDataType = Automatica.Core.Base.Templates.NodeDataType;

namespace P3.Driver.OmsDriverFactory
{
    public class OmsDriverFactory : DriverFactory
    {
        public override string DriverName => "OMS.MBus";

        public static readonly Guid DeviceDriverGuid = new Guid("aa514f7d-4cc0-44a0-831d-26493dd159d4");
        public override Guid DriverGuid => DeviceDriverGuid;

        public override string ImageName => "automaticacore/plugin-p3.driver.oms";
        public override Version DriverVersion => new Version(0, 1, 0, 2);
        public override void InitNodeTemplates(INodeTemplateFactory factory)
        {
            var interfaceGuid = new Guid("49be612a-3331-454c-8027-3db1b3674024");
            factory.CreateInterfaceType(interfaceGuid, "MBUS-OMS.DEVICE.NAME", "MBUS-OMS.DEVICE.DESCRIPTION", 1, 1,
                true);

            factory.CreateNodeTemplate(DriverGuid, "MBUS-OMS.DEVICE.NAME", "MBUS-OMS.DEVICE.DESCRIPTION",
                "mbus-oms-device", GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.UsbIr), interfaceGuid, false, false, true, false, true,
                NodeDataType.NoAttribute, 1, false);

            factory.CreatePropertyTemplate(new Guid("15e87af7-963a-4386-aee5-7d5afe4b2e01"), "MBUS-OMS.DEVICE.PORT.NAME",
                "MBUS-OMS.DEVICE.PORT.DESCRIPTION", "mbus-oms-port", PropertyTemplateType.UsbPort, DriverGuid,
                "COMMON.CATEGORY.ADDRESS", true, false, null, null, 1, 1);

            factory.CreatePropertyTemplate(new Guid("cd8a6277-a8d3-4051-b757-4aa18a88aa3c"), "MBUS-OMS.DEVICE.KEY.NAME",
                "MBUS-OMS.DEVICE.KEY.DESCRIPTION", "mbus-oms-key", PropertyTemplateType.Text, DriverGuid,
                "COMMON.CATEGORY.ADDRESS", true, false, null, null, 1, 1);

            factory.CreateNodeTemplate(new Guid("49250543-3dac-43dd-abef-42fa6982189e"), "MBUS-OMS.DEVICE.DATETIME.NAME", "MBUS-OMS.DEVICE.DATETIME.DESCRIPTION", "mbus-oms-datetime", interfaceGuid,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.DateTime, 1, false, false);

            factory.CreateNodeTemplate(new Guid("cc736fc9-48e9-43aa-bc6e-c4a0356b77cb"), "MBUS-OMS.DEVICE.ENERGY_A+.NAME", "MBUS-OMS.DEVICE.ENERGY_A+.DESCRIPTION", "mbus-oms-a+", interfaceGuid,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.Double, 1, false, false);

            factory.CreateNodeTemplate(new Guid("bdfbaff0-c3c6-4447-b02a-7c42b7ff413f"), "MBUS-OMS.DEVICE.ENERGY_A-.NAME", "MBUS-OMS.DEVICE.ENERGY_A-.DESCRIPTION", "mbus-oms-a-", interfaceGuid,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.Double, 1, false, false);

            factory.CreateNodeTemplate(new Guid("34ce3377-49b1-4a87-8b5e-fd03d2c40363"), "MBUS-OMS.DEVICE.ENERGY_R+.NAME", "MBUS-OMS.DEVICE.ENERGY_R+.DESCRIPTION", "mbus-oms-r+", interfaceGuid,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.Double, 1, false, false);

            factory.CreateNodeTemplate(new Guid("887d9211-ed89-4aeb-846c-40cc53ef6345"), "MBUS-OMS.DEVICE.ENERGY_R-.NAME", "MBUS-OMS.DEVICE.ENERGY_R-.DESCRIPTION", "mbus-oms-r-", interfaceGuid,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.Double, 1, false, false);

            factory.CreateNodeTemplate(new Guid("3b0ce6dd-df80-4351-abd8-fcc6724a30f3   "), "MBUS-OMS.DEVICE.POWER+.NAME", "MBUS-OMS.DEVICE.POWER.DESCRIPTION", "mbus-oms-power+", interfaceGuid,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.Double, 1, false, false);

            factory.CreateNodeTemplate(new Guid("b6b4291b-1ce7-4716-9670-1701a0401259"), "MBUS-OMS.DEVICE.POWER-.NAME", "MBUS-OMS.DEVICE.POWER-.DESCRIPTION", "mbus-oms-power-", interfaceGuid,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.Double, 1, false, false);

            factory.CreateNodeTemplate(new Guid("4621c5b5-aaba-4e38-845a-a08808201ac8"), "MBUS-OMS.DEVICE.REACTIVE_POWER+.NAME", "MBUS-OMS.DEVICE.REACTIVE_POWER+.DESCRIPTION", "mbus-oms-reactive-power+", interfaceGuid,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.Double, 1, false, false);

            factory.CreateNodeTemplate(new Guid("6fb733ff-9a2f-427c-9b3d-f4e7d222cc72"), "MBUS-OMS.DEVICE.REACTIVE_POWER-.NAME", "MBUS-OMS.DEVICE.REACTIVE_POWER-.DESCRIPTION", "mbus-oms-reactive-power-", interfaceGuid,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.Double, 1, false, false);
        }

        public override IDriver CreateDriver(IDriverContext config)
        {
            return new OmsDriver(config);
        }
    }
}
