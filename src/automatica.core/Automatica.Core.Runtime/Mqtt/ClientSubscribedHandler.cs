using Automatica.Core.Runtime.Core;
using MQTTnet.Server;
using System.Threading.Tasks;
using Automatica.Core.Runtime.Abstraction;

namespace Automatica.Core.Runtime.Mqtt
{
    internal class ClientSubscribedHandler : IMqttServerClientSubscribedTopicHandler
    {
        private readonly IMqttHandler _handlerInstance;

        public ClientSubscribedHandler(IMqttHandler handler)
        {
            _handlerInstance = handler;
        }
        public Task HandleClientSubscribedTopicAsync(MqttServerClientSubscribedTopicEventArgs eventArgs)
        {
            return _handlerInstance.MqttServerClientSubscribedTopic(eventArgs);
        }
    }
}
