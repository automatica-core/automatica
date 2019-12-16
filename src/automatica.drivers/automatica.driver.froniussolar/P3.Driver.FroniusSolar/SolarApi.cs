using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using P3.Driver.FroniusSolar.Model.Realtime;

namespace P3.Driver.FroniusSolar
{
    public sealed class SolarApi : ISolarApi
    {
        public const string SolarApiUri = "/solar_api/v1";

        private readonly Uri _inverterIp;
        private readonly HttpClient _client = new HttpClient();

        public SolarApi(string inverterIp)
        {
            _inverterIp = new Uri($"http://{inverterIp}");
        }

        public async Task<T> GetRequest<T>(string apiUrl) where T : class
        {
            T result = null;
            try
            {
                using var response = await _client.GetAsync(new Uri(_inverterIp, $"{SolarApiUri}/{apiUrl}"))
                    .ConfigureAwait(false);

                response.EnsureSuccessStatusCode();

                await response.Content.ReadAsStringAsync().ContinueWith(x =>
                {
                    if (x.IsFaulted)
                        throw x.Exception;

                    result = JsonConvert.DeserializeObject<T>(x.Result);
                });

            }
            catch (Exception e)
            {
                throw;
            }

            return result;
        }

        public async Task<InverterInfo> GetInverterInfo()
        {
            return await GetRequest<InverterInfo>("GetInverterInfo.cgi");
        }

        public void Dispose()
        {
            _client?.Dispose();
        }
    }
}
