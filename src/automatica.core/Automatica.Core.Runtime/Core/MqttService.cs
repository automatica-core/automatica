using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.Common;
using Automatica.Core.Base.IO;
using Automatica.Core.Base.Remote;
using Automatica.Core.Driver;
using Automatica.Core.Driver.LeanMode;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals.Core;
using Automatica.Core.Runtime.Abstraction.Remote;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Protocol;
using MQTTnet.Server;
using Newtonsoft.Json;

namespace Automatica.Core.Runtime.Core
{
    internal class MqttService : IRemoteHandler, IRemoteServerHandler
    {
        private readonly ILogger _logger;

        private readonly IDictionary<string, NodeInstance> _mqttNodes = new Dictionary<string, NodeInstance>();
        private readonly IDictionary<string, IDictionary<NodeInstance, IDriverFactory>> _mqttSlaves = new Dictionary<string, IDictionary<NodeInstance, IDriverFactory>>();
        // MQTTnet raises the client events on its own threads, only access under _clientsLock
        private readonly List<string> _connectedMqttClients = new List<string>();
        private readonly object _clientsLock = new object();
        private readonly MqttServer _mqttServer;
        private readonly IDispatcher _dispatcher;
        private readonly ILearnMode _learnModeHandler;
        private readonly MqttNodeRegistry _registry;
        private readonly MqttBrokerAuthenticator _authenticator;
        private readonly MqttConnectionValidation _brokerSecurity;
        private readonly IConfiguration _configuration;

        public MqttService(IServiceProvider services)
        {
            _logger = services.GetRequiredService<ILogger<MqttService>>();

            _mqttServer = services.GetRequiredService<MqttServer>();
            _mqttServer.ClientConnectedAsync += e => ClientConnected(new RemoteConnectedEvent(e.ClientId));
            _mqttServer.ClientDisconnectedAsync += e => ClientDisconnected(new RemoteDisconnectedEvent(e.ClientId));
            _mqttServer.ClientSubscribedTopicAsync += e => ClientSubscribedTopic(new RemoteSubscribedEvent(e.ClientId, e.TopicFilter.Topic));
            _mqttServer.InterceptingPublishAsync += OnClientPublish;
            _mqttServer.StartedAsync += _ =>
            {
                _logger.LogInformation("Remote Server started...");
                return Task.CompletedTask;
            };

            _learnModeHandler = services.GetRequiredService<ILearnMode>();
            _dispatcher = services.GetRequiredService<IDispatcher>();
            _registry = services.GetRequiredService<MqttNodeRegistry>();
            _authenticator = services.GetRequiredService<MqttBrokerAuthenticator>();
            _brokerSecurity = services.GetService<MqttConnectionValidation>();
            _configuration = services.GetRequiredService<IConfiguration>();
        }

        private async Task OnClientPublish(InterceptingPublishEventArgs e)
        {
            // Messages the server injects itself (see PublishAsync) pass through here as well, without a client id.
            if (string.IsNullOrEmpty(e.ClientId))
            {
                return;
            }

            // the ACL (MqttConnectionValidation) denies it as well, checked here too so the handler order does not matter
            var identity = MqttConnectionValidation.GetIdentity(e.SessionItems);
            if (!e.ProcessPublish || !MqttAccessControl.CanPublish(identity, e.ApplicationMessage.Topic, _registry))
            {
                return;
            }

            try
            {
                await MessageReceived(new RemoteMessageEvent(e.ClientId, e.ApplicationMessage.Topic, e.ApplicationMessage.ConvertPayloadToString()), identity);
            }
            catch (Exception)
            {
                // already logged in MessageReceived; the message is still delivered to subscribers (as with MQTTnet 3)
            }
        }

        private static bool IsMatch(string topic, string filter)
        {
            return MqttTopicFilterComparer.Compare(topic, filter) == MqttTopicFilterCompareResult.IsMatch;
        }

        /// <summary>
        /// Config and actions are not retained: they carry driver secrets and per-plugin passwords, and a retained action
        /// would replay on every reconnect. Clients get them when they subscribe (see <see cref="ClientSubscribedTopic"/>).
        /// </summary>
        internal static MqttApplicationMessage BuildMessage(string topic, byte[] payload, MqttQualityOfServiceLevel qos, bool retain = false)
        {
            var builder = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithQualityOfServiceLevel(qos)
                .WithRetainFlag(retain);

            if (payload != null)
            {
                builder.WithPayload(payload);
            }

            return builder.Build();
        }

        private Task PublishAsync(MqttApplicationMessage message)
        {
            return _mqttServer.InjectApplicationMessage(new InjectedMqttApplicationMessage(message), CancellationToken.None);
        }

