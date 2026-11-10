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
using P3.Driver.Byd.Driver.Crypto;

namespace P3.Driver.Byd.Driver
{
    internal class BydApiException : Exception
    {
        public BydApiException(string message, string code = null) : base(message)
        {
            Code = code;
        }

        public string Code { get; }

        /// <summary>
        /// Decoded outer answer of the server, for diagnostics
        /// </summary>
        public string Response { get; init; }
    }

    internal sealed record BydVehicleInfo(string Vin, string Alias, string ModelName, string EnergyType);

    /// <summary>
    /// BYD overseas app API (dilinkappoversea): every request is a Bangcle envelope around a signed JSON payload whose
    /// data part is AES encrypted with a key derived from the session tokens. Port of the protocol of pyBYD.
    /// </summary>
    internal sealed class BydCloud
    {
        public const string DefaultBaseUrl = "https://dilinkappoversea-eu.byd.auto";
        private const string UserAgent = "okhttp/4.12.0";
        private const string AppName = "Automatica+1.0.0";
        private const string AppVersion = "3.5.1";
        private const string AppInnerVersion = "351";
        private const string TboxVersion = "3";
        private static readonly HashSet<string> SessionExpiredCodes = new() { "1002", "1005", "1010" };

        // device profile sent with every request (pyBYD's defaults)
        private static readonly KeyValuePair<string, string>[] DeviceFields =
        {
            new("ostype", "and"), new("imei", "BANGCLE01234"), new("mac", "00:00:00:00:00:00"),
            new("model", "POCO F1"), new("sdk", "35"), new("mod", "Xiaomi")
        };

        private readonly HttpClient _http;
        private readonly ILogger _logger;
        private readonly Func<DateTimeOffset> _now;
        private readonly string _baseUrl;
        private readonly string _username;
        private readonly string _password;
        private readonly string _countryCode;
        private readonly string _language;
        private readonly string _imeiMd5;
        private readonly Bangcle _bangcle;
        private readonly SemaphoreSlim _sessionLock = new(1, 1);

        private Session _session;

        private sealed record Session(string UserId, string ContentKey, string SignKey, DateTimeOffset Created);

        public BydCloud(HttpClient http, ILogger logger, Func<DateTimeOffset> now, string baseUrl, string username, string password, string countryCode, string language)
        {
            _http = http;
            _logger = logger;
            _now = now;
            _baseUrl = (string.IsNullOrWhiteSpace(baseUrl) ? DefaultBaseUrl : baseUrl.Trim()).TrimEnd('/');
            _username = username?.Trim();
            _password = password;
            _countryCode = string.IsNullOrWhiteSpace(countryCode) ? "DE" : countryCode.Trim().ToUpperInvariant();
            _language = string.IsNullOrWhiteSpace(language) ? "en" : language.Trim().ToLowerInvariant();
            _imeiMd5 = BydCrypto.Md5(_username ?? "");
            _bangcle = Bangcle.Default;
        }

        public TimeSpan SessionTtl { get; set; } = TimeSpan.FromHours(12);
        public int PollAttempts { get; set; } = 10;
        public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(1.5);

        /// <summary>
        /// IANA time zone sent with the login
        /// </summary>
        public string TimeZoneId { get; set; } = TimeZone();

        internal static string TimeZone()
        {
            var id = TimeZoneInfo.Local.Id;
            return TimeZoneInfo.TryConvertWindowsIdToIanaId(id, out var iana) ? iana : id;
        }

        private static string RandomHex() => Convert.ToHexString(RandomNumberGenerator.GetBytes(16));

        #region transport

        private async Task<JsonElement> PostAsync(string endpoint, IEnumerable<KeyValuePair<string, string>> outer, CancellationToken token)
        {
            var envelope = _bangcle.Encode(BydCrypto.PythonJson(outer, ensureAscii: true));
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}{endpoint}");
            request.Headers.TryAddWithoutValidation("accept-encoding", "identity");
            request.Headers.TryAddWithoutValidation("user-agent", UserAgent);
            request.Content = new StringContent(JsonSerializer.Serialize(new { request = envelope }), Encoding.UTF8, "application/json");

