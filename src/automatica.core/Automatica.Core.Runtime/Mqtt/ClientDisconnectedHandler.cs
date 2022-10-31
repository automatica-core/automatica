using Automatica.Core.Runtime.Abstraction.Remote;
using MQTTnet.Server;
using System.Threading.Tasks;

namespace Automatica.Core.Runtime.Mqtt
{
    internal class ClientDisconnectedHandler : IMqttServerClientDisconnectedHandler
    {
        private readonly IRemoteHandler _handlerInstance;
        public ClientDisconnectedHandler(IRemoteHandler handler)
        {
            _handlerInstance = handler;
        }

        public async Task HandleClientDisconnectedAsync(MqttServerClientDisconnectedEventArgs eventArgs)
        {
            await _handlerInstance.ClientDisconnected(new RemoteDisconnectedEvent(eventArgs.ClientId));
        }

    }
}
