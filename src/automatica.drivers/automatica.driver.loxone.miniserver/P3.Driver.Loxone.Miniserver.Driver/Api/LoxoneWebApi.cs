using Automatica.Core.Base.Cryptography;
using Newtonsoft.Json;
using P3.Driver.Loxone.Miniserver.Driver.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace P3.Driver.Loxone.Miniserver.Driver.Api
{
    public class LoxoneWebApi
    {
        private readonly string _password;

        public string BaseAddress { get; }
        public string User { get; }

        public LoxoneWebApi(string baseAddress, string user, string password)
        {
            BaseAddress = baseAddress;
            User = user;
            _password = password;
        }
        private HttpClient SetupClient()
        {
            // Three versions in one.
            HttpClient client = new HttpClient();

            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(
            Encoding.ASCII.GetBytes($"{User}:{_password}")));
            

            return client;
        }

        public async Task<LoxoneApiResponse<T>> GetRequest<T>(string apiUrl) where T : LoxoneApiResponseLL
        {
            LoxoneApiResponse<T> result = null;
            using (var client = SetupClient())
            {
                var response = await client.GetAsync(new Uri(new Uri(BaseAddress), apiUrl)).ConfigureAwait(false);

                response.EnsureSuccessStatusCode();

                await response.Content.ReadAsStringAsync().ContinueWith(x =>
                {
                    if (x.IsFaulted)
                        throw x.Exception;

                    result = JsonConvert.DeserializeObject<LoxoneApiResponse<T>>(x.Result);
                });
            }

            return result;
        }

        public async Task<T> GetBasicAuthRequest<T>(string apiUrl) where T : class
        {
            T result = null;
            using (var client = SetupClient())
            {
                var response = await client.GetAsync(new Uri(new Uri(BaseAddress), apiUrl)).ConfigureAwait(false);

                response.EnsureSuccessStatusCode();

                await response.Content.ReadAsStringAsync().ContinueWith(x =>
                {
                    if (x.IsFaulted)
                        throw x.Exception;

                    result = JsonConvert.DeserializeObject<T>(x.Result);
                });
            }

            return result;
        }
        public async Task<object> GetRequest(string apiUrl)
        {
            object result = null;
            using (var client = SetupClient())
            {
                var response = await client.GetAsync(new Uri(new Uri(BaseAddress), apiUrl)).ConfigureAwait(false);

                response.EnsureSuccessStatusCode();

                await response.Content.ReadAsStringAsync().ContinueWith(x =>
                {
                    if (x.IsFaulted)
                        throw x.Exception;

                    result = JsonConvert.DeserializeObject(x.Result);
                });
            }

            return result;
        }

        public void EncryptRequest(string url, string pubKey)
        {
            var salt = Guid.NewGuid().ToByteArray();
            var salt2Byte = new byte[2];

            Array.Copy(salt, salt2Byte, 2);

            var saltString = salt2Byte.ToHex(true);

            var payload = $"salt/{saltString}/{url}";
            var payloadByte = Encoding.UTF8.GetBytes(payload);

            var aesKey = GetRandomData(256);
            var aesIv = GetRandomData(128);

            byte[] result;
            using (var aes = Aes.Create())
            {
                aes.Key = aesKey;
                aes.IV = aesIv;

                using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
                using (var resultStream = new MemoryStream())
                {
                    using (var aesStream = new CryptoStream(resultStream, encryptor, CryptoStreamMode.Write))
                    using (var plainStream = new MemoryStream(payloadByte))
                    {
                        plainStream.CopyTo(aesStream);
                    }

                    result = resultStream.ToArray();
                }
            }

            
            var cert = new X509Certificate2(Encoding.UTF8.GetBytes(pubKey));
            string aesData = aesKey.ToHex(true) + ":" + aesIv.ToHex(true);
            using (RSA rsa = cert.GetRSAPublicKey())
            {
                rsa.Encrypt(Encoding.UTF8.GetBytes(aesData), RSAEncryptionPadding.OaepSHA1);
            }

        }

        private static byte[] GetRandomData(int bits)
        {
            var result = new byte[bits / 8];
            RandomNumberGenerator.Create().GetBytes(result);
            return result;
        }
    }
}
