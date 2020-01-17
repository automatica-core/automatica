using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using MQTTnet.Client.Disconnecting;

namespace Automatica.Core.Plugin.Standalone
{
    internal class MqttDisconnectedHandler : IMqttClientDisconnectedHandler
    {
        private readonly MqttConnection _mqttConnection;

        public MqttDisconnectedHandler(MqttConnection mqttConnection)
        {
            _mqttConnection = mqttConnection;
        }

        public Task HandleDisconnectedAsync(MqttClientDisconnectedEventArgs eventArgs)
        {
            _mqttConnection.OnClientDisconnected(eventArgs);
            return Task.CompletedTask;
        }
    }
}
