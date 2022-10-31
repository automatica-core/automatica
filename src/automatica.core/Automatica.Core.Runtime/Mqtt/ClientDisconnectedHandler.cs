using Automatica.Core.Runtime.Abstraction.Remote;
using MQTTnet.Client.Disconnecting;
using System.Threading.Tasks;

namespace Automatica.Core.Runtime.Mqtt
{
    internal class ClientDisconnectedHandler : IMqttClientDisconnectedHandler
    {
        private readonly IRemoteHandler _handlerInstance;
        public ClientDisconnectedHandler(IRemoteHandler handler)
        {
            _handlerInstance = handler;
        }

        public async Task HandleDisconnectedAsync(MqttClientDisconnectedEventArgs eventArgs)
        {
            await _handlerInstance.ClientDisconnected(new RemoteDisconnectedEvent(eventArgs.ConnectResult.AssignedClientIdentifier));
        }
    }
}
