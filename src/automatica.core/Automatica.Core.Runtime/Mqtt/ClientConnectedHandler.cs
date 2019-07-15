using MQTTnet.Server;
using System.Threading.Tasks;
using Automatica.Core.Runtime.Abstraction;

namespace Automatica.Core.Runtime.Mqtt
{
    internal class ClientConnectedHandler : IMqttServerClientConnectedHandler
    {
        private readonly IMqttHandler _handlerInstance;
        public ClientConnectedHandler(IMqttHandler handler)
        {
            _handlerInstance = handler;
        }


        public async Task HandleClientConnectedAsync(MqttServerClientConnectedEventArgs e)
        {
            await _handlerInstance.MqttServerClientConnected(e);
        }
    }
}
