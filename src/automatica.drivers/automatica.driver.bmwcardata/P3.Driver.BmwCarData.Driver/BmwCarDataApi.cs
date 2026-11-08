using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace P3.Driver.BmwCarData.Driver
{
    internal sealed class BmwCarDataApiException : Exception
    {
        public BmwCarDataApiException(string message, HttpStatusCode? statusCode = null, string errorId = null) : base(message)
        {
            StatusCode = statusCode;
            ErrorId = errorId;
        }

        public HttpStatusCode? StatusCode { get; }

        /// <summary>
        /// exveErrorId, e.g. CU-105 (no permission for the container), CU-124 (container limit), CU-402 (unknown telematic key)
        /// </summary>
        public string ErrorId { get; }
    }

    internal sealed class RateLimitedException : Exception
    {
        public RateLimitedException(DateTimeOffset until) : base($"BMW CarData daily request limit reached, paused until {until:u}")
        {
            Until = until;
        }

        public DateTimeOffset Until { get; }
    }

    internal sealed record TelematicValue(object Value, string Unit, DateTimeOffset? Timestamp);

    internal sealed record BmwContainer(string Id, string Name, string State);

    /// <summary>
    /// BMW CarData customer API, https://bmw-cardata.bmwgroup.com/customer/public/api-documentation
    /// 50 requests per day and user, reset at 00:00 UTC. Read only.
    /// </summary>
    internal sealed class BmwCarDataApi
    {
        public const int DailyLimit = 50;

        private readonly HttpClient _http;
        private readonly ILogger _logger;
        private readonly Func<DateTimeOffset> _now;
        private readonly object _countLock = new();
        private DateOnly _countDay;
        private int _count;

        public BmwCarDataApi(HttpClient http, ILogger logger, Func<DateTimeOffset> now, string baseUrl, OAuthTokenClient tokens)
        {
            _http = http;
            _logger = logger;
            _now = now;
            BaseUrl = baseUrl.TrimEnd('/');
            Tokens = tokens;
        }

        public string BaseUrl { get; }
        public OAuthTokenClient Tokens { get; }
        public DateTimeOffset BlockedUntil { get; private set; } = DateTimeOffset.MinValue;
        public bool IsBlocked => _now() < BlockedUntil;

        /// <summary>
        /// Requests sent today (UTC)
        /// </summary>
        public int RequestsToday
        {
            get
            {
                lock (_countLock)
                {
                    return _countDay == DateOnly.FromDateTime(_now().UtcDateTime) ? _count : 0;
                }
            }
        }

        private DateTimeOffset NextReset()
        {
            var today = _now().UtcDateTime.Date;
            return new DateTimeOffset(today.AddDays(1), TimeSpan.Zero);
        }

        private void Count()
        {
            lock (_countLock)
            {
                var today = DateOnly.FromDateTime(_now().UtcDateTime);
                if (_countDay != today)
                {
                    _countDay = today;
                    _count = 0;
                }
                if (_count >= DailyLimit)
                {
                    BlockedUntil = NextReset();
                    throw new RateLimitedException(BlockedUntil);
                }
                _count++;
            }
        }

        private async Task<JsonNode> SendAsync(HttpMethod method, string path, object body, CancellationToken token)
        {
            if (IsBlocked)
            {
                throw new RateLimitedException(BlockedUntil);
            }
            for (var attempt = 0; ; attempt++)
            {
                var accessToken = await Tokens.GetAccessTokenAsync(attempt > 0, token);
                using var request = new HttpRequestMessage(method, BaseUrl + path);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                request.Headers.Add("x-version", "v1");
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                if (body != null)
                {
                    request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
                }

                Count();
                using var response = await _http.SendAsync(request, token);
                var text = await response.Content.ReadAsStringAsync(token);
                JsonNode json = null;
                try
                {
                    json = string.IsNullOrWhiteSpace(text) ? null : JsonNode.Parse(text);
                }
                catch (JsonException)
                {
                    // reported below
                }
                var errorId = json is JsonObject o ? o["exveErrorId"]?.ToString() : null;

                if (response.StatusCode == HttpStatusCode.Unauthorized && attempt == 0)
                {
                    _logger.LogInformation("BMW CarData rejected the access token, refreshing it");
                    continue;
                }
                if (response.StatusCode == HttpStatusCode.TooManyRequests || errorId == "CU-429")
                {
                    BlockedUntil = NextReset();
                    _logger.LogWarning($"BMW CarData daily limit reached, pausing until {BlockedUntil:u}");
                    throw new RateLimitedException(BlockedUntil);
                }
                if (!response.IsSuccessStatusCode)
                {
                    var message = json is JsonObject e ? e["exveErrorMsg"]?.ToString() : null;
                    throw new BmwCarDataApiException($"{method} {path.Split('?')[0]} failed ({(int)response.StatusCode} {errorId}): {message ?? Truncate(text)}", response.StatusCode, errorId);
                }
                return json;
            }
        }

        private static string Truncate(string text) => text == null ? "" : text.Length > 300 ? text[..300] : text;

        /// <summary>
        /// VINs mapped to the account (the spec shows one object, the service answers a list)
        /// </summary>
        public async Task<IReadOnlyList<(string Vin, string MappingType)>> GetMappingsAsync(CancellationToken token = default)
        {
            var json = await SendAsync(HttpMethod.Get, "/customers/vehicles/mappings", null, token);
            IEnumerable<JsonNode> items = json switch
            {
                JsonArray array => array,
                JsonObject obj when obj["mappings"] is JsonArray inner => inner,
                JsonObject obj => new JsonNode[] { obj },
                _ => Array.Empty<JsonNode>()
            };
            return items.OfType<JsonObject>()
                .Where(m => m["vin"] != null)
                .Select(m => (m["vin"]!.ToString(), m["mappingType"]?.ToString()))
                .ToList();
        }

        public async Task<JsonObject> GetBasicDataAsync(string vin, CancellationToken token = default)
        {
            return await SendAsync(HttpMethod.Get, $"/customers/vehicles/{Uri.EscapeDataString(vin)}/basicData", null, token) as JsonObject;
        }

        public async Task<IReadOnlyList<BmwContainer>> GetContainersAsync(CancellationToken token = default)
        {
            var json = await SendAsync(HttpMethod.Get, "/customers/containers", null, token);
            return (json?["containers"] as JsonArray ?? new JsonArray()).OfType<JsonObject>()
                .Select(c => new BmwContainer(c["containerId"]?.ToString(), c["name"]?.ToString(), c["state"]?.ToString()))
                .Where(c => c.Id != null)
                .ToList();
        }

        public async Task<string> CreateContainerAsync(string name, string purpose, IReadOnlyCollection<string> descriptors, CancellationToken token = default)
        {
            var json = await SendAsync(HttpMethod.Post, "/customers/containers", new { name, purpose, technicalDescriptors = descriptors }, token);
            return json?["containerId"]?.ToString() ?? throw new BmwCarDataApiException("The container was created without an id");
        }

        public async Task DeleteContainerAsync(string containerId, CancellationToken token = default)
        {
            await SendAsync(HttpMethod.Delete, $"/customers/containers/{Uri.EscapeDataString(containerId)}", null, token);
        }

        /// <summary>
        /// {"telematicData":{"key":{"value":"..","unit":"..","timestamp":".."}}}, values are strings
        /// </summary>
        public async Task<IReadOnlyDictionary<string, TelematicValue>> GetTelematicDataAsync(string vin, string containerId, CancellationToken token = default)
        {
            var json = await SendAsync(HttpMethod.Get,
                $"/customers/vehicles/{Uri.EscapeDataString(vin)}/telematicData?containerId={Uri.EscapeDataString(containerId)}", null, token);
            return ParseValues(json?["telematicData"] as JsonObject);
        }

        /// <summary>
        /// Parses {"key":{"value":..,"unit":..,"timestamp":..}} (REST and stream)
        /// </summary>
        internal static IReadOnlyDictionary<string, TelematicValue> ParseValues(JsonObject data)
        {
            var result = new Dictionary<string, TelematicValue>();
            if (data == null)
            {
                return result;
            }
            foreach (var (key, node) in data)
            {
                if (node is not JsonObject entry)
                {
                    continue;
                }
                object value = entry["value"] is JsonValue v ? Conv.Scalar(v.Deserialize<JsonElement>()) : null;
                DateTimeOffset? timestamp = DateTimeOffset.TryParse(entry["timestamp"]?.ToString(), System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.AssumeUniversal, out var ts) ? ts : null;
                result[key] = new TelematicValue(value, entry["unit"]?.ToString(), timestamp);
            }
            return result;
        }
    }
}
