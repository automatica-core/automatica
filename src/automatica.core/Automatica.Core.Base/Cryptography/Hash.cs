using System.Security.Cryptography;
using System.Text;

namespace Automatica.Core.Base.Cryptography
{
    /// <summary>
    /// Provides static functions for hashing
    /// </summary>
    public static class Hash
    {
        /// <summary>
        /// Calculates a Hmac Sha1 hash
        /// </summary>
        /// <param name="text">Cipher</param>
        /// <param name="key">Key</param>
        /// <returns>Hmac Sha1 hash</returns>
        public static string CalculateHmacSha1(this string text, byte[] key)
        {
            byte[] byteArray = Encoding.UTF8.GetBytes(text);
            using (var myhmacsha1 = new HMACSHA1(key))
            {
                myhmacsha1.Initialize();

                var hashArray = myhmacsha1.ComputeHash(byteArray);
                
                return hashArray.ToHex(false);
            }
            
        }

        /// <summary>
        /// Calculates a sha1
        /// </summary>
        /// <param name="text">Text</param>
        /// <returns>Sha1 hash</returns>
        public static string CalculateSha1(this string text)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(text);

            var sha1 = SHA1.Create("sha1");

            var hash = sha1.ComputeHash(buffer);

            return hash.ToHex(true);
        }

        /// <summary>
        /// Converts a byte array to a hex string
        /// </summary>
        /// <param name="bytes">data</param>
        /// <param name="upperCase">if true the string will be uppercase</param>
        /// <returns>A hex string</returns>
        public static string ToHex(this byte[] bytes, bool upperCase)
        {
            StringBuilder result = new StringBuilder(bytes.Length * 2);

            for (int i = 0; i < bytes.Length; i++)
                result.Append(bytes[i].ToString(upperCase ? "X2" : "x2"));

            return result.ToString();
        }


    }
}
