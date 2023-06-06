using System;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.Common;
using Automatica.Core.Internals.Cloud;
using FluffySpoon.Ngrok;
using Microsoft.Build.Framework;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Automatica.Core.Runtime.Ngrok
{
    internal class AutomaticaNgrokService : IAutomaticaNgrokService
    {
        private readonly INgrokService _ngrokService;
        private readonly ICloudApi _cloudApi;
        private readonly ILogger<AutomaticaNgrokService> _logger;
        private readonly bool _useNgrok;

        public AutomaticaNgrokService(IConfiguration config, INgrokService ngrokService, ICloudApi cloudApi, ILogger<AutomaticaNgrokService> logger)
        {
            _ngrokService = ngrokService;
            _cloudApi = cloudApi;
            _logger = logger;
            var useNgrok = config["server:ngrok:enabled"];
            if (useNgrok != null && bool.Parse(useNgrok))
            {
                _useNgrok = true;
            }

        }
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (_useNgrok)
            {
                await _ngrokService.InitializeAsync(cancellationToken);

                _logger.LogInformation($"Bind ngrok to address http://localhost:{ServerInfo.WebPort}");

                try
                {
                    var tunnel = await _ngrokService.StartAsync(new Uri($"http://localhost:{ServerInfo.WebPort}"),
                        cancellationToken);
                    await _ngrokService.WaitUntilReadyAsync(cancellationToken);

                    await _cloudApi.SendNgrokTunnelUrl(tunnel.PublicUrl);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, $"Could not bin logger...{e}");
                }
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_useNgrok)
            {
                await _ngrokService.StopAsync(cancellationToken);
            }
        }
    }
}
