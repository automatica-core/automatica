using Newtonsoft.Json;
using System.Text;
using System;
using System.Security.Cryptography;

namespace Automatica.Driver.Shelly.Gen2.Models
{
    internal class AuthModel
    {
        [JsonProperty("realm")]
        public string Realm { get; set; }

        [JsonProperty("username")]
        public string Username { get; set; }

        [JsonProperty("nonce")]
        public long Nonce { get; set; }

        [JsonProperty("cnonce")]
        public long Cnonce { get; set; }

        [JsonProperty("response")]
        public string Response { get; set; }

        [JsonProperty("algorithm")]
        public string Algorithm { get; set; }

        public static AuthModel CreateAuthModel(string password, string realm, long nonce, long cNonce, string username, string algo)
        {
            var authModel = new AuthModel();
            authModel.Realm = realm;
            authModel.Algorithm = algo;
            authModel.Nonce = nonce;
            authModel.Username = username;
            authModel.Cnonce = cNonce;

            var ha1 = $"{username}:{realm}:{password}";
            var ha2 = "dummy_method:dummy_uri";
            using (SHA256 hash = SHA256.Create())
            {
                var ha1Hashed = HashString(hash, ha1);
                var ha2Hashed = HashString(hash, ha2);
                var unHashedString = $"{ha1Hashed}:{nonce}:1:{cNonce}:auth:{ha2Hashed}";

                authModel.Response = HashString(hash, unHashedString);
            }

            return authModel;
        }
        private static string HashString(SHA256 hasher, string unHashedString)
        {
            Byte[] result = hasher.ComputeHash(Encoding.UTF8.GetBytes(unHashedString));
            StringBuilder Sb = new StringBuilder();

            foreach (Byte b in result)
                Sb.Append(b.ToString("x2"));
            return Sb.ToString();
        }
    }
}
