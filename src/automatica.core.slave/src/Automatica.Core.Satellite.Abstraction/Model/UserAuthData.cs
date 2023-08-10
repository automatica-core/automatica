using System.Security.Cryptography;
using System;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace Automatica.Core.Satellite.Abstraction.Model
{
    public class UserAuthData
    {
        public string Username { get; set; }

        public string Password { get; set; }

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
