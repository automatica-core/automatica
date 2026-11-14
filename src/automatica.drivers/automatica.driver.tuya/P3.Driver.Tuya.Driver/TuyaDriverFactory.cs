using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using NodeDataType = Automatica.Core.Base.Templates.NodeDataType;

namespace P3.Driver.Tuya.Driver
{
    /// <summary>
    /// Tuya Wi-Fi devices (LSC Smart Connect, Smart Life, ...) through the local protocol 3.3 / 3.4 on the LAN, no cloud.
    /// Tuya → plug (address, device id, local key, protocol version) → switch, power, voltage, current, energy
    /// </summary>
    public class TuyaDriverFactory : DriverFactory
    {
        public static readonly Guid PlugInterface = new("c5896acd-8376-4e39-a558-359c93681e43");
        public static readonly Guid PlugTemplate = new("324a4ab3-abac-4ec1-a34b-1dce6e6f0269");

        public const string HostKey = "host";
        public const string DeviceIdKey = "device_id";
        public const string LocalKeyKey = "local_key";
        public const string VersionKey = "version";
        public const string PollKey = "poll";
        public const string PlugTemplateKey = "tuya_plug";

        public override string DriverName => "Tuya";

        public override Guid DriverGuid => new("ad5e0c5d-70ee-484d-9bfd-ffc4ebdaa8be");

        public override Version DriverVersion => new(1, 0, 0, 0);

        public override string ImageName => "automaticacore/plugin-p3.driver.tuya";

        public override bool InDevelopmentMode => false;

        public override IDriver CreateDriver(IDriverContext config)
        {
            return new TuyaDriver(config);
        }

        public override void InitNodeTemplates(INodeTemplateFactory factory)
        {
            factory.CreateInterfaceType(DriverGuid, "TUYA.NAME", "TUYA.DESCRIPTION", int.MaxValue, 1, true);
            factory.CreateNodeTemplate(DriverGuid, "TUYA.NAME", "TUYA.DESCRIPTION", "tuya",
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Ethernet), DriverGuid, false, false, true, false, true, NodeDataType.NoAttribute, int.MaxValue, false);
            factory.CreatePropertyTemplate(new Guid("b0ec5e56-eab3-4f1f-95e7-140c3a18a0e1"), "TUYA.SCAN.NAME", "TUYA.SCAN.DESCRIPTION", "scan",
                PropertyTemplateType.Scan, DriverGuid, "COMMON.CATEGORY.DISCOVERY", true, false, "", null, 0, 1);

            factory.CreateInterfaceType(PlugInterface, "TUYA.PLUG.NAME", "TUYA.PLUG.DESCRIPTION", int.MaxValue, int.MaxValue, false);
            factory.CreateNodeTemplate(PlugTemplate, "TUYA.PLUG.NAME", "TUYA.PLUG.DESCRIPTION", PlugTemplateKey, DriverGuid,
                PlugInterface, false, false, true, false, true, NodeDataType.NoAttribute, int.MaxValue, false);
            factory.CreatePropertyTemplate(new Guid("25c8598a-ce0f-4ae5-8d1c-a7143c052a4a"), "TUYA.PLUG.HOST.NAME", "TUYA.PLUG.HOST.DESCRIPTION", HostKey,
                PropertyTemplateType.Text, PlugTemplate, "COMMON.CATEGORY.ADDRESS", true, false, null, "", 1, 1);
            factory.CreatePropertyTemplate(new Guid("eaed1aa0-15a8-4dcc-9020-ce99b8270d67"), "TUYA.PLUG.DEVICE_ID.NAME", "TUYA.PLUG.DEVICE_ID.DESCRIPTION", DeviceIdKey,
                PropertyTemplateType.Text, PlugTemplate, "COMMON.CATEGORY.ADDRESS", true, false, null, "", 1, 2);
            factory.CreatePropertyTemplate(new Guid("36deaee8-7ff9-4bca-a8cc-c6cb200cce9a"), "TUYA.PLUG.LOCAL_KEY.NAME", "TUYA.PLUG.LOCAL_KEY.DESCRIPTION", LocalKeyKey,
                PropertyTemplateType.Password, PlugTemplate, "COMMON.CATEGORY.ADDRESS", true, false, null, "", 1, 3);
            factory.CreatePropertyTemplate(new Guid("25307c58-e18d-413c-90b4-07cbb3a19167"), "TUYA.PLUG.VERSION.NAME", "TUYA.PLUG.VERSION.DESCRIPTION", VersionKey,
                PropertyTemplateType.Text, PlugTemplate, "COMMON.CATEGORY.ADDRESS", true, false, null, "3.4", 1, 4);
            factory.CreatePropertyTemplate(new Guid("2e18defb-416f-4a43-83a1-eeb58e788fdc"), "TUYA.PLUG.POLL.NAME", "TUYA.PLUG.POLL.DESCRIPTION", PollKey,
                PropertyTemplateType.Range, PlugTemplate, "COMMON.CATEGORY.ADDRESS", true, false, PropertyHelper.CreateRangeMetaString(2, 3600), 10, 1, 5);

            foreach (var dp in TuyaDataPoints.Plug)
            {
                factory.CreateNodeTemplate(dp.Template, $"TUYA.PLUG.{dp.I18N}.NAME", $"TUYA.PLUG.{dp.I18N}.DESCRIPTION", dp.Key, PlugInterface,
                    GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, dp.Writable, !dp.Writable, dp.Type, 1, false);
            }
        }
    }
}
