using System;
using System.Security.Cryptography;
using System.Text;

namespace P3.Logic.Surveillance.AccessController
{
    /// <summary>
    /// A code or tag, stored as PBKDF2-SHA256 hash with its own salt (never in clear text)
    /// </summary>
    public sealed class AccessSecret
    {
        public const int Iterations = 10000;

        public string Salt { get; set; }
        public string Hash { get; set; }

        public static AccessSecret Create(string secret)
        {
            var salt = RandomNumberGenerator.GetBytes(16);
            return new AccessSecret { Salt = Convert.ToBase64String(salt), Hash = Convert.ToBase64String(Derive(secret, salt)) };
        }

        public bool Matches(string secret)
        {
            if (string.IsNullOrEmpty(secret) || string.IsNullOrEmpty(Salt) || string.IsNullOrEmpty(Hash))
            {
                return false;
            }

            try
            {
                return CryptographicOperations.FixedTimeEquals(Derive(secret, Convert.FromBase64String(Salt)), Convert.FromBase64String(Hash));
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private static byte[] Derive(string secret, byte[] salt) =>
            Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(secret), salt, Iterations, HashAlgorithmName.SHA256, 32);
    }
}
