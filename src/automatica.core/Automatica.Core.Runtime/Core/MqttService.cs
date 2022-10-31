using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.Base.Remote;
using Automatica.Core.Driver;
using Automatica.Core.Driver.LeanMode;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals;
using Automatica.Core.Internals.Core;
using Automatica.Core.Runtime.Abstraction.Remote;
using Automatica.Core.Runtime.Mqtt;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Protocol;
using MQTTnet.Server;
using MQTTnet.Server.Internal;
using Newtonsoft.Json;

namespace Automatica.Core.Runtime.Core
{
    internal class MqttService : IRemoteHandler, IRemoteServerHandler
    {
        private readonly ILogger _logger;

        private readonly IDictionary<string, NodeInstance> _mqttNodes = new Dictionary<string, NodeInstance>();
        private readonly IDictionary<string, IDictionary<NodeInstance, IDriverFactory>> _mqttSlaves = new Dictionary<string, IDictionary<NodeInstance, IDriverFactory>>();
        private readonly IList<string> _connectedMqttClients = new List<string>();
        private readonly IMqttServer _mqttServer;
        private readonly IDispatcher _dispatcher;
        private readonly ILearnMode _learnModeHandler;

        public MqttService(IServiceProvider services)
        {
            _logger = SystemLogger.Instance;

            _mqttServer = services.GetRequiredService<IMqttServer>();
            _mqttServer.ClientConnectedHandler = new ClientConnectedHandler(this);
            _mqttServer.ClientSubscribedTopicHandler = new ClientSubscribedHandler(this);
            _mqttServer.ApplicationMessageReceivedHandler = new ApplicationMessageHandler(this);
            _mqttServer.StartedHandler = new ServerStartedHandler(_logger);


            _learnModeHandler = services.GetRequiredService<ILearnMode>();
            _dispatcher = services.GetRequiredService<IDispatcher>();
        }

        public static void ValidateConnection(MqttConnectionValidatorContext context, IConfiguration config, ILogger logger)
        {
            logger.LogDebug($"Validating connection from {context.Endpoint} clientId: {context.ClientId}, userName: {context.Username}, password: *****");
            using (var db = new AutomaticaContext(config))
            {
                if (db.Slaves.Any(a => a.ClientId == context.Username && a.ClientKey == context.Password))
                {
                    //leave for compatibility reasons
#pragma warning disable 618
                    context.ReturnCode = MqttConnectReturnCode.ConnectionAccepted;
#pragma warning restore 618
                    context.ReasonCode = MqttConnectReasonCode.Success;
                }
                else
                {
                    //leave for compatibility reasons
#pragma warning disable 618
                    context.ReturnCode = MqttConnectReturnCode.ConnectionRefusedBadUsernameOrPassword;
#pragma warning restore 618

                    context.ReasonCode = MqttConnectReasonCode.BadUserNameOrPassword;
                }
            }
        }

        public Task ClientConnected(RemoteConnectedEvent connectedEvent)
        {
            _logger.LogDebug($"Client {connectedEvent.ClientId} connected...");
            _connectedMqttClients.Add(connectedEvent.ClientId);

            return Task.CompletedTask;
        }

        public async Task ClientSubscribedTopic(RemoteSubscribedEvent subscribedEvent)
        {
            _logger.LogDebug($"Client {subscribedEvent.ClientId} subscribed to {subscribedEvent.Topic}");

            if (MqttTopicFilterComparer.IsMatch(subscribedEvent.Topic, $"{RemoteTopicConstants.CONFIG_TOPIC}/#"))
            {
                if (_mqttNodes.ContainsKey(subscribedEvent.ClientId))
                {
                    await PublishConfig(subscribedEvent.ClientId, _mqttNodes[subscribedEvent.ClientId]);
                }
            }
            else if (MqttTopicFilterComparer.IsMatch(subscribedEvent.Topic, $"{RemoteTopicConstants.SLAVE_TOPIC}/+/actions"))
            {
                if (_mqttSlaves.ContainsKey(subscribedEvent.ClientId))
                {
                    var factoryNode = _mqttSlaves[subscribedEvent.ClientId];
                    foreach (var node in factoryNode)
                    {
                        await StartInstance(subscribedEvent.ClientId, node.Value, node.Key);
                    }
                }
            }
        }

