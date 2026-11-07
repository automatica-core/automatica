using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using NodeDataType = Automatica.Core.Base.Templates.NodeDataType;

namespace P3.Driver.Tesla.Driver
{
    /// <summary>
    /// Tesla vehicles through the official Fleet API. Account (client id + refresh token) → vehicle (VIN) → data points.
    /// </summary>
    public class TeslaDriverFactory : DriverFactory
    {
        public static readonly Guid VehicleInterface = new("3a183369-f90d-4f2f-a401-d552dccbd59d");
        public static readonly Guid VehicleTemplate = new("1ddf8127-0f56-47d4-a116-52471b988d2b");

        public const string ApiUrlKey = "api_url";
        public const string ClientIdKey = "client_id";
        public const string RefreshTokenKey = "refresh_token";
        public const string CommandUrlKey = "command_url";
        public const string PollKey = "poll";
        public const string TokenStateKey = "token_state";
        public const string VinKey = "vin";

        public const string DefaultApiUrl = "https://fleet-api.prd.eu.vn.cloud.tesla.com";

        public override string DriverName => "Tesla";

        public override Guid DriverGuid => new("af4a93fa-5dfa-4c93-b14b-8096d76f1f44");

        public override Version DriverVersion => new(1, 0, 0, 0);

        public override string ImageName => "automaticacore/plugin-p3.driver.tesla";

        public override bool InDevelopmentMode => false;

        public override IDriver CreateDriver(IDriverContext config)
        {
            return new TeslaDriver(config);
        }

        public override void InitNodeTemplates(INodeTemplateFactory factory)
        {
            factory.CreateInterfaceType(DriverGuid, "TESLA.NAME", "TESLA.DESCRIPTION", int.MaxValue, 1, true);
            factory.CreateNodeTemplate(DriverGuid, "TESLA.NAME", "TESLA.DESCRIPTION", "tesla",
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Ethernet), DriverGuid, false, false, true, false, true, NodeDataType.NoAttribute, int.MaxValue, false);

            factory.CreatePropertyTemplate(new Guid("249b409c-dad7-4478-bd69-b5ddd2ff041e"), "TESLA.API_URL.NAME", "TESLA.API_URL.DESCRIPTION", ApiUrlKey,
                PropertyTemplateType.Text, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, null, DefaultApiUrl, 1, 1);
            factory.CreatePropertyTemplate(new Guid("e0f57aba-f825-46cf-aa04-abd2d3a7e7b9"), "TESLA.CLIENT_ID.NAME", "TESLA.CLIENT_ID.DESCRIPTION", ClientIdKey,
                PropertyTemplateType.Text, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, null, "", 1, 2);
            factory.CreatePropertyTemplate(new Guid("6935ce98-0a40-41ba-b36c-7dad5ba1750f"), "TESLA.REFRESH_TOKEN.NAME", "TESLA.REFRESH_TOKEN.DESCRIPTION", RefreshTokenKey,
                PropertyTemplateType.Password, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, null, "", 1, 3);
            factory.CreatePropertyTemplate(new Guid("04370b05-e2c3-4aae-84e6-01c30e76663d"), "TESLA.COMMAND_URL.NAME", "TESLA.COMMAND_URL.DESCRIPTION", CommandUrlKey,
                PropertyTemplateType.Text, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, null, "", 1, 4);
            factory.CreatePropertyTemplate(new Guid("e5c4ae6f-7480-448d-a0e5-25c3a9527122"), "TESLA.POLL.NAME", "TESLA.POLL.DESCRIPTION", PollKey,
                PropertyTemplateType.Range, DriverGuid, "COMMON.CATEGORY.ADDRESS", true, false, PropertyHelper.CreateRangeMetaString(60, 3600), 300, 1, 5);
            // the rotated refresh token, Tesla invalidates the configured one after its first use
            factory.CreatePropertyTemplate(new Guid("93f03b45-ac36-4120-a85d-ab15b7051b4c"), "TESLA.TOKEN_STATE.NAME", "TESLA.TOKEN_STATE.DESCRIPTION", TokenStateKey,
                PropertyTemplateType.Text, DriverGuid, "", false, true, null, null, 0, 0);

            factory.CreateInterfaceType(VehicleInterface, "TESLA.VEHICLE.NAME", "TESLA.VEHICLE.DESCRIPTION", int.MaxValue, int.MaxValue, false);
            factory.CreateNodeTemplate(VehicleTemplate, "TESLA.VEHICLE.NAME", "TESLA.VEHICLE.DESCRIPTION", "vehicle", DriverGuid,
                VehicleInterface, false, false, true, false, true, NodeDataType.NoAttribute, int.MaxValue, false);
            factory.CreatePropertyTemplate(new Guid("ab9cc4a5-8212-440a-9498-c72bcb40e72f"), "TESLA.VEHICLE.VIN.NAME", "TESLA.VEHICLE.VIN.DESCRIPTION", VinKey,
                PropertyTemplateType.Text, VehicleTemplate, "COMMON.CATEGORY.ADDRESS", true, false, null, "", 1, 1);

            foreach (var dp in TeslaDataPoints.All)
            {
                factory.CreateNodeTemplate(dp.Template, $"TESLA.VEHICLE.{dp.I18N}.NAME", $"TESLA.VEHICLE.{dp.I18N}.DESCRIPTION", dp.Key, VehicleInterface,
                    GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, dp.Read != null || dp.Key == TeslaDataPoints.StateKey, true,
                    dp.IsWriteable, true, dp.Type, 1, false);
            }
        }
    }
}
