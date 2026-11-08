using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using NodeDataType = Automatica.Core.Base.Templates.NodeDataType;

namespace P3.Driver.Gardena.Driver
{
    /// <summary>
    /// GARDENA smart system (valves, irrigation control, smart mowers, power sockets, sensors) through the official
    /// GARDENA smart system API v2. Account (application key/secret) → service (found by scan) → data points
    /// </summary>
    public class GardenaDriverFactory : DriverFactory
    {
        public static readonly Guid StatusTemplate = new("da8484e5-939c-4a1e-b61b-9dcc58cf4f17");
        public static readonly Guid SocketConnectedTemplate = new("9168ac3a-930e-4472-a5ed-6f4284668e9d");

        public const string AuthUrlKey = "auth_url";
        public const string ApiUrlKey = "api_url";
        public const string ApplicationKeyKey = "application_key";
        public const string ApplicationSecretKey = "application_secret";
        public const string LocationIdKey = "location_id";
        public const string ServiceIdKey = "service_id";
        public const string DefaultMinutesKey = "default_minutes";
        public const string StatusKey = "status";
        public const string SocketConnectedKey = "socket_connected";

        public const string DefaultAuthUrl = "https://api.authentication.husqvarnagroup.dev/v1/oauth2/token";
        public const string DefaultApiUrl = "https://api.smart.gardena.dev/v2";

        public override string DriverName => "Gardena";

        public override Guid DriverGuid => new("ab299f84-a581-45f3-bb58-2099d5fe106c");

        public override Version DriverVersion => new(1, 0, 0, 0);

        public override string ImageName => "automaticacore/plugin-p3.driver.gardena";

        public override bool InDevelopmentMode => false;

        public override IDriver CreateDriver(IDriverContext config)
        {
            return new GardenaDriver(config);
        }

        public override void InitNodeTemplates(INodeTemplateFactory factory)
        {
            factory.CreateInterfaceType(DriverGuid, "GARDENA.NAME", "GARDENA.DESCRIPTION", int.MaxValue, 1, true);
            factory.CreateNodeTemplate(DriverGuid, "GARDENA.NAME", "GARDENA.DESCRIPTION", "gardena",
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Ethernet), DriverGuid, false, false, true, false, true, NodeDataType.NoAttribute, int.MaxValue, false);

            factory.CreatePropertyTemplate(new Guid("1a6a8593-125c-4fdd-9651-ef903cac87d4"), "GARDENA.APPLICATION_KEY.NAME", "GARDENA.APPLICATION_KEY.DESCRIPTION", ApplicationKeyKey,
                PropertyTemplateType.Text, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, null, "", 1, 1);
            factory.CreatePropertyTemplate(new Guid("cf621195-dd58-4832-b004-99f33b0cc718"), "GARDENA.APPLICATION_SECRET.NAME", "GARDENA.APPLICATION_SECRET.DESCRIPTION", ApplicationSecretKey,
                PropertyTemplateType.Password, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, null, "", 1, 2);
            factory.CreatePropertyTemplate(new Guid("b62fabec-583c-4f7e-8975-f2247917dfa9"), "GARDENA.LOCATION_ID.NAME", "GARDENA.LOCATION_ID.DESCRIPTION", LocationIdKey,
                PropertyTemplateType.Text, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, null, "", 1, 3);
            factory.CreatePropertyTemplate(new Guid("83f1257e-396f-4c80-8b47-e18fb7927982"), "GARDENA.API_URL.NAME", "GARDENA.API_URL.DESCRIPTION", ApiUrlKey,
                PropertyTemplateType.Text, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, null, DefaultApiUrl, 1, 4);
            factory.CreatePropertyTemplate(new Guid("43474356-9b9f-4aeb-8d3b-bb739496e061"), "GARDENA.AUTH_URL.NAME", "GARDENA.AUTH_URL.DESCRIPTION", AuthUrlKey,
                PropertyTemplateType.Text, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, null, DefaultAuthUrl, 1, 5);
            factory.CreatePropertyTemplate(new Guid("8b426bf0-3122-44fa-8959-757ae41d5be2"), "GARDENA.SCAN.NAME", "GARDENA.SCAN.DESCRIPTION", "scan",
                PropertyTemplateType.Scan, DriverGuid, "COMMON.CATEGORY.DISCOVERY", true, false, "", null, 0, 1);

            factory.CreateNodeTemplate(StatusTemplate, "GARDENA.STATUS.NAME", "GARDENA.STATUS.DESCRIPTION", StatusKey, DriverGuid,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.String, 1, false);
            factory.CreateNodeTemplate(SocketConnectedTemplate, "GARDENA.SOCKET_CONNECTED.NAME", "GARDENA.SOCKET_CONNECTED.DESCRIPTION", SocketConnectedKey, DriverGuid,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.Boolean, 1, false);

            var propertyGuids = new[]
            {
                (new Guid("25158a8e-cfc2-4b18-affe-9fd49ff03791"), new Guid("5b025949-6f40-4c6e-aaa8-776bb0949b05")),
                (new Guid("96b3cb37-c7f0-4ce0-beb1-7180d346c6c1"), new Guid("26a68a90-144c-4dba-b913-1a8a16025365")),
                (new Guid("3a680737-c67c-429e-b545-c67860314850"), new Guid("eb52441a-e319-41b0-b783-2ec40192f692")),
                (new Guid("35fbe78e-5f6f-4d63-8c58-28047d700325"), new Guid("2c1329c6-04e1-4c63-886f-a5af099bd18c")),
            };
            var index = 0;
            foreach (var kind in GardenaDataPoints.Kinds)
            {
                var (serviceIdGuid, minutesGuid) = propertyGuids[index++];
                factory.CreateInterfaceType(kind.Interface, $"GARDENA.{kind.I18N}.NAME", $"GARDENA.{kind.I18N}.DESCRIPTION", int.MaxValue, int.MaxValue, false);
                factory.CreateNodeTemplate(kind.Template, $"GARDENA.{kind.I18N}.NAME", $"GARDENA.{kind.I18N}.DESCRIPTION", kind.Key, DriverGuid,
                    kind.Interface, false, false, true, false, true, NodeDataType.NoAttribute, int.MaxValue, false);
                factory.CreatePropertyTemplate(serviceIdGuid, "GARDENA.SERVICE_ID.NAME", "GARDENA.SERVICE_ID.DESCRIPTION", ServiceIdKey,
                    PropertyTemplateType.Text, kind.Template, "COMMON.CATEGORY.ADDRESS", true, false, null, "", 1, 1);
                if (kind.DefaultMinutes > 0)
                {
                    factory.CreatePropertyTemplate(minutesGuid, "GARDENA.DEFAULT_MINUTES.NAME", "GARDENA.DEFAULT_MINUTES.DESCRIPTION", DefaultMinutesKey,
                        PropertyTemplateType.Range, kind.Template, "COMMON.CATEGORY.ADDRESS", true, false, PropertyHelper.CreateRangeMetaString(1, 600), kind.DefaultMinutes, 1, 2);
                }

                foreach (var dp in GardenaDataPoints.Of(kind.Key))
                {
                    factory.CreateNodeTemplate(dp.Template, $"GARDENA.{kind.I18N}.{dp.I18N}.NAME", $"GARDENA.{kind.I18N}.{dp.I18N}.DESCRIPTION", dp.Key, kind.Interface,
                        GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, dp.Attribute != null, true, dp.IsWriteable, true, dp.Type, 1, false);
                }
            }
        }
    }
}
