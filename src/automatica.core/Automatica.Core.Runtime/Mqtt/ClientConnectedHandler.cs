using MQTTnet.Server;
using System.Threading.Tasks;
using Automatica.Core.Runtime.Abstraction.Remote;

namespace Automatica.Core.Runtime.Mqtt
{
    internal class ClientConnectedHandler : IMqttServerClientConnectedHandler
    {
        private readonly IRemoteHandler _handlerInstance;
        public ClientConnectedHandler(IRemoteHandler handler)
        {
            _handlerInstance = handler;
        }


        public async Task HandleClientConnectedAsync(MqttServerClientConnectedEventArgs e)
        {
            await _handlerInstance.ClientConnected(new RemoteConnectedEvent(e.ClientId));
        }
    }
}
