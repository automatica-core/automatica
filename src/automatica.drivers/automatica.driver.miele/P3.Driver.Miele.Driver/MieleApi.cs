using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace P3.Driver.Miele.Driver
{
    internal sealed class MieleApiException : Exception
    {
        public MieleApiException(string message, HttpStatusCode? statusCode = null) : base(message)
        {
            StatusCode = statusCode;
        }

        public HttpStatusCode? StatusCode { get; }
    }

    /// <summary>
    /// Miele answers 503 (or 429) when a client or IP sends too much
    /// </summary>
    internal sealed class RateLimitedException : Exception
    {
        public RateLimitedException(DateTimeOffset until) : base($"Miele request limit reached, paused until {until:u}")
        {
            Until = until;
        }

        public DateTimeOffset Until { get; }
    }

    internal sealed record MieleDevice(string Id, string Name, string Type, JsonElement State);

    /// <summary>
    /// Miele 3rd party API, https://developer.miele.com/docs/third-party-api
    /// </summary>
    internal sealed class MieleApi
    {
        private readonly HttpClient _http;
        private readonly HttpClient _streamHttp;
        private readonly ILogger _logger;
        private readonly Func<DateTimeOffset> _now;

        public MieleApi(HttpClient http, HttpClient streamHttp, ILogger logger, Func<DateTimeOffset> now, string baseUrl, string language, OAuthTokenClient tokens)
        {
            _http = http;
            _streamHttp = streamHttp;
            _logger = logger;
            _now = now;
            BaseUrl = baseUrl.TrimEnd('/');
            Language = string.IsNullOrWhiteSpace(language) ? "en" : language.Trim();
            Tokens = tokens;
        }

        public string BaseUrl { get; }
        public string Language { get; }
        public OAuthTokenClient Tokens { get; }

        public DateTimeOffset BlockedUntil { get; private set; } = DateTimeOffset.MinValue;

        public bool IsBlocked => _now() < BlockedUntil;

        private bool CheckThrottled(HttpResponseMessage response)
        {
            if (response.StatusCode is not (HttpStatusCode.ServiceUnavailable or HttpStatusCode.TooManyRequests))
            {
                return false;
            }
            var retry = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromMinutes(5);
            BlockedUntil = _now() + (retry < TimeSpan.FromSeconds(1) ? TimeSpan.FromSeconds(1) : retry);
            _logger.LogWarning($"Miele answered {(int)response.StatusCode}, pausing for {retry.TotalSeconds:0}s");
            return true;
        }

        private async Task<JsonDocument> SendAsync(HttpMethod method, string path, object body, CancellationToken token)
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
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                if (body != null)
                {
                    request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
                }
                using var response = await _http.SendAsync(request, token);
                var text = await response.Content.ReadAsStringAsync(token);
                if (response.StatusCode == HttpStatusCode.Unauthorized && attempt == 0)
                {
                    _logger.LogInformation("Miele rejected the access token, refreshing it");
                    continue;
                }
                if (CheckThrottled(response))
                {
                    throw new RateLimitedException(BlockedUntil);
                }
                if (!response.IsSuccessStatusCode)
                {
                    throw new MieleApiException($"{method} {path} failed ({(int)response.StatusCode}): {(text.Length > 300 ? text[..300] : text)}", response.StatusCode);
                }
                return JsonDocument.Parse(string.IsNullOrWhiteSpace(text) ? "{}" : text);
            }
        }

        /// <summary>
        /// The devices map ({deviceId: {ident, state}}) of GET /devices and of the "devices" event
        /// </summary>
        public static IReadOnlyList<MieleDevice> ParseDevices(JsonElement root)
        {
            var result = new List<MieleDevice>();
            if (root.ValueKind != JsonValueKind.Object)
            {
                return result;
            }
            foreach (var device in root.EnumerateObject())
            {
                if (device.Value.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }
                var ident = device.Value.TryGetProperty("ident", out var i) ? i : default;
                var state = device.Value.TryGetProperty("state", out var s) ? s.Clone() : default;
                var name = ident.ValueKind == JsonValueKind.Object ? Conv.Str(ident, "deviceName") : null;
                var type = ident.ValueKind == JsonValueKind.Object ? Conv.Str(ident, "type", "value_localized") : null;
                result.Add(new MieleDevice(device.Name, string.IsNullOrWhiteSpace(name) ? null : name, type, state));
            }
            return result;
        }

        public async Task<IReadOnlyList<MieleDevice>> GetDevicesAsync(CancellationToken token = default)
        {
            using var json = await SendAsync(HttpMethod.Get, $"/v1/devices?language={Uri.EscapeDataString(Language)}", null, token);
            return ParseDevices(json.RootElement);
        }

        public async Task PutAsync(string deviceId, string endpoint, object body, CancellationToken token = default)
        {
            using var _ = await SendAsync(HttpMethod.Put, $"/v1/devices/{Uri.EscapeDataString(deviceId)}/{endpoint}", body, token);
        }

        public async Task<HttpResponseMessage> OpenEventStreamAsync(CancellationToken token = default)
        {
            if (IsBlocked)
            {
                throw new RateLimitedException(BlockedUntil);
            }
            for (var attempt = 0; ; attempt++)
            {
                var accessToken = await Tokens.GetAccessTokenAsync(attempt > 0, token);
                var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/v1/devices/all/events?language={Uri.EscapeDataString(Language)}");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
                var response = await _streamHttp.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
                if (response.IsSuccessStatusCode)
                {
                    return response;
                }
                using (response)
                {
                    if (response.StatusCode == HttpStatusCode.Unauthorized && attempt == 0)
                    {
                        continue;
                    }
                    if (CheckThrottled(response))
                    {
                        throw new RateLimitedException(BlockedUntil);
                    }
                    throw new MieleApiException($"Event stream failed ({(int)response.StatusCode})", response.StatusCode);
                }
            }
        }
    }
}
