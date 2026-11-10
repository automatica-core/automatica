using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace P3.Driver.Refoss.Driver
{
    internal sealed class RefossRpcException : Exception
    {
        public int Code { get; }

        public RefossRpcException(int code, string message) : base(message)
        {
            Code = code;
        }
    }

    internal sealed record RefossDigestChallenge(string Realm, string Nonce, string Qop);

    /// <summary>
    /// Local JSON-RPC API of Refoss devices over HTTP GET: http://&lt;ip&gt;/rpc/&lt;Method&gt;?&lt;params&gt;.
    /// With authentication switched on (Refoss.Auth.Set) the device answers 401 and the request is repeated with an
    /// SHA-256 digest: user "admin", realm = device id (the same scheme as Shelly Gen2).
    /// </summary>
    internal sealed class RefossRpcClient
    {
        public const string User = "admin";

        private static readonly JsonDocumentOptions ParseOptions = new()
        {
            // the examples of the Refoss documentation contain trailing commas
            AllowTrailingCommas = true,
            CommentHandling = JsonCommentHandling.Skip
        };

        private readonly HttpClient _http;
        private readonly SemaphoreSlim _authLock = new(1, 1);
        private RefossDigestChallenge _challenge;
        private string _cnonce = NewCnonce();
        private int _nc;

        public RefossRpcClient(HttpClient http, string host, string password)
        {
            _http = http;
            BaseUrl = NormalizeUrl(host);
            Password = password ?? "";
        }

        public string BaseUrl { get; }

        public string Password { get; }

        /// <summary>
        /// "192.168.1.60" → "http://192.168.1.60"
        /// </summary>
        internal static string NormalizeUrl(string value)
        {
            var url = (value ?? "").Trim().TrimEnd('/');
            if (url.Length == 0)
            {
                return url;
            }
            return url.Contains("://", StringComparison.Ordinal) ? url : "http://" + url;
        }

        /// <summary>
        /// Calls a method and returns its result (the "result" member when the device wraps it, otherwise the whole body).
        /// </summary>
        public async Task<JsonElement> CallAsync(string method, IReadOnlyDictionary<string, object> parameters = null, CancellationToken token = default)
        {
            if (string.IsNullOrEmpty(BaseUrl))
            {
                throw new RefossRpcException(0, "No address of the Refoss device configured");
            }

            var pathAndQuery = "/rpc/" + method + Query(parameters);
            // a cached nonce may have expired: one retry with a fresh challenge, a second refusal means a wrong password
            var refused = 0;
            for (var attempt = 0; attempt < 3; attempt++)
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, BaseUrl + pathAndQuery);
                var (header, nonce) = await AuthorizationAsync(pathAndQuery);
                if (header != null)
                {
                    request.Headers.TryAddWithoutValidation("Authorization", header);
                }

                using var response = await _http.SendAsync(request, token);
                var body = await response.Content.ReadAsStringAsync(token);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    if (string.IsNullOrEmpty(Password))
                    {
                        throw new RefossRpcException(401, $"{BaseUrl} requires authentication, set the password of the device");
                    }
                    var challenge = ParseChallenge(response.Headers.WwwAuthenticate.ToString()) ?? ParseBodyChallenge(body);
                    if (header != null)
                    {
                        refused++;
                    }
                    if (challenge == null || refused >= 2 || header != null && challenge.Nonce == nonce)
                    {
                        throw new RefossRpcException(401, $"{BaseUrl} rejected the password");
                    }
                    await SetChallengeAsync(challenge);
                    continue;
                }

                if (!response.IsSuccessStatusCode && string.IsNullOrWhiteSpace(body))
                {
                    throw new RefossRpcException((int)response.StatusCode, $"{method}: HTTP {(int)response.StatusCode}");
                }

                return Unwrap(method, body);
            }

            throw new RefossRpcException(401, $"{BaseUrl} rejected the password");
        }

        internal static JsonElement Unwrap(string method, string body)
        {
            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(body) ? "{}" : body, ParseOptions);
            var root = doc.RootElement;
            if (root.ValueKind == JsonValueKind.Object)
            {
                if (root.TryGetProperty("error", out var error) && error.ValueKind == JsonValueKind.Object)
                {
                    var code = error.TryGetProperty("code", out var c) && c.ValueKind == JsonValueKind.Number ? c.GetInt32() : 0;
                    var message = error.TryGetProperty("message", out var m) ? m.ToString() : error.GetRawText();
                    throw new RefossRpcException(code, $"{method}: {message} ({code})");
                }
                if (root.TryGetProperty("result", out var result))
                {
                    return result.Clone();
                }
            }
            return root.Clone();
        }

        private async Task<(string Header, string Nonce)> AuthorizationAsync(string uri)
        {
            await _authLock.WaitAsync();
            try
            {
                if (_challenge == null)
                {
                    return (null, null);
                }
                _nc++;
                return (CreateAuthorizationHeader(_challenge, Password, "GET", uri, _nc, _cnonce), _challenge.Nonce);
            }
            finally
            {
                _authLock.Release();
            }
        }

        private async Task SetChallengeAsync(RefossDigestChallenge challenge)
        {
            await _authLock.WaitAsync();
            try
            {
                _challenge = challenge;
                _cnonce = NewCnonce();
                _nc = 0;
            }
            finally
            {
                _authLock.Release();
            }
        }

        internal static string Query(IReadOnlyDictionary<string, object> parameters)
        {
            if (parameters == null || parameters.Count == 0)
            {
                return "";
            }
            return "?" + string.Join("&", parameters.Select(p => Uri.EscapeDataString(p.Key) + "=" + Uri.EscapeDataString(Format(p.Value))));
        }

        private static string Format(object value) => value switch
        {
            null => "null",
            bool b => b ? "true" : "false",
            IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString()
        };

        internal static string Sha256Hex(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

        private static string NewCnonce() => Convert.ToHexString(RandomNumberGenerator.GetBytes(8)).ToLowerInvariant();

        /// <summary>
        /// Parses 'Digest qop="auth", realm="refoss-em16p-...", nonce="60dc59c6", algorithm=SHA-256'
        /// </summary>
        internal static RefossDigestChallenge ParseChallenge(string header)
        {
            if (string.IsNullOrWhiteSpace(header))
            {
                return null;
            }
            var value = header.Trim();
            if (value.StartsWith("Digest", StringComparison.OrdinalIgnoreCase))
            {
                value = value.Substring(6);
            }

            var parts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var i = 0;
            while (i < value.Length)
            {
                while (i < value.Length && (value[i] == ' ' || value[i] == ','))
                {
                    i++;
                }
                var eq = value.IndexOf('=', i);
                if (eq < 0)
                {
                    break;
                }
                var key = value.Substring(i, eq - i).Trim();
                i = eq + 1;
                string v;
                if (i < value.Length && value[i] == '"')
                {
                    var end = value.IndexOf('"', i + 1);
                    if (end < 0)
                    {
                        end = value.Length;
                    }
                    v = value.Substring(i + 1, end - i - 1);
                    i = end + 1;
                }
                else
                {
                    var end = value.IndexOf(',', i);
                    if (end < 0)
                    {
                        end = value.Length;
                    }
                    v = value.Substring(i, end - i).Trim();
                    i = end;
                }
                parts[key] = v;
            }

            if (!parts.TryGetValue("realm", out var realm) || !parts.TryGetValue("nonce", out var nonce))
            {
                return null;
            }
            parts.TryGetValue("qop", out var qop);
            return new RefossDigestChallenge(realm, nonce, string.IsNullOrEmpty(qop) ? "auth" : qop);
        }

        /// <summary>
        /// Some firmware answers 401 with the challenge in the JSON-RPC error message instead of the header:
        /// {"error":{"code":401,"message":"{\"auth_type\":\"digest\",\"nonce\":123,\"nc\":1,\"realm\":\"refoss-...\",\"algorithm\":\"SHA-256\"}"}}
        /// </summary>
        internal static RefossDigestChallenge ParseBodyChallenge(string body)
        {
            try
            {
                using var doc = JsonDocument.Parse(body, ParseOptions);
                var root = doc.RootElement;
                if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("error", out var error) && error.ValueKind == JsonValueKind.Object)
                {
                    root = error;
                }
                if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("message", out var message) && message.ValueKind == JsonValueKind.String)
                {
                    using var inner = JsonDocument.Parse(message.GetString()!, ParseOptions);
                    return FromObject(inner.RootElement);
                }
                return FromObject(root);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static RefossDigestChallenge FromObject(JsonElement e)
        {
            if (e.ValueKind != JsonValueKind.Object || !e.TryGetProperty("realm", out var realm) || !e.TryGetProperty("nonce", out var nonce))
            {
                return null;
            }
            var nonceText = nonce.ValueKind == JsonValueKind.String ? nonce.GetString() : nonce.GetRawText();
            return new RefossDigestChallenge(realm.GetString(), nonceText, "auth");
        }

        /// <summary>
        /// The HTTP Authorization header for a request to <paramref name="uri"/> (path and query).
        /// </summary>
        internal static string CreateAuthorizationHeader(RefossDigestChallenge challenge, string password, string method, string uri, int nc, string cnonce)
        {
            var ha1 = Sha256Hex($"{User}:{challenge.Realm}:{password}");
            var ha2 = Sha256Hex($"{method}:{uri}");
            var ncText = nc.ToString("x8", CultureInfo.InvariantCulture);
            var response = Sha256Hex($"{ha1}:{challenge.Nonce}:{ncText}:{cnonce}:auth:{ha2}");

            return $"Digest username=\"{User}\", realm=\"{challenge.Realm}\", nonce=\"{challenge.Nonce}\", uri=\"{uri}\", " +
                   $"algorithm=SHA-256, response=\"{response}\", qop=auth, nc={ncText}, cnonce=\"{cnonce}\"";
        }
    }
}
