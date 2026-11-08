using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace P3.Driver.Automower.Driver
{
    internal sealed class AutomowerApiException : Exception
    {
        public AutomowerApiException(string message, HttpStatusCode? statusCode = null) : base(message)
        {
            StatusCode = statusCode;
        }

        public HttpStatusCode? StatusCode { get; }
    }

    internal sealed class RateLimitedException : Exception
    {
        public RateLimitedException(DateTimeOffset until) : base($"Automower Connect request limit reached, paused until {until:u}")
        {
            Until = until;
        }

        public DateTimeOffset Until { get; }
    }

    internal sealed record AutomowerMower(string Id, JsonObject Attributes);

    /// <summary>
    /// Husqvarna Automower Connect API v1, https://developer.husqvarnagroup.cloud/apis/automower-connect-api
    /// </summary>
    internal sealed class AutomowerApi
    {
        public const string MediaType = "application/vnd.api+json";

        private readonly HttpClient _http;
        private readonly ILogger _logger;
        private readonly Func<DateTimeOffset> _now;

        public AutomowerApi(HttpClient http, ILogger logger, Func<DateTimeOffset> now, string baseUrl, OAuthTokenClient tokens)
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
                request.Headers.Add("X-Api-Key", Tokens.ClientId);
                request.Headers.Add("Authorization-Provider", "husqvarna");
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaType));
                if (body != null)
                {
                    request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8);
                    request.Content.Headers.ContentType = new MediaTypeHeaderValue(MediaType);
                }

                using var response = await _http.SendAsync(request, token);
                var text = await response.Content.ReadAsStringAsync(token);
                if (response.StatusCode == HttpStatusCode.Unauthorized && attempt == 0)
                {
                    _logger.LogInformation("Automower Connect rejected the access token, requesting a new one");
                    continue;
                }
                // over the quota the API answers 429, or 403 with a limit message
                if (response.StatusCode == HttpStatusCode.TooManyRequests || (response.StatusCode == HttpStatusCode.Forbidden &&
                        (text.Contains("limit", StringComparison.OrdinalIgnoreCase) || text.Contains("quota", StringComparison.OrdinalIgnoreCase))))
                {
                    var retry = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromMinutes(15);
                    BlockedUntil = _now() + retry;
                    _logger.LogWarning($"Automower Connect request limit reached, pausing for {retry.TotalMinutes:0} min");
                    throw new RateLimitedException(BlockedUntil);
                }
                if (!response.IsSuccessStatusCode)
                {
                    throw new AutomowerApiException($"{method} {path} failed ({(int)response.StatusCode}): {(text.Length > 300 ? text[..300] : text)}", response.StatusCode);
                }
                return string.IsNullOrWhiteSpace(text) ? null : JsonNode.Parse(text);
            }
        }

        public async Task<IReadOnlyList<AutomowerMower>> GetMowersAsync(CancellationToken token = default)
        {
            var json = await SendAsync(HttpMethod.Get, "/mowers", null, token);
            var result = new List<AutomowerMower>();
            if (json?["data"] is JsonArray data)
            {
                foreach (var mower in data)
                {
                    var id = mower?["id"]?.GetValue<string>();
                    if (id != null && mower["attributes"] is JsonObject attributes)
                    {
                        result.Add(new AutomowerMower(id, (JsonObject)attributes.DeepClone()));
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Start, Pause, Park, ParkUntilNextSchedule, ParkUntilFurtherNotice, ResumeSchedule (202 = queued for the mower)
        /// </summary>
        public async Task ActionAsync(string mowerId, string type, int? durationMinutes, CancellationToken token = default)
        {
            object data = durationMinutes.HasValue ? new { type, attributes = new { duration = durationMinutes.Value } } : new { type };
            await SendAsync(HttpMethod.Post, $"/mowers/{Uri.EscapeDataString(mowerId)}/actions", new { data }, token);
        }

        public async Task SettingsAsync(string mowerId, object attributes, CancellationToken token = default)
        {
            await SendAsync(HttpMethod.Post, $"/mowers/{Uri.EscapeDataString(mowerId)}/settings", new { data = new { type = "settings", attributes } }, token);
        }
    }
}
