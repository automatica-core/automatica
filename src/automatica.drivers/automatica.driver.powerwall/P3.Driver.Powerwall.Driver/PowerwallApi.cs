using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace P3.Driver.Powerwall.Driver
{
    internal sealed class PowerwallApiException : Exception
    {
        public HttpStatusCode? StatusCode { get; }

        public PowerwallApiException(string message, HttpStatusCode? statusCode = null) : base(message)
        {
            StatusCode = statusCode;
        }
    }

    /// <summary>
    /// Local REST API of the Tesla Backup Gateway 2 (Powerwall 2 / +): customer login and the read endpoints.
    /// Operation changes (mode, reserve) are refused (403) by current firmware for the customer login.
    /// </summary>
    internal sealed class PowerwallApi
    {
        private readonly HttpClient _http;
        private readonly SemaphoreSlim _loginLock = new(1, 1);
        private string _token;

        public PowerwallApi(HttpClient http, string baseUrl, string password, string email)
        {
            _http = http;
            BaseUrl = NormalizeUrl(baseUrl);
            Password = password ?? "";
            Email = string.IsNullOrWhiteSpace(email) ? "automatica@localhost" : email.Trim();
        }

        public string BaseUrl { get; }
        public string Password { get; }
        public string Email { get; }

        /// <summary>
        /// "192.168.1.50" → "https://192.168.1.50"
        /// </summary>
        internal static string NormalizeUrl(string value)
        {
            var url = (value ?? "").Trim().TrimEnd('/');
            if (url.Length == 0)
            {
                return url;
            }
            return url.Contains("://", StringComparison.Ordinal) ? url : "https://" + url;
        }

        public async Task LoginAsync(CancellationToken token)
        {
            var body = JsonSerializer.Serialize(new
            {
                username = "customer",
                password = Password,
                email = Email,
                clientInfo = new { timezone = IanaZone() }
            });
            using var request = new HttpRequestMessage(HttpMethod.Post, BaseUrl + "/api/login/Basic")
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
            using var response = await _http.SendAsync(request, token);
            var text = await response.Content.ReadAsStringAsync(token);
            if (!response.IsSuccessStatusCode)
            {
                throw new PowerwallApiException($"Login failed ({(int)response.StatusCode})", response.StatusCode);
            }

            using var doc = JsonDocument.Parse(text);
            if (!doc.RootElement.TryGetProperty("token", out var t) || t.ValueKind != JsonValueKind.String || string.IsNullOrEmpty(t.GetString()))
            {
                throw new PowerwallApiException("Login response has no token");
            }
            _token = t.GetString();
        }

        private static string IanaZone()
        {
            var zone = TimeZoneInfo.Local;
            return !zone.HasIanaId && TimeZoneInfo.TryConvertWindowsIdToIanaId(zone.Id, out var iana) ? iana : zone.Id;
        }

        private async Task EnsureLoginAsync(CancellationToken token)
        {
            if (_token != null)
            {
                return;
            }
            await _loginLock.WaitAsync(token);
            try
            {
                if (_token == null)
                {
                    await LoginAsync(token);
                }
            }
            finally
            {
                _loginLock.Release();
            }
        }

        public Task<JsonDocument> GetAsync(string path, CancellationToken token = default) => SendAsync(HttpMethod.Get, path, null, token);

        public Task<JsonDocument> PostAsync(string path, object body, CancellationToken token = default) => SendAsync(HttpMethod.Post, path, body, token);

        /// <summary>
        /// Sends with the bearer token, logs in again once when the token expired (401)
        /// </summary>
        private async Task<JsonDocument> SendAsync(HttpMethod method, string path, object body, CancellationToken token)
        {
            for (var attempt = 0; ; attempt++)
            {
                await EnsureLoginAsync(token);
                using var request = new HttpRequestMessage(method, BaseUrl + path);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
                if (body != null)
                {
                    request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
                }

                using var response = await _http.SendAsync(request, token);
                var text = await response.Content.ReadAsStringAsync(token);
                if (response.StatusCode == HttpStatusCode.Unauthorized && attempt == 0)
                {
                    _token = null;
                    continue;
                }
                if (!response.IsSuccessStatusCode)
                {
                    throw new PowerwallApiException($"{method} {path} failed ({(int)response.StatusCode})", response.StatusCode);
                }
                return JsonDocument.Parse(string.IsNullOrWhiteSpace(text) ? "{}" : text);
            }
        }

        /// <summary>
        /// HTTP handler for the gateway: its certificate is self-signed, so it is pinned by its SHA-256 fingerprint.
        /// An empty pin trusts the first certificate and reports it through <paramref name="pinned"/> (trust on first use).
        /// </summary>
        public static HttpClientHandler CreateHandler(Func<string> pin, Action<string> pinned)
        {
            return new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (_, certificate, _, errors) => Validate(certificate, errors, pin(), pinned)
            };
        }

        internal static bool Validate(X509Certificate2 certificate, SslPolicyErrors errors, string pin, Action<string> pinned)
        {
            if (certificate == null)
            {
                return false;
            }
            var fingerprint = Fingerprint(certificate);
            if (string.IsNullOrWhiteSpace(pin))
            {
                pinned?.Invoke(fingerprint);
                return true;
            }
            return string.Equals(Normalize(pin), fingerprint, StringComparison.Ordinal);
        }

        internal static string Fingerprint(X509Certificate2 certificate) => Convert.ToHexString(SHA256.HashData(certificate.RawData));

        private static string Normalize(string pin) => pin.Replace(":", "").Replace(" ", "").Trim().ToUpperInvariant();
    }
}
