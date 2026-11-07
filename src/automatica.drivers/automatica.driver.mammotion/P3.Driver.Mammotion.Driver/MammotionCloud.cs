using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace P3.Driver.Mammotion.Driver
{
    internal sealed class MammotionApiException : Exception
    {
        public MammotionApiException(string message) : base(message)
        {
        }
    }

    internal sealed record MammotionDevice(string IotId, string ProductKey, string DeviceName, string NickName);

    internal sealed record MqttCredentials(string Host, string ClientId, string UserName, string Password);

    /// <summary>
    /// Mammotion cloud (as used by the Mammotion app and PyMammotion): OAuth2 login at id.mammotion.com, device list,
    /// MQTT credentials and the protobuf command invoke on the IoT domain from the access token
    /// </summary>
    internal sealed class MammotionCloud
    {
        public const string DefaultAuthUrl = "https://id.mammotion.com";
        internal const string OAuthClientId = "GxebgSt8si6pKqR";
        private const string OAuthClientSecret = "JP0508SRJFa0A90ADpzLINDBxMa4Vj";
        private const string TokenEndpoint = "/oauth2/token";
        private const string UserAgent = "okhttp/4.9.3";
        private const string AppVersion = "NOT HA,1.0.0";

        private readonly HttpClient _http;
        private readonly ILogger _logger;
        private readonly Func<DateTimeOffset> _now;
        private readonly string _authUrl;
        private readonly string _username;
        private readonly string _password;
        private readonly string _clientId;
        private readonly SemaphoreSlim _tokenLock = new(1, 1);

        private string _accessToken;
        private string _refreshToken;
        private DateTimeOffset _expiresAt;

        public MammotionCloud(HttpClient http, ILogger logger, Func<DateTimeOffset> now, string authUrl, string username, string password)
        {
            _http = http;
            _logger = logger;
            _now = now;
            _authUrl = (string.IsNullOrWhiteSpace(authUrl) ? DefaultAuthUrl : authUrl).TrimEnd('/');
            _username = username?.Trim();
            _password = password;
            _clientId = $"{now().ToUnixTimeMilliseconds()}_{RandomNumberGenerator.GetInt32(1_000_000, 10_000_000)}_1";
        }

        /// <summary>
        /// Base URL of the IoT API, from the "iot" claim of the access token
        /// </summary>
        public string IotUrl { get; private set; }

        /// <summary>
        /// Numeric user account, sent as subtype in every LubaMsg
        /// </summary>
        public int UserAccount { get; private set; }

        #region signing

        internal static string Md5Hex(string value)
        {
            return Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
        }

        /// <summary>
        /// json.dumps(params, ensure_ascii=False, separators=(",", ":")) for a flat string dictionary
        /// </summary>
        internal static string PythonJson(IEnumerable<KeyValuePair<string, string>> parameters)
        {
            var sb = new StringBuilder("{");
            var first = true;
            foreach (var (key, value) in parameters)
            {
                if (!first)
                {
                    sb.Append(',');
                }
                first = false;
                AppendString(sb, key);
                sb.Append(':');
                AppendString(sb, value);
            }
            return sb.Append('}').ToString();
        }

        private static void AppendString(StringBuilder sb, string value)
        {
            sb.Append('"');
            foreach (var c in value ?? "")
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    default:
                        if (c < 0x20)
                        {
                            sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            sb.Append(c);
                        }
                        break;
                }
            }
            sb.Append('"');
        }

        /// <summary>
        /// HMAC-SHA256(key = md5hex(secret), clientId + timestamp + endpoint + json(params)) as lowercase hex
        /// </summary>
        internal static string Sign(long timestampMs, IEnumerable<KeyValuePair<string, string>> parameters)
        {
            var message = $"{OAuthClientId}{timestampMs}{TokenEndpoint}{PythonJson(parameters)}";
            var key = Encoding.UTF8.GetBytes(Md5Hex(OAuthClientSecret));
            return Convert.ToHexString(HMACSHA256.HashData(key, Encoding.UTF8.GetBytes(message))).ToLowerInvariant();
        }

        /// <summary>
        /// The "iot" claim of a JWT access token
        /// </summary>
        internal static string IotFromJwt(string jwt)
        {
            var parts = jwt?.Split('.');
            if (parts == null || parts.Length < 2)
            {
                return null;
            }
            var payload = parts[1].Replace('-', '+').Replace('_', '/');
            payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
            try
            {
                using var doc = JsonDocument.Parse(Convert.FromBase64String(payload));
                if (doc.RootElement.TryGetProperty("iot", out var iot) && iot.ValueKind == JsonValueKind.String)
                {
                    var url = iot.GetString()!.TrimEnd('/');
                    return url.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? url : $"https://{url}";
                }
            }
            catch (Exception ex) when (ex is FormatException or JsonException)
            {
                // not a JWT we understand
            }
            return null;
        }

        #endregion

        #region token

        private async Task<JsonElement> TokenRequestAsync(List<KeyValuePair<string, string>> parameters, CancellationToken token)
        {
            var timestamp = _now().ToUnixTimeMilliseconds();
            var query = string.Join("&", parameters.Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value ?? "")}"));
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{_authUrl}{TokenEndpoint}?{query}");
            request.Headers.TryAddWithoutValidation("User-Agent", UserAgent);
            request.Headers.TryAddWithoutValidation("App-Version", AppVersion);
            request.Headers.TryAddWithoutValidation("Ma-App-Key", OAuthClientId);
            request.Headers.TryAddWithoutValidation("Ma-Signature", Sign(timestamp, parameters));
            request.Headers.TryAddWithoutValidation("Ma-Timestamp", timestamp.ToString(CultureInfo.InvariantCulture));
            request.Headers.TryAddWithoutValidation("Client-Id", _clientId);
            request.Headers.TryAddWithoutValidation("Client-Type", "1");
            request.Content = new StringContent("", Encoding.UTF8, "application/json");

            using var response = await _http.SendAsync(request, token);
            var body = await response.Content.ReadAsStringAsync(token);
            if (!response.IsSuccessStatusCode)
            {
                throw new MammotionApiException($"Mammotion login failed with HTTP {(int)response.StatusCode}");
            }
            using var doc = JsonDocument.Parse(body);
            return doc.RootElement.Clone();
        }

        private bool ApplyToken(JsonElement root)
        {
            if (Code(root) != 0 || !root.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Object)
            {
                return false;
            }
            _accessToken = Str(data, "access_token");
            if (string.IsNullOrEmpty(_accessToken))
            {
                return false;
            }
            _refreshToken = Str(data, "refresh_token") ?? _refreshToken;
            var expiresIn = data.TryGetProperty("expires_in", out var e) && e.TryGetInt64(out var seconds) ? seconds : 3600;
            _expiresAt = _now().AddSeconds(expiresIn);

            if (data.TryGetProperty("userInformation", out var user) && user.ValueKind == JsonValueKind.Object)
            {
                var account = Str(user, "userAccount");
                if (int.TryParse(account, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number))
                {
                    UserAccount = number;
                }
            }
            IotUrl = IotFromJwt(_accessToken) ?? IotUrl;
            if (string.IsNullOrEmpty(IotUrl))
            {
                throw new MammotionApiException("The Mammotion access token carries no IoT domain");
            }
            return true;
        }

        public async Task LoginAsync(CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(_username) || string.IsNullOrEmpty(_password))
            {
                throw new MammotionApiException("User name and password of the Mammotion account are required");
            }

            // the app sends the password base64 encoded, older accounts only accepted it plain
            foreach (var password in new[] { Convert.ToBase64String(Encoding.UTF8.GetBytes(_password)), _password })
            {
                var parameters = new List<KeyValuePair<string, string>>
                {
                    new("username", _username),
                    new("password", password),
                    new("client_id", OAuthClientId),
                    new("grant_type", "password"),
                    new("authType", "0")
                };
                var root = await TokenRequestAsync(parameters, token);
                if (ApplyToken(root))
                {
                    _logger.LogInformation($"Logged in to the Mammotion cloud ({IotUrl})");
                    return;
                }
                _logger.LogDebug($"Mammotion login attempt failed: {Str(root, "msg")}");
            }
            throw new MammotionApiException("Mammotion login failed, check user name and password");
        }

        private async Task<bool> RefreshAsync(CancellationToken token)
        {
            if (string.IsNullOrEmpty(_refreshToken))
            {
                return false;
            }
            try
            {
                var root = await TokenRequestAsync(new List<KeyValuePair<string, string>>
                {
                    new("client_id", OAuthClientId),
                    new("grant_type", "refresh_token"),
                    new("refresh_token", _refreshToken)
                }, token);
                return ApplyToken(root);
            }
            catch (Exception ex) when (ex is MammotionApiException or HttpRequestException or JsonException)
            {
                _logger.LogWarning($"Mammotion token refresh failed: {ex.Message}");
                return false;
            }
        }

        public async Task EnsureTokenAsync(CancellationToken token = default)
        {
            await _tokenLock.WaitAsync(token);
            try
            {
                if (_accessToken != null && _now() < _expiresAt - TimeSpan.FromMinutes(5))
                {
                    return;
                }
                if (_accessToken != null && await RefreshAsync(token))
                {
                    return;
                }
                await LoginAsync(token);
            }
            finally
            {
                _tokenLock.Release();
            }
        }

        #endregion

        #region iot api

        private async Task<JsonElement> IotPostAsync(string path, string json, CancellationToken token)
        {
            await EnsureTokenAsync(token);
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{IotUrl}{path}");
            request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {_accessToken}");
            request.Headers.TryAddWithoutValidation("User-Agent", UserAgent);
            request.Headers.TryAddWithoutValidation("Client-Id", _clientId);
            request.Headers.TryAddWithoutValidation("Client-Type", "1");
            request.Headers.TryAddWithoutValidation("Accept-Language", "en-US");
            request.Headers.TryAddWithoutValidation("Request-Id", string.Concat(Enumerable.Range(0, 21).Select(_ => (char)('0' + RandomNumberGenerator.GetInt32(10)))));
            request.Headers.TryAddWithoutValidation("L-T-Z", $"{_now().ToUnixTimeSeconds()}/0/0");
            request.Content = new StringContent(json ?? "", Encoding.UTF8, "application/json");

            using var response = await _http.SendAsync(request, token);
            var body = await response.Content.ReadAsStringAsync(token);
            if ((int)response.StatusCode == 401)
            {
                _accessToken = null; // log in again with the next call
            }
            if (!response.IsSuccessStatusCode)
            {
                throw new MammotionApiException($"{path} failed with HTTP {(int)response.StatusCode}");
            }
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement.Clone();
            if (Code(root) != 0)
            {
                throw new MammotionApiException($"{path} failed: {Str(root, "msg") ?? Str(root, "code")}");
            }
            return root;
        }

        public async Task<IReadOnlyList<MammotionDevice>> ListDevicesAsync(CancellationToken token = default)
        {
            var root = await IotPostAsync("/v1/user/device/page", "{\"iotId\":\"\",\"pageNumber\":1,\"pageSize\":100}", token);
            var result = new List<MammotionDevice>();
            if (!root.TryGetProperty("data", out var data))
            {
                return result;
            }
            var records = data.ValueKind == JsonValueKind.Array ? data : data.TryGetProperty("records", out var r) ? r : default;
            if (records.ValueKind != JsonValueKind.Array)
            {
                return result;
            }
            foreach (var record in records.EnumerateArray())
            {
                var iotId = Str(record, "iotId");
                if (string.IsNullOrEmpty(iotId))
                {
                    continue;
                }
                result.Add(new MammotionDevice(iotId, Str(record, "productKey"), Str(record, "deviceName"), Str(record, "nickName") ?? Str(record, "nickname")));
            }
            return result;
        }

        public async Task<MqttCredentials> GetMqttCredentialsAsync(CancellationToken token = default)
        {
            var root = await IotPostAsync("/v1/mqtt/auth/jwt", "", token);
            if (!root.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Object)
            {
                throw new MammotionApiException("No MQTT credentials in the response");
            }
            return new MqttCredentials(Str(data, "host"), Str(data, "clientId"), Str(data, "username"), Str(data, "jwt"));
        }

        /// <summary>
        /// Sends a LubaMsg to the mower (device_protobuf_sync_service)
        /// </summary>
        public async Task InvokeAsync(string iotId, byte[] lubaMsg, CancellationToken token = default)
        {
            var json = JsonSerializer.Serialize(new
            {
                args = new { content = Convert.ToBase64String(lubaMsg) },
                deviceName = "",
                identifier = "device_protobuf_sync_service",
                iotId,
                productKey = ""
            });
            await IotPostAsync("/v1/mqtt/rpc/thing/service/invoke", json, token);
        }

        #endregion

        private static int Code(JsonElement root)
        {
            if (!root.TryGetProperty("code", out var code))
            {
                return -1;
            }
            return code.ValueKind switch
            {
                JsonValueKind.Number when code.TryGetInt32(out var i) => i,
                JsonValueKind.String when int.TryParse(code.GetString(), out var i) => i,
                _ => -1
            };
        }

        internal static string Str(JsonElement element, string name)
        {
            if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(name, out var value))
            {
                return null;
            }
            return value.ValueKind switch
            {
                JsonValueKind.String => value.GetString(),
                JsonValueKind.Number => value.GetRawText(),
                _ => null
            };
        }
    }
}
