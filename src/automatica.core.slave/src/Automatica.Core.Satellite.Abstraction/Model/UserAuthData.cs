using System.Security.Cryptography;
using System;
using System.Text;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace Automatica.Core.Satellite.Abstraction.Model
{
    public class UserAuthData
    {
        public string Username { get; set; }

        public string Password { get; set; }

        /// <summary>
        /// One-time token the satellite prints to its log on start while no user exists, required by /webapi/setup
        /// </summary>
        public string SetupToken { get; set; }

        public const string HashPrefix = "pbkdf2-sha512$";
        public const int Iterations = 210_000;
        public const int MinimumPasswordLength = 10;
        private const int LegacyIterations = 10_000;
        private const int HashSize = 32;

        /// <summary>
        /// PBKDF2-SHA512 with 210k iterations, format <c>pbkdf2-sha512$&lt;iterations&gt;$&lt;base64 salt&gt;$&lt;base64 hash&gt;</c>
        /// (the salt is part of the value, the separate "salt" setting is only needed for legacy hashes)
        /// </summary>
        public static string HashPassword(string password)
        {
            var salt = RandomNumberGenerator.GetBytes(16);
            var hash = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password), salt, Iterations, HashAlgorithmName.SHA512, HashSize);
            return $"{HashPrefix}{Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
        }

        /// <summary>
        /// Verifies a password against a stored hash. Legacy hashes (PBKDF2-SHA1, 10k iterations, separate salt) are
        /// still accepted and reported with <paramref name="needsRehash"/>, the caller then stores <see cref="HashPassword(string)"/>.
        /// </summary>
        public static bool VerifyPassword(string password, string storedHash, string legacySalt, out bool needsRehash)
        {
            needsRehash = false;
            if (String.IsNullOrEmpty(password) || String.IsNullOrEmpty(storedHash))
            {
                return false;
            }

            byte[] expected;
            byte[] actual;

            if (storedHash.StartsWith(HashPrefix, StringComparison.Ordinal))
            {
                var parts = storedHash.Split('$');
                if (parts.Length != 4 || !Int32.TryParse(parts[1], out var iterations) || iterations < 1)
                {
                    return false;
                }
                try
                {
                    var salt = Convert.FromBase64String(parts[2]);
                    expected = Convert.FromBase64String(parts[3]);
                    actual = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password), salt, iterations, HashAlgorithmName.SHA512, expected.Length);
                }
                catch (FormatException)
                {
                    return false;
                }

                var ok = CryptographicOperations.FixedTimeEquals(actual, expected);
                needsRehash = ok && iterations < Iterations;
                return ok;
            }

            if (String.IsNullOrEmpty(legacySalt))
            {
                return false;
            }

            try
            {
                expected = Convert.FromBase64String(storedHash);
                actual = KeyDerivation.Pbkdf2(password, Convert.FromBase64String(legacySalt), KeyDerivationPrf.HMACSHA1, LegacyIterations, 256 / 8);
            }
            catch (FormatException)
            {
                return false;
            }

            var legacyOk = CryptographicOperations.FixedTimeEquals(actual, expected);
            needsRehash = legacyOk;
            return legacyOk;
        }

        /// <summary>
        /// Constant time string compare (lengths are hidden by hashing first)
        /// </summary>
        public static bool FixedTimeEquals(string a, string b)
        {
            if (a == null || b == null)
            {
                return false;
            }
            return CryptographicOperations.FixedTimeEquals(SHA256.HashData(Encoding.UTF8.GetBytes(a)), SHA256.HashData(Encoding.UTF8.GetBytes(b)));
        }

        /// <summary>
        /// A random url-safe token with 256 bit
        /// </summary>
        public static string GenerateToken(int bytes = 32)
        {
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(bytes)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }
    }
}