        public Task ClientConnected(RemoteConnectedEvent connectedEvent)
        {
            _logger.LogInformation($"Client {connectedEvent.ClientId} connected...");
            AddConnectedClient(connectedEvent.ClientId);

            return Task.CompletedTask;
        }

        public Task ClientDisconnected(RemoteDisconnectedEvent disconnectedEvent)
        {
            _logger.LogInformation($"Client {disconnectedEvent.ClientId} disconnected...");

            lock (_clientsLock)
            {
                _connectedMqttClients.Remove(disconnectedEvent.ClientId);
            }

            return Task.CompletedTask;
        }

        public async Task ClientSubscribedTopic(RemoteSubscribedEvent subscribedEvent)
        {
            _logger.LogInformation($"Client {subscribedEvent.ClientId} subscribed to {subscribedEvent.Topic}");

            if (IsMatch(subscribedEvent.Topic, $"{RemoteTopicConstants.CONFIG_TOPIC}/#"))
            {
                if (_mqttNodes.TryGetValue(subscribedEvent.ClientId, out var node))
                {
                    await PublishConfig(subscribedEvent.ClientId, node);
                }
            }
            else if (IsMatch(subscribedEvent.Topic, $"{RemoteTopicConstants.SLAVE_TOPIC}/+/actions"))
            {
                if (_mqttSlaves.TryGetValue(subscribedEvent.ClientId, out var factoryNode))
                {
                    foreach (var node in factoryNode)
                    {
                        await StartInstance(subscribedEvent.ClientId, node.Value, node.Key);
                    }
                }
            }
        }

        public Task MessageReceived(RemoteMessageEvent messageEvent)
        {
            return MessageReceived(messageEvent, null);
        }

        internal async Task MessageReceived(RemoteMessageEvent messageEvent, MqttClientIdentity identity)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug($"Received message on {messageEvent.Topic} from {messageEvent.ClientId} with data {messageEvent.Message}...");
            }

            try
            {
                AddConnectedClient(messageEvent.ClientId);

                if (IsMatch(messageEvent.Topic, $"{RemoteTopicConstants.DISPATCHER_TOPIC}/#"))
                {
                    _dispatcher.MqttDispatch(messageEvent.Topic, messageEvent.Message);
                }
                else if (IsMatch(messageEvent.Topic,
                             $"{RemoteTopicConstants.ACTION_TOPIC_START}/+/{RemoteTopicConstants.NOTIFY_LEARN_MODE}"))
                {
                    var learnModeDtoJson = messageEvent.Message;
                    var learnModeDto = JsonConvert.DeserializeObject<LearnModeDto>(learnModeDtoJson);

                    if (learnModeDto == null)
                    {
                        throw new ArgumentNullException(nameof(learnModeDto));
                    }

                    // a plugin may only learn nodes below its own driver node
                    if (identity != null && (learnModeDto.Self == null || !_registry.IsInPluginTree(identity.NodeId, learnModeDto.Self.ObjId)))
                    {
                        _logger.LogWarning($"{messageEvent.ClientId} sent a learn mode message for a node it does not own, ignored");
                        return;
                    }

                    _logger.LogInformation($"{messageEvent.ClientId} Received LearnMode message");
                    await _learnModeHandler.NotifyLearnNode(learnModeDto.Name, learnModeDto.Description,
                        learnModeDto.Self,
                        learnModeDto.Templates, learnModeDto.PropertyInstances);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error handling message on {messageEvent.Topic} from {messageEvent.ClientId}");
                throw;
            }
        }

