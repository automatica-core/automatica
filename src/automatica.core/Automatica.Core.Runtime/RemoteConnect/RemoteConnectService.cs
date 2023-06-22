using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.Common;
using Automatica.Core.Base.Tunneling;
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

        private string _currentDomainName;

        private bool _isRunning;

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
                _logger.LogInformation("RemoteControl is enabled....");
            }
            else
            {
                _logger.LogInformation("RemoteControl is disabled....");
                return;
            }

            if (String.IsNullOrEmpty(_currentDomainName))
            {
                _logger.LogError("Cannot start RemoteControl, domain is not set....");
            }

            try
            {
                await _frpService.StartAsync(cancellationToken);
                
                _isRunning = true;

            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Could not initialize RemoteControl service ....{e}");
                return;
            }
            _logger.LogInformation($"Bind RemoteControl to address http://localhost:{ServerInfo.WebPort}");
        }

        public async Task<string> CreateTunnelAsync(TunnelingProtocol protocol, string name, string localIp, int localPort, Guid driverGuid,
            CancellationToken token)
        {
            if (_isRunning)
            {
                throw new ArgumentException($"Cannot establish a new tunnel while already running...");
            }

            var remotePortResponse = await _cloudApi.GetRemoteConnectPort(driverGuid, name, protocol);
            if (remotePortResponse == null)
            {
                throw new ArgumentException($"Could not get remote port for tunnel {name}...");
            }
            var remotePort = remotePortResponse.Port;

            await FrpcHelper.CreateServiceFileFromTemplate(protocol, name, localIp, localPort, remotePort, token);

            return _currentDomainName;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await _frpService.StopAsync(cancellationToken);
        }

        public async Task InitAsync()
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
                await _frpService.InitConfigurationsAsync();

                _currentDomainName = response.TunnelUrl;
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Could not initialize RemoteControl service ....{e}");
                return;
            }
        }

        public Task<bool> IsRunning(CancellationToken token)
        {
            return Task.FromResult(false);
        }
    }
}
