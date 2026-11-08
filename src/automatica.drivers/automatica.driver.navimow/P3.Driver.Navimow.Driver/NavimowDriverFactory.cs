using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using NodeDataType = Automatica.Core.Base.Templates.NodeDataType;

namespace P3.Driver.Navimow.Driver
{
    /// <summary>
    /// Segway Navimow robot mowers through the Navimow smart-home cloud (EU).
    /// Account (client from Segway + login) → mower (device id, found by scan) → data points
    /// </summary>
    public class NavimowDriverFactory : DriverFactory
    {
        public static readonly Guid MowerInterface = new("f2bb377b-b12d-494d-a338-b75b553676fa");
        public static readonly Guid MowerTemplate = new("e8a72fff-4e5d-46f9-a8b9-0839f1ed864f");
        public static readonly Guid StatusTemplate = new("823d467a-75c8-4fbe-99f2-42b7e69cbc57");

        public const string ClientIdKey = "client_id";
        public const string ClientSecretKey = "client_secret";
        public const string AuthorizationCodeKey = "authorization_code";
        public const string RefreshTokenKey = "refresh_token";
        public const string RedirectUriKey = "redirect_uri";
        public const string LoginUrlKey = "login_url";
        public const string TokenUrlKey = "token_url";
        public const string ApiUrlKey = "api_url";
        public const string PollKey = "poll";
        public const string TokenStateKey = "token_state";
        public const string DeviceIdKey = "device_id";
        public const string StatusKey = "status";

        public const string DefaultLoginUrl = "https://navimow-h5-fra.willand.com/smartHome/login";
        public const string DefaultTokenUrl = "https://navimow-fra.ninebot.com/openapi/oauth/getAccessToken";
        public const string DefaultApiUrl = "https://navimow-fra.ninebot.com";

        public override string DriverName => "Navimow";

        public override Guid DriverGuid => new("99f76812-8c5b-424d-9f47-6e6ac656d1d6");

        public override Version DriverVersion => new(1, 0, 0, 0);

        public override string ImageName => "automaticacore/plugin-p3.driver.navimow";

        public override bool InDevelopmentMode => false;

        public override IDriver CreateDriver(IDriverContext config)
        {
            return new NavimowDriver(config);
        }

        public override void InitNodeTemplates(INodeTemplateFactory factory)
        {
            factory.CreateInterfaceType(DriverGuid, "NAVIMOW.NAME", "NAVIMOW.DESCRIPTION", int.MaxValue, 1, true);
            factory.CreateNodeTemplate(DriverGuid, "NAVIMOW.NAME", "NAVIMOW.DESCRIPTION", "navimow",
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Ethernet), DriverGuid, false, false, true, false, true, NodeDataType.NoAttribute, int.MaxValue, false);

