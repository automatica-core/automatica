using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Automatica.Core.Slave.Abstraction;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client.Receiving;
using MQTTnet.Server;
using MQTTnet.Server.Internal;
using Newtonsoft.Json;

namespace Automatica.Core.Slave.Runtime
{
    internal class ApplicationMessageReceivedHandler : IMqttApplicationMessageReceivedHandler
    {
        private readonly SlaveRuntime _slave;
        private readonly ILogger _logger;
        private readonly string _actionTopic;
        private readonly string _actionTopics;
        private readonly string _reinitTopic;

        public ApplicationMessageReceivedHandler(SlaveRuntime slave, ILogger logger)
        {
            _slave = slave;
            _logger = logger;

            _actionTopic = $"slave/{_slave.SlaveId}/action";
            _actionTopics = $"slave/{_slave.SlaveId}/actions";
            _reinitTopic = $"slave/{_slave.SlaveId}/reinit";

        }

        public async Task HandleApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs e)
        {
            _logger.LogDebug($"Mqtt message received for topic {e.ApplicationMessage.Topic}");



            if (MqttTopicFilterComparer.IsMatch(_actionTopic, e.ApplicationMessage.Topic))
            {
                var json = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
                var action = JsonConvert.DeserializeObject<ActionRequest>(json);
                try
                {
                    await _slave.ExecuteAction(action);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Could not execute request");
                }
            }
            else if (MqttTopicFilterComparer.IsMatch(_actionTopics, e.ApplicationMessage.Topic))
            {
                var json = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
                var actions = JsonConvert.DeserializeObject<ActionRequest[]>(json);
                try
                {
                    foreach (var action in actions)
                    {
                        await _slave.ExecuteAction(action);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Could not execute request");
                }
            }
            else if (MqttTopicFilterComparer.IsMatch(_reinitTopic, e.ApplicationMessage.Topic))
            {
                await _slave.ReInit();
            }

        }
    }
}
