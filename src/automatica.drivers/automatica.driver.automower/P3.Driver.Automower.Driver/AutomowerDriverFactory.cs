using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using NodeDataType = Automatica.Core.Base.Templates.NodeDataType;

namespace P3.Driver.Automower.Driver
{
    /// <summary>
    /// Husqvarna Automower robot mowers through the official Automower Connect API.
    /// Account (application key/secret) → mower (id, found by scan) → data points
    /// </summary>
    public class AutomowerDriverFactory : DriverFactory
    {
        public static readonly Guid MowerInterface = new("31b711b5-8cac-4b53-8543-46515cabf22a");
        public static readonly Guid MowerTemplate = new("23a23152-7059-44e1-8691-58adce39e4b4");
        public static readonly Guid StatusTemplate = new("ef6153dd-875b-493d-9f3b-c04f37aa1ad6");
        public static readonly Guid SocketConnectedTemplate = new("1098029b-506c-407f-8e07-1b9a6a0db6e8");

        public const string AuthUrlKey = "auth_url";
        public const string ApiUrlKey = "api_url";
        public const string WebSocketUrlKey = "websocket_url";
        public const string ApplicationKeyKey = "application_key";
        public const string ApplicationSecretKey = "application_secret";
        public const string PollKey = "poll";
        public const string MowerIdKey = "mower_id";
        public const string DefaultMinutesKey = "default_minutes";
        public const string StatusKey = "status";
        public const string SocketConnectedKey = "socket_connected";

        public const string DefaultAuthUrl = "https://api.authentication.husqvarnagroup.dev/v1/oauth2/token";
        public const string DefaultApiUrl = "https://api.amc.husqvarna.dev/v1";
        public const string DefaultWebSocketUrl = "wss://ws.openapi.husqvarna.dev/v1";

        public override string DriverName => "Automower";

        public override Guid DriverGuid => new("29dee0d7-ce71-4965-bb84-257ea36f4cbf");

        public override Version DriverVersion => new(1, 0, 0, 0);

        public override string ImageName => "automaticacore/plugin-p3.driver.automower";

        public override bool InDevelopmentMode => false;

        public override IDriver CreateDriver(IDriverContext config)
        {
            return new AutomowerDriver(config);
        }

        public override void InitNodeTemplates(INodeTemplateFactory factory)
        {
            factory.CreateInterfaceType(DriverGuid, "AUTOMOWER.NAME", "AUTOMOWER.DESCRIPTION", int.MaxValue, 1, true);
            factory.CreateNodeTemplate(DriverGuid, "AUTOMOWER.NAME", "AUTOMOWER.DESCRIPTION", "automower",
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Ethernet), DriverGuid, false, false, true, false, true, NodeDataType.NoAttribute, int.MaxValue, false);

