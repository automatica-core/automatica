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

        public FrpcApiClient(IOptionsMonitor<FrpcOptions> options, ILogger<FrpcApiClient> logger)
        {
            _options = options;
            _logger = logger;

            _httpClient.Timeout = TimeSpan.FromMilliseconds(300);
        }
        public async Task<bool> IsReady(CancellationToken cancellationToken = default)
        {
            
            try
            {
                var response = await _httpClient.GetAsync($"http://127.0.0.1:{_options.CurrentValue.AdminPort}/api/status", cancellationToken);

                var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogInformation($"frpc response is {responseText}");
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