            using var response = await _http.SendAsync(request, token);
            var text = await response.Content.ReadAsStringAsync(token);
            if (!response.IsSuccessStatusCode)
            {
                throw new BydApiException($"HTTP {(int)response.StatusCode} from {endpoint}");
            }
            string encoded;
            using (var doc = JsonDocument.Parse(text))
            {
                encoded = doc.RootElement.TryGetProperty("response", out var r) && r.ValueKind == JsonValueKind.String ? r.GetString() : null;
            }
            if (string.IsNullOrWhiteSpace(encoded))
            {
                throw new BydApiException($"No response payload from {endpoint}");
            }
            var decoded = _bangcle.Decode(encoded).Trim();
            if (decoded.StartsWith("F{", StringComparison.Ordinal) || decoded.StartsWith("F[", StringComparison.Ordinal))
            {
                decoded = decoded[1..];
            }
            using var result = JsonDocument.Parse(decoded);
            return result.RootElement.Clone();
        }

        internal static string Code(JsonElement response) => Str(response, "code") ?? "";

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
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                _ => null
            };
        }

        #endregion

        #region login

        public async Task LoginAsync(CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(_username) || string.IsNullOrEmpty(_password))
            {
                throw new BydApiException("User name and password of the BYD account are required");
            }

            var now = _now().ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture);
            var inner = new List<KeyValuePair<string, string>>
            {
                new("agreeStatus", "0"),
                new("agreementType", "[1,2]"),
                new("appInnerVersion", AppInnerVersion),
                new("appVersion", AppVersion),
                new("deviceName", "XIAOMIPOCO F1"),
                new("deviceType", "0"),
                new("imeiMD5", _imeiMd5),
                new("isAuto", "1"),
                new("mobileBrand", "XIAOMI"),
                new("mobileModel", "POCO F1"),
                new("networkType", "wifi"),
                new("osType", "15"),
                new("osVersion", "35"),
                new("random", RandomHex()),
                new("softType", "0"),
                new("timeStamp", now),
                new("timeZone", TimeZoneId)
            };
            var loginKey = BydCrypto.LoginKey(_password);
            var signFields = inner.Concat(new KeyValuePair<string, string>[]
            {
                new("appName", AppName), new("countryCode", _countryCode), new("functionType", "pwdLogin"), new("identifier", _username),
                new("identifierType", "0"), new("language", _language), new("reqTimestamp", now)
            });

            var outer = new List<KeyValuePair<string, string>>
            {
                new("appName", AppName),
                new("countryCode", _countryCode),
                new("encryData", BydCrypto.AesEncryptHex(BydCrypto.PythonJson(inner, ensureAscii: true), loginKey)),
                new("functionType", "pwdLogin"),
                new("identifier", _username),
                new("identifierType", "0"),
                new("imeiMD5", _imeiMd5),
                new("isAuto", "1"),
                new("language", _language),
                new("reqTimestamp", now),
                new("sign", BydCrypto.Sign(signFields, BydCrypto.Md5(_password))),
                new("signKey", _password)
            };
            outer.AddRange(DeviceFields);
            outer.Add(new("serviceTime", now));
            outer.Add(new("checkcode", BydCrypto.CheckCode(outer)));

            var response = await PostAsync("/app/account/login", outer, token);
            var code = Code(response);
            if (code != "0")
            {
                throw new BydApiException($"BYD login failed: code {code} {Str(response, "message")}", code);
            }
            var respondData = Str(response, "respondData");
            if (string.IsNullOrEmpty(respondData))
            {
                throw new BydApiException("BYD login response has no data");
            }
            using var doc = JsonDocument.Parse(BydCrypto.AesDecryptHex(respondData, loginKey));
            if (!doc.RootElement.TryGetProperty("token", out var t) || t.ValueKind != JsonValueKind.Object)
            {
                throw new BydApiException("BYD login response has no token");
            }
            var userId = Str(t, "userId");
            var signToken = Str(t, "signToken");
            var encryToken = Str(t, "encryToken");
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(signToken) || string.IsNullOrEmpty(encryToken))
            {
                throw new BydApiException("BYD login response misses token fields");
            }
            _session = new Session(userId, BydCrypto.Md5(encryToken), BydCrypto.Md5(signToken), _now());
            _logger.LogInformation($"Logged in to the BYD cloud (time zone {TimeZoneId})");
        }

        private async Task<Session> EnsureSessionAsync(CancellationToken token)
        {
            await _sessionLock.WaitAsync(token);
            try
            {
                if (_session == null || _now() - _session.Created >= SessionTtl)
                {
                    await LoginAsync(token);
                }
                return _session;
            }
            finally
            {
                _sessionLock.Release();
            }
        }

        #endregion

        #region token requests

        private List<KeyValuePair<string, string>> InnerBase(string vin = null, string requestSerial = null)
        {
            var inner = new List<KeyValuePair<string, string>>
            {
                new("deviceType", "0"),
                new("imeiMD5", _imeiMd5),
                new("networkType", "wifi"),
                new("random", RandomHex()),
                new("timeStamp", _now().ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture)),
                new("version", AppInnerVersion)
            };
            if (!string.IsNullOrEmpty(vin))
            {
                inner.Add(new("vin", vin));
            }
            if (!string.IsNullOrEmpty(requestSerial))
            {
                inner.Add(new("requestSerial", requestSerial));
            }
            return inner;
        }

        private async Task<JsonElement> PostTokenAsync(string endpoint, List<KeyValuePair<string, string>> inner, CancellationToken token, bool retried = false)
        {
            var session = await EnsureSessionAsync(token);
            var now = _now().ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture);
            var signFields = inner.Concat(new KeyValuePair<string, string>[]
            {
                new("countryCode", _countryCode), new("identifier", session.UserId), new("imeiMD5", _imeiMd5), new("language", _language), new("reqTimestamp", now)
            });
            var outer = new List<KeyValuePair<string, string>>
            {
                new("countryCode", _countryCode),
                new("encryData", BydCrypto.AesEncryptHex(BydCrypto.PythonJson(inner, ensureAscii: true), session.ContentKey)),
                new("identifier", session.UserId),
                new("imeiMD5", _imeiMd5),
                new("language", _language),
                new("reqTimestamp", now),
                new("sign", BydCrypto.Sign(signFields, session.SignKey))
            };
            outer.AddRange(DeviceFields);
            outer.Add(new("serviceTime", now));
            outer.Add(new("checkcode", BydCrypto.CheckCode(outer)));

            var response = await PostAsync(endpoint, outer, token);
            var code = Code(response);
            if (SessionExpiredCodes.Contains(code) && !retried)
            {
                _logger.LogDebug($"BYD session expired (code {code}), logging in again");
                _session = null;
                return await PostTokenAsync(endpoint, inner, token, true);
            }
            if (code != "0")
            {
                _logger.LogDebug($"{endpoint} answered {response.GetRawText()}");
                throw new BydApiException($"{endpoint} failed: code {code} {Str(response, "message")}", code) { Response = response.GetRawText() };
            }
            var respondData = Str(response, "respondData");
            if (string.IsNullOrEmpty(respondData))
            {
                return default;
            }
            var plain = BydCrypto.AesDecryptHex(respondData, session.ContentKey);
            if (string.IsNullOrWhiteSpace(plain))
            {
                return default;
            }
            using var doc = JsonDocument.Parse(plain);
            return doc.RootElement.Clone();
        }

        /// <summary>
        /// Calls the trigger endpoint, then polls the result endpoint with the request serial until the data is complete
        /// </summary>
        private async Task<JsonElement> TriggerAndPollAsync(string trigger, string poll, Func<string, List<KeyValuePair<string, string>>> inner, Func<JsonElement, bool> isReady, CancellationToken token)
        {
            var latest = await PostTokenAsync(trigger, inner(null), token);
            if (latest.ValueKind != JsonValueKind.Object || isReady(latest))
            {
                return latest;
            }
            var serial = Str(latest, "requestSerial");
            if (string.IsNullOrEmpty(serial))
            {
                return latest;
            }
            for (var attempt = 0; attempt < PollAttempts; attempt++)
            {
                if (PollInterval > TimeSpan.Zero)
                {
                    await Task.Delay(PollInterval, token);
                }
                var result = await PostTokenAsync(poll, inner(serial), token);
                if (result.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }
                latest = result;
                serial = Str(result, "requestSerial") ?? serial;
                if (isReady(result))
                {
                    break;
                }
            }
            return latest;
        }

        #endregion

        #region api

        public async Task<IReadOnlyList<BydVehicleInfo>> ListVehiclesAsync(CancellationToken token = default)
        {
            var data = await PostTokenAsync("/app/account/getAllListByUserId", InnerBase(), token);
            var result = new List<BydVehicleInfo>();
            if (data.ValueKind != JsonValueKind.Array)
            {
                return result;
            }
            foreach (var item in data.EnumerateArray())
            {
                var vin = Str(item, "vin");
                if (!string.IsNullOrEmpty(vin))
                {
                    result.Add(new BydVehicleInfo(vin, Str(item, "autoAlias"), Str(item, "modelName"), Str(item, "energyType")));
                }
            }
            return result;
        }

        internal static double? Number(JsonElement element, string name)
        {
            var s = Str(element, name);
            return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var d) ? d : null;
        }

        /// <summary>
        /// Data is complete when the car is not offline and reports tyre pressure, a timestamp or a range
        /// </summary>
        internal static bool IsRealtimeReady(JsonElement info)
        {
            if (info.ValueKind != JsonValueKind.Object || info.EnumerateObject().All(p => p.Name == "requestSerial"))
            {
                return false;
            }
            if (Number(info, "onlineState") == 2)
            {
                return false;
            }
            return new[] { "leftFrontTirepressure", "rightFrontTirepressure", "leftRearTirepressure", "rightRearTirepressure" }.Any(f => Number(info, f) > 0)
                   || Number(info, "time") > 0
                   || Number(info, "enduranceMileage") > 0;
        }

        public Task<JsonElement> GetRealtimeAsync(string vin, string energyType, CancellationToken token = default)
        {
            return TriggerAndPollAsync("/vehicleInfo/vehicle/vehicleRealTimeRequest", "/vehicleInfo/vehicle/vehicleRealTimeResult", serial =>
            {
                var inner = InnerBase(vin, serial);
                inner.Add(new("energyType", string.IsNullOrEmpty(energyType) ? "0" : energyType));
                inner.Add(new("tboxVersion", TboxVersion));
                return inner;
            }, IsRealtimeReady, token);
        }

        public Task<JsonElement> GetGpsAsync(string vin, CancellationToken token = default)
        {
            return TriggerAndPollAsync("/control/getGpsInfo", "/control/getGpsInfoResult", serial => InnerBase(vin, serial),
                info => info.ValueKind == JsonValueKind.Object && info.EnumerateObject().Any(p => p.Name != "requestSerial"), token);
        }

        /// <summary>
        /// 0 pending, 1 success, 2 failure; "res" 1 pending, 2 success, others failure
        /// </summary>
        internal static int ControlState(JsonElement result)
        {
            if (result.ValueKind != JsonValueKind.Object)
            {
                return 0;
            }
            if (Number(result, "controlState") is { } state)
            {
                return state is 1 or 2 ? (int)state : 0;
            }
            if (Number(result, "res") is { } res)
            {
                return res == 1 ? 0 : res == 2 ? 1 : 2;
            }
            return 0;
        }

        /// <summary>
        /// Sends a remote command (LOCKDOOR, OPENDOOR, OPENAIR, CLOSEAIR, FINDCAR, FLASHLIGHTNOWHISTLE, ...) and waits for the car
        /// </summary>
        public async Task RemoteControlAsync(string vin, string commandType, string pin, string controlParamsMap = null, CancellationToken token = default)
        {
            if (string.IsNullOrWhiteSpace(pin))
            {
                throw new BydApiException("The remote control PIN of the BYD app is required for commands");
            }
            var pinMd5 = BydCrypto.Md5(pin.Trim());
            var result = await TriggerAndPollAsync("/control/remoteControl", "/control/remoteControlResult", serial =>
            {
                var inner = InnerBase(vin, serial);
                inner.Add(new("commandPwd", pinMd5));
                inner.Add(new("commandType", commandType));
                if (controlParamsMap != null)
                {
                    inner.Add(new("controlParamsMap", controlParamsMap));
                }
                return inner;
            }, r => ControlState(r) != 0, token);

            switch (ControlState(result))
            {
                case 1:
                    return;
                case 2:
                    throw new BydApiException($"{commandType} was rejected by the car");
                default:
                    throw new BydApiException($"{commandType}: no answer from the car");
            }
        }

        /// <summary>
        /// controlParamsMap of OPENAIR: immediate start with the temperature on BYD's 1..17 scale (15..31 °C) and the duration code
        /// </summary>
        internal static string ClimateParams(double temperature, int minutes)
        {
            var scale = Math.Clamp((int)Math.Round(temperature - 14.0, MidpointRounding.ToEven), 1, 17);
            var timeSpan = Math.Clamp((int)Math.Round(minutes / 5.0) - 1, 1, 5); // 10 → 1 ... 30 → 5
            return $"{{\"airAccuracy\":1,\"airConditioningMode\":1,\"airSet\":null,\"copilotSettingTemp\":{scale},\"cycleMode\":2,\"mainSettingTemp\":{scale},\"remoteMode\":4,\"timeSpan\":{timeSpan}}}";
        }

        #endregion
    }
}
