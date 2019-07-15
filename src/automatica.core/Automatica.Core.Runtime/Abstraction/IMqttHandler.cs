using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Server;

namespace Automatica.Core.Runtime.Abstraction
{
    public interface IMqttHandler
    {
        Task MqttServerClientConnected(MqttServerClientConnectedEventArgs mqttServerClientConnectedEventArgs);
        Task MqttServerClientSubscribedTopic(MqttServerClientSubscribedTopicEventArgs mqttServerClientSubscribedTopicEventArgs);
        Task MqttServerApplicationMessageReceived(MqttApplicationMessageReceivedEventArgs mqttApplicationMessageReceivedEventArgs);
    }
}
