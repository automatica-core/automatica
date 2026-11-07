using System;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;

namespace Automatica.Core.Plugin.Standalone
{
    /// <summary>
    /// The core's broker certificate is self-signed, it is pinned by its SHA-256 fingerprint
    /// </summary>
    internal static class BrokerCertificatePin
    {
        public static string Normalize(string fingerprint)
        {
            if (String.IsNullOrWhiteSpace(fingerprint))
            {
                return null;
            }
            var hex = new string(fingerprint.Where(Uri.IsHexDigit).ToArray()).ToLowerInvariant();
            return hex.Length == 64 ? hex : null;
        }

        public static string Fingerprint(byte[] rawData)
        {
            using (var sha = SHA256.Create())
            {
                return BitConverter.ToString(sha.ComputeHash(rawData)).Replace("-", "").ToLowerInvariant();
            }
        }

        public static bool Matches(X509Certificate certificate, X509Chain chain, string pinned, ILogger logger)
        {
            if (certificate == null || pinned == null)
            {
                return false;
            }

            if (FixedTimeEquals(Fingerprint(certificate.GetRawCertData()), pinned))
            {
                return true;
            }

            if (chain != null && chain.ChainElements.Cast<X509ChainElement>().Any(e => FixedTimeEquals(Fingerprint(e.Certificate.RawData), pinned)))
            {
                return true;
            }

            logger.LogError($"Broker certificate does not match the pinned fingerprint {pinned}, not connecting");
            return false;
        }

        private static bool FixedTimeEquals(string a, string b)
        {
            if (a.Length != b.Length)
            {
                return false;
            }
            var diff = 0;
            for (var i = 0; i < a.Length; i++)
            {
                diff |= a[i] ^ b[i];
            }
            return diff == 0;
        }
    }
}
