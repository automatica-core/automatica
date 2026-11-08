using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using P3.Driver.EBusFactory.Wolf;
using NodeDataType = Automatica.Core.Base.Templates.NodeDataType;

namespace P3.Driver.EBusFactory
{
    /// <summary>
    /// eBUS through a network adapter in raw mode. The bus node connects to the adapter, the device nodes below it are polled
    /// with read requests and take the values the devices broadcast. Nothing is written to the devices.
    /// </summary>
    public class EBusFactory : DriverFactory
    {
        public const string IpKey = "ebus-ip";
        public const string PortKey = "ebus-port";
        public const string MasterAddressKey = "ebus-master-address";
        public const string PollIntervalKey = "ebus-poll-interval";
        public const string DeviceAddressKey = "ebus-device-address";
        public const string RegisterKey = "register";
        public const string ScaleKey = "scale";
        public const string SignedKey = "signed";

        public override string DriverName => "ebus";

        public override Guid DriverGuid => new Guid("15dc0233-fdee-4420-b037-781b13b02cfc");

        public override Version DriverVersion => new Version(1, 0, 0, 0);

        public override bool InDevelopmentMode => false;

        public override string ImageName => "automaticacore/plugin-p3.driver.ebus";

        public override IDriver CreateDriver(IDriverContext config)
        {
            return new EBusDriver(config);
        }

        public override void InitNodeTemplates(INodeTemplateFactory factory)
        {
            factory.CreateInterfaceType(DriverGuid, "EBUS.NAME", "EBUS.DESCRIPTION", int.MaxValue, 1, true);
            factory.CreateNodeTemplate(DriverGuid, "EBUS.NAME", "EBUS.DESCRIPTION", "EBUS", GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Ethernet),
                DriverGuid, false, true, true, false, true, NodeDataType.NoAttribute, Int32.MaxValue, false);

            factory.CreatePropertyTemplate(new Guid("a2aca7a5-c54c-4c86-a856-76169acf2f22"), "COMMON.PROPERTY.IP.NAME", "EBUS.IP.DESCRIPTION", IpKey,
                PropertyTemplateType.Ip, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, null, null, 1, 1);
            factory.CreatePropertyTemplate(new Guid("d6e0f121-05b4-4ef7-aeb8-67e196b1e83a"), "COMMON.PROPERTY.IPPORT.NAME", "EBUS.PORT.DESCRIPTION", PortKey,
                PropertyTemplateType.Range, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, PropertyHelper.CreateRangeMetaString(1, ushort.MaxValue), 3333, 1, 2);
            factory.CreatePropertyTemplate(new Guid("1e2a8e2b-f3ff-4a29-b728-de6bd4d33f86"), "EBUS.MASTER_ADDRESS.NAME", "EBUS.MASTER_ADDRESS.DESCRIPTION", MasterAddressKey,
                PropertyTemplateType.Range, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, PropertyHelper.CreateRangeMetaString(0, 255), 255, 1, 3);
            factory.CreatePropertyTemplate(new Guid("8b1131e4-3c4e-4a3f-9f1c-d64b916ab69a"), "EBUS.POLL_INTERVAL.NAME", "EBUS.POLL_INTERVAL.DESCRIPTION", PollIntervalKey,
                PropertyTemplateType.Range, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, PropertyHelper.CreateRangeMetaString(10, 3600), 60, 1, 4);

            factory.CreateInterfaceType(WolfBwl1.Interface, "EBUS.WOLF_BWL1.NAME", "EBUS.WOLF_BWL1.DESCRIPTION", int.MaxValue, int.MaxValue, false);
            factory.CreateNodeTemplate(WolfBwl1.Template, "EBUS.WOLF_BWL1.NAME", "EBUS.WOLF_BWL1.DESCRIPTION", WolfBwl1.Key, DriverGuid,
                WolfBwl1.Interface, false, false, true, false, true, NodeDataType.NoAttribute, int.MaxValue, false);
            factory.CreatePropertyTemplate(new Guid("1c7a20b7-b00b-40b5-8b40-cd9f1097c785"), "EBUS.DEVICE_ADDRESS.NAME", "EBUS.DEVICE_ADDRESS.DESCRIPTION", DeviceAddressKey,
                PropertyTemplateType.Range, WolfBwl1.Template, "COMMON.CATEGORY.ADDRESS", true, false, PropertyHelper.CreateRangeMetaString(0, 255), (int)WolfBwl1.DefaultAddress, 1, 1);

            foreach (var dp in WolfBwl1.DataPoints)
            {
                factory.CreateNodeTemplate(dp.Template, $"EBUS.WOLF_BWL1.{dp.I18N}.NAME", $"EBUS.WOLF_BWL1.{dp.I18N}.DESCRIPTION", dp.Key, WolfBwl1.Interface,
                    GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, dp.Type, 1, false);
            }

            // any other register of the device, for registers that are not mapped yet
            factory.CreateNodeTemplate(WolfBwl1.CustomRegisterTemplate, "EBUS.WOLF_REGISTER.NAME", "EBUS.WOLF_REGISTER.DESCRIPTION", WolfBwl1.CustomRegisterKey, WolfBwl1.Interface,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), false, true, true, false, true, NodeDataType.Double, int.MaxValue, false);
            factory.CreatePropertyTemplate(new Guid("0fcd58f1-6f11-4d7d-8a71-bd7690e84ade"), "EBUS.WOLF_REGISTER.REGISTER.NAME", "EBUS.WOLF_REGISTER.REGISTER.DESCRIPTION", RegisterKey,
                PropertyTemplateType.Range, WolfBwl1.CustomRegisterTemplate, "COMMON.CATEGORY.ADDRESS", true, false, PropertyHelper.CreateRangeMetaString(0, ushort.MaxValue), 0, 1, 1);
            factory.CreatePropertyTemplate(new Guid("406dec72-5aba-48ba-9be8-7d1c233d8b61"), "EBUS.WOLF_REGISTER.SCALE.NAME", "EBUS.WOLF_REGISTER.SCALE.DESCRIPTION", ScaleKey,
                PropertyTemplateType.Numeric, WolfBwl1.CustomRegisterTemplate, "COMMON.CATEGORY.ADDRESS", true, false, "", 1, 1, 2);
            factory.CreatePropertyTemplate(new Guid("5514bc32-b890-4b3b-a75e-a674c1eaf567"), "EBUS.WOLF_REGISTER.SIGNED.NAME", "EBUS.WOLF_REGISTER.SIGNED.DESCRIPTION", SignedKey,
                PropertyTemplateType.Bool, WolfBwl1.CustomRegisterTemplate, "COMMON.CATEGORY.ADDRESS", true, false, null, false, 1, 3);
        }
    }
}
