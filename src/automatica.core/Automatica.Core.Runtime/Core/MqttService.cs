using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.Base.Mqtt;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals;
using Automatica.Core.Internals.Core;
using Automatica.Core.Runtime.Abstraction;
using Automatica.Core.Runtime.Mqtt;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Protocol;
using MQTTnet.Server;
using Newtonsoft.Json;

namespace Automatica.Core.Runtime.Core
{
    internal class MqttService : IMqttHandler, IMqttServerHandler
    {
        private readonly ILogger _logger;

        private readonly IDictionary<string, NodeInstance> _mqttNodes = new Dictionary<string, NodeInstance>();
        private readonly IDictionary<string, IList<IDriverFactory>> _mqttSlaves = new Dictionary<string, IList<IDriverFactory>>();
        private readonly IList<string> _connectedMqttClients = new List<string>();
        private readonly IMqttServer _mqttServer;
        private readonly IDispatcher _dispatcher;

        public MqttService(IServiceProvider services)
        {
            _logger = SystemLogger.Instance;

            _mqttServer = services.GetRequiredService<IMqttServer>();
            _mqttServer.ClientConnectedHandler = new ClientConnectedHandler(this);
            _mqttServer.ClientSubscribedTopicHandler = new ClientSubscribedHandler(this);
            _mqttServer.ApplicationMessageReceivedHandler = new ApplicationMessageHandler(this);
            _mqttServer.StartedHandler = new ServerStartedHandler(_logger);

            _dispatcher = services.GetRequiredService<IDispatcher>();
        }

        public static void ValidateConnection(MqttConnectionValidatorContext context, IConfiguration config, ILogger logger)
        {
            logger.LogDebug($"Validating connection from {context.Endpoint} clientId: {context.ClientId}, userName: {context.Username}, password: *****");
            using (var db = new AutomaticaContext(config))
            {
                if (db.Slaves.Any(a => a.ClientId == context.Username && a.ClientKey == context.Password))
                {
                    context.ReturnCode = MqttConnectReturnCode.ConnectionAccepted;
                }
                else
                {
                    context.ReturnCode = MqttConnectReturnCode.ConnectionRefusedBadUsernameOrPassword;
                }
            }
        }

        public Task MqttServerClientConnected(MqttServerClientConnectedEventArgs mqttServerClientConnectedEventArgs)
        {
            _logger.LogDebug($"Client {mqttServerClientConnectedEventArgs.ClientId} connected...");
            _connectedMqttClients.Add(mqttServerClientConnectedEventArgs.ClientId);

            return Task.CompletedTask;
        }

        public async Task MqttServerClientSubscribedTopic(MqttServerClientSubscribedTopicEventArgs mqttServerClientSubscribedTopicEventArgs)
        {
            _logger.LogDebug($"Client {mqttServerClientSubscribedTopicEventArgs.ClientId} subscribed to {mqttServerClientSubscribedTopicEventArgs.TopicFilter}");

            if (MqttTopicFilterComparer.IsMatch(mqttServerClientSubscribedTopicEventArgs.TopicFilter.Topic, $"{MqttTopicConstants.CONFIG_TOPIC}/#"))
            {
                if (_mqttNodes.ContainsKey(mqttServerClientSubscribedTopicEventArgs.ClientId))
                {
                    await PublishConfig(mqttServerClientSubscribedTopicEventArgs.ClientId, _mqttNodes[mqttServerClientSubscribedTopicEventArgs.ClientId]);
                }
            }
            else if (MqttTopicFilterComparer.IsMatch(mqttServerClientSubscribedTopicEventArgs.TopicFilter.Topic, $"{MqttTopicConstants.SLAVE_TOPIC}/+/actions"))
            {
                if (_mqttSlaves.ContainsKey(mqttServerClientSubscribedTopicEventArgs.ClientId))
                {
                    await StartInstances(mqttServerClientSubscribedTopicEventArgs.ClientId, _mqttSlaves[mqttServerClientSubscribedTopicEventArgs.ClientId]);
                }
            }
        }

