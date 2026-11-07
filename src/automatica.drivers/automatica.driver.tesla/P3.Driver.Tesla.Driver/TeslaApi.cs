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

namespace P3.Driver.Tesla.Driver
{
    internal sealed class TeslaApiException : Exception
    {
        public HttpStatusCode? StatusCode { get; }

        public TeslaApiException(string message, HttpStatusCode? statusCode = null) : base(message)
        {
            StatusCode = statusCode;
        }
    }

    /// <summary>
    /// The vehicle is asleep or offline (HTTP 408), data requests do not wake it
    /// </summary>
    internal sealed class VehicleUnavailableException : Exception
    {
        public VehicleUnavailableException(string vin) : base($"Vehicle {vin} is not reachable (asleep or offline)")
        {
        }
    }

    /// <summary>
    /// Tesla Fleet API client: OAuth refresh-token flow (refresh tokens rotate on every use) and the vehicle endpoints.
    /// https://developer.tesla.com/docs/fleet-api
    /// </summary>
    internal sealed class TeslaApi
    {
        public const string DefaultAuthUrl = "https://fleet-auth.prd.vn.cloud.tesla.com/oauth2/v3/token";

        internal const string VehicleDataEndpoints = "charge_state;climate_state;drive_state;location_data;vehicle_state";

        private static readonly TimeSpan RefreshMargin = TimeSpan.FromMinutes(5);

        private readonly HttpClient _http;
        private readonly ILogger _logger;
        private readonly Func<DateTimeOffset> _now;
        private readonly SemaphoreSlim _tokenLock = new(1, 1);

        private string _accessToken;
        private DateTimeOffset _accessTokenExpires = DateTimeOffset.MinValue;

        public TeslaApi(HttpClient http, ILogger logger, Func<DateTimeOffset> now, string apiUrl, string commandUrl, string authUrl, string clientId, string refreshToken)
        {
            _http = http;
            _logger = logger;
            _now = now;
            ApiUrl = apiUrl.TrimEnd('/');
            CommandUrl = string.IsNullOrWhiteSpace(commandUrl) ? ApiUrl : commandUrl.TrimEnd('/');
            AuthUrl = authUrl;
            ClientId = clientId;
            RefreshToken = refreshToken;
        }

        public string ApiUrl { get; }
        public string CommandUrl { get; }
        public string AuthUrl { get; }
        public string ClientId { get; }

        /// <summary>
        /// The refresh token to use next. Tesla invalidates the old one after a successful refresh.
        /// </summary>
        public string RefreshToken { get; private set; }

        /// <summary>
        /// Raised with the new refresh token after every successful refresh, it has to be persisted
        /// </summary>
        public event Action<string> RefreshTokenRotated;

        private async Task<string> GetAccessTokenAsync(bool forceRefresh, CancellationToken token)
        {
            await _tokenLock.WaitAsync(token);
            try
            {
                if (!forceRefresh && _accessToken != null && _now() < _accessTokenExpires - RefreshMargin)
                {
                    return _accessToken;
                }

                using var request = new HttpRequestMessage(HttpMethod.Post, AuthUrl)
                {
                    Content = new FormUrlEncodedContent(new Dictionary<string, string>
                    {
                        ["grant_type"] = "refresh_token",
                        ["client_id"] = ClientId ?? "",
                        ["refresh_token"] = RefreshToken ?? ""
                    })
                };
                using var response = await _http.SendAsync(request, token);
                var body = await response.Content.ReadAsStringAsync(token);
                if (!response.IsSuccessStatusCode)
                {
                    throw new TeslaApiException($"Token refresh failed ({(int)response.StatusCode}): {Truncate(body)}", response.StatusCode);
                }

                using var json = JsonDocument.Parse(body);
                var root = json.RootElement;
                _accessToken = root.GetProperty("access_token").GetString();
                var expiresIn = root.TryGetProperty("expires_in", out var e) && e.TryGetInt32(out var seconds) ? seconds : 3600;
                _accessTokenExpires = _now().AddSeconds(expiresIn);

                if (root.TryGetProperty("refresh_token", out var rt) && rt.GetString() is { Length: > 0 } newRefresh && newRefresh != RefreshToken)
                {
                    RefreshToken = newRefresh;
                    RefreshTokenRotated?.Invoke(newRefresh);
                }

                _logger.LogDebug($"Tesla access token refreshed, valid for {expiresIn}s");
                return _accessToken;
            }
            finally
            {
                _tokenLock.Release();
            }
        }

