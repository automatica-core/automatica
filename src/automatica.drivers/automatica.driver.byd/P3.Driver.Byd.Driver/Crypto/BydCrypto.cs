using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace P3.Driver.Byd.Driver.Crypto
{
    /// <summary>
    /// Hashing, signing and the inner AES layer of the BYD app API (port of pyBYD _crypto/hashing.py, signing.py, aes.py)
    /// </summary>
    internal static class BydCrypto
    {
        /// <summary>
        /// MD5 as uppercase hex
        /// </summary>
        public static string Md5(string value) => Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(value ?? "")));

        /// <summary>
        /// AES key of the login request and response: md5(md5(password))
        /// </summary>
        public static string LoginKey(string password) => Md5(Md5(password));

        /// <summary>
        /// SHA1 as hex with even bytes upper and odd bytes lower case, then every "0" on an even position removed
        /// </summary>
        public static string Sha1Mixed(string value)
        {
            var digest = SHA1.HashData(Encoding.UTF8.GetBytes(value));
            var mixed = new StringBuilder(40);
            for (var i = 0; i < digest.Length; i++)
            {
                mixed.Append(digest[i].ToString(i % 2 == 0 ? "X2" : "x2", CultureInfo.InvariantCulture));
            }
            var filtered = new StringBuilder(40);
            for (var j = 0; j < mixed.Length; j++)
            {
                if (mixed[j] == '0' && j % 2 == 0)
                {
                    continue;
                }
                filtered.Append(mixed[j]);
            }
            return filtered.ToString();
        }

        /// <summary>
        /// "k1=v1&amp;k2=v2...&amp;password=..." with the keys sorted ordinally, null as "null".
        /// pyBYD builds the fields as a dict ({**inner, "imeiMD5": ...}), so a key given twice appears once with its last value.
        /// Token requests carry imeiMD5 in the inner payload and the outer fields: listing it twice breaks the signature (BYD answers 1007).
        /// </summary>
        public static string SignString(IEnumerable<KeyValuePair<string, string>> fields, string password)
        {
            var merged = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var (key, value) in fields)
            {
                merged[key] = value;
            }
            var joined = string.Join("&", merged.OrderBy(f => f.Key, StringComparer.Ordinal).Select(f => $"{f.Key}={f.Value ?? "null"}"));
            return $"{joined}&password={password}";
        }

        public static string Sign(IEnumerable<KeyValuePair<string, string>> fields, string password) => Sha1Mixed(SignString(fields, password));

        /// <summary>
        /// md5 of the compact JSON (ensure_ascii=False), the four 8 char chunks reordered 3, 1, 2, 0
        /// </summary>
        public static string CheckCode(IEnumerable<KeyValuePair<string, string>> payload)
        {
            var md5 = Md5(PythonJson(payload, ensureAscii: false)).ToLowerInvariant();
            return md5[24..32] + md5[8..16] + md5[16..24] + md5[..8];
        }

        /// <summary>
        /// json.dumps(dict, separators=(",", ":"), ensure_ascii=...) for a flat dictionary of strings (null allowed)
        /// </summary>
        public static string PythonJson(IEnumerable<KeyValuePair<string, string>> fields, bool ensureAscii)
        {
            var sb = new StringBuilder("{");
            var first = true;
            foreach (var (key, value) in fields)
            {
                if (!first)
                {
                    sb.Append(',');
                }
                first = false;
                AppendString(sb, key, ensureAscii);
                sb.Append(':');
                if (value == null)
                {
                    sb.Append("null");
                }
                else
                {
                    AppendString(sb, value, ensureAscii);
                }
            }
            return sb.Append('}').ToString();
        }

        private static void AppendString(StringBuilder sb, string value, bool ensureAscii)
        {
            sb.Append('"');
            foreach (var c in value)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    default:
                        if (c < 0x20 || (ensureAscii && c > 0x7F))
                        {
                            sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            sb.Append(c);
                        }
                        break;
                }
            }
            sb.Append('"');
        }

        /// <summary>
        /// AES-CBC, zero IV, PKCS#7, uppercase hex; the key is given as hex (md5 → 16 bytes)
        /// </summary>
        public static string AesEncryptHex(string plaintext, string keyHex)
        {
            using var aes = Aes.Create();
            aes.Key = Convert.FromHexString(keyHex);
            return Convert.ToHexString(aes.EncryptCbc(Encoding.UTF8.GetBytes(plaintext), new byte[16], PaddingMode.PKCS7));
        }

        public static string AesDecryptHex(string cipherHex, string keyHex)
        {
            using var aes = Aes.Create();
            aes.Key = Convert.FromHexString(keyHex);
            return Encoding.UTF8.GetString(aes.DecryptCbc(Convert.FromHexString(cipherHex), new byte[16], PaddingMode.PKCS7));
        }
    }
}
