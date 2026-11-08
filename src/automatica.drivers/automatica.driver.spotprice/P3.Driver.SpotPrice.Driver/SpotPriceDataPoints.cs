using System;
using System.Collections.Generic;
using NodeDataType = Automatica.Core.Base.Templates.NodeDataType;

namespace P3.Driver.SpotPrice.Driver
{
    internal sealed record SpotPriceDataPoint(Guid Template, string Key, string I18N, NodeDataType Type);

    /// <summary>
    /// Prices in EUR/kWh. "Price" values include surcharge and VAT, the market price does not.
    /// </summary>
    internal static class SpotPriceDataPoints
    {
        public const string ConnectedKey = "connected";

        public static readonly IReadOnlyList<SpotPriceDataPoint> All = new List<SpotPriceDataPoint>
        {
            new(new Guid("3bfbf9e4-5ac0-40a6-97cb-a354d5efc5a1"), "price_now", "PRICE_NOW", NodeDataType.Double),
            new(new Guid("46b4fa5f-621b-47cd-9e6b-6781ad1ea62f"), "market_price_now", "MARKET_PRICE_NOW", NodeDataType.Double),
            new(new Guid("ce682dbc-f1c7-4683-896d-ddd2b263023b"), "price_next_hour", "PRICE_NEXT_HOUR", NodeDataType.Double),
            new(new Guid("8f7b37da-0418-4d08-b17d-0b821693ddf2"), "price_min_today", "PRICE_MIN_TODAY", NodeDataType.Double),
            new(new Guid("0f2f77e5-0bd8-4b9d-9121-5df061fdf7c6"), "price_max_today", "PRICE_MAX_TODAY", NodeDataType.Double),
            new(new Guid("037165c8-86e7-4d94-9a4d-6c8ba0c9066f"), "price_avg_today", "PRICE_AVG_TODAY", NodeDataType.Double),
            new(new Guid("1d81fcaf-24f8-49ca-979e-b8f932dd2a7a"), "prices_until", "PRICES_UNTIL", NodeDataType.DateTime),
            new(new Guid("1af76f0b-94e6-4aca-a0f1-89e1fd5baee7"), ConnectedKey, "CONNECTED", NodeDataType.Boolean),
        };
    }
}
