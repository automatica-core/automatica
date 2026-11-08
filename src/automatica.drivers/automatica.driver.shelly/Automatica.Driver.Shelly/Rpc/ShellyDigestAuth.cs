using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json.Linq;

namespace Automatica.Driver.Shelly.Rpc
{
    /// <summary>
    /// A digest challenge of a Gen2+ device (HTTP: WWW-Authenticate header, websocket: error 401 message).
    /// </summary>
    public sealed record ShellyDigestChallenge(string Realm, string Nonce, string Qop, string Algorithm);

    /// <summary>
    /// Gen2+ authentication: SHA-256 digest, user is always "admin", realm is the device id.
    /// https://shelly-api-docs.shelly.cloud/gen2/General/Authentication
    /// </summary>
    public static class ShellyDigestAuth
    {
        public const string User = "admin";

        public static string Sha256Hex(string value)
        {
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        public static string Ha1(string realm, string password) => Sha256Hex($"{User}:{realm}:{password}");

        public static string NewCnonce() => Convert.ToHexString(RandomNumberGenerator.GetBytes(8)).ToLowerInvariant();

        /// <summary>
        /// Parses "Digest qop="auth", realm="shellyplus1-...", nonce="60dc59c6", algorithm=SHA-256".
        /// </summary>
        public static ShellyDigestChallenge ParseChallenge(string header)
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
            parts.TryGetValue("algorithm", out var algorithm);
            return new ShellyDigestChallenge(realm, nonce, string.IsNullOrEmpty(qop) ? "auth" : qop, string.IsNullOrEmpty(algorithm) ? "SHA-256" : algorithm);
        }

        /// <summary>
        /// The HTTP Authorization header for a request to <paramref name="uri"/>.
        /// </summary>
        public static string CreateAuthorizationHeader(ShellyDigestChallenge challenge, string password, string method, string uri, int nc, string cnonce)
        {
            var ha1 = Ha1(challenge.Realm, password);
            var ha2 = Sha256Hex($"{method}:{uri}");
            var ncText = nc.ToString("x8", CultureInfo.InvariantCulture);
            var response = Sha256Hex($"{ha1}:{challenge.Nonce}:{ncText}:{cnonce}:auth:{ha2}");

            return $"Digest username=\"{User}\", realm=\"{challenge.Realm}\", nonce=\"{challenge.Nonce}\", uri=\"{uri}\", " +
                   $"algorithm=SHA-256, response=\"{response}\", qop=auth, nc={ncText}, cnonce=\"{cnonce}\"";
        }

        /// <summary>
        /// The "auth" object of a JSON-RPC frame (websocket transport).
        /// </summary>
        public static JObject CreateRpcAuth(string realm, long nonce, string password, long cnonce, int nc = 1)
        {
            var ha1 = Ha1(realm, password);
            var ha2 = Sha256Hex("dummy_method:dummy_uri");
            var response = Sha256Hex($"{ha1}:{nonce}:{nc}:{cnonce}:auth:{ha2}");

            return new JObject
            {
                ["realm"] = realm,
                ["username"] = User,
                ["nonce"] = nonce,
                ["cnonce"] = cnonce,
                ["response"] = response,
                ["algorithm"] = "SHA-256"
            };
        }
    }
}
