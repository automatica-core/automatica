using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.Common;
using Automatica.Core.Base.Tunneling;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals.Cache.Common;
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
        private readonly ISettingsCache _settingsCache;
        private readonly ILogger<RemoteConnectService> _logger;

        private string _currentDomainName;

        private bool _isRunning;
        private bool _isEnabled;

        private int _activatedServices;

        public RemoteConnectService(IConfiguration config, ICloudApi cloudApi, ILicenseContext licenseContext, IFrpService frpService, ISettingsCache settingsCache, ILogger<RemoteConnectService> logger)
        {
            _config = config;
            _cloudApi = cloudApi;
            _licenseContext = licenseContext;
            _frpService = frpService;
            _settingsCache = settingsCache;
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
                _activatedServices++;

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

            if (_activatedServices >= _licenseContext.MaxRemoteTunnels)
            {
                throw new ArgumentException($"Cannot establish another tunnel. Max remote tunnels limit reached...");
            }

            var remotePortResponse = await _cloudApi.GetRemoteConnectPort(driverGuid, name, protocol);
            if (remotePortResponse == null)
            {
                throw new ArgumentException($"Could not get remote port for tunnel {name}...");
            }
            var remotePort = remotePortResponse.Port;

            if (protocol is TunnelingProtocol.Tcp || protocol is TunnelingProtocol.Udp)
            {
                await FrpcHelper.CreateTransportServiceFileFromTemplate(protocol, name, localIp, localPort, remotePort,
                    token);
            }
            else
            {
                throw new ArgumentException("Only Udp and Tcp are allowed!");
            }

            _activatedServices++;

            return _currentDomainName;
        }

        public async Task<string> CreateWebTunnelAsync(TunnelingProtocol protocol, string name, string subDomain, string localIp, int localPort,
            Guid driverGuid, string basicUser, string basicPassword, CancellationToken token = default)
        {
            if (_isRunning)
            {
                throw new ArgumentException($"Cannot establish a new tunnel while already running...");
            }

            if (_activatedServices >= _licenseContext.MaxRemoteTunnels)
            {
                throw new ArgumentException($"Cannot establish another tunnel. Max remote tunnels limit reached...");
            }

            if (protocol is TunnelingProtocol.Tcp || protocol is TunnelingProtocol.Udp)
            {
                throw new ArgumentException("Only Http and Https are supported!");
            }

            var response = await _cloudApi.CreateRemoteConnectUrl(driverGuid, subDomain);

            if (response == null)
            {
                _logger.LogError($"Could not create target domain {subDomain}");
                throw new ArgumentException($"Could not create target domain { subDomain }");
            }

            await FrpcHelper.CreateApplicationServiceFileFromTemplate(protocol, name, localIp, localPort, subDomain,
                basicUser, basicPassword,
                token);
            return response.TunnelUrl;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _isRunning = false;
            await _frpService.StopAsync(cancellationToken);
        }

        public async Task InitAsync()
        {
            try
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
                    _isEnabled = true;
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
                remoteDomain.Value = response.SubDomain;
                if (response.SubDomain != domain)
                {
                    context.Update(remoteDomain);
                    await context.SaveChangesAsync();
                    _settingsCache.UpdateByKey(remoteDomain.ValueKey, remoteDomain);
                }

                try
                {
                    if (await _frpService.InitConfigurationsAsync())
                    {
                        _currentDomainName = response.TunnelUrl;
                    }
                    else
                    {
                        _logger.LogError($"Could not initialize RemoteControl service ....");
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, $"Could not initialize RemoteControl service ....{e}");
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Something went wrong init frp services....");
                _isEnabled = false;
            }
        }

        public Task<bool> IsRunning(CancellationToken token)
        {
            return Task.FromResult(_isEnabled);
        }
    }
}
