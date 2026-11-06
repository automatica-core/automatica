using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace Automatica.Core.Internals.UserHelper
{
    public static class UserHelper
    {
        /// <summary>
        /// Prefix of the current password hash format: <c>pbkdf2-sha512$&lt;iterations&gt;$&lt;base64 hash&gt;</c> (the salt is stored separately).
        /// Hashes without a prefix are the legacy format (PBKDF2-SHA1, 10000 iterations, see <see cref="HashPassword"/>).
        /// </summary>
        public const string HashPrefix = "pbkdf2-sha512$";

        /// <summary>
        /// OWASP recommendation for PBKDF2-HMAC-SHA512. Stored in every hash, so it can be raised later
        /// (existing hashes are upgraded on the next login).
        /// </summary>
        public const int Iterations = 210000;

        private const int HashSize = 256 / 8;

        /// <summary>
        /// Legacy hash (PBKDF2-SHA1, 10000 iterations): only used to verify passwords that were stored before the
        /// current format, new passwords are hashed with <see cref="CreatePasswordHash"/>.
        /// </summary>
        public static string HashPassword(string password, string saltString)
        {
            byte[] salt = Convert.FromBase64String(saltString);

            string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password,
                salt,
                KeyDerivationPrf.HMACSHA1,
                10000,
                256 / 8));
            return hashed;
        }

        /// <returns>the password hash in the current format</returns>
        public static string CreatePasswordHash(string password, string saltString)
        {
            return CreatePasswordHash(password, saltString, Iterations);
        }

        private static string CreatePasswordHash(string password, string saltString, int iterations)
        {
            var hash = KeyDerivation.Pbkdf2(password, Convert.FromBase64String(saltString), KeyDerivationPrf.HMACSHA512,
                iterations, HashSize);
            return $"{HashPrefix}{iterations.ToString(CultureInfo.InvariantCulture)}${Convert.ToBase64String(hash)}";
        }

        /// <summary>
        /// Verifies a password against a stored hash in the current or in the legacy format.
        /// </summary>
        /// <param name="needsRehash">true if the password is correct but the hash should be replaced by <see cref="CreatePasswordHash"/></param>
        public static bool VerifyPassword(string password, string saltString, string storedHash, out bool needsRehash)
        {
            needsRehash = false;
            if (password == null || string.IsNullOrEmpty(saltString) || string.IsNullOrEmpty(storedHash))
            {
                return false;
            }

            string computed;
            if (storedHash.StartsWith(HashPrefix, StringComparison.Ordinal))
            {
                var parts = storedHash.Substring(HashPrefix.Length).Split('$');
                if (parts.Length != 2 || !int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var iterations) || iterations <= 0)
                {
                    return false;
                }

                computed = CreatePasswordHash(password, saltString, iterations);
                needsRehash = iterations < Iterations;
            }
            else
            {
                computed = HashPassword(password, saltString);
                needsRehash = true;
            }

            var ok = CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(computed), Encoding.UTF8.GetBytes(storedHash));
            if (!ok)
            {
                needsRehash = false;
            }

            return ok;
        }

        public static string GenerateNewSalt()
        {
            byte[] salt = new byte[128 / 8];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            return Convert.ToBase64String(salt);
        }
    }
}