        public async Task MessageReceived(RemoteMessageEvent messageEvent)
        {
            _logger.LogDebug($"Received message on {messageEvent.Topic} from {messageEvent.ClientId} with data {messageEvent.Message}");

            if (MqttTopicFilterComparer.IsMatch(messageEvent.Topic, $"{RemoteTopicConstants.DISPATCHER_TOPIC}/#"))
            {
                _dispatcher.MqttDispatch(messageEvent.Topic, messageEvent.Message);
            }
            else if (MqttTopicFilterComparer.IsMatch(messageEvent.Topic, $"{RemoteTopicConstants.ACTION_TOPIC_START}/+/{RemoteTopicConstants.NOTIFY_LEARN_MODE}"))
            {
                var learnModeDtoJson = messageEvent.Message;
                var learnModeDto = JsonConvert.DeserializeObject<LearnModeDto>(learnModeDtoJson);

                _logger.LogDebug($"Received LearnMode message");
                await _learnModeHandler.NotifyLearnNode(learnModeDto.Name, learnModeDto.Description, learnModeDto.Self,
                    learnModeDto.Templates, learnModeDto.PropertyInstances);
            }
        }

        public async Task SendAction(Guid client, DriverNodeRemoteAction action, object data)
        {
            var msg = new MqttApplicationMessage
            {
                Topic = $"{RemoteTopicConstants.ACTION_TOPIC_START}/{client}/{action}",
                QualityOfServiceLevel = MqttQualityOfServiceLevel.ExactlyOnce,
                Retain = true
            };

            if (data != null)
            {
                msg.Payload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data));
            }

            await _mqttServer.PublishAsync(msg);
        }

        public Task Init()
        {
            _mqttSlaves.Clear();
            _mqttNodes.Clear();

            return Task.CompletedTask;
        }

        public async Task AddNode(string id, NodeInstance node)
        {
            _mqttNodes.Add(id, node);

            await PublishConfig(id, node);
        }

        public async Task AddSlave(string id, IDriverFactory factory, NodeInstance nodeInstance)
        {
            if (!_mqttSlaves.ContainsKey(id))
            {
                _mqttSlaves.Add(id, new Dictionary<NodeInstance, IDriverFactory>());
            }
            _mqttSlaves[id].Add(nodeInstance, factory);

            // start instance asap
            if (_connectedMqttClients.Contains(id))
            {
                await StartInstance(id, factory, nodeInstance);
            }

        }

        public async Task Stop()
        {
            foreach (var slave in _mqttSlaves)
            {
                foreach (var node in slave.Value)
                {
                    await StopInstance(slave.Key, node.Value, node.Key);
                }
            }
        }

        private async Task SendActions(string clientId, params ActionRequest[] actionRequests)
        {
            try
            {
                await _mqttServer.PublishAsync(new MqttApplicationMessage
                {
                    Topic = $"{RemoteTopicConstants.SLAVE_TOPIC}/{clientId}/{RemoteTopicConstants.ACTIONS_TOPIC_START}",
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
                    Topic = $"{RemoteTopicConstants.SLAVE_TOPIC}/{clientId}/{RemoteTopicConstants.ACTION_TOPIC_START}",
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
                _logger.LogDebug($"Publish to {RemoteTopicConstants.CONFIG_TOPIC}/{clientId}");
                await _mqttServer.PublishAsync(new MqttApplicationMessage
                {
                    Topic = $"{RemoteTopicConstants.CONFIG_TOPIC}/{clientId}",
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

        private async Task StartInstance(string clientId, IDriverFactory factory, NodeInstance nodeInstance)
        {
            try
            {
                var actionRequests = new List<ActionRequest>();
                    _logger.LogDebug($"Publish to slave/{clientId}/actions (Start {factory.ImageName}:{factory.Tag})");

                    var actionRequest = new ActionRequest
                    {
                        Action = SlaveAction.Start,
                        ImageSource = factory.ImageSource,
                        ImageName = factory.ImageName,
                        Tag = factory.Tag,
                        Id = nodeInstance.ObjId
                    };

                    actionRequests.Add(actionRequest);
               
                await SendAction(clientId, actionRequest);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Could not send start instances message...");
            }
        }



        private async Task StopInstance(string clientId, IDriverFactory factory, NodeInstance nodeInstance)
        {
            try
            {
                _logger.LogDebug($"Publish to slave/{clientId}/action");

                var actionRequest = new ActionRequest()
                {
                    Action = SlaveAction.Stop,
                    ImageSource = factory.ImageSource,
                    ImageName = factory.ImageName,
                    Tag = factory.Tag,
                    Id = nodeInstance.ObjId
                };

                await SendAction(clientId, actionRequest);

            }
            catch (Exception e)
            {
                _logger.LogError(e, "Could not send start instances message...");
            }
        }

        public IList<string> GetConnectedClients()
        {
            return _connectedMqttClients;
        }
    }
}