        public async Task SendAction(Guid client, DriverNodeRemoteAction action, object data)
        {
            var msg = BuildMessage($"{RemoteTopicConstants.ACTION_TOPIC_START}/{client}/{action}",
                data != null ? Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data)) : null,
                MqttQualityOfServiceLevel.ExactlyOnce);

            _logger.LogInformation($"Send action {action} to {client} {JsonConvert.SerializeObject(data)}");
            await PublishAsync(msg);
        }

        public async Task Init()
        {
            _mqttSlaves.Clear();
            _mqttNodes.Clear();
            _registry.Clear();

            // a satellite was added or removed: the broker switches between localhost and all addresses
            if (_brokerSecurity != null)
            {
                await _brokerSecurity.RefreshBindingAsync();
            }
        }


        public async Task ReInit()
        {
            foreach(var client in GetConnectedClients())
            {
                _logger.LogInformation($"Send reinit to client {client}");
                await PublishAsync(BuildMessage($"{RemoteTopicConstants.SLAVE_TOPIC}/{client}/{RemoteTopicConstants.ACTIONS_TOPIC_REINIT}", Encoding.UTF8.GetBytes(""), MqttQualityOfServiceLevel.ExactlyOnce));
            }
        }

        public async Task AddNode(string id, NodeInstance node)
        {
            _logger.LogInformation($"Add node for {id}, NodeInstance is {node.ObjId}");
            _mqttNodes.Add(id, node);

            await PublishConfig(id, node);
        }

        public async Task AddSlave(string id, IDriverFactory factory, NodeInstance nodeInstance)
        {
            _logger.LogInformation($"Add slave with {id}, NodeInstance is {nodeInstance.ObjId}");
            if (!_mqttSlaves.ContainsKey(id))
            {
                _mqttSlaves.Add(id, new Dictionary<NodeInstance, IDriverFactory>());
            }
            _mqttSlaves[id].Add(nodeInstance, factory);
            _registry.Register(id, nodeInstance);

            // start instance asap
            bool connected;
            lock (_clientsLock)
            {
                connected = _connectedMqttClients.Contains(id);
            }
            if (connected)
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
                await PublishAsync(BuildMessage($"{RemoteTopicConstants.SLAVE_TOPIC}/{clientId}/{RemoteTopicConstants.ACTIONS_TOPIC_START}", Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(actionRequests)), MqttQualityOfServiceLevel.ExactlyOnce));
            }
            catch (Exception e)
            {
                _logger.LogError($"Could not send action {e}");
            }
        }

        private async Task SendAction(string clientId, ActionRequest actionRequest)
        {
            try
            {
                _logger.LogInformation($"Send action {actionRequest.Action} for {actionRequest.Id} ({actionRequest.ImageName}:{actionRequest.Tag}) to client {clientId}");
                await PublishAsync(BuildMessage($"{RemoteTopicConstants.SLAVE_TOPIC}/{clientId}/{RemoteTopicConstants.ACTION_TOPIC_START}", Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(actionRequest)), MqttQualityOfServiceLevel.ExactlyOnce));
            }
            catch (Exception e)
            {
                _logger.LogError($"Could not send action {e}");
            }
        }

        private async Task PublishConfig(string clientId, NodeInstance nodeInstance)
        {
            try
            {
                _logger.LogInformation($"Publish to {RemoteTopicConstants.CONFIG_TOPIC}/{clientId}");
                await PublishAsync(BuildMessage($"{RemoteTopicConstants.CONFIG_TOPIC}/{clientId}", Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(nodeInstance)), MqttQualityOfServiceLevel.ExactlyOnce));
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
                var actionRequests  = new List<ActionRequest>();
                _logger.LogInformation(
                    $"Publish to slave/{clientId}/actions (Start {factory.ImageName}:{factory.Tag})");

                // ImageSource is not sent any more: satellites reject anything but a registry image
                var actionRequest = new SatelliteActionRequest
                {
                    Action = SlaveAction.Start,
                    ImageName = factory.ImageName,
                    Tag = factory.Tag,
                    Id = nodeInstance.ObjId,
                    ClientKey = _authenticator.PluginCredentials.GetPassword(clientId, nodeInstance.ObjId)
                };
                AddBrokerTls(actionRequest);

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
                _logger.LogInformation($"Publish StopInstance to slave/{clientId}/action");

                var actionRequest = new ActionRequest()
                {
                    Action = SlaveAction.Stop,
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

        private void AddBrokerTls(SatelliteActionRequest request)
        {
            if (String.Equals(_configuration["server:mqttTls"], "false", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            try
            {
                var port = _configuration["server:mqttTlsPort"];
                request.BrokerTlsPort = String.IsNullOrEmpty(port) ? 8883 : Convert.ToInt32(port);
                request.BrokerFingerprint = MqttBrokerCertificate.Fingerprint(MqttBrokerCertificate.Get(_logger));
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Could not add the broker certificate to the start action");
            }
        }

        private void AddConnectedClient(string clientId)
        {
            lock (_clientsLock)
            {
                if (!_connectedMqttClients.Contains(clientId))
                {
                    _connectedMqttClients.Add(clientId);
                }
            }
        }

        /// <summary>
        /// Snapshot of the connected clients
        /// </summary>
        public IList<string> GetConnectedClients()
        {
            lock (_clientsLock)
            {
                return _connectedMqttClients.ToList();
            }
        }

    }

    /// <summary>
    /// Start action with the fields satellites since the broker hardening understand; older satellites ignore them
    /// </summary>
    internal class SatelliteActionRequest : ActionRequest
    {
        /// <summary>
        /// Broker password of the plugin container (see <see cref="MqttPluginCredentials"/>)
        /// </summary>
        public string ClientKey { get; set; }

        /// <summary>
        /// Port of the broker's TLS listener, 0 if there is none
        /// </summary>
        public int BrokerTlsPort { get; set; }

        /// <summary>
        /// SHA-256 fingerprint of the broker certificate (lowercase hex), for pinning
        /// </summary>
        public string BrokerFingerprint { get; set; }

        /// <summary>
        /// Image digest (sha256:...) to pin the image to, when known
        /// </summary>
        public string ImageDigest { get; set; }
    }
}
