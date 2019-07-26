using Microsoft.Extensions.Logging;
using MQTTnet.Server;
using System;
using System.Threading.Tasks;

namespace Automatica.Core.Runtime.Mqtt
{
    public class ServerStartedHandler : IMqttServerStartedHandler
    {
        private readonly ILogger _logger;

        public ServerStartedHandler(ILogger logger)
        {
            _logger = logger;
        }

        public Task HandleServerStartedAsync(EventArgs eventArgs)
        {
            _logger.LogInformation($"Remote Server started...");
            return Task.CompletedTask;
        }
    }
}
