using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using NodeDataType = Automatica.Core.Base.Templates.NodeDataType;

namespace P3.Driver.Refoss.Driver
{
    /// <summary>
    /// Refoss energy monitors (EM16P, EM06P) through their local JSON-RPC API on the LAN.
    /// Device (address, password) → channels (CT 1–18, found by scan) → power, current, voltage, pf and energies
    /// </summary>
    public class RefossDriverFactory : DriverFactory
    {
        public static readonly Guid ChannelInterface = new("0282cc75-5ddb-411d-8d37-4c39f4197835");
        public static readonly Guid ChannelTemplate = new("ab0b2cd5-1118-4bd5-8df1-94756508a792");

        public const string HostKey = "host";
        public const string PasswordKey = "password";
        public const string PollKey = "poll";
        public const string ChannelKey = "channel";
        public const string ChannelTemplateKey = "em_channel";

        public override string DriverName => "Refoss";

        public override Guid DriverGuid => new("ac7ee285-f73d-4c93-9648-5221fcf688ab");

        public override Version DriverVersion => new(1, 0, 0, 0);

        public override string ImageName => "automaticacore/plugin-p3.driver.refoss";

        public override bool InDevelopmentMode => false;

        public override IDriver CreateDriver(IDriverContext config)
        {
            return new RefossDriver(config);
        }

        public override void InitNodeTemplates(INodeTemplateFactory factory)
        {
            factory.CreateInterfaceType(DriverGuid, "REFOSS.NAME", "REFOSS.DESCRIPTION", int.MaxValue, 1, true);
            factory.CreateNodeTemplate(DriverGuid, "REFOSS.NAME", "REFOSS.DESCRIPTION", "refoss",
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Ethernet), DriverGuid, false, false, true, false, true, NodeDataType.NoAttribute, int.MaxValue, false);

            factory.CreatePropertyTemplate(new Guid("c32b3809-a359-416d-a60e-f90fa5521138"), "REFOSS.HOST.NAME", "REFOSS.HOST.DESCRIPTION", HostKey,
                PropertyTemplateType.Text, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, null, "", 1, 1);
            factory.CreatePropertyTemplate(new Guid("25fb3031-7b2e-41d0-9472-5405fac768b0"), "REFOSS.PASSWORD.NAME", "REFOSS.PASSWORD.DESCRIPTION", PasswordKey,
                PropertyTemplateType.Password, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, null, "", 1, 2);
            factory.CreatePropertyTemplate(new Guid("6d75b3d1-a1bb-42f6-b076-b1b030321e64"), "REFOSS.POLL.NAME", "REFOSS.POLL.DESCRIPTION", PollKey,
                PropertyTemplateType.Range, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, PropertyHelper.CreateRangeMetaString(2, 3600), 10, 1, 3);
            factory.CreatePropertyTemplate(new Guid("63c3e830-9859-4203-b604-43d18eba30d6"), "REFOSS.SCAN.NAME", "REFOSS.SCAN.DESCRIPTION", "scan",
                PropertyTemplateType.Scan, DriverGuid, "COMMON.CATEGORY.DISCOVERY", true, false, "", null, 0, 1);

            foreach (var dp in RefossDataPoints.Device)
            {
                factory.CreateNodeTemplate(dp.Template, $"REFOSS.{dp.I18N}.NAME", $"REFOSS.{dp.I18N}.DESCRIPTION", dp.Key, DriverGuid,
                    GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, dp.Type, 1, false);
            }

            factory.CreateInterfaceType(ChannelInterface, "REFOSS.CHANNEL.NAME", "REFOSS.CHANNEL.DESCRIPTION", int.MaxValue, int.MaxValue, false);
            factory.CreateNodeTemplate(ChannelTemplate, "REFOSS.CHANNEL.NAME", "REFOSS.CHANNEL.DESCRIPTION", ChannelTemplateKey, DriverGuid,
                ChannelInterface, false, false, true, false, true, NodeDataType.NoAttribute, int.MaxValue, false);
            factory.CreatePropertyTemplate(new Guid("741f12be-4f90-4649-b8f5-bc15778ce2de"), "REFOSS.CHANNEL.ID.NAME", "REFOSS.CHANNEL.ID.DESCRIPTION", ChannelKey,
                PropertyTemplateType.Range, ChannelTemplate, "COMMON.CATEGORY.ADDRESS", true, false, PropertyHelper.CreateRangeMetaString(1, 65534), 1, 1, 1);

            foreach (var dp in RefossDataPoints.Channel)
            {
                factory.CreateNodeTemplate(dp.Template, $"REFOSS.CHANNEL.{dp.I18N}.NAME", $"REFOSS.CHANNEL.{dp.I18N}.DESCRIPTION", dp.Key, ChannelInterface,
                    GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, dp.Type, 1, false);
            }
        }
    }
}
