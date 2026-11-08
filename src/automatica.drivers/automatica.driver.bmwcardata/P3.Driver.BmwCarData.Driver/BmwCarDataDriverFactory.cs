using System;
using System.Linq;
using Automatica.Core.Base.Templates;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using NodeDataType = Automatica.Core.Base.Templates.NodeDataType;

namespace P3.Driver.BmwCarData.Driver
{
    /// <summary>
    /// BMW / MINI vehicles through the official BMW CarData customer API (read only).
    /// Account (CarData client id + device login) → vehicle (VIN, found by scan) → values
    /// </summary>
    public class BmwCarDataDriverFactory : DriverFactory
    {
        public static readonly Guid VehicleInterface = new("da4398f4-897c-4c8f-9983-fd41ad62d7b1");
        public static readonly Guid VehicleTemplate = new("ec377aa1-d036-4a50-b576-197809631e39");
        public static readonly Guid LoginStatusTemplate = new("ffda1b01-5feb-4ab8-9d57-dc2a6f28ee0d");
        public static readonly Guid StreamConnectedTemplate = new("5e6345e7-e1cd-4393-91b2-b96c9412027b");

        public const string ClientIdKey = "client_id";
        public const string PollKey = "poll";
        public const string ContainerIdKey = "container_id";
        public const string StreamingKey = "streaming";
        public const string MqttHostKey = "mqtt_host";
        public const string ApiUrlKey = "api_url";
        public const string AuthUrlKey = "auth_url";
        public const string StateKey = "token_state";
        public const string VinKey = "vin";
        public const string CustomKeyKey = "key";
        public const string LoginStatusKey = "login_status";
        public const string StreamConnectedKey = "stream_connected";

        public const string DefaultApiUrl = "https://api-cardata.bmwgroup.com";
        public const string DefaultAuthUrl = "https://customer.bmwgroup.com/gcdm/oauth";
        public const string DefaultMqttHost = "customer.streaming-cardata.bmwgroup.com:9000";

        public override string DriverName => "BmwCarData";

        public override Guid DriverGuid => new("25219e35-db7a-4e7e-88fe-b8c2ea12f7d1");

        public override Version DriverVersion => new(1, 0, 0, 0);

        public override string ImageName => "automaticacore/plugin-p3.driver.bmwcardata";

        public override bool InDevelopmentMode => false;

        public override IDriver CreateDriver(IDriverContext config)
        {
            return new BmwCarDataDriver(config);
        }

        public override void InitNodeTemplates(INodeTemplateFactory factory)
        {
            factory.CreateInterfaceType(DriverGuid, "BMWCARDATA.NAME", "BMWCARDATA.DESCRIPTION", int.MaxValue, 1, true);
            factory.CreateNodeTemplate(DriverGuid, "BMWCARDATA.NAME", "BMWCARDATA.DESCRIPTION", "bmwcardata",
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Ethernet), DriverGuid, false, false, true, false, true, NodeDataType.NoAttribute, int.MaxValue, false);

