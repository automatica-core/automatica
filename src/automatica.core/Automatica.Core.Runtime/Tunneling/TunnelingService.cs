using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.Common;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals.Cloud;
using Automatica.Ngrok;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Automatica.Core.Runtime.Tunneling
{
    internal class TunnelingService : ITunnelingService
    {
        private INgrokService _ngrokService;
        private readonly IConfiguration _config;
        private readonly ICloudApi _cloudApi;
        private readonly ILogger<TunnelingService> _logger;

        public TunnelingService(IConfiguration config, ICloudApi cloudApi, ILogger<TunnelingService> logger)
        {
            _config = config;
            _cloudApi = cloudApi;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await using var context = new AutomaticaContext(_config);
            var ngrokEnabled = context.Settings.SingleOrDefault(a => a.ValueKey == "ngrokEnabled");

            if (ngrokEnabled != null && (bool)ngrokEnabled.Value)
            {
                _logger.LogInformation($"Ngrok is enabled....");
            }
            else
            {
                _logger.LogInformation($"Ngrok is disabled....");
                return;

            }

            var ngrokDomain = context.Settings.SingleOrDefault(a => a.ValueKey == "ngrokDomain");
            var domain = ngrokDomain.ValueText;

            var ngrokToken = context.Settings.SingleOrDefault(a => a.ValueKey == "ngrokToken");

            if (ngrokToken == null || String.IsNullOrEmpty(ngrokToken.ValueText))
            {
                _logger.LogWarning($"Ngrok token is not set!");
                return;
            }

            var token = ngrokToken.ValueText;

            _ngrokService = new NgrokService(_logger, new NgrokOptions
            {
                AuthToken = token,
                Domain = domain,
                ShowNgrokWindow = false

            });
            try
            {

                await _ngrokService.InitializeAsync(cancellationToken);

            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Could not initialize ngrok service ....{e}");
                return;
            }
            _logger.LogInformation($"Bind ngrok to address http://localhost:{ServerInfo.WebPort}");

            try
            {
                var tunnel = await _ngrokService.StartAsync(new Uri($"http://localhost:{ServerInfo.WebPort}"), domain,
                    cancellationToken);
                await _ngrokService.WaitUntilReadyAsync(cancellationToken);
                

                await _cloudApi.SendNgrokTunnelUrl(tunnel.PublicUrl);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Could not bin logger...{e}");
            }

        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_ngrokService != null)
            {
                await _ngrokService.StopAsync(cancellationToken);
            }
        }

        public Task<bool> IsRunning(CancellationToken token)
        {
            return Task.FromResult(_ngrokService != null);
        }

        public async Task<string> CreateTunnelAsync(Uri uri, string domain, CancellationToken token)
        {
            if (_ngrokService == null)
            {
                return null;
            }

            var tunnelResponse = await _ngrokService.CreateTunnelAsync(uri, domain, token);
            return tunnelResponse.PublicUrl;
        }
    }
}
