using System;
using System.Security.Cryptography;
using System.Text;

namespace Automatica.Core.Runtime.Core
{
    /// <summary>
    /// Hashing of the satellite broker key (<c>Slave.ClientKey</c>). The hash is stored in the existing column with a
    /// format prefix, so plaintext keys from older installations are still recognized and are replaced by the hash on
    /// the next successful CONNECT of the satellite.
    /// Format: <c>pbkdf2-sha512$&lt;iterations&gt;$&lt;base64 salt&gt;$&lt;base64 hash&gt;</c>
    /// </summary>
    public static class SatelliteClientKey
    {
        public const string Prefix = "pbkdf2-sha512$";
        public const int Iterations = 210_000;
        private const int SaltSize = 16;
        private const int HashSize = 32;

        /// <summary>
        /// Minimum length of a key typed by the admin, the generated keys are much longer
        /// </summary>
        public const int MinimumKeyLength = 16;

        public static bool IsHashed(string stored)
        {
            return stored != null && stored.StartsWith(Prefix, StringComparison.Ordinal);
        }

        public static string Hash(string key)
        {
            if (String.IsNullOrEmpty(key))
            {
                throw new ArgumentException("key must not be empty", nameof(key));
            }

            var salt = RandomNumberGenerator.GetBytes(SaltSize);
            var hash = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(key), salt, Iterations, HashAlgorithmName.SHA512, HashSize);
            return $"{Prefix}{Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
        }

        /// <summary>
        /// Verifies <paramref name="key"/> against the stored value. Plaintext values (no prefix) are compared in
        /// constant time and reported with <paramref name="needsRehash"/>, the caller should then store <see cref="Hash"/>.
        /// </summary>
        public static bool Verify(string key, string stored, out bool needsRehash)
        {
            needsRehash = false;

            if (String.IsNullOrEmpty(key) || String.IsNullOrEmpty(stored))
            {
                return false;
            }

            if (!IsHashed(stored))
            {
                var equal = FixedTimeEquals(key, stored);
                needsRehash = equal;
                return equal;
            }

            var parts = stored.Split('$');
            if (parts.Length != 4 || !Int32.TryParse(parts[1], out var iterations) || iterations < 1)
            {
                return false;
            }

            byte[] salt, expected;
            try
            {
                salt = Convert.FromBase64String(parts[2]);
                expected = Convert.FromBase64String(parts[3]);
            }
            catch (FormatException)
            {
                return false;
            }

            var actual = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(key), salt, iterations, HashAlgorithmName.SHA512, expected.Length);
            var valid = CryptographicOperations.FixedTimeEquals(actual, expected);
            needsRehash = valid && iterations < Iterations;
            return valid;
        }

        /// <summary>
        /// A random key (256 bit, base64url) for a new satellite
        /// </summary>
        public static string Generate()
        {
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
                .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        internal static bool FixedTimeEquals(string a, string b)
        {
            // hash both sides first, so neither the content nor the length leaks through the timing
            var ha = SHA256.HashData(Encoding.UTF8.GetBytes(a ?? ""));
            var hb = SHA256.HashData(Encoding.UTF8.GetBytes(b ?? ""));
            return CryptographicOperations.FixedTimeEquals(ha, hb) && a != null && b != null;
        }
    }
}
