using System;
using System.Threading.Tasks;
using Automatica.Core.Satellite.Abstraction;
using Microsoft.Extensions.Logging;
using MQTTnet;
using Newtonsoft.Json;

namespace Automatica.Core.Satellite.Runtime
{
    internal class ApplicationMessageReceivedHandler
    {
        private readonly SatelliteRuntime _slave;
        private readonly ILogger _logger;
        private readonly string _actionTopic;
        private readonly string _actionTopics;
        private readonly string _reinitTopic;
        private readonly string _watchdogTopic;

        public ApplicationMessageReceivedHandler(SatelliteRuntime slave, ILogger logger)
        {
            _slave = slave;
            _logger = logger;

            _actionTopic = $"slave/{_slave.SlaveId}/action";
            _actionTopics = $"slave/{_slave.SlaveId}/actions";
            _reinitTopic = $"slave/{_slave.SlaveId}/reinit";
            _watchdogTopic = $"watchdog/{_slave.SlaveId}";

        }

        // same argument order as MqttTopicFilterComparer.IsMatch(topic, filter) in MQTTnet 3
        private static bool IsMatch(string topic, string filter)
        {
            return MqttTopicFilterComparer.Compare(topic, filter) == MqttTopicFilterCompareResult.IsMatch;
        }

        public async Task HandleApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs e)
        {
            _logger.LogDebug($"Mqtt message received for topic {e.ApplicationMessage.Topic}");

            if (IsMatch(_actionTopic, e.ApplicationMessage.Topic))
            {
                var json = e.ApplicationMessage.ConvertPayloadToString();
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
            else if (IsMatch(_actionTopics, e.ApplicationMessage.Topic))
            {
                var json = e.ApplicationMessage.ConvertPayloadToString();
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
            else if (IsMatch(_reinitTopic, e.ApplicationMessage.Topic))
            {
                await _slave.ReInit();
            }
            else if (IsMatch(_watchdogTopic, e.ApplicationMessage.Topic))
            {
                await _slave.ReInit();
            }

        }
    }
}
