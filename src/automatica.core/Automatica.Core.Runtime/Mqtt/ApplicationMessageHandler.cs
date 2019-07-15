using MQTTnet;
using MQTTnet.Client.Receiving;
using System.Threading.Tasks;
using Automatica.Core.Runtime.Abstraction;

namespace Automatica.Core.Runtime.Mqtt
{
    internal class ApplicationMessageHandler : IMqttApplicationMessageReceivedHandler
    {
        private readonly IMqttHandler _handlerInstance;

        public ApplicationMessageHandler(IMqttHandler handler)
        {
            _handlerInstance = handler;
        }

        public Task HandleApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs eventArgs)
        {
            return _handlerInstance.MqttServerApplicationMessageReceived(eventArgs);
        }
    }
}
