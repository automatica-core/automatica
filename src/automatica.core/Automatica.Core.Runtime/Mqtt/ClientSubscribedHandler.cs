using MQTTnet.Server;
using System.Threading.Tasks;
using Automatica.Core.Runtime.Abstraction.Remote;

namespace Automatica.Core.Runtime.Mqtt
{
    internal class ClientSubscribedHandler : IMqttServerClientSubscribedTopicHandler
    {
        private readonly IRemoteHandler _handlerInstance;

        public ClientSubscribedHandler(IRemoteHandler handler)
        {
            _handlerInstance = handler;
        }
        public Task HandleClientSubscribedTopicAsync(MqttServerClientSubscribedTopicEventArgs eventArgs)
        {
            return _handlerInstance.ClientSubscribedTopic(new RemoteSubscribedEvent(eventArgs.ClientId, eventArgs.TopicFilter.Topic));
        }
    }
}
