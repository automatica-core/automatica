using System;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.Common;
using Automatica.Core.Internals.Cloud;
using FluffySpoon.Ngrok;
using Microsoft.Extensions.Configuration;

namespace Automatica.Core.Runtime.Ngrok
{
    internal class AutomaticaNgrokService : IAutomaticaNgrokService
    {
        private readonly INgrokService _ngrokService;
        private readonly ICloudApi _cloudApi;
        private readonly bool _useNgrok;

        public AutomaticaNgrokService(IConfiguration config, INgrokService ngrokService, ICloudApi cloudApi)
        {
            _ngrokService = ngrokService;
            _cloudApi = cloudApi;
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

                var tunnel = await _ngrokService.StartAsync(new Uri($"http://localhost:{ServerInfo.WebPort}"), cancellationToken);
                await _ngrokService.WaitUntilReadyAsync(cancellationToken);

                await _cloudApi.SendNgrokTunnelUrl(tunnel.PublicUrl);
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
