using System.Text;
using MQTTnet;
using MQTTnet.Client.Receiving;
using System.Threading.Tasks;
using Automatica.Core.Runtime.Abstraction.Remote;

namespace Automatica.Core.Runtime.Mqtt
{
    internal class ApplicationMessageHandler : IMqttApplicationMessageReceivedHandler
    {
        private readonly IRemoteHandler _handlerInstance;

        public ApplicationMessageHandler(IRemoteHandler handler)
        {
            _handlerInstance = handler;
        }

        public Task HandleApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs eventArgs)
        {
            return _handlerInstance.MessageReceived(new RemoteMessageEvent(eventArgs.ClientId, eventArgs.ApplicationMessage.Topic, Encoding.UTF8.GetString(eventArgs.ApplicationMessage.Payload)));
        }
    }
}
