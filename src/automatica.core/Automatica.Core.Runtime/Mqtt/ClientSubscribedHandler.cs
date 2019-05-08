using Automatica.Core.Runtime.Core;
using MQTTnet.Server;
using System.Threading.Tasks;

namespace Automatica.Core.Runtime.Mqtt
{
    internal class ClientSubscribedHandler : IMqttServerClientSubscribedTopicHandler
    {
        private readonly CoreServer _coreServer;

        public ClientSubscribedHandler(CoreServer coreServer)
        {
            _coreServer = coreServer;
        }
        public Task HandleClientSubscribedTopicAsync(MqttServerClientSubscribedTopicEventArgs eventArgs)
        {
            return _coreServer.MqttServerClientSubscribedTopic(eventArgs);
        }
    }
}