            factory.CreatePropertyTemplate(new Guid("b8fe4988-36a5-4c68-b523-25ee87e666a8"), "AUTOMOWER.APPLICATION_KEY.NAME", "AUTOMOWER.APPLICATION_KEY.DESCRIPTION", ApplicationKeyKey,
                PropertyTemplateType.Text, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, null, "", 1, 1);
            factory.CreatePropertyTemplate(new Guid("ed866c7a-b0c3-4be1-93e8-c4c7bb1d2195"), "AUTOMOWER.APPLICATION_SECRET.NAME", "AUTOMOWER.APPLICATION_SECRET.DESCRIPTION", ApplicationSecretKey,
                PropertyTemplateType.Password, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, null, "", 1, 2);
            factory.CreatePropertyTemplate(new Guid("13410285-f86b-4288-b2b2-61d082b3eb90"), "AUTOMOWER.POLL.NAME", "AUTOMOWER.POLL.DESCRIPTION", PollKey,
                PropertyTemplateType.Range, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, PropertyHelper.CreateRangeMetaString(300, 86400), 900, 1, 3);
            factory.CreatePropertyTemplate(new Guid("ef4f02f1-8197-4491-944d-82b92f218deb"), "AUTOMOWER.API_URL.NAME", "AUTOMOWER.API_URL.DESCRIPTION", ApiUrlKey,
                PropertyTemplateType.Text, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, null, DefaultApiUrl, 1, 4);
            factory.CreatePropertyTemplate(new Guid("7dfee0f7-6f54-485e-afb4-57bc2a1baec1"), "AUTOMOWER.WEBSOCKET_URL.NAME", "AUTOMOWER.WEBSOCKET_URL.DESCRIPTION", WebSocketUrlKey,
                PropertyTemplateType.Text, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, null, DefaultWebSocketUrl, 1, 5);
            factory.CreatePropertyTemplate(new Guid("aace2c1c-579e-4e5a-9f92-4c967d51d8e0"), "AUTOMOWER.AUTH_URL.NAME", "AUTOMOWER.AUTH_URL.DESCRIPTION", AuthUrlKey,
                PropertyTemplateType.Text, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, null, DefaultAuthUrl, 1, 6);
            factory.CreatePropertyTemplate(new Guid("3e05a5e5-9b88-4d38-8216-c22a587364e9"), "AUTOMOWER.SCAN.NAME", "AUTOMOWER.SCAN.DESCRIPTION", "scan",
                PropertyTemplateType.Scan, DriverGuid, "COMMON.CATEGORY.DISCOVERY", true, false, "", null, 0, 1);

            factory.CreateNodeTemplate(StatusTemplate, "AUTOMOWER.STATUS.NAME", "AUTOMOWER.STATUS.DESCRIPTION", StatusKey, DriverGuid,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.String, 1, false);
            factory.CreateNodeTemplate(SocketConnectedTemplate, "AUTOMOWER.SOCKET_CONNECTED.NAME", "AUTOMOWER.SOCKET_CONNECTED.DESCRIPTION", SocketConnectedKey, DriverGuid,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.Boolean, 1, false);

            factory.CreateInterfaceType(MowerInterface, "AUTOMOWER.MOWER.NAME", "AUTOMOWER.MOWER.DESCRIPTION", int.MaxValue, int.MaxValue, false);
            factory.CreateNodeTemplate(MowerTemplate, "AUTOMOWER.MOWER.NAME", "AUTOMOWER.MOWER.DESCRIPTION", "mower", DriverGuid,
                MowerInterface, false, false, true, false, true, NodeDataType.NoAttribute, int.MaxValue, false);
            factory.CreatePropertyTemplate(new Guid("d512d0bb-09b7-4a7c-801a-4e4bdffa42d7"), "AUTOMOWER.MOWER.MOWER_ID.NAME", "AUTOMOWER.MOWER.MOWER_ID.DESCRIPTION", MowerIdKey,
                PropertyTemplateType.Text, MowerTemplate, "COMMON.CATEGORY.ADDRESS", true, false, null, "", 1, 1);
            factory.CreatePropertyTemplate(new Guid("d7d0fbc4-e71b-4b72-900e-5f18ba4efdce"), "AUTOMOWER.MOWER.DEFAULT_MINUTES.NAME", "AUTOMOWER.MOWER.DEFAULT_MINUTES.DESCRIPTION", DefaultMinutesKey,
                PropertyTemplateType.Range, MowerTemplate, "COMMON.CATEGORY.ADDRESS", true, false, PropertyHelper.CreateRangeMetaString(1, 1440), 60, 1, 2);

            foreach (var dp in AutomowerDataPoints.All)
            {
                factory.CreateNodeTemplate(dp.Template, $"AUTOMOWER.MOWER.{dp.I18N}.NAME", $"AUTOMOWER.MOWER.{dp.I18N}.DESCRIPTION", dp.Key, MowerInterface,
                    GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, dp.Read != null, true, dp.IsWriteable, true, dp.Type, 1, false);
            }
        }
    }
}
