using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace P3.Driver.Refoss.Driver
{
    /// <summary>
    /// Local API of the Refoss devices with Meross firmware (EM06): POST http://&lt;ip&gt;/public with a JSON message
    /// {header, payload}. The header is signed with md5(messageId + key + timestamp); the empty key works even when the
    /// device is bound to the Refoss cloud (same as the Home Assistant integration). An empty "uuid" in the header makes
    /// the device drop the connection, so none is sent.
    /// </summary>
    internal sealed class RefossMerossClient
    {
        private readonly HttpClient _http;

        public RefossMerossClient(HttpClient http, string baseUrl)
        {
            _http = http;
            BaseUrl = baseUrl;
        }

        public string BaseUrl { get; }

        /// <summary>
        /// A GET of <paramref name="ns"/>, returns the payload of the GETACK. Anything that is not a Meross answer
        /// (another HTTP status, no JSON, an ERROR) is a <see cref="RefossRpcException"/>, network errors pass through.
        /// </summary>
        public async Task<JsonElement> GetAsync(string ns, object payload = null, CancellationToken token = default)
        {
            if (string.IsNullOrEmpty(BaseUrl))
            {
                throw new RefossRpcException(0, "No address of the Refoss device configured");
            }

            var random = Convert.ToHexString(RandomNumberGenerator.GetBytes(8));
            var messageId = Md5Hex(random);
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var message = new Dictionary<string, object>
            {
                ["header"] = new Dictionary<string, object>
                {
                    ["from"] = $"/app/{random}/subscribe",
                    ["messageId"] = messageId,
                    ["method"] = "GET",
                    ["namespace"] = ns,
                    ["payloadVersion"] = 1,
                    ["sign"] = Md5Hex(messageId + timestamp.ToString(CultureInfo.InvariantCulture)),
                    ["timestamp"] = timestamp,
                    ["triggerSrc"] = "Automatica"
                },
                ["payload"] = payload ?? new Dictionary<string, object>()
            };

            using var content = new StringContent(JsonSerializer.Serialize(message), Encoding.UTF8, "application/json");
            using var response = await _http.PostAsync(BaseUrl + "/public", content, token);
            var body = await response.Content.ReadAsStringAsync(token);
            if (!response.IsSuccessStatusCode)
            {
                throw new RefossRpcException((int)response.StatusCode, $"{ns}: HTTP {(int)response.StatusCode}");
            }

            JsonDocument doc;
            try
            {
                doc = JsonDocument.Parse(body);
            }
            catch (JsonException)
            {
                throw new RefossRpcException(-1, $"{ns}: no Meross answer");
            }

            using (doc)
            {
                var root = doc.RootElement;
                var method = root.ValueKind == JsonValueKind.Object && root.TryGetProperty("header", out var header) ? RefossDataPoints.Text(header, "method") : null;
                root.TryGetProperty("payload", out var answer);
                if (method == "GETACK")
                {
                    return answer.Clone();
                }

                if (method == "ERROR" && answer.ValueKind == JsonValueKind.Object && answer.TryGetProperty("error", out var error))
                {
                    var code = RefossDataPoints.Number(error, "code") is { } c ? (int)c : -1;
                    throw new RefossRpcException(code, $"{ns}: {RefossDataPoints.Text(error, "detail")} ({code})");
                }
                throw new RefossRpcException(-1, $"{ns}: no Meross answer");
            }
        }

        private static string Md5Hex(string value) => Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

        /// <summary>
        /// Appliance.Control.ElectricityX entries in the shape of an Em.Status entry: mW / mA / mV / Wh become W / A / V / kWh,
        /// a negative month consumption is returned energy.
        /// </summary>
        public static Dictionary<int, JsonElement> Electricity(JsonElement payload)
        {
            var result = new Dictionary<int, JsonElement>();
            if (payload.ValueKind != JsonValueKind.Object || !payload.TryGetProperty("electricity", out var list))
            {
                return result;
            }

            if (list.ValueKind != JsonValueKind.Array)
            {
                return result;
            }

            foreach (var entry in list.EnumerateArray())
            {
                if (RefossDataPoints.Number(entry, "channel") is not { } channel)
                {
                    continue;
                }

                var values = new Dictionary<string, object> { ["id"] = (int)channel };
                Scale(values, "power", entry, "power", 1000);
                Scale(values, "current", entry, "current", 1000);
                Scale(values, "voltage", entry, "voltage", 1000);
                Scale(values, "pf", entry, "factor", 1);
                if (RefossDataPoints.Number(entry, "mConsume") is { } consumed)
                {
                    values["month_energy"] = Math.Max(0, consumed) / 1000;
                    values["month_ret_energy"] = Math.Max(0, -consumed) / 1000;
                }
                result[(int)channel] = JsonSerializer.SerializeToElement(values);
            }
            return result;
        }

        private static void Scale(Dictionary<string, object> values, string key, JsonElement entry, string name, double divisor)
        {
            if (RefossDataPoints.Number(entry, name) is { } v)
            {
                values[key] = v / divisor;
            }
        }
    }
}
