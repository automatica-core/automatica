using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.Common;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals.Cloud;
using Automatica.Core.Internals.License;
using Automatica.Core.Runtime.RemoteConnect.Frp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Automatica.Core.Runtime.RemoteConnect
{
    internal class RemoteConnectService : IRemoteConnectService
    {
        private readonly IConfiguration _config;
        private readonly ICloudApi _cloudApi;
        private readonly ILicenseContext _licenseContext;
        private readonly IFrpService _frpService;
        private readonly ILogger<RemoteConnectService> _logger;

        public RemoteConnectService(IConfiguration config, ICloudApi cloudApi, ILicenseContext licenseContext, IFrpService frpService, ILogger<RemoteConnectService> logger)
        {
            _config = config;
            _cloudApi = cloudApi;
            _licenseContext = licenseContext;
            _frpService = frpService;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {                                                                                                                                                                                                                                           
            if (!_licenseContext.AllowRemoteControl)
            {
                _logger.LogInformation($"Remote control is not licensed...:");
                return;
            }

            await using var context = new AutomaticaContext(_config);
            var remoteEnabled = context.Settings.SingleOrDefault(a => a.ValueKey == "remoteEnabled");

            if (remoteEnabled != null && (bool)remoteEnabled.Value)
            {
                _logger.LogInformation($"RemoteControl is enabled....");
            }
            else
            {
                _logger.LogInformation($"RemoteControl is disabled....");
                return;

            }

            var remoteDomain = context.Settings.SingleOrDefault(a => a.ValueKey == "remoteDomain");
            var domain = remoteDomain.ValueText;

            var response = await _cloudApi.CreateRemoteConnectUrl(domain);

            if (response == null)
            {
                _logger.LogError($"Could not create target domain {domain}");
                return;
            }
            
            try
            {
                await _frpService.InitConfigurationsAsync(cancellationToken);
                await _frpService.StartAsync(cancellationToken);

            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Could not initialize RemoteControl service ....{e}");
                return;
            }
            _logger.LogInformation($"Bind RemoteControl to address http://localhost:{ServerInfo.WebPort}");

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

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await _frpService.StopAsync(cancellationToken);
            
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
