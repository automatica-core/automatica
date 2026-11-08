using System;
using Automatica.Core.Base.Templates;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using NodeDataType = Automatica.Core.Base.Templates.NodeDataType;

namespace P3.Driver.SpotPrice.Driver
{
    /// <summary>
    /// Day-ahead electricity prices from aWATTar (AT/DE, no key) or ENTSO-E (any bidding zone, security token),
    /// with cheapest/most expensive window outputs (Loxone "Spot Price Optimizer").
    /// </summary>
    public class SpotPriceDriverFactory : DriverFactory
    {
        public static readonly Guid WindowTemplate = new("66869875-3737-4eff-bd54-12f12f80d749");

        public const string ProviderKey = "provider";
        public const string ApiUrlKey = "api_url";
        public const string TokenKey = "entsoe_token";
        public const string ZoneKey = "entsoe_zone";
        public const string SurchargeKey = "surcharge";
        public const string VatKey = "vat";
        public const string RefreshKey = "refresh";

        public const string WindowKey = "window";
        public const string WindowHoursKey = "hours";
        public const string WindowPeriodKey = "period";
        public const string WindowContiguousKey = "contiguous";
        public const string WindowModeKey = "mode";

        public override string DriverName => "SpotPrice";

        public override Guid DriverGuid => new("515feaea-b0c5-45b4-97ee-bffea6f4262e");

        public override Version DriverVersion => new(1, 0, 0, 0);

        public override string ImageName => "automaticacore/plugin-p3.driver.spotprice";

        public override bool InDevelopmentMode => false;

        public override IDriver CreateDriver(IDriverContext config)
        {
            return new SpotPriceDriver(config);
        }

        public override void InitNodeTemplates(INodeTemplateFactory factory)
        {
            factory.CreateInterfaceType(DriverGuid, "SPOT_PRICE.NAME", "SPOT_PRICE.DESCRIPTION", int.MaxValue, 1, true);
            factory.CreateNodeTemplate(DriverGuid, "SPOT_PRICE.NAME", "SPOT_PRICE.DESCRIPTION", "spot-price",
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Ethernet), DriverGuid, false, false, true, false, true, NodeDataType.NoAttribute, int.MaxValue, false);

            const string address = "COMMON.CATEGORY.ADDRESS";
            factory.CreatePropertyTemplate(new Guid("c173b61a-b764-4320-8b29-3dc69246ef43"), "SPOT_PRICE.PROVIDER.NAME", "SPOT_PRICE.PROVIDER.DESCRIPTION", ProviderKey,
                PropertyTemplateType.Enum, DriverGuid, address, true, false, PropertyHelper.CreateEnumMetaString(typeof(SpotPriceProvider)), (int)SpotPriceProvider.AwattarAt, 1, 1);
            factory.CreatePropertyTemplate(new Guid("38fedf79-aefb-40a1-bc90-f4e5968dcd14"), "SPOT_PRICE.TOKEN.NAME", "SPOT_PRICE.TOKEN.DESCRIPTION", TokenKey,
                PropertyTemplateType.Password, DriverGuid, address, true, false, null, "", 1, 2);
            factory.CreatePropertyTemplate(new Guid("429fa3af-5254-4376-8aa6-24cb181f8527"), "SPOT_PRICE.ZONE.NAME", "SPOT_PRICE.ZONE.DESCRIPTION", ZoneKey,
                PropertyTemplateType.Text, DriverGuid, address, true, false, null, EntsoeSource.AustriaZone, 1, 3);
            factory.CreatePropertyTemplate(new Guid("63087a8f-818f-4b18-993d-c42ed439e54f"), "SPOT_PRICE.SURCHARGE.NAME", "SPOT_PRICE.SURCHARGE.DESCRIPTION", SurchargeKey,
                PropertyTemplateType.Numeric, DriverGuid, address, true, false, null, 0d, 1, 4);
            factory.CreatePropertyTemplate(new Guid("6b8a46b7-b3ed-48f2-8cb2-9dc90a0d5b52"), "SPOT_PRICE.VAT.NAME", "SPOT_PRICE.VAT.DESCRIPTION", VatKey,
                PropertyTemplateType.Numeric, DriverGuid, address, true, false, null, 0d, 1, 5);
            factory.CreatePropertyTemplate(new Guid("f4a9c2e1-7b3d-4e58-a6c0-91d2e8b5f374"), "SPOT_PRICE.REFRESH.NAME", "SPOT_PRICE.REFRESH.DESCRIPTION", RefreshKey,
                PropertyTemplateType.Range, DriverGuid, address, true, false, PropertyHelper.CreateRangeMetaString(15, 1440), 60, 1, 6);
            factory.CreatePropertyTemplate(new Guid("0d6e3b8a-52c4-4f19-b7e2-6a8c1f4d9e05"), "SPOT_PRICE.API_URL.NAME", "SPOT_PRICE.API_URL.DESCRIPTION", ApiUrlKey,
                PropertyTemplateType.Text, DriverGuid, address, true, false, null, "", 1, 7);

            foreach (var dp in SpotPriceDataPoints.All)
            {
                factory.CreateNodeTemplate(dp.Template, $"SPOT_PRICE.{dp.I18N}.NAME", $"SPOT_PRICE.{dp.I18N}.DESCRIPTION", dp.Key, DriverGuid,
                    GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), true, true, true, false, true, dp.Type, 1, false);
            }

            factory.CreateNodeTemplate(WindowTemplate, "SPOT_PRICE.WINDOW.NAME", "SPOT_PRICE.WINDOW.DESCRIPTION", WindowKey, DriverGuid,
                GuidTemplateTypeAttribute.GetFromEnum(InterfaceTypeEnum.Value), false, true, true, false, true, NodeDataType.Boolean, int.MaxValue, false, true);
            factory.CreatePropertyTemplate(new Guid("9e27d4b1-3c8a-4f6e-85d0-b2a7c6e1f938"), "SPOT_PRICE.WINDOW.HOURS.NAME", "SPOT_PRICE.WINDOW.HOURS.DESCRIPTION", WindowHoursKey,
                PropertyTemplateType.Numeric, WindowTemplate, address, true, false, null, 3d, 0, 1);
            factory.CreatePropertyTemplate(new Guid("5a81f3c6-d29e-4b07-9c4a-e3f6b8d2a154"), "SPOT_PRICE.WINDOW.PERIOD.NAME", "SPOT_PRICE.WINDOW.PERIOD.DESCRIPTION", WindowPeriodKey,
                PropertyTemplateType.Numeric, WindowTemplate, address, true, false, null, 24d, 0, 2);
            factory.CreatePropertyTemplate(new Guid("c7b2e5d9-81f4-4a3c-b6e0-4d9a2f7c1e86"), "SPOT_PRICE.WINDOW.CONTIGUOUS.NAME", "SPOT_PRICE.WINDOW.CONTIGUOUS.DESCRIPTION", WindowContiguousKey,
                PropertyTemplateType.Bool, WindowTemplate, address, true, false, null, true, 0, 3);
            factory.CreatePropertyTemplate(new Guid("2f8d6a4c-b1e3-4c97-a5d8-7e0c3b9f6a21"), "SPOT_PRICE.WINDOW.MODE.NAME", "SPOT_PRICE.WINDOW.MODE.DESCRIPTION", WindowModeKey,
                PropertyTemplateType.Enum, WindowTemplate, address, true, false, PropertyHelper.CreateEnumMetaString(typeof(SpotWindowMode)), (int)SpotWindowMode.Cheapest, 0, 4);
        }
    }
}
