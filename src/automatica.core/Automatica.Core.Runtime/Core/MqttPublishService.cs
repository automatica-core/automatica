using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.Base.Remote;
using Automatica.Core.Base.Serialization;
using MQTTnet.Protocol;
using MQTTnet.Server;

namespace Automatica.Core.Runtime.Core
{
    internal class MqttPublishService : IRemoteSender
    {
        private readonly MqttServer _mqttServer;

        public MqttPublishService(MqttServer mqttServer)
        {
            _mqttServer = mqttServer;
        }

        public async Task DispatchValue(IDispatchable dispatchable, object value)
        {
            var topic = $"{RemoteTopicConstants.DISPATCHER_TOPIC}/{dispatchable.Type}/{dispatchable.Id}";
            var message = MqttService.BuildMessage(topic, BinarySerializer.Serialize(value), MqttQualityOfServiceLevel.AtLeastOnce);
            await _mqttServer.InjectApplicationMessage(new InjectedMqttApplicationMessage(message), CancellationToken.None);
        }
    }
}