        public Task MqttServerApplicationMessageReceived(MqttApplicationMessageReceivedEventArgs mqttApplicationMessageReceivedEventArgs)
        {
            if (MqttTopicFilterComparer.IsMatch(mqttApplicationMessageReceivedEventArgs.ApplicationMessage.Topic, $"{MqttTopicConstants.DISPATCHER_TOPIC}/#"))
            {
                _dispatcher.MqttDispatch(mqttApplicationMessageReceivedEventArgs.ApplicationMessage.Topic, mqttApplicationMessageReceivedEventArgs.ApplicationMessage.Payload);
            }

            return Task.CompletedTask;
        }

        public Task Init()
        {
            _mqttSlaves.Clear();
            _mqttNodes.Clear();

            return Task.CompletedTask;
        }

        public Task AddMqttNode(string id, NodeInstance node)
        {
            _mqttNodes.Add(id, node);

            return Task.CompletedTask;
        }

        public async Task AddMqttSlave(string id, IDriverFactory factory)
        {
            if (!_mqttSlaves.ContainsKey(id))
            {
                _mqttSlaves.Add(id, new List<IDriverFactory>());
            }
            _mqttSlaves[id].Add(factory);

            // start instance asap
            if (_connectedMqttClients.Contains(id))
            {
                await StartInstances(id, new List<IDriverFactory> { factory });
            }

        }

        public async Task Stop()
        {
            foreach (var slave in _mqttSlaves.Keys)
            {
                await StopInstances(slave, _mqttSlaves[slave]);
            }
        }

        private async Task SendActions(string clientId, params ActionRequest[] actionRequests)
        {
            try
            {
                await _mqttServer.PublishAsync(new MqttApplicationMessage
                {
                    Topic = $"{MqttTopicConstants.SLAVE_TOPIC}/{clientId}/{MqttTopicConstants.ACTIONS_TOPIC_START}",
                    QualityOfServiceLevel = MqttQualityOfServiceLevel.ExactlyOnce,
                    Payload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(actionRequests)),
                    Retain = true
                });
            }
            catch (Exception e)
            {
                _logger.LogError("Could not send action", e);
            }
        }

        private async Task SendAction(string clientId, ActionRequest actionRequest)
        {
            try
            {
                await _mqttServer.PublishAsync(new MqttApplicationMessage
                {
                    Topic = $"{MqttTopicConstants.SLAVE_TOPIC}/{clientId}/{MqttTopicConstants.ACTION_TOPIC_START}",
                    QualityOfServiceLevel = MqttQualityOfServiceLevel.ExactlyOnce,
                    Payload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(actionRequest)),
                    Retain = true
                });
            }
            catch (Exception e)
            {
                _logger.LogError("Could not send action", e);
            }
        }

        private async Task PublishConfig(string clientId, NodeInstance nodeInstance)
        {
            try
            {
                _logger.LogDebug($"Publish to config/{clientId}");
                await _mqttServer.PublishAsync(new MqttApplicationMessage()
                {
                    Topic = $"{MqttTopicConstants.CONFIG_TOPIC}/{clientId}",
                    QualityOfServiceLevel = MqttQualityOfServiceLevel.ExactlyOnce,
                    Payload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(nodeInstance)),
                    Retain = true
                });
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Could not publish config...");
            }
        }

        private async Task StartInstances(string clientId, IList<IDriverFactory> driverFactories)
        {
            try
            {
                var actionRequests = new List<ActionRequest>();
                foreach (var driver in driverFactories)
                {
                    _logger.LogDebug($"Publish to slave/{clientId}/actions (Start {driver.ImageName}:{driver.Tag})");

                    var actionRequest = new ActionRequest
                    {
                        Action = SlaveAction.Start,
                        ImageSource = driver.ImageSource,
                        ImageName = driver.ImageName,
                        Tag = driver.Tag
                    };

                    actionRequests.Add(actionRequest);
                }
                await SendActions(clientId, actionRequests.ToArray());
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Could not send start instances message...");
            }
        }



        private async Task StopInstances(string clientId, IList<IDriverFactory> driverFactories)
        {
            try
            {
                foreach (var driver in driverFactories)
                {
                    _logger.LogDebug($"Publish to slave/{clientId}/action");

                    var actionRequest = new ActionRequest()
                    {
                        Action = SlaveAction.Stop,
                        ImageSource = driver.ImageSource,
                        ImageName = driver.ImageName,
                        Tag = driver.Tag
                    };

                    await SendAction(clientId, actionRequest);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Could not send start instances message...");
            }
        }
    }
}
