using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.Common;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals.Cloud;
using Automatica.Core.Internals.License;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Automatica.Core.Runtime.RemoteConnect
{
    internal class RemoteConnectService : IRemoteConnectService
    {
        private readonly IConfiguration _config;
        private readonly ICloudApi _cloudApi;
        private readonly ILicenseContext _licenseContext;
        private readonly ILogger<RemoteConnectService> _logger;

        public RemoteConnectService(IConfiguration config, ICloudApi cloudApi, ILicenseContext licenseContext, ILogger<RemoteConnectService> logger)
        {
            _config = config;
            _cloudApi = cloudApi;
            _licenseContext = licenseContext;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await using var context = new AutomaticaContext(_config);
            var remoteEnabled = context.Settings.SingleOrDefault(a => a.ValueKey == "remoteEnabled");

            if (remoteEnabled != null && (bool)remoteEnabled.Value)
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

            
            try
            {

               // await _ngrokService.InitializeAsync(cancellationToken);

            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Could not initialize ngrok service ....{e}");
                return;
            }
            _logger.LogInformation($"Bind ngrok to address http://localhost:{ServerInfo.WebPort}");

            try
            {
                //var tunnel = await _ngrokService.StartAsync(new Uri($"http://localhost:{ServerInfo.WebPort}"), domain,
                //    cancellationToken);
                //await _ngrokService.WaitUntilReadyAsync(cancellationToken);
                

               // await _cloudApi.SendRemoteConnectUrl(tunnel.PublicUrl);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Could not bin logger...{e}");
            }

        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task<bool> IsRunning(CancellationToken token)
        {
            return Task.FromResult(false);
        }

        public Task<string> CreateTunnelAsync(Uri uri, string domain, CancellationToken token)
        {
            return Task.FromResult("");
        }
    }
}
