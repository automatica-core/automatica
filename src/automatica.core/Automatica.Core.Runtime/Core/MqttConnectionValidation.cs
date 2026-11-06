using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MQTTnet.Server;

namespace Automatica.Core.Runtime.Core
{
    /// <summary>
    /// Attaches the satellite/plugin credential check to the MQTT server. Must be registered before
    /// AddHostedMqttServer so it runs before the server accepts connections.
    /// </summary>
    internal class MqttConnectionValidation : IHostedService
    {
        private readonly MqttServer _mqttServer;
        private readonly IConfiguration _configuration;
        private readonly ILogger _logger;

        public MqttConnectionValidation(MqttServer mqttServer, IConfiguration configuration)
        {
            _mqttServer = mqttServer;
            _configuration = configuration;
            _logger = new CoreLogger(configuration, null);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _mqttServer.ValidatingConnectionAsync += Validate;
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _mqttServer.ValidatingConnectionAsync -= Validate;
            return Task.CompletedTask;
        }

        private Task Validate(ValidatingConnectionEventArgs context)
        {
            return MqttService.ValidateConnection(context, _configuration, _logger);
        }
    }
}
