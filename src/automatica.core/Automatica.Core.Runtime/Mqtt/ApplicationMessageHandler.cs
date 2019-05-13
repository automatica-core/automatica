using Automatica.Core.Runtime.Core;
using MQTTnet;
using MQTTnet.Client.Receiving;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Automatica.Core.Runtime.Mqtt
{
    internal class ApplicationMessageHandler : IMqttApplicationMessageReceivedHandler
    {
        private readonly CoreServer _coreServer;

        public ApplicationMessageHandler(CoreServer coreServer)
        {
            _coreServer = coreServer;
        }

        public Task HandleApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs eventArgs)
        {
            return _coreServer.MqttServerApplicationMessageReceived(eventArgs);
        }
    }
}
