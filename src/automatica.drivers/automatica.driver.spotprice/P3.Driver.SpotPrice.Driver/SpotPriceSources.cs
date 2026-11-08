using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Automatica.Core.Base.Templates;

namespace P3.Driver.SpotPrice.Driver
{
    public enum SpotPriceProvider
    {
        [EnumName("SPOT_PRICE.PROVIDER.AWATTAR_AT")]
        AwattarAt = 0,
        [EnumName("SPOT_PRICE.PROVIDER.AWATTAR_DE")]
        AwattarDe = 1,
        [EnumName("SPOT_PRICE.PROVIDER.ENTSOE")]
        Entsoe = 2
    }

    internal sealed class SpotPriceException : Exception
    {
        public SpotPriceException(string message) : base(message)
        {
        }
    }

    internal interface ISpotPriceSource
    {
        Task<IReadOnlyList<PriceSlot>> FetchAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken token);
    }

    /// <summary>
    /// aWATTar market data (no key, fair use 100 requests a day): GET /v1/marketdata?start=ms&amp;end=ms, prices in EUR/MWh
    /// </summary>
    internal sealed class AwattarSource : ISpotPriceSource
    {
        public const string DefaultAtUrl = "https://api.awattar.at";
        public const string DefaultDeUrl = "https://api.awattar.de";

        private readonly HttpClient _http;
        private readonly string _baseUrl;

        public AwattarSource(HttpClient http, string baseUrl)
        {
            _http = http;
            _baseUrl = baseUrl.TrimEnd('/');
        }

        public async Task<IReadOnlyList<PriceSlot>> FetchAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken token)
        {
            var url = $"{_baseUrl}/v1/marketdata?start={from.ToUnixTimeMilliseconds()}&end={to.ToUnixTimeMilliseconds()}";
            using var response = await _http.GetAsync(url, token);
            var text = await response.Content.ReadAsStringAsync(token);
            if (!response.IsSuccessStatusCode)
            {
                throw new SpotPriceException($"aWATTar answered {(int)response.StatusCode}");
            }
            return Parse(text);
        }

        internal static IReadOnlyList<PriceSlot> Parse(string json)
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array)
            {
                throw new SpotPriceException("aWATTar response has no data");
            }

            var slots = new List<PriceSlot>();
            foreach (var item in data.EnumerateArray())
            {
                if (!item.TryGetProperty("start_timestamp", out var start) || !item.TryGetProperty("end_timestamp", out var end) ||
                    !item.TryGetProperty("marketprice", out var price) || price.ValueKind != JsonValueKind.Number)
                {
                    continue;
                }
                var unit = item.TryGetProperty("unit", out var u) ? u.GetString() : "Eur/MWh";
                var market = price.GetDouble();
                if (string.Equals(unit, "Eur/kWh", StringComparison.OrdinalIgnoreCase))
                {
                    market *= 1000;
                }
                slots.Add(new PriceSlot(DateTimeOffset.FromUnixTimeMilliseconds(start.GetInt64()), DateTimeOffset.FromUnixTimeMilliseconds(end.GetInt64()), market));
            }
            return slots;
        }
    }

    /// <summary>
    /// ENTSO-E transparency platform, day-ahead prices (document type A44) of one bidding zone. Needs a security token.
    /// </summary>
    internal sealed class EntsoeSource : ISpotPriceSource
    {
        public const string DefaultUrl = "https://web-api.tp.entsoe.eu/api";
        public const string AustriaZone = "10YAT-APG------L";

        private readonly HttpClient _http;
        private readonly string _url;
        private readonly string _token;
        private readonly string _zone;

        public EntsoeSource(HttpClient http, string url, string token, string zone)
        {
            _http = http;
            _url = url.TrimEnd('/');
            _token = token;
            _zone = string.IsNullOrWhiteSpace(zone) ? AustriaZone : zone.Trim();
        }

        public async Task<IReadOnlyList<PriceSlot>> FetchAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(_token))
            {
                throw new SpotPriceException("No ENTSO-E security token configured");
            }
            var url = $"{_url}?securityToken={Uri.EscapeDataString(_token)}&documentType=A44&contract_MarketAgreement.type=A01" +
                      $"&in_Domain={Uri.EscapeDataString(_zone)}&out_Domain={Uri.EscapeDataString(_zone)}" +
                      $"&periodStart={Format(from)}&periodEnd={Format(to)}";
            using var response = await _http.GetAsync(url, token);
            var text = await response.Content.ReadAsStringAsync(token);
            // the URL carries the token: it must never end up in a message
            if (!response.IsSuccessStatusCode)
            {
                throw new SpotPriceException($"ENTSO-E answered {(int)response.StatusCode}: {Reason(text) ?? response.ReasonPhrase}");
            }
            return Parse(text);
        }

        private static string Format(DateTimeOffset time) => time.UtcDateTime.ToString("yyyyMMddHHmm", CultureInfo.InvariantCulture);

        private static XDocument Load(string xml)
        {
            try
            {
                return XDocument.Parse(xml);
            }
            catch (XmlException ex)
            {
                throw new SpotPriceException($"ENTSO-E response is no XML: {ex.Message}");
            }
        }

        internal static string Reason(string xml)
        {
            try
            {
                var doc = XDocument.Parse(xml);
                return doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "Reason")?.Elements().FirstOrDefault(e => e.Name.LocalName == "text")?.Value;
            }
            catch (XmlException)
            {
                return null;
            }
        }

        /// <summary>
        /// Reads all periods; curve type A03 leaves out points that repeat the previous price. Where 15 and 60 minute series
        /// overlap, the schedule keeps the finer one.
        /// </summary>
        internal static IReadOnlyList<PriceSlot> Parse(string xml)
        {
            var doc = Load(xml);
            var root = doc.Root!;
            if (root.Name.LocalName == "Acknowledgement_MarketDocument")
            {
                var reason = Reason(xml) ?? "";
                if (reason.Contains("No matching data found", StringComparison.OrdinalIgnoreCase))
                {
                    return Array.Empty<PriceSlot>();
                }
                throw new SpotPriceException($"ENTSO-E: {reason}");
            }

            var slots = new List<PriceSlot>();
            foreach (var period in root.Descendants().Where(e => e.Name.LocalName == "Period"))
            {
                var interval = Child(period, "timeInterval");
                if (!DateTimeOffset.TryParse(Child(interval, "start")?.Value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var start) ||
                    !DateTimeOffset.TryParse(Child(interval, "end")?.Value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var end))
                {
                    continue;
                }
                TimeSpan resolution;
                try
                {
                    resolution = XmlConvert.ToTimeSpan(Child(period, "resolution")?.Value ?? "");
                }
                catch (FormatException)
                {
                    continue;
                }
                if (resolution <= TimeSpan.Zero)
                {
                    continue;
                }

                var points = new Dictionary<int, double>();
                foreach (var point in period.Elements().Where(e => e.Name.LocalName == "Point"))
                {
                    if (int.TryParse(Child(point, "position")?.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var position) &&
                        double.TryParse(Child(point, "price.amount")?.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var price))
                    {
                        points[position] = price;
                    }
                }

                var count = (int)Math.Round((end - start) / resolution);
                double? last = null;
                for (var position = 1; position <= count; position++)
                {
                    if (points.TryGetValue(position, out var price))
                    {
                        last = price;
                    }
                    if (last.HasValue)
                    {
                        var slotStart = start + resolution * (position - 1);
                        slots.Add(new PriceSlot(slotStart, slotStart + resolution, last.Value));
                    }
                }
            }
            return slots;
        }

        private static XElement Child(XElement element, string name) => element?.Elements().FirstOrDefault(e => e.Name.LocalName == name);
    }
}
