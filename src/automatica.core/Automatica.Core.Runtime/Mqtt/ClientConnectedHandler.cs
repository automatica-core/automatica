using Automatica.Core.Runtime.Core;
using MQTTnet.Server;
using System.Threading.Tasks;

namespace Automatica.Core.Runtime.Mqtt
{
    internal class ClientConnectedHandler : IMqttServerClientConnectedHandler
    {
        private readonly CoreServer _coreServer;
        public ClientConnectedHandler(CoreServer coreServer)
        {
            _coreServer = coreServer;
        }


        public async Task HandleClientConnectedAsync(MqttServerClientConnectedEventArgs e)
        {
            await _coreServer.MqttServerClientConnected(e);
        }
    }
}