        private async Task<JsonDocument> SendAsync(HttpMethod method, string url, object body, CancellationToken token, string vin = null)
        {
            for (var attempt = 0; ; attempt++)
            {
                var accessToken = await GetAccessTokenAsync(attempt > 0, token);
                using var request = new HttpRequestMessage(method, url);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                if (body != null)
                {
                    request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
                }

                using var response = await _http.SendAsync(request, token);
                var text = await response.Content.ReadAsStringAsync(token);

                if (response.StatusCode == HttpStatusCode.Unauthorized && attempt == 0)
                {
                    _logger.LogInformation("Tesla rejected the access token, refreshing it");
                    continue;
                }
                if (response.StatusCode == HttpStatusCode.RequestTimeout && vin != null)
                {
                    throw new VehicleUnavailableException(vin);
                }
                if (!response.IsSuccessStatusCode)
                {
                    throw new TeslaApiException($"{method} {new Uri(url).AbsolutePath} failed ({(int)response.StatusCode}): {Truncate(text)}", response.StatusCode);
                }
                return JsonDocument.Parse(string.IsNullOrWhiteSpace(text) ? "{}" : text);
            }
        }

        /// <summary>
        /// The vehicle summary (state online / asleep / offline). Does not wake the vehicle and is not billed as vehicle data.
        /// </summary>
        public async Task<string> GetVehicleStateAsync(string vin, CancellationToken token = default)
        {
            using var json = await SendAsync(HttpMethod.Get, $"{ApiUrl}/api/1/vehicles/{Uri.EscapeDataString(vin)}", null, token);
            return json.RootElement.TryGetProperty("response", out var r) && r.TryGetProperty("state", out var s) ? s.GetString() : null;
        }

        /// <summary>
        /// Live vehicle data, only answered while the vehicle is online (HTTP 408 otherwise)
        /// </summary>
        public async Task<JsonDocument> GetVehicleDataAsync(string vin, CancellationToken token = default)
        {
            var url = $"{ApiUrl}/api/1/vehicles/{Uri.EscapeDataString(vin)}/vehicle_data?endpoints={Uri.EscapeDataString(VehicleDataEndpoints)}";
            return await SendAsync(HttpMethod.Get, url, null, token, vin);
        }

        public async Task WakeUpAsync(string vin, CancellationToken token = default)
        {
            using var _ = await SendAsync(HttpMethod.Post, $"{ApiUrl}/api/1/vehicles/{Uri.EscapeDataString(vin)}/wake_up", null, token);
        }

        /// <summary>
        /// Sends a vehicle command. Vehicles that require the Vehicle Command Protocol need a signing proxy as command URL.
        /// </summary>
        public async Task CommandAsync(string vin, string command, object parameters, CancellationToken token = default)
        {
            using var json = await SendAsync(HttpMethod.Post, $"{CommandUrl}/api/1/vehicles/{Uri.EscapeDataString(vin)}/command/{command}", parameters ?? new { }, token, vin);
            if (json.RootElement.TryGetProperty("response", out var r) && r.ValueKind == JsonValueKind.Object &&
                r.TryGetProperty("result", out var result) && result.ValueKind == JsonValueKind.False)
            {
                var reason = r.TryGetProperty("reason", out var why) ? why.ToString() : "";
                throw new TeslaApiException($"Command {command} was rejected: {reason}");
            }
        }

        private static string Truncate(string text) => text == null ? "" : text.Length > 300 ? text[..300] : text;
    }
}
