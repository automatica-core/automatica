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

namespace P3.Driver.Navimow.Driver
{
    internal sealed class NavimowApiException : Exception
    {
        public NavimowApiException(string message, HttpStatusCode? statusCode = null, string code = null) : base(message)
        {
            StatusCode = statusCode;
            Code = code;
        }

        public HttpStatusCode? StatusCode { get; }

        /// <summary>
        /// The code of the response envelope (1 = success)
        /// </summary>
        public string Code { get; }
    }

    internal sealed class RateLimitedException : Exception
    {
        public RateLimitedException(DateTimeOffset until) : base($"Navimow cloud rejected requests as too frequent, paused until {until:u}")
        {
            Until = until;
        }

        public DateTimeOffset Until { get; }
    }

    internal sealed record NavimowDevice(string Id, string Name, string Model, string Firmware, bool? Online);

    /// <summary>
    /// The Navimow smart-home cloud API as used by Segway's own open-source Home Assistant integration and navimow-sdk
    /// (github.com/segwaynavimow). Segway publishes no separate API reference.
    /// </summary>
    internal sealed class NavimowApi
    {
        public const string StartStop = "action.devices.commands.StartStop";
        public const string PauseUnpause = "action.devices.commands.PauseUnpause";
        public const string Dock = "action.devices.commands.Dock";

        private readonly HttpClient _http;
        private readonly ILogger _logger;
        private readonly Func<DateTimeOffset> _now;

        public NavimowApi(HttpClient http, ILogger logger, Func<DateTimeOffset> now, string baseUrl, OAuthTokenClient tokens)
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
        public TimeSpan RateLimitPause { get; set; } = TimeSpan.FromMinutes(5);

        private RateLimitedException Block(TimeSpan? retryAfter)
        {
            var pause = retryAfter ?? RateLimitPause;
            BlockedUntil = _now() + pause;
            _logger.LogWarning($"Navimow cloud rate limit, pausing for {pause.TotalMinutes:0} min");
            return new RateLimitedException(BlockedUntil);
        }

        private static bool LooksLikeRateLimit(string text) =>
            text != null && (text.Contains("frequent", StringComparison.OrdinalIgnoreCase) || text.Contains("rate limit", StringComparison.OrdinalIgnoreCase) ||
                             text.Contains("too many", StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// Sends a request and returns data.payload (or data) of the {"code":1,"desc":..,"data":..} envelope
        /// </summary>
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
                request.Headers.Add("requestId", Guid.NewGuid().ToString());
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                if (body != null)
                {
                    request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
                }

                using var response = await _http.SendAsync(request, token);
                var text = await response.Content.ReadAsStringAsync(token);
                if (response.StatusCode == HttpStatusCode.Unauthorized && attempt == 0 && Tokens.RefreshToken != null)
                {
                    _logger.LogInformation("Navimow rejected the access token, refreshing it");
                    continue;
                }
                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    throw Block(response.Headers.RetryAfter?.Delta);
                }
                if (!response.IsSuccessStatusCode)
                {
                    if (LooksLikeRateLimit(text))
                    {
                        throw Block(null);
                    }
                    throw new NavimowApiException($"{method} {path} failed ({(int)response.StatusCode}): {Truncate(text)}", response.StatusCode);
                }

                var json = string.IsNullOrWhiteSpace(text) ? null : JsonNode.Parse(text);
                var code = json?["code"]?.ToString();
                if (code != null && code != "1")
                {
                    var desc = json["desc"]?.ToString() ?? json["message"]?.ToString();
                    if (LooksLikeRateLimit(desc))
                    {
                        throw Block(null);
                    }
                    throw new NavimowApiException($"{method} {path} failed (code {code}): {desc}", response.StatusCode, code);
                }
                var data = json?["data"];
                return data?["payload"] ?? data;
            }
        }

        private static string Truncate(string text) => text == null ? "" : text.Length > 300 ? text[..300] : text;

        private static string Text(JsonNode node, params string[] names)
        {
            foreach (var name in names)
            {
                if (node?[name] is JsonValue value)
                {
                    return value.ToString();
                }
            }
            return null;
        }

        public async Task<IReadOnlyList<NavimowDevice>> GetDevicesAsync(CancellationToken token = default)
        {
            var payload = await SendAsync(HttpMethod.Get, "/openapi/smarthome/authList", null, token);
            var result = new List<NavimowDevice>();
            if (payload?["devices"] is JsonArray devices)
            {
                foreach (var device in devices.OfType<JsonObject>())
                {
                    var id = Text(device, "id");
                    if (id == null)
                    {
                        continue;
                    }
                    bool? online = device["online"] is JsonValue o && o.TryGetValue<bool>(out var b) ? b : null;
                    result.Add(new NavimowDevice(id, Text(device, "name") ?? id, Text(device, "model"), Text(device, "firmware_version", "firmwareVersion"), online));
                }
            }
            return result;
        }

        /// <summary>
        /// One request for all mowers
        /// </summary>
        public async Task<IReadOnlyDictionary<string, JsonObject>> GetStatusAsync(IEnumerable<string> deviceIds, CancellationToken token = default)
        {
            var body = new { devices = deviceIds.Select(id => new { id }).ToArray() };
            var payload = await SendAsync(HttpMethod.Post, "/openapi/smarthome/getVehicleStatus", body, token);
            var result = new Dictionary<string, JsonObject>();
            if (payload?["devices"] is JsonArray devices)
            {
                foreach (var device in devices.OfType<JsonObject>())
                {
                    if (Text(device, "id") is { } id)
                    {
                        result[id] = (JsonObject)device.DeepClone();
                    }
                }
            }
            return result;
        }

        public async Task SendCommandAsync(string deviceId, string command, object parameters, CancellationToken token = default)
        {
            var body = new
            {
                commands = new[]
                {
                    new { devices = new[] { new { id = deviceId } }, execution = new { command, @params = parameters ?? new { } } }
                }
            };
            var payload = await SendAsync(HttpMethod.Post, "/openapi/smarthome/sendCommands", body, token);
            if (payload?["commands"] is JsonArray results)
            {
                foreach (var result in results.OfType<JsonObject>())
                {
                    var errorCode = Text(result, "errorCode");
                    if (string.Equals(Text(result, "status"), "ERROR", StringComparison.OrdinalIgnoreCase) && errorCode != "alreadyInState")
                    {
                        throw new NavimowApiException($"{command} was rejected: {errorCode}");
                    }
                }
            }
        }
    }
}
