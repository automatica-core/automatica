using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.Base.Remote;
using Automatica.Core.Base.Serialization;
using MQTTnet;
using MQTTnet.Protocol;
using MQTTnet.Server;

namespace Automatica.Core.Runtime.Core
{
    internal class MqttPublishService : IRemoteSender
    {
        private readonly IMqttServer _mqttServer;

        public MqttPublishService(IMqttServer mqttServer)
        {
            _mqttServer = mqttServer;
        }

        public async Task DispatchValue(IDispatchable dispatchable, object value)
        {
            var topic = $"{RemoteTopicConstants.DISPATCHER_TOPIC}/{dispatchable.Type}/{dispatchable.Id}";
            await _mqttServer.PublishAsync(new MqttApplicationMessage()
            {
                Topic = topic,
                QualityOfServiceLevel = MqttQualityOfServiceLevel.AtLeastOnce,
                Payload = BinarySerializer.Serialize(value),
                Retain = true
            });
        }
    }
}
