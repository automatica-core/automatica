using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using NodeDataType = Automatica.Core.Base.Templates.NodeDataType;

namespace P3.Driver.Miele.Driver
{
    /// <summary>
    /// Miele@home appliances through the official Miele 3rd party API.
    /// Account (client id/secret, refresh token) → appliance (device id, found by scan) → data points
    /// </summary>
    public class MieleDriverFactory : DriverFactory
    {
        public static readonly Guid ApplianceInterface = new("21b95fd8-b4ac-4134-ab0b-2428bcba1350");
        public static readonly Guid ApplianceTemplate = new("9e588f42-0cb5-4798-9789-f009550e555b");
        public static readonly Guid LoginStatusTemplate = new("0e15c7c4-d63a-426b-82d0-ae7647ef64ea");
        public static readonly Guid StreamConnectedTemplate = new("d05c7b9e-0e72-4473-b14e-54d0f79cfa84");

        public const string ApiUrlKey = "api_url";
        public const string TokenUrlKey = "token_url";
        public const string ClientIdKey = "client_id";
        public const string ClientSecretKey = "client_secret";
        public const string RefreshTokenKey = "refresh_token";
        public const string LanguageKey = "language";
        public const string PollKey = "poll";
        public const string TokenStateKey = "token_state";
        public const string DeviceIdKey = "device_id";
        public const string LoginStatusKey = "login_status";
        public const string StreamConnectedKey = "stream_connected";

        public const string DefaultApiUrl = "https://api.mcs3.miele.com";
        public const string DefaultTokenUrl = "https://auth.domestic.miele-iot.com/partner/realms/mcs/protocol/openid-connect/token";

        public override string DriverName => "Miele";

        public override Guid DriverGuid => new("aa200cca-5140-49f4-8d33-d5c5762a216a");

        public override Version DriverVersion => new(1, 0, 0, 0);

        public override string ImageName => "automaticacore/plugin-p3.driver.miele";

        public override bool InDevelopmentMode => false;

        public override IDriver CreateDriver(IDriverContext config)
        {
            return new MieleDriver(config);
        }

        public override void InitNodeTemplates(INodeTemplateFactory factory)
        {
            factory.CreateInterfaceType(DriverGuid, "MIELE.NAME", "MIELE.DESCRIPTION", int.MaxValue, 1, true);
            factory.CreateNodeTemplate(DriverGuid, "MIELE.NAME", "MIELE.DESCRIPTION", "miele",
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Ethernet), DriverGuid, false, false, true, false, true, NodeDataType.NoAttribute, int.MaxValue, false);

            factory.CreatePropertyTemplate(new Guid("cbc585ba-92b9-4167-b018-baeea2ec83d4"), "MIELE.API_URL.NAME", "MIELE.API_URL.DESCRIPTION", ApiUrlKey,
                PropertyTemplateType.Text, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, null, DefaultApiUrl, 1, 1);
            factory.CreatePropertyTemplate(new Guid("b42b8016-78a8-49c7-82cf-b09ae6860593"), "MIELE.TOKEN_URL.NAME", "MIELE.TOKEN_URL.DESCRIPTION", TokenUrlKey,
                PropertyTemplateType.Text, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, null, DefaultTokenUrl, 1, 2);
            factory.CreatePropertyTemplate(new Guid("9c22682a-6bfa-4414-9108-09f60b8ff259"), "MIELE.CLIENT_ID.NAME", "MIELE.CLIENT_ID.DESCRIPTION", ClientIdKey,
                PropertyTemplateType.Text, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, null, "", 1, 3);
            factory.CreatePropertyTemplate(new Guid("fccf7ee1-4a3d-4462-933f-a59d5d550ef0"), "MIELE.CLIENT_SECRET.NAME", "MIELE.CLIENT_SECRET.DESCRIPTION", ClientSecretKey,
                PropertyTemplateType.Password, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, null, "", 1, 4);
            factory.CreatePropertyTemplate(new Guid("afc806ca-4263-4955-bac0-5ddb383ef1a0"), "MIELE.REFRESH_TOKEN.NAME", "MIELE.REFRESH_TOKEN.DESCRIPTION", RefreshTokenKey,
                PropertyTemplateType.Password, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, null, "", 1, 5);
            factory.CreatePropertyTemplate(new Guid("54df651a-5df3-4c15-9a09-37374ad32d95"), "MIELE.LANGUAGE.NAME", "MIELE.LANGUAGE.DESCRIPTION", LanguageKey,
                PropertyTemplateType.Text, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, null, "en", 1, 6);
            factory.CreatePropertyTemplate(new Guid("81ea67b8-3bb6-4565-89fe-4fe2f143bf76"), "MIELE.POLL.NAME", "MIELE.POLL.DESCRIPTION", PollKey,
                PropertyTemplateType.Range, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, PropertyHelper.CreateRangeMetaString(300, 86400), 1800, 1, 7);
            factory.CreatePropertyTemplate(new Guid("1f69653c-1597-4a42-9264-477ac2f88e79"), "MIELE.SCAN.NAME", "MIELE.SCAN.DESCRIPTION", "scan",
                PropertyTemplateType.Scan, DriverGuid, "COMMON.CATEGORY.DISCOVERY", true, false, "", null, 0, 1);
            factory.CreatePropertyTemplate(new Guid("5099f539-1894-47dc-b297-6f4b871e1edf"), "MIELE.TOKEN_STATE.NAME", "MIELE.TOKEN_STATE.DESCRIPTION", TokenStateKey,
                PropertyTemplateType.Text, DriverGuid, "", false, true, null, null, 0, 0);

            factory.CreateNodeTemplate(LoginStatusTemplate, "MIELE.LOGIN_STATUS.NAME", "MIELE.LOGIN_STATUS.DESCRIPTION", LoginStatusKey, DriverGuid,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.String, 1, false);
            factory.CreateNodeTemplate(StreamConnectedTemplate, "MIELE.STREAM_CONNECTED.NAME", "MIELE.STREAM_CONNECTED.DESCRIPTION", StreamConnectedKey, DriverGuid,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.Boolean, 1, false);

            factory.CreateInterfaceType(ApplianceInterface, "MIELE.APPLIANCE.NAME", "MIELE.APPLIANCE.DESCRIPTION", int.MaxValue, int.MaxValue, false);
            factory.CreateNodeTemplate(ApplianceTemplate, "MIELE.APPLIANCE.NAME", "MIELE.APPLIANCE.DESCRIPTION", "appliance", DriverGuid,
                ApplianceInterface, false, false, true, false, true, NodeDataType.NoAttribute, int.MaxValue, false);
            factory.CreatePropertyTemplate(new Guid("980323c3-1a51-4749-8217-f98d4598120a"), "MIELE.APPLIANCE.DEVICE_ID.NAME", "MIELE.APPLIANCE.DEVICE_ID.DESCRIPTION", DeviceIdKey,
                PropertyTemplateType.Text, ApplianceTemplate, "COMMON.CATEGORY.ADDRESS", true, false, null, "", 1, 1);

            foreach (var dp in MieleDataPoints.All)
            {
                factory.CreateNodeTemplate(dp.Template, $"MIELE.APPLIANCE.{dp.I18N}.NAME", $"MIELE.APPLIANCE.{dp.I18N}.DESCRIPTION", dp.Key, ApplianceInterface,
                    GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, dp.IsWriteable, true, dp.Type, 1, false);
            }
        }
    }
}
