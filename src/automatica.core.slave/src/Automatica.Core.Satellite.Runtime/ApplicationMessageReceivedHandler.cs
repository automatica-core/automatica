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
        private readonly string _watchdogFilter;

        public ApplicationMessageReceivedHandler(SatelliteRuntime slave, ILogger logger)
        {
            _slave = slave;
            _logger = logger;

            _actionTopic = $"slave/{_slave.SlaveId}/action";
            _actionTopics = $"slave/{_slave.SlaveId}/actions";
            _reinitTopic = $"slave/{_slave.SlaveId}/reinit";
            // will messages of the plugins: watchdog/<satellite>/<node>
            _watchdogFilter = $"watchdog/{_slave.SlaveId}/+";
        }

        // argument order as MqttTopicFilterComparer.IsMatch(topic, filter) in MQTTnet 3: the received topic first
        internal static bool IsMatch(string topic, string filter)
        {
            return MqttTopicFilterComparer.Compare(topic, filter) == MqttTopicFilterCompareResult.IsMatch;
        }

        public async Task HandleApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs e)
        {
            var topic = e.ApplicationMessage.Topic;
            _logger.LogDebug($"Mqtt message received for topic {topic}");

            try
            {
                if (IsMatch(topic, _actionTopic))
                {
                    var action = JsonConvert.DeserializeObject<ActionRequest>(e.ApplicationMessage.ConvertPayloadToString());
                    if (action != null)
                    {
                        await _slave.ExecuteAction(action);
                    }
                }
                else if (IsMatch(topic, _actionTopics))
                {
                    var actions = JsonConvert.DeserializeObject<ActionRequest[]>(e.ApplicationMessage.ConvertPayloadToString());
                    foreach (var action in actions ?? Array.Empty<ActionRequest>())
                    {
                        await _slave.ExecuteAction(action);
                    }
                }
                else if (IsMatch(topic, _reinitTopic))
                {
                    await _slave.ReInit();
                }
                else if (IsMatch(topic, _watchdogFilter))
                {
                    await _slave.RestartPlugin(topic.Substring(topic.LastIndexOf('/') + 1));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Could not execute request on {topic}");
            }
        }
    }
}
