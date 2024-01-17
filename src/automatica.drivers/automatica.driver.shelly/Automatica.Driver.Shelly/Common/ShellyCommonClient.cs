using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Automatica.Driver.Shelly.Common
{
    public class ShellyCommonClient : IShellyCommonClient
    {
        private readonly IShellyAddress _address;
        private readonly IShellyCommonAuthOptions _authOptions;
        protected HttpClient HttpClient { get; }


        public ShellyCommonClient(IShellyAddress address, IShellyCommonAuthOptions authOptions)
        {
            _address = address;
            _authOptions = authOptions;

            HttpClient = new HttpClient();
            HttpClient.BaseAddress = new Uri($"http://{_address.IpAddress}");
        }

        public async Task<ShellyInfoDto> GetInfo(CancellationToken token = default)
        {
            var shellyInfo = await HttpClient.GetAsync("/shelly", token);

            shellyInfo.EnsureSuccessStatusCode();

            var json = await shellyInfo.Content.ReadAsStringAsync(token);
            return JsonConvert.DeserializeObject<ShellyInfoDto>(json);
        }
    }
}