            factory.CreatePropertyTemplate(new Guid("699dbfd9-71cc-419f-a22d-896ecbf8413c"), "NAVIMOW.CLIENT_ID.NAME", "NAVIMOW.CLIENT_ID.DESCRIPTION", ClientIdKey,
                PropertyTemplateType.Text, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, null, "", 1, 1);
            factory.CreatePropertyTemplate(new Guid("62b0c108-bd8f-4c4f-8135-c18403b62769"), "NAVIMOW.CLIENT_SECRET.NAME", "NAVIMOW.CLIENT_SECRET.DESCRIPTION", ClientSecretKey,
                PropertyTemplateType.Password, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, null, "", 1, 2);
            factory.CreatePropertyTemplate(new Guid("b823129c-d3a5-4ce5-912c-109e07a66c18"), "NAVIMOW.REDIRECT_URI.NAME", "NAVIMOW.REDIRECT_URI.DESCRIPTION", RedirectUriKey,
                PropertyTemplateType.Text, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, null, "", 1, 3);
            factory.CreatePropertyTemplate(new Guid("d76eb5ba-7094-45b3-ac93-259bfbe8bb03"), "NAVIMOW.AUTHORIZATION_CODE.NAME", "NAVIMOW.AUTHORIZATION_CODE.DESCRIPTION", AuthorizationCodeKey,
                PropertyTemplateType.Password, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, null, "", 1, 4);
            factory.CreatePropertyTemplate(new Guid("95538946-dd66-45d1-96c3-571156800d72"), "NAVIMOW.REFRESH_TOKEN.NAME", "NAVIMOW.REFRESH_TOKEN.DESCRIPTION", RefreshTokenKey,
                PropertyTemplateType.Password, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, null, "", 1, 5);
            factory.CreatePropertyTemplate(new Guid("820f0e89-98e4-4d1c-970d-e64a754717da"), "NAVIMOW.POLL.NAME", "NAVIMOW.POLL.DESCRIPTION", PollKey,
                PropertyTemplateType.Range, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, PropertyHelper.CreateRangeMetaString(30, 3600), 60, 1, 6);
            factory.CreatePropertyTemplate(new Guid("a6bbfc52-95db-48ff-b261-337872d1b5ee"), "NAVIMOW.LOGIN_URL.NAME", "NAVIMOW.LOGIN_URL.DESCRIPTION", LoginUrlKey,
                PropertyTemplateType.Text, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, null, DefaultLoginUrl, 1, 7);
            factory.CreatePropertyTemplate(new Guid("40329ba3-2a0a-4cd2-a426-1f1c94417198"), "NAVIMOW.TOKEN_URL.NAME", "NAVIMOW.TOKEN_URL.DESCRIPTION", TokenUrlKey,
                PropertyTemplateType.Text, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, null, DefaultTokenUrl, 1, 8);
            factory.CreatePropertyTemplate(new Guid("e83dc3f9-88b8-4811-90ea-5fde76729d78"), "NAVIMOW.API_URL.NAME", "NAVIMOW.API_URL.DESCRIPTION", ApiUrlKey,
                PropertyTemplateType.Text, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, null, DefaultApiUrl, 1, 9);
            factory.CreatePropertyTemplate(new Guid("6b8a1263-d06b-4f95-a194-40cdfc8f8aab"), "NAVIMOW.SCAN.NAME", "NAVIMOW.SCAN.DESCRIPTION", "scan",
                PropertyTemplateType.Scan, DriverGuid, "COMMON.CATEGORY.DISCOVERY", true, false, "", null, 0, 1);
            factory.CreatePropertyTemplate(new Guid("5c4baa61-facf-4e5d-9669-39777ae97f97"), "NAVIMOW.TOKEN_STATE.NAME", "NAVIMOW.TOKEN_STATE.DESCRIPTION", TokenStateKey,
                PropertyTemplateType.Text, DriverGuid, "", false, true, null, null, 0, 0);

            factory.CreateNodeTemplate(StatusTemplate, "NAVIMOW.STATUS.NAME", "NAVIMOW.STATUS.DESCRIPTION", StatusKey, DriverGuid,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.String, 1, false);

            factory.CreateInterfaceType(MowerInterface, "NAVIMOW.MOWER.NAME", "NAVIMOW.MOWER.DESCRIPTION", int.MaxValue, int.MaxValue, false);
            factory.CreateNodeTemplate(MowerTemplate, "NAVIMOW.MOWER.NAME", "NAVIMOW.MOWER.DESCRIPTION", "mower", DriverGuid,
                MowerInterface, false, false, true, false, true, NodeDataType.NoAttribute, int.MaxValue, false);
            factory.CreatePropertyTemplate(new Guid("9bdc70f0-334e-4e41-8381-945af35749d8"), "NAVIMOW.MOWER.DEVICE_ID.NAME", "NAVIMOW.MOWER.DEVICE_ID.DESCRIPTION", DeviceIdKey,
                PropertyTemplateType.Text, MowerTemplate, "COMMON.CATEGORY.ADDRESS", true, false, null, "", 1, 1);

            foreach (var dp in NavimowDataPoints.All)
            {
                factory.CreateNodeTemplate(dp.Template, $"NAVIMOW.MOWER.{dp.I18N}.NAME", $"NAVIMOW.MOWER.{dp.I18N}.DESCRIPTION", dp.Key, MowerInterface,
                    GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, dp.Read != null, true, dp.IsWriteable, true, dp.Type, 1, false);
            }
        }
    }
}
