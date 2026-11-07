using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace P3.Driver.Nuki.Driver
{
    /// <summary>
    /// Nuki bridge HTTP API authentication. The hashed token (bridge firmware 1.13+) never sends the token itself:
    /// hash = SHA256("ts,rnr,token") with a UTC timestamp and a random number the bridge accepts only once.
    /// </summary>
    internal static class NukiTokenAuth
    {
        public static string CreateQuery(string token, bool hashed)
        {
            return hashed
                ? CreateHashedQuery(token, DateTime.UtcNow, RandomNumberGenerator.GetInt32(0, 65536))
                : $"token={Uri.EscapeDataString(token ?? string.Empty)}";
        }

        internal static string CreateHashedQuery(string token, DateTime utcNow, int randomNumber)
        {
            var ts = utcNow.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);
            var rnr = randomNumber.ToString(CultureInfo.InvariantCulture);

            var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"{ts},{rnr},{token}"));
            var hashHex = Convert.ToHexString(hash).ToLowerInvariant();

            return $"ts={Uri.EscapeDataString(ts)}&rnr={rnr}&hash={hashHex}";
        }
    }
}
