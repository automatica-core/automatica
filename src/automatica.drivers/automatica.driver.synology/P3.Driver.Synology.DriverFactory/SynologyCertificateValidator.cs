using System;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("P3.Driver.Synology.Tests")]

namespace P3.Driver.Synology.DriverFactory
{
    internal static class SynologyCertificateValidator
    {
        /// <summary>
        /// A pinned thumbprint (SHA-1 or SHA-256 hex, separators allowed) must match, also for self-signed certificates.
        /// Without a pin, the certificate must be valid unless errors are ignored explicitly.
        /// </summary>
        public static bool Validate(X509Certificate2 certificate, SslPolicyErrors sslPolicyErrors, bool ignoreSslErrors, string pinnedThumbprint)
        {
            var pin = Normalize(pinnedThumbprint);

            if (!string.IsNullOrEmpty(pin))
            {
                if (certificate == null)
                {
                    return false;
                }

                var sha1 = certificate.GetCertHashString(HashAlgorithmName.SHA1);
                var sha256 = certificate.GetCertHashString(HashAlgorithmName.SHA256);

                return FixedTimeEquals(pin, sha1) || FixedTimeEquals(pin, sha256);
            }

            return sslPolicyErrors == SslPolicyErrors.None || ignoreSslErrors;
        }

        internal static string Normalize(string thumbprint)
        {
            if (string.IsNullOrWhiteSpace(thumbprint))
            {
                return null;
            }

            var chars = new System.Text.StringBuilder(thumbprint.Length);
            foreach (var c in thumbprint)
            {
                if (Uri.IsHexDigit(c))
                {
                    chars.Append(char.ToUpperInvariant(c));
                }
            }

            return chars.ToString();
        }

        private static bool FixedTimeEquals(string a, string b)
        {
            if (a == null || b == null || a.Length != b.Length)
            {
                return false;
            }

            return CryptographicOperations.FixedTimeEquals(System.Text.Encoding.ASCII.GetBytes(a), System.Text.Encoding.ASCII.GetBytes(b.ToUpperInvariant()));
        }
    }
}
