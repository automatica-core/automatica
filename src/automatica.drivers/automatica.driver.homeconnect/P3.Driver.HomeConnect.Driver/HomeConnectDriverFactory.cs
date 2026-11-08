using System;
using System.Linq;
using Automatica.Core.Base.Templates;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using NodeDataType = Automatica.Core.Base.Templates.NodeDataType;

namespace P3.Driver.HomeConnect.Driver
{
    /// <summary>
    /// Home Connect appliances (Bosch, Siemens, Neff, Gaggenau, ...) through the official Home Connect API.
    /// Account (client id/secret, login) → appliance (haId, found by scan) → data points
    /// </summary>
    public class HomeConnectDriverFactory : DriverFactory
    {
        public static readonly Guid ApplianceInterface = new("b5add52f-1f72-41b6-87da-768b7bb9f32a");
        public static readonly Guid ApplianceTemplate = new("70d1d8b9-6d11-462a-8a2f-b3f6c8612d90");

        public const string ApiUrlKey = "api_url";
        public const string ClientIdKey = "client_id";
        public const string ClientSecretKey = "client_secret";
        public const string RefreshTokenKey = "refresh_token";
        public const string PollKey = "poll";
        public const string TokenStateKey = "token_state";
        public const string HaIdKey = "ha_id";
        public const string CustomKeyKey = "key";
        public const string LoginStatusKey = "login_status";
        public const string StreamConnectedKey = "stream_connected";

        public const string DefaultApiUrl = "https://api.home-connect.com";

        public override string DriverName => "HomeConnect";

        public override Guid DriverGuid => new("499adc1d-a63f-4705-aa58-041518ccd090");

        public override Version DriverVersion => new(1, 0, 0, 0);

        public override string ImageName => "automaticacore/plugin-p3.driver.homeconnect";

        public override bool InDevelopmentMode => false;

        public override IDriver CreateDriver(IDriverContext config)
        {
            return new HomeConnectDriver(config);
        }

        public override void InitNodeTemplates(INodeTemplateFactory factory)
        {
            factory.CreateInterfaceType(DriverGuid, "HOMECONNECT.NAME", "HOMECONNECT.DESCRIPTION", int.MaxValue, 1, true);
            factory.CreateNodeTemplate(DriverGuid, "HOMECONNECT.NAME", "HOMECONNECT.DESCRIPTION", "homeconnect",
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Ethernet), DriverGuid, false, false, true, false, true, NodeDataType.NoAttribute, int.MaxValue, false);

            factory.CreatePropertyTemplate(new Guid("a74161f9-cee6-4e65-a251-520d524daffe"), "HOMECONNECT.API_URL.NAME", "HOMECONNECT.API_URL.DESCRIPTION", ApiUrlKey,
                PropertyTemplateType.Text, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, null, DefaultApiUrl, 1, 1);
            factory.CreatePropertyTemplate(new Guid("a818c038-3340-4fb5-8938-dc329311e27e"), "HOMECONNECT.CLIENT_ID.NAME", "HOMECONNECT.CLIENT_ID.DESCRIPTION", ClientIdKey,
                PropertyTemplateType.Text, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, null, "", 1, 2);
            factory.CreatePropertyTemplate(new Guid("5e963cb1-279e-4b79-9fa0-6fa99fb6d10b"), "HOMECONNECT.CLIENT_SECRET.NAME", "HOMECONNECT.CLIENT_SECRET.DESCRIPTION", ClientSecretKey,
                PropertyTemplateType.Password, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, null, "", 1, 3);
            factory.CreatePropertyTemplate(new Guid("61edb2ea-5203-4ce4-bd01-7fa510c53d53"), "HOMECONNECT.REFRESH_TOKEN.NAME", "HOMECONNECT.REFRESH_TOKEN.DESCRIPTION", RefreshTokenKey,
                PropertyTemplateType.Password, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, null, "", 1, 4);
            factory.CreatePropertyTemplate(new Guid("5e3642aa-6c3a-4d5c-9420-71e662daa484"), "HOMECONNECT.POLL.NAME", "HOMECONNECT.POLL.DESCRIPTION", PollKey,
                PropertyTemplateType.Range, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, PropertyHelper.CreateRangeMetaString(600, 86400), 3600, 1, 5);
            factory.CreatePropertyTemplate(new Guid("200f6824-81c6-4628-8a56-fd0ce7617377"), "HOMECONNECT.SCAN.NAME", "HOMECONNECT.SCAN.DESCRIPTION", "scan",
                PropertyTemplateType.Scan, DriverGuid, "COMMON.CATEGORY.DISCOVERY", true, false, "", null, 0, 1);
            // the newest refresh token (from the device login or a rotation)
            factory.CreatePropertyTemplate(new Guid("fc2b0af3-6634-42cd-a6c1-3d79863e1f1b"), "HOMECONNECT.TOKEN_STATE.NAME", "HOMECONNECT.TOKEN_STATE.DESCRIPTION", TokenStateKey,
                PropertyTemplateType.Text, DriverGuid, "", false, true, null, null, 0, 0);

