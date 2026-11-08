using System;
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

namespace P3.Driver.Gardena.Driver
{
    internal sealed class GardenaApiException : Exception
    {
        public GardenaApiException(string message, HttpStatusCode? statusCode = null) : base(message)
        {
            StatusCode = statusCode;
        }

        public HttpStatusCode? StatusCode { get; }
    }

    internal sealed class RateLimitedException : Exception
    {
        public RateLimitedException(DateTimeOffset until) : base($"GARDENA request limit reached (700 per week), paused until {until:u}")
        {
            Until = until;
        }

        public DateTimeOffset Until { get; }
    }

    /// <summary>
    /// GARDENA smart system API v2, https://developer.husqvarnagroup.cloud/apis/gardena-smart-system-api
    /// </summary>
    internal sealed class GardenaApi
    {
        public const string MediaType = "application/vnd.api+json";

        private readonly HttpClient _http;
        private readonly ILogger _logger;
        private readonly Func<DateTimeOffset> _now;

        public GardenaApi(HttpClient http, ILogger logger, Func<DateTimeOffset> now, string baseUrl, OAuthTokenClient tokens)
        {
            _http = http;
            _logger = logger;
            _now = now;
            BaseUrl = baseUrl.TrimEnd('/');
            Tokens = tokens;
        }

        public string BaseUrl { get; }
        public OAuthTokenClient Tokens { get; }

        /// <summary>
        /// Every REST request counts against the weekly quota
        /// </summary>
        public int RequestCount { get; private set; }

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

                RequestCount++;
                using var response = await _http.SendAsync(request, token);
                var text = await response.Content.ReadAsStringAsync(token);
                if (response.StatusCode == HttpStatusCode.Unauthorized && attempt == 0)
                {
                    _logger.LogInformation("GARDENA rejected the access token, requesting a new one");
                    continue;
                }
                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    var retry = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromHours(1);
                    BlockedUntil = _now() + retry;
                    _logger.LogWarning($"GARDENA request limit reached, pausing for {retry.TotalMinutes:0} min");
                    throw new RateLimitedException(BlockedUntil);
                }
                if (!response.IsSuccessStatusCode)
                {
                    throw new GardenaApiException($"{method} {path} failed ({(int)response.StatusCode}): {(text.Length > 300 ? text[..300] : text)}", response.StatusCode);
                }
                return string.IsNullOrWhiteSpace(text) ? null : JsonNode.Parse(text);
            }
        }

        /// <summary>
        /// The configured location, or the first one of the account
        /// </summary>
        public async Task<string> GetLocationIdAsync(string configured, CancellationToken token = default)
        {
            if (!string.IsNullOrWhiteSpace(configured))
            {
                return configured.Trim();
            }
            JsonNode json;
            try
            {
                json = await SendAsync(HttpMethod.Get, "/locations", null, token);
            }
            catch (GardenaApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                throw new GardenaApiException("The account has no GARDENA location, set up the smart system in the GARDENA app first", ex.StatusCode);
            }
            var id = json?["data"]?.AsArray().FirstOrDefault()?["id"]?.GetValue<string>();
            return id ?? throw new GardenaApiException("The account has no GARDENA location");
        }

        /// <summary>
        /// The location with all devices and services (data + included)
        /// </summary>
        public Task<JsonNode> GetLocationAsync(string locationId, CancellationToken token = default)
        {
            return SendAsync(HttpMethod.Get, $"/locations/{Uri.EscapeDataString(locationId)}", null, token);
        }

        /// <summary>
        /// A pre-authenticated web socket URL, valid for a few seconds
        /// </summary>
        public async Task<string> CreateWebSocketAsync(string locationId, CancellationToken token = default)
        {
            var json = await SendAsync(HttpMethod.Post, "/websocket", new
            {
                data = new { id = Guid.NewGuid().ToString(), type = "WEBSOCKET", attributes = new { locationId } }
            }, token);
            return json?["data"]?["attributes"]?["url"]?.GetValue<string>() ?? throw new GardenaApiException("No web socket URL returned");
        }

        public async Task CommandAsync(string serviceId, GardenaCommand command, CancellationToken token = default)
        {
            object attributes = command.Seconds.HasValue
                ? new { command = command.Command, seconds = command.Seconds.Value }
                : new { command = command.Command };
            await SendAsync(HttpMethod.Put, $"/command/{Uri.EscapeDataString(serviceId)}", new
            {
                data = new { id = Guid.NewGuid().ToString(), type = command.ControlType, attributes }
            }, token);
        }
    }
}
