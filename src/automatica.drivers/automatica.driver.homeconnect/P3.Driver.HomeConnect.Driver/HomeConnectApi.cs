using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace P3.Driver.HomeConnect.Driver
{
    internal sealed class HomeConnectApiException : Exception
    {
        public HomeConnectApiException(string message, HttpStatusCode? statusCode = null, string errorKey = null) : base(message)
        {
            StatusCode = statusCode;
            ErrorKey = errorKey;
        }

        public HttpStatusCode? StatusCode { get; }

        /// <summary>
        /// The Home Connect error key, e.g. SDK.Error.NoProgramActive
        /// </summary>
        public string ErrorKey { get; }
    }

    /// <summary>
    /// HTTP 429: the daily or per-minute request limit is used up
    /// </summary>
    internal sealed class RateLimitedException : Exception
    {
        public RateLimitedException(DateTimeOffset until) : base($"Home Connect request limit reached, paused until {until:u}")
        {
            Until = until;
        }

        public DateTimeOffset Until { get; }
    }

    internal sealed record HomeConnectItem(string Key, JsonElement Value);

    internal sealed record HomeConnectApplianceInfo(string HaId, string Name, string Brand, string Type, bool Connected);

    internal sealed record HomeConnectProgram(string Key, IReadOnlyList<HomeConnectItem> Options);

    /// <summary>
    /// Home Connect REST API and event stream, https://api-docs.home-connect.com
    /// </summary>
    internal sealed class HomeConnectApi
    {
        public const string MediaType = "application/vnd.bsh.sdk.v1+json";

        private readonly HttpClient _http;
        private readonly HttpClient _streamHttp;
        private readonly ILogger _logger;
        private readonly Func<DateTimeOffset> _now;

        public HomeConnectApi(HttpClient http, HttpClient streamHttp, ILogger logger, Func<DateTimeOffset> now, string baseUrl, OAuthTokenClient tokens)
        {
            _http = http;
            _streamHttp = streamHttp;
            _logger = logger;
            _now = now;
            BaseUrl = baseUrl.TrimEnd('/');
            Tokens = tokens;
        }

        public string BaseUrl { get; }

        public OAuthTokenClient Tokens { get; }

        /// <summary>
        /// Requests are not sent before this time (after a 429)
        /// </summary>
        public DateTimeOffset BlockedUntil { get; private set; } = DateTimeOffset.MinValue;

        public bool IsBlocked => _now() < BlockedUntil;

        private void Block(HttpResponseMessage response)
        {
            var retry = response.Headers.RetryAfter?.Delta ?? (response.Headers.RetryAfter?.Date is { } date ? date - _now() : (TimeSpan?)null) ?? TimeSpan.FromMinutes(1);
            if (retry < TimeSpan.FromSeconds(1))
            {
                retry = TimeSpan.FromSeconds(1);
            }
            BlockedUntil = _now() + retry;
            _logger.LogWarning($"Home Connect request limit reached, pausing for {retry.TotalSeconds:0}s");
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
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaType));
                request.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue("en-GB"));
                if (body != null)
                {
                    request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8);
                    request.Content.Headers.ContentType = new MediaTypeHeaderValue(MediaType);
                }

                using var response = await _http.SendAsync(request, token);
                var text = await response.Content.ReadAsStringAsync(token);
                if (response.StatusCode == HttpStatusCode.Unauthorized && attempt == 0)
                {
                    _logger.LogInformation("Home Connect rejected the access token, refreshing it");
                    continue;
                }
                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    Block(response);
                    throw new RateLimitedException(BlockedUntil);
                }
                if (!response.IsSuccessStatusCode)
                {
                    string key = null, description = null;
                    try
                    {
                        using var error = JsonDocument.Parse(text);
                        key = Conv.Str(error.RootElement, "error", "key");
                        description = Conv.Str(error.RootElement, "error", "description");
                    }
                    catch (JsonException)
                    {
                        description = text;
                    }
                    throw new HomeConnectApiException($"{method} {path} failed ({(int)response.StatusCode}): {key} {description}".Trim(), response.StatusCode, key);
                }
                return JsonDocument.Parse(string.IsNullOrWhiteSpace(text) ? "{}" : text);
            }
        }

        private static string Ha(string haId) => Uri.EscapeDataString(haId);

        public async Task<IReadOnlyList<HomeConnectApplianceInfo>> GetAppliancesAsync(CancellationToken token = default)
        {
            using var json = await SendAsync(HttpMethod.Get, "/api/homeappliances", null, token);
            var result = new List<HomeConnectApplianceInfo>();
            if (Conv.Path(json.RootElement, "data", "homeappliances") is { ValueKind: JsonValueKind.Array } list)
            {
                foreach (var a in list.EnumerateArray())
                {
                    result.Add(new HomeConnectApplianceInfo(Conv.Str(a, "haId"), Conv.Str(a, "name"), Conv.Str(a, "brand"), Conv.Str(a, "type"),
                        Conv.Bool(a, "connected") ?? false));
                }
            }
            return result;
        }

        private static IReadOnlyList<HomeConnectItem> Items(JsonElement root, string list)
        {
            var result = new List<HomeConnectItem>();
            if (Conv.Path(root, "data", list) is { ValueKind: JsonValueKind.Array } items)
            {
                foreach (var item in items.EnumerateArray())
                {
                    var key = Conv.Str(item, "key");
                    if (key != null)
                    {
                        result.Add(new HomeConnectItem(key, item.TryGetProperty("value", out var v) ? v.Clone() : default));
                    }
                }
            }
            return result;
        }

        public async Task<IReadOnlyList<HomeConnectItem>> GetStatusAsync(string haId, CancellationToken token = default)
        {
            using var json = await SendAsync(HttpMethod.Get, $"/api/homeappliances/{Ha(haId)}/status", null, token);
            return Items(json.RootElement, "status");
        }

        public async Task<IReadOnlyList<HomeConnectItem>> GetSettingsAsync(string haId, CancellationToken token = default)
        {
            using var json = await SendAsync(HttpMethod.Get, $"/api/homeappliances/{Ha(haId)}/settings", null, token);
            return Items(json.RootElement, "settings");
        }

        private async Task<HomeConnectProgram> GetProgramAsync(string haId, string which, CancellationToken token)
        {
            try
            {
                using var json = await SendAsync(HttpMethod.Get, $"/api/homeappliances/{Ha(haId)}/programs/{which}", null, token);
                var key = Conv.Str(json.RootElement, "data", "key");
                return key == null ? null : new HomeConnectProgram(key, Items(json.RootElement, "options"));
            }
            catch (HomeConnectApiException ex) when (ex.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Conflict)
            {
                return null; // SDK.Error.NoProgramActive / NoProgramSelected
            }
        }

        public Task<HomeConnectProgram> GetActiveProgramAsync(string haId, CancellationToken token = default) => GetProgramAsync(haId, "active", token);

        public Task<HomeConnectProgram> GetSelectedProgramAsync(string haId, CancellationToken token = default) => GetProgramAsync(haId, "selected", token);

        public async Task SetSettingAsync(string haId, string key, object value, CancellationToken token = default)
        {
            using var _ = await SendAsync(HttpMethod.Put, $"/api/homeappliances/{Ha(haId)}/settings/{Uri.EscapeDataString(key)}", new { data = new { key, value } }, token);
        }

        public async Task StartProgramAsync(string haId, string key, IReadOnlyList<HomeConnectItem> options, CancellationToken token = default)
        {
            object data = options is { Count: > 0 }
                ? new { key, options = options.Select(o => new { key = o.Key, value = o.Value }).ToList() }
                : new { key };
            using var _ = await SendAsync(HttpMethod.Put, $"/api/homeappliances/{Ha(haId)}/programs/active", new { data }, token);
        }

        public async Task StopProgramAsync(string haId, CancellationToken token = default)
        {
            using var _ = await SendAsync(HttpMethod.Delete, $"/api/homeappliances/{Ha(haId)}/programs/active", null, token);
        }

        public async Task SelectProgramAsync(string haId, string key, CancellationToken token = default)
        {
            using var _ = await SendAsync(HttpMethod.Put, $"/api/homeappliances/{Ha(haId)}/programs/selected", new { data = new { key } }, token);
        }

        public async Task SetActiveOptionAsync(string haId, string key, object value, CancellationToken token = default)
        {
            using var _ = await SendAsync(HttpMethod.Put, $"/api/homeappliances/{Ha(haId)}/programs/active/options/{Uri.EscapeDataString(key)}", new { data = new { key, value } }, token);
        }

        public async Task CommandAsync(string haId, string key, CancellationToken token = default)
        {
            using var _ = await SendAsync(HttpMethod.Put, $"/api/homeappliances/{Ha(haId)}/commands/{Uri.EscapeDataString(key)}", new { data = new { key, value = true } }, token);
        }

        /// <summary>
        /// Opens the event stream of all appliances of the account
        /// </summary>
        public async Task<HttpResponseMessage> OpenEventStreamAsync(CancellationToken token = default)
        {
            if (IsBlocked)
            {
                throw new RateLimitedException(BlockedUntil);
            }
            for (var attempt = 0; ; attempt++)
            {
                var accessToken = await Tokens.GetAccessTokenAsync(attempt > 0, token);
                var request = new HttpRequestMessage(HttpMethod.Get, BaseUrl + "/api/homeappliances/events");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
                request.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue("en-GB"));
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
                    if (response.StatusCode == HttpStatusCode.TooManyRequests)
                    {
                        Block(response);
                        throw new RateLimitedException(BlockedUntil);
                    }
                    throw new HomeConnectApiException($"Event stream failed ({(int)response.StatusCode})", response.StatusCode);
                }
            }
        }

        /// <summary>
        /// The last part of an enum value: BSH.Common.EnumType.OperationState.Run → Run
        /// </summary>
        public static string ShortEnum(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }
            var dot = value.LastIndexOf('.');
            return dot >= 0 && value.Contains(".EnumType.", StringComparison.Ordinal) ? value[(dot + 1)..] : value;
        }

        public static string Format(object value) => Convert.ToString(value, CultureInfo.InvariantCulture);
    }
}
