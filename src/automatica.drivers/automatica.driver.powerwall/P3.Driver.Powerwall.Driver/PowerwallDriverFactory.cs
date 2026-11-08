using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using NodeDataType = Automatica.Core.Base.Templates.NodeDataType;

namespace P3.Driver.Powerwall.Driver
{
    /// <summary>
    /// Tesla Powerwall through the local API of the Backup Gateway 2 (LAN). Separate from the Tesla driver, which talks to cars through the Fleet API.
    /// </summary>
    public class PowerwallDriverFactory : DriverFactory
    {
        public const string HostKey = "host";
        public const string PasswordKey = "password";
        public const string EmailKey = "email";
        public const string PollKey = "poll";
        public const string AllowWritesKey = "allow_writes";
        public const string FingerprintKey = "certificate_fingerprint";

        public override string DriverName => "Powerwall";

        public override Guid DriverGuid => new("c935d1e6-15b7-4770-a866-036666b8c982");

        public override Version DriverVersion => new(1, 0, 0, 0);

        public override string ImageName => "automaticacore/plugin-p3.driver.powerwall";

        public override bool InDevelopmentMode => false;

        public override IDriver CreateDriver(IDriverContext config)
        {
            return new PowerwallDriver(config);
        }

        public override void InitNodeTemplates(INodeTemplateFactory factory)
        {
            factory.CreateInterfaceType(DriverGuid, "POWERWALL.NAME", "POWERWALL.DESCRIPTION", int.MaxValue, 1, true);
            factory.CreateNodeTemplate(DriverGuid, "POWERWALL.NAME", "POWERWALL.DESCRIPTION", "powerwall",
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Ethernet), DriverGuid, false, false, true, false, true, NodeDataType.NoAttribute, int.MaxValue, false);

            factory.CreatePropertyTemplate(new Guid("c0afbfd8-7660-45d8-84c5-e7736bad7ed0"), "POWERWALL.HOST.NAME", "POWERWALL.HOST.DESCRIPTION", HostKey,
                PropertyTemplateType.Text, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, null, "", 1, 1);
            factory.CreatePropertyTemplate(new Guid("b7188301-0560-462b-af45-2b7575ffc64b"), "POWERWALL.PASSWORD.NAME", "POWERWALL.PASSWORD.DESCRIPTION", PasswordKey,
                PropertyTemplateType.Password, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, null, "", 1, 2);
            factory.CreatePropertyTemplate(new Guid("69e3395c-8a85-465a-8577-b25d09958451"), "POWERWALL.EMAIL.NAME", "POWERWALL.EMAIL.DESCRIPTION", EmailKey,
                PropertyTemplateType.Text, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, null, "", 1, 3);
            factory.CreatePropertyTemplate(new Guid("66271818-c748-440d-8667-74cc527f8ad9"), "POWERWALL.POLL.NAME", "POWERWALL.POLL.DESCRIPTION", PollKey,
                PropertyTemplateType.Range, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, PropertyHelper.CreateRangeMetaString(2, 3600), 5, 1, 4);
            factory.CreatePropertyTemplate(new Guid("e80c0dbf-fb9c-4cb3-9fbe-5f799e92281a"), "POWERWALL.ALLOW_WRITES.NAME", "POWERWALL.ALLOW_WRITES.DESCRIPTION", AllowWritesKey,
                PropertyTemplateType.Bool, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, null, false, 1, 5);
            factory.CreatePropertyTemplate(new Guid("9bdb076a-784c-4a3c-99cc-671b4ad886b4"), "POWERWALL.FINGERPRINT.NAME", "POWERWALL.FINGERPRINT.DESCRIPTION", FingerprintKey,
                PropertyTemplateType.Text, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, null, "", 1, 6);

            foreach (var dp in PowerwallDataPoints.All)
            {
                factory.CreateNodeTemplate(dp.Template, $"POWERWALL.{dp.I18N}.NAME", $"POWERWALL.{dp.I18N}.DESCRIPTION", dp.Key, DriverGuid,
                    GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, dp.IsWriteable, true, dp.Type, 1, false);
            }
        }
    }
}