            factory.CreatePropertyTemplate(new Guid("f7057af1-5fae-4e6a-9c16-124698610c9c"), "BMWCARDATA.CLIENT_ID.NAME", "BMWCARDATA.CLIENT_ID.DESCRIPTION", ClientIdKey,
                PropertyTemplateType.Text, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, null, "", 1, 1);
            factory.CreatePropertyTemplate(new Guid("25080686-d4b6-44e2-8750-9c6c326f6847"), "BMWCARDATA.POLL.NAME", "BMWCARDATA.POLL.DESCRIPTION", PollKey,
                PropertyTemplateType.Range, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, PropertyHelper.CreateRangeMetaString(900, 86400), 3600, 1, 2);
            factory.CreatePropertyTemplate(new Guid("1b7a2d13-8fc5-4bd7-b978-823c189d6920"), "BMWCARDATA.STREAMING.NAME", "BMWCARDATA.STREAMING.DESCRIPTION", StreamingKey,
                PropertyTemplateType.Bool, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, null, false, 1, 3);
            factory.CreatePropertyTemplate(new Guid("b14bfedd-0e00-4845-97d5-c0e68eb5c4c0"), "BMWCARDATA.MQTT_HOST.NAME", "BMWCARDATA.MQTT_HOST.DESCRIPTION", MqttHostKey,
                PropertyTemplateType.Text, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, null, DefaultMqttHost, 1, 4);
            factory.CreatePropertyTemplate(new Guid("0bf524cd-694d-46a6-8885-b351d1865fb3"), "BMWCARDATA.CONTAINER_ID.NAME", "BMWCARDATA.CONTAINER_ID.DESCRIPTION", ContainerIdKey,
                PropertyTemplateType.Text, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, null, "", 1, 5);
            factory.CreatePropertyTemplate(new Guid("13216209-c034-42a7-b451-2fb4daf2b3c0"), "BMWCARDATA.API_URL.NAME", "BMWCARDATA.API_URL.DESCRIPTION", ApiUrlKey,
                PropertyTemplateType.Text, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, null, DefaultApiUrl, 1, 6);
            factory.CreatePropertyTemplate(new Guid("a4abfee2-a7e2-43a9-94a8-1eaa74d6bb42"), "BMWCARDATA.AUTH_URL.NAME", "BMWCARDATA.AUTH_URL.DESCRIPTION", AuthUrlKey,
                PropertyTemplateType.Text, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, null, DefaultAuthUrl, 1, 7);
            factory.CreatePropertyTemplate(new Guid("fc033287-96ec-4d22-925b-18554c278f6e"), "BMWCARDATA.SCAN.NAME", "BMWCARDATA.SCAN.DESCRIPTION", "scan",
                PropertyTemplateType.Scan, DriverGuid, "COMMON.CATEGORY.DISCOVERY", true, false, "", null, 0, 1);
            factory.CreatePropertyTemplate(new Guid("4317a0c5-4203-406f-9477-9a89b93b0460"), "BMWCARDATA.TOKEN_STATE.NAME", "BMWCARDATA.TOKEN_STATE.DESCRIPTION", StateKey,
                PropertyTemplateType.Text, DriverGuid, "", false, true, null, null, 0, 0);

            factory.CreateNodeTemplate(LoginStatusTemplate, "BMWCARDATA.LOGIN_STATUS.NAME", "BMWCARDATA.LOGIN_STATUS.DESCRIPTION", LoginStatusKey, DriverGuid,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.String, 1, false);
            factory.CreateNodeTemplate(StreamConnectedTemplate, "BMWCARDATA.STREAM_CONNECTED.NAME", "BMWCARDATA.STREAM_CONNECTED.DESCRIPTION", StreamConnectedKey, DriverGuid,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, NodeDataType.Boolean, 1, false);

            factory.CreateInterfaceType(VehicleInterface, "BMWCARDATA.VEHICLE.NAME", "BMWCARDATA.VEHICLE.DESCRIPTION", int.MaxValue, int.MaxValue, false);
            factory.CreateNodeTemplate(VehicleTemplate, "BMWCARDATA.VEHICLE.NAME", "BMWCARDATA.VEHICLE.DESCRIPTION", "vehicle", DriverGuid,
                VehicleInterface, false, false, true, false, true, NodeDataType.NoAttribute, int.MaxValue, false);
            factory.CreatePropertyTemplate(new Guid("613f3eaa-99d2-4118-b621-7e5f49a1087f"), "BMWCARDATA.VEHICLE.VIN.NAME", "BMWCARDATA.VEHICLE.VIN.DESCRIPTION", VinKey,
                PropertyTemplateType.Text, VehicleTemplate, "COMMON.CATEGORY.ADDRESS", true, false, null, "", 1, 1);

            foreach (var dp in BmwCarDataDataPoints.All)
            {
                var custom = BmwCarDataDataPoints.IsCustom(dp.Key);
                factory.CreateNodeTemplate(dp.Template, $"BMWCARDATA.VEHICLE.{dp.I18N}.NAME", $"BMWCARDATA.VEHICLE.{dp.I18N}.DESCRIPTION", dp.Key, VehicleInterface,
                    GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), dp.DefaultCreated, true, true, false, true, dp.Type, custom ? int.MaxValue : 1, false);
            }

            foreach (var (key, guid) in new[]
                     {
                         (BmwCarDataDataPoints.CustomNumberKey, new Guid("f783b67c-4275-42fb-a04b-e499c3e1d596")),
                         (BmwCarDataDataPoints.CustomTextKey, new Guid("c41a46e6-7de9-43a3-abcc-53574d48c22f"))
                     })
            {
                var template = BmwCarDataDataPoints.All.First(d => d.Key == key).Template;
                factory.CreatePropertyTemplate(guid, "BMWCARDATA.VEHICLE.CUSTOM_KEY.NAME", "BMWCARDATA.VEHICLE.CUSTOM_KEY.DESCRIPTION", CustomKeyKey,
                    PropertyTemplateType.Text, template, "COMMON.CATEGORY.ADDRESS", true, false, null, "", 1, 1);
            }
        }
    }
}
