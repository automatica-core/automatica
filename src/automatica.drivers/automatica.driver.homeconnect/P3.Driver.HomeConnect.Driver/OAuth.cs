using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace P3.Driver.HomeConnect.Driver
{
    internal sealed class OAuthException : Exception
    {
        public OAuthException(string message, string error = null, HttpStatusCode? statusCode = null) : base(message)
        {
            Error = error;
            StatusCode = statusCode;
        }

        /// <summary>
        /// The OAuth error code (invalid_grant, authorization_pending, ...)
        /// </summary>
        public string Error { get; }

        public HttpStatusCode? StatusCode { get; }

        /// <summary>
        /// The refresh token was revoked or expired, a new login is needed
        /// </summary>
        public bool IsInvalidGrant => Error is "invalid_grant" or "invalid_token" || StatusCode == HttpStatusCode.Unauthorized;
    }

    internal sealed record DeviceAuthorization(string DeviceCode, string UserCode, string VerificationUri, string VerificationUriComplete, TimeSpan Interval, TimeSpan ExpiresIn);

    /// <summary>
    /// OAuth 2 token handling: refresh-token grant (with rotation), client-credentials grant and the device authorization grant (RFC 8628)
    /// </summary>
    internal sealed class OAuthTokenClient
    {
        private static readonly TimeSpan RefreshMargin = TimeSpan.FromMinutes(5);

        private readonly HttpClient _http;
        private readonly ILogger _logger;
        private readonly Func<DateTimeOffset> _now;
        private readonly SemaphoreSlim _lock = new(1, 1);

        private string _accessToken;
        private DateTimeOffset _accessTokenExpires = DateTimeOffset.MinValue;

        public OAuthTokenClient(HttpClient http, ILogger logger, Func<DateTimeOffset> now, string tokenUrl, string clientId, string clientSecret, string refreshToken)
        {
            _http = http;
            _logger = logger;
            _now = now;
            TokenUrl = tokenUrl;
            ClientId = clientId ?? "";
            ClientSecret = clientSecret;
            RefreshToken = string.IsNullOrWhiteSpace(refreshToken) ? null : refreshToken.Trim();
        }

        public string TokenUrl { get; }
        public string ClientId { get; }
        public string ClientSecret { get; }

        /// <summary>
        /// Use the client-credentials grant instead of a refresh token
        /// </summary>
        public bool ClientCredentials { get; init; }

        /// <summary>
        /// Send the client secret with refresh and device-code requests
        /// </summary>
        public bool SendSecret { get; init; } = true;

        public string RefreshToken { get; private set; }

        /// <summary>
        /// The last token response, for APIs that need more than the access token (id_token, gcid, ...)
        /// </summary>
        public JsonElement LastResponse { get; private set; }

        public bool HasCredentials => ClientCredentials ? !string.IsNullOrWhiteSpace(ClientId) && !string.IsNullOrWhiteSpace(ClientSecret) : RefreshToken != null;

        /// <summary>
        /// Raised with the new refresh token whenever the server hands out a new one, it has to be persisted
        /// </summary>
        public event Action<string> RefreshTokenRotated;

        public void Invalidate()
        {
            _accessToken = null;
        }

        public async Task<string> GetAccessTokenAsync(bool forceRefresh = false, CancellationToken token = default)
        {
            await _lock.WaitAsync(token);
            try
            {
                if (!forceRefresh && _accessToken != null && _now() < _accessTokenExpires - RefreshMargin)
                {
                    return _accessToken;
                }

                Dictionary<string, string> form;
                if (ClientCredentials)
                {
                    form = new Dictionary<string, string> { ["grant_type"] = "client_credentials", ["client_id"] = ClientId, ["client_secret"] = ClientSecret ?? "" };
                }
                else
                {
                    if (RefreshToken == null)
                    {
                        throw new OAuthException("Not logged in (no refresh token)", "login_required");
                    }
                    form = new Dictionary<string, string> { ["grant_type"] = "refresh_token", ["refresh_token"] = RefreshToken, ["client_id"] = ClientId };
                    AddSecret(form);
                }

                await RequestTokenAsync(form, token);
                return _accessToken;
            }
            finally
            {
                _lock.Release();
            }
        }

        private void AddSecret(IDictionary<string, string> form)
        {
            if (SendSecret && !string.IsNullOrWhiteSpace(ClientSecret))
            {
                form["client_secret"] = ClientSecret;
            }
        }

        private async Task RequestTokenAsync(IDictionary<string, string> form, CancellationToken token)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, TokenUrl) { Content = new FormUrlEncodedContent(form) };
            using var response = await _http.SendAsync(request, token);
            var body = await response.Content.ReadAsStringAsync(token);
            JsonElement root = default;
            try
            {
                root = JsonDocument.Parse(string.IsNullOrWhiteSpace(body) ? "{}" : body).RootElement;
            }
            catch (JsonException)
            {
                // reported below
            }

            if (!response.IsSuccessStatusCode || root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("access_token", out _))
            {
                var error = root.ValueKind == JsonValueKind.Object && root.TryGetProperty("error", out var e) ? e.ToString() : null;
                var description = root.ValueKind == JsonValueKind.Object && root.TryGetProperty("error_description", out var d) ? d.ToString() : null;
                throw new OAuthException($"Token request failed ({(int)response.StatusCode}): {error} {description}".Trim(), error, response.StatusCode);
            }

            LastResponse = root.Clone();
            _accessToken = root.GetProperty("access_token").GetString();
            var expiresIn = root.TryGetProperty("expires_in", out var exp) && exp.TryGetInt32(out var seconds) ? seconds : 3600;
            _accessTokenExpires = _now().AddSeconds(expiresIn);

            if (root.TryGetProperty("refresh_token", out var rt) && rt.ValueKind == JsonValueKind.String && rt.GetString() is { Length: > 0 } newRefresh && newRefresh != RefreshToken)
            {
                RefreshToken = newRefresh;
                RefreshTokenRotated?.Invoke(newRefresh);
            }
            _logger.LogDebug($"Access token received, valid for {expiresIn}s");
        }

        /// <summary>
        /// Starts a device authorization: the user opens the verification URI and enters the user code
        /// </summary>
        public async Task<DeviceAuthorization> StartDeviceAuthorizationAsync(string url, IDictionary<string, string> form, CancellationToken token = default)
        {
            var content = new Dictionary<string, string>(form) { ["client_id"] = ClientId };
            using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = new FormUrlEncodedContent(content) };
            using var response = await _http.SendAsync(request, token);
            var body = await response.Content.ReadAsStringAsync(token);
            if (!response.IsSuccessStatusCode)
            {
                throw new OAuthException($"Device authorization failed ({(int)response.StatusCode}): {Truncate(body)}", null, response.StatusCode);
            }
            using var json = JsonDocument.Parse(body);
            var root = json.RootElement;
            string Str(string name) => root.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
            int Int(string name, int fallback) => root.TryGetProperty(name, out var v) && v.TryGetInt32(out var i) ? i : fallback;
            return new DeviceAuthorization(Str("device_code"), Str("user_code"), Str("verification_uri") ?? Str("verification_url"),
                Str("verification_uri_complete"), TimeSpan.FromSeconds(Math.Max(1, Int("interval", 5))), TimeSpan.FromSeconds(Int("expires_in", 600)));
        }

        /// <summary>
        /// Asks once for the tokens of a device authorization. False while the user has not confirmed yet.
        /// </summary>
        public async Task<bool> TryCompleteDeviceAuthorizationAsync(string deviceCode, IDictionary<string, string> extra = null, CancellationToken token = default)
        {
            var form = new Dictionary<string, string>
            {
                ["grant_type"] = "urn:ietf:params:oauth:grant-type:device_code",
                ["device_code"] = deviceCode,
                ["client_id"] = ClientId
            };
            AddSecret(form);
            if (extra != null)
            {
                foreach (var (key, value) in extra)
                {
                    form[key] = value;
                }
            }

            await _lock.WaitAsync(token);
            try
            {
                await RequestTokenAsync(form, token);
                return true;
            }
            catch (OAuthException ex) when (ex.Error is "authorization_pending" or "slow_down")
            {
                return false;
            }
            finally
            {
                _lock.Release();
            }
        }

        private static string Truncate(string text) => text == null ? "" : text.Length > 300 ? text[..300] : text;
    }

    /// <summary>
    /// Persists the newest refresh token together with a hash of the configured one: when the user enters a new token
    /// in the settings, the stored one is discarded.
    /// </summary>
    internal static class TokenState
    {
        private sealed class State
        {
            public string Source { get; set; }
            public string Token { get; set; }
        }

        public static string Serialize(string configured, string current)
        {
            return JsonSerializer.Serialize(new State { Source = Hash(configured), Token = current });
        }

        public static string Resolve(string stateJson, string configured)
        {
            if (string.IsNullOrWhiteSpace(stateJson))
            {
                return configured;
            }
            try
            {
                var state = JsonSerializer.Deserialize<State>(stateJson);
                return state != null && state.Source == Hash(configured) && !string.IsNullOrWhiteSpace(state.Token) ? state.Token : configured;
            }
            catch (JsonException)
            {
                return configured;
            }
        }

        private static string Hash(string value)
        {
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value?.Trim() ?? "")));
        }
    }
}
