using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Automatica.Core.Runtime.RemoteConnect.Frp
{
    internal class FrpcApiClient : IFrpcApiClient
    {
        private readonly IOptionsMonitor<FrpcOptions> _options;
        private readonly ILogger<FrpcApiClient> _logger;

        private readonly HttpClient _httpClient = new HttpClient();

        public FrpcApiClient(IOptionsMonitor<FrpcOptions> options, FrpcCredentials credentials, ILogger<FrpcApiClient> logger)
        {
            _options = options;
            _logger = logger;

            _httpClient.Timeout = TimeSpan.FromMilliseconds(300);
            // the admin api of frpc requires the credentials of this start (see frpc.ini)
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic",
                Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{credentials.AdminUser}:{credentials.AdminPassword}")));
        }
        public async Task<bool> IsReady(CancellationToken cancellationToken = default)
        {
            
            try
            {
                var response = await _httpClient.GetAsync($"http://127.0.0.1:{_options.CurrentValue.AdminPort}/api/status", cancellationToken);

                var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogDebug($"frpc response is {responseText}");
                response.EnsureSuccessStatusCode();
            }
            catch
            {
                return false;
            }

            return true;
        }
    }
}