            factory.CreateNodeTemplate(new Guid("1d9e0121-7e49-426f-afc6-4a7685ff5148"), "HOMECONNECT.LOGIN_STATUS.NAME", "HOMECONNECT.LOGIN_STATUS.DESCRIPTION", LoginStatusKey, DriverGuid,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.String, 1, false);
            factory.CreateNodeTemplate(new Guid("43991f25-830d-4216-9713-0f81c714fd3b"), "HOMECONNECT.STREAM_CONNECTED.NAME", "HOMECONNECT.STREAM_CONNECTED.DESCRIPTION", StreamConnectedKey, DriverGuid,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.Boolean, 1, false);

            factory.CreateInterfaceType(ApplianceInterface, "HOMECONNECT.APPLIANCE.NAME", "HOMECONNECT.APPLIANCE.DESCRIPTION", int.MaxValue, int.MaxValue, false);
            factory.CreateNodeTemplate(ApplianceTemplate, "HOMECONNECT.APPLIANCE.NAME", "HOMECONNECT.APPLIANCE.DESCRIPTION", "appliance", DriverGuid,
                ApplianceInterface, false, false, true, false, true, NodeDataType.NoAttribute, int.MaxValue, false);
            factory.CreatePropertyTemplate(new Guid("1352b49a-de77-4624-9bda-e6d8917f59f0"), "HOMECONNECT.APPLIANCE.HA_ID.NAME", "HOMECONNECT.APPLIANCE.HA_ID.DESCRIPTION", HaIdKey,
                PropertyTemplateType.Text, ApplianceTemplate, "COMMON.CATEGORY.ADDRESS", true, false, null, "", 1, 1);

            foreach (var dp in HomeConnectDataPoints.All)
            {
                var custom = HomeConnectDataPoints.IsCustom(dp.Key);
                factory.CreateNodeTemplate(dp.Template, $"HOMECONNECT.APPLIANCE.{dp.I18N}.NAME", $"HOMECONNECT.APPLIANCE.{dp.I18N}.DESCRIPTION", dp.Key, ApplianceInterface,
                    GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), dp.DefaultCreated, true, true, dp.IsWriteable, true, dp.Type, custom ? int.MaxValue : 1, false);
            }

            foreach (var (dp, guid) in new[]
                     {
                         (HomeConnectDataPoints.CustomNumberKey, new Guid("e040d339-a6f0-4fec-99e3-875cd3db0d9e")),
                         (HomeConnectDataPoints.CustomSwitchKey, new Guid("d86e3ba4-dc07-48c9-9c78-d16026338292")),
                         (HomeConnectDataPoints.CustomTextKey, new Guid("c2ec6f87-82ae-4cb2-8ba0-29d2618c1dcd"))
                     })
            {
                var template = HomeConnectDataPoints.All.First(d => d.Key == dp).Template;
                factory.CreatePropertyTemplate(guid, "HOMECONNECT.APPLIANCE.CUSTOM_KEY.NAME", "HOMECONNECT.APPLIANCE.CUSTOM_KEY.DESCRIPTION", CustomKeyKey,
                    PropertyTemplateType.Text, template, "COMMON.CATEGORY.ADDRESS", true, false, null, "", 1, 1);
            }
        }
    }
}
