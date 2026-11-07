using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace P3.Driver.Dreame.Driver
{
    internal sealed class DreameApiException : Exception
    {
        public DreameApiException(string message) : base(message)
        {
        }
    }

    internal sealed record DreameDevice(string Did, string Model, string Name, string Mac);

    /// <summary>
    /// Dreamehome cloud (the account of the Dreamehome app). Protocol as implemented by Tasshack/dreame-vacuum:
    /// form login with an MD5 salted password, then signed JSON requests; device commands are MIoT get_properties / set_properties / action.
    /// </summary>
    internal sealed class DreameCloud
    {
        internal const string Host = ".iot.dreame.tech";
        internal const int Port = 13267;
        internal const string PasswordSalt = "RAylYC%fmSKp7%Tq";
        internal const string BasicAuth = "ZHJlYW1lX2FwcHYxOkFQXmR2QHpAU1FZVnhOODg=";
        internal const string DefaultTenant = "000000";
        internal const string SignKey = "EETjszu*XI5znHsI";
        internal const string ClientVersion = "102060300";
        internal const string UserAgent = "Dart/3.9 (dart:io)";

        private static readonly JsonSerializerOptions Compact = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
        private static readonly TimeSpan RefreshMargin = TimeSpan.FromMinutes(10);

        private readonly HttpClient _http;
        private readonly ILogger _logger;
        private readonly Func<DateTimeOffset> _now;
        private readonly string _username;
        private readonly string _password;
        private readonly string _visitorHash;
        private readonly SemaphoreSlim _loginLock = new(1, 1);
        private int _requestId = RandomNumberGenerator.GetInt32(1, 100);

        private string _accessToken;
        private string _refreshToken;
        private DateTimeOffset _expires = DateTimeOffset.MinValue;
        private string _tenant;
        private string _region;
        private string _lang;
        private string _country;

        public DreameCloud(HttpClient http, ILogger logger, Func<DateTimeOffset> now, string baseUrl, string username, string password)
        {
            _http = http;
            _logger = logger;
            _now = now;
            BaseUrl = baseUrl.TrimEnd('/');
            _username = username ?? "";
            _password = password ?? "";
            _visitorHash = Md5Hex(Guid.NewGuid().ToString("N"));
        }

        /// <summary>
        /// https://{region}.iot.dreame.tech:13267
        /// </summary>
        public static string RegionUrl(string region) => $"https://{(string.IsNullOrWhiteSpace(region) ? "eu" : region.Trim().ToLowerInvariant())}{Host}:{Port}";

        public string BaseUrl { get; }

        public bool LoggedIn => _accessToken != null;

        internal static string Md5Hex(string value) => Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

        internal static string HashPassword(string password) => Md5Hex(password + PasswordSalt);

        private Dictionary<string, string> BaseHeaders()
        {
            var headers = new Dictionary<string, string>
            {
                ["user-agent"] = UserAgent,
                ["dreame-meta"] = $"cv=a_{ClientVersion};canvasHash={Md5Hex(_username + "c")[..8]};webglHash={Md5Hex(_username + "w")[..8]};visitorIdHash={_visitorHash}",
                ["tenant-id"] = _tenant ?? DefaultTenant,
                ["authorization"] = "Basic " + BasicAuth,
            };
            if (_region != null && _lang != null && _country != null)
            {
                headers["dreame-rlc"] = RegionHeader(_region, _lang, _country);
            }
            return headers;
        }

        /// <summary>
        /// base64(AES-128-ECB(sign key, PKCS7("region|lang|country")))
        /// </summary>
        internal static string RegionHeader(string region, string lang, string country)
        {
            using var aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(SignKey);
            return Convert.ToBase64String(aes.EncryptEcb(Encoding.UTF8.GetBytes($"{region}|{lang}|{country}"), PaddingMode.PKCS7));
        }

        public async Task LoginAsync(CancellationToken token = default)
        {
            await _loginLock.WaitAsync(token);
            try
            {
                await LoginLockedAsync(token);
            }
            finally
            {
                _loginLock.Release();
            }
        }

        private async Task LoginLockedAsync(CancellationToken token)
        {
            if (_refreshToken != null)
            {
                if (await TryLoginAsync($"grant_type=refresh_token&scope=all&platform=ANDROID&type=account&refresh_token={_refreshToken}", token))
                {
                    return;
                }
                _refreshToken = null;
            }

            var body = $"grant_type=password&scope=all&platform=ANDROID&type=account&username={Uri.EscapeDataString(_username)}&password={HashPassword(_password)}";
            if (!await TryLoginAsync(body, token))
            {
                throw new DreameApiException("Dreame login failed, check user name, password and region");
            }
        }

        private async Task<bool> TryLoginAsync(string body, CancellationToken token)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, BaseUrl + "/dreame-auth/oauth/token")
            {
                Content = new StringContent(body, Encoding.UTF8, "application/x-www-form-urlencoded")
            };
            foreach (var (k, v) in BaseHeaders())
            {
                request.Headers.TryAddWithoutValidation(k, v);
            }

            using var response = await _http.SendAsync(request, token);
            var text = await response.Content.ReadAsStringAsync(token);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                _logger.LogWarning($"Dreame login answered {(int)response.StatusCode}: {Truncate(text)}");
                return false;
            }

            using var json = JsonDocument.Parse(text);
            var root = json.RootElement;
            if (!root.TryGetProperty("access_token", out var accessToken))
            {
                _logger.LogWarning($"Dreame login without access token: {Truncate(text)}");
                return false;
            }

            _accessToken = accessToken.GetString();
            _refreshToken = root.TryGetProperty("refresh_token", out var rt) ? rt.GetString() : null;
            var expiresIn = root.TryGetProperty("expires_in", out var e) && e.TryGetInt32(out var s) ? s : 3600;
            _expires = _now().AddSeconds(expiresIn);
            _tenant = Str(root, "tenant_id") ?? _tenant;
            _region = Str(root, "region") ?? _region;
            _lang = Str(root, "lang") ?? _lang;
            _country = Str(root, "country") ?? _country;
            return true;
        }

        private static string Str(JsonElement e, string name) => e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

        /// <summary>
        /// Adds "sign" = md5(spliced parameters + epoch ms + key) and "timestamp" = epoch ms
        /// </summary>
        internal static JsonObject Sign(JsonObject parameters, long epochMs)
        {
            var signed = (JsonObject)parameters.DeepClone();
            signed["sign"] = Md5Hex(Splice(parameters, true) + epochMs.ToString(CultureInfo.InvariantCulture) + SignKey);
            signed["timestamp"] = epochMs;
            return signed;
        }

        /// <summary>
        /// Port of the Python signing string: keys sorted, "k=v" joined with "&amp;"; nested objects as "k=[...]";
        /// lists only at the top level (as sorted compact JSON); below the top level scalars are JSON encoded.
        /// </summary>
        internal static string Splice(JsonObject obj, bool top)
        {
            var parts = new List<string>();
            foreach (var (key, value) in obj.OrderBy(p => p.Key, StringComparer.Ordinal))
            {
                switch (value)
                {
                    case JsonObject nested:
                        var inner = Splice(nested, false);
                        parts.Add(inner.Length > 0 ? $"{key}=[{inner}]" : $"{key}=]");
                        break;
                    case JsonArray array:
                        if (top)
                        {
                            parts.Add($"{key}={SortedJson(array)}");
                        }
                        break;
                    case null:
                        parts.Add($"{key}=null");
                        break;
                    case JsonValue scalar when scalar.TryGetValue<bool>(out var b):
                        parts.Add($"{key}={(b ? "true" : "false")}");
                        break;
                    case JsonValue scalar:
                        parts.Add(top ? $"{key}={ScalarText(scalar)}" : $"{key}={scalar.ToJsonString(Compact)}");
                        break;
                }
            }
            return string.Join("&", parts);
        }

        private static string ScalarText(JsonValue value)
        {
            if (value.TryGetValue<string>(out var s))
            {
                return s;
            }
            return value.ToJsonString(Compact);
        }

        private static string SortedJson(JsonNode node)
        {
            return node switch
            {
                JsonObject o => "{" + string.Join(",", o.OrderBy(p => p.Key, StringComparer.Ordinal).Select(p => JsonSerializer.Serialize(p.Key, Compact) + ":" + (p.Value == null ? "null" : SortedJson(p.Value)))) + "}",
                JsonArray a => "[" + string.Join(",", a.Select(v => v == null ? "null" : SortedJson(v))) + "]",
                _ => node.ToJsonString(Compact)
            };
        }

        private async Task EnsureLoggedInAsync(CancellationToken token)
        {
            if (_accessToken == null || _now() >= _expires - RefreshMargin)
            {
                await LoginAsync(token);
            }
        }

        internal async Task<JsonElement> PostAsync(string path, JsonObject parameters, CancellationToken token = default)
        {
            for (var attempt = 0; ; attempt++)
            {
                await EnsureLoggedInAsync(token);
                var body = parameters == null ? null : Sign(parameters, _now().ToUnixTimeMilliseconds()).ToJsonString(Compact);

                using var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/{path}");
                foreach (var (k, v) in BaseHeaders())
                {
                    request.Headers.TryAddWithoutValidation(k, v);
                }
                request.Headers.TryAddWithoutValidation("dreame-auth", _accessToken);
                request.Content = new StringContent(body ?? "", Encoding.UTF8, "application/json");

                using var response = await _http.SendAsync(request, token);
                var text = await response.Content.ReadAsStringAsync(token);
                if (response.StatusCode == HttpStatusCode.Unauthorized && attempt == 0)
                {
                    _logger.LogInformation("Dreame session expired, logging in again");
                    _accessToken = null;
                    continue;
                }
                if (!response.IsSuccessStatusCode)
                {
                    throw new DreameApiException($"{path} failed ({(int)response.StatusCode}): {Truncate(text)}");
                }

                using var json = JsonDocument.Parse(text);
                return json.RootElement.Clone();
            }
        }

        public async Task<IReadOnlyList<DreameDevice>> ListDevicesAsync(CancellationToken token = default)
        {
            var response = await PostAsync("dreame-user-iot/iotuserbind/device/listV2", null, token);
            if (!IsOk(response) || !response.TryGetProperty("data", out var data) ||
                !data.TryGetProperty("page", out var page) || !page.TryGetProperty("records", out var records) || records.ValueKind != JsonValueKind.Array)
            {
                throw new DreameApiException($"Unexpected device list: {Truncate(response.ToString())}");
            }

            var devices = new List<DreameDevice>();
            foreach (var record in records.EnumerateArray())
            {
                var did = record.TryGetProperty("did", out var d) ? d.ToString() : null;
                if (string.IsNullOrWhiteSpace(did))
                {
                    continue;
                }
                var name = Str(record, "customName");
                if (string.IsNullOrWhiteSpace(name) && record.TryGetProperty("deviceInfo", out var info) && info.ValueKind == JsonValueKind.Object)
                {
                    name = Str(info, "displayName");
                }
                devices.Add(new DreameDevice(did, Str(record, "model"), name, Str(record, "mac")));
            }
            return devices;
        }

        /// <summary>
        /// The device's IoT host ("10000.mt.eu.iot.dreame.tech:19973"), its first label selects the command service
        /// </summary>
        public async Task<string> GetBindDomainAsync(string did, CancellationToken token = default)
        {
            var response = await PostAsync("dreame-user-iot/iotuserbind/device/info", new JsonObject { ["did"] = did }, token);
            if (!IsOk(response) || !response.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Object)
            {
                throw new DreameApiException($"Device {did} not found: {Truncate(response.ToString())}");
            }
            return Str(data, "bindDomain");
        }

        internal static string CommandPath(string bindDomain)
        {
            var prefix = string.IsNullOrWhiteSpace(bindDomain) ? "" : "-" + bindDomain.Split('.')[0];
            return $"dreame-iot-com{prefix}/device/sendCommand";
        }

        /// <summary>
        /// Sends a MIoT method to the device and returns "result"
        /// </summary>
        public async Task<JsonElement> SendAsync(string did, string bindDomain, string method, JsonNode parameters, CancellationToken token = default)
        {
            var id = Interlocked.Increment(ref _requestId);
            var body = new JsonObject
            {
                ["did"] = did,
                ["id"] = id,
                ["data"] = new JsonObject
                {
                    ["did"] = did,
                    ["id"] = id,
                    ["method"] = method,
                    ["params"] = parameters
                }
            };
            var response = await PostAsync(CommandPath(bindDomain), body, token);
            if (response.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Object && data.TryGetProperty("result", out var result))
            {
                return result.Clone();
            }
            throw new DreameApiException($"{method} failed: {Truncate(response.ToString())}");
        }

        public async Task<IReadOnlyDictionary<(int Siid, int Piid), JsonElement>> GetPropertiesAsync(string did, string bindDomain, IEnumerable<(int Siid, int Piid)> properties, CancellationToken token = default)
        {
            var list = new JsonArray();
            foreach (var (siid, piid) in properties.Distinct())
            {
                list.Add(new JsonObject { ["did"] = $"{siid}.{piid}", ["siid"] = siid, ["piid"] = piid });
            }

            var result = await SendAsync(did, bindDomain, "get_properties", list, token);
            var values = new Dictionary<(int, int), JsonElement>();
            if (result.ValueKind != JsonValueKind.Array)
            {
                return values;
            }
            foreach (var item in result.EnumerateArray())
            {
                var code = item.TryGetProperty("code", out var c) && c.TryGetInt32(out var ci) ? ci : 0;
                if (code != 0 || !item.TryGetProperty("siid", out var s) || !item.TryGetProperty("piid", out var p) || !item.TryGetProperty("value", out var v))
                {
                    continue;
                }
                values[(s.GetInt32(), p.GetInt32())] = v.Clone();
            }
            return values;
        }

        public async Task SetPropertyAsync(string did, string bindDomain, int siid, int piid, JsonNode value, CancellationToken token = default)
        {
            var parameters = new JsonArray { new JsonObject { ["did"] = did, ["siid"] = siid, ["piid"] = piid, ["value"] = value } };
            var result = await SendAsync(did, bindDomain, "set_properties", parameters, token);
            ThrowOnErrorCode(result, $"set {siid}.{piid}");
        }

        public async Task ActionAsync(string did, string bindDomain, int siid, int aiid, CancellationToken token = default)
        {
            var parameters = new JsonObject { ["did"] = did, ["siid"] = siid, ["aiid"] = aiid, ["in"] = new JsonArray() };
            var result = await SendAsync(did, bindDomain, "action", parameters, token);
            ThrowOnErrorCode(result, $"action {siid}.{aiid}");
        }

        private static void ThrowOnErrorCode(JsonElement result, string what)
        {
            var item = result.ValueKind == JsonValueKind.Array ? result.EnumerateArray().FirstOrDefault() : result;
            if (item.ValueKind == JsonValueKind.Object && item.TryGetProperty("code", out var code) && code.TryGetInt32(out var c) && c != 0)
            {
                throw new DreameApiException($"{what} answered code {c}");
            }
        }

        private static bool IsOk(JsonElement response) => response.TryGetProperty("code", out var code) && code.TryGetInt32(out var c) && c == 0;

        private static string Truncate(string text) => text == null ? "" : text.Length > 300 ? text[..300] : text;
    }
}
