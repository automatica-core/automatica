using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.Base.Remote;
using Automatica.Core.Base.Templates;
using Automatica.Core.Driver;
using Automatica.Core.Driver.Loader;
using Automatica.Core.EF.Models;
using Automatica.Core.Plugin.Standalone.Abstraction;
using Automatica.Core.Plugin.Standalone.Dispatcher;
using Automatica.Core.Plugin.Standalone.Factories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;
using MQTTnet.Server;
using Newtonsoft.Json;
using MqttClientDisconnectedEventArgs = MQTTnet.Client.MqttClientDisconnectedEventArgs;

namespace Automatica.Core.Plugin.Standalone
{
    internal class IdObject
    {
        public Guid Id { get; set; }
    }

    internal class MqttConnection : IDriverConnection
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly INodeTemplateFactory _nodeTemplateFactory;
        public string MasterAddress { get; }
        public string NodeId { get; }
        public string Username { get; }
        public string Password { get; }
        public ILogger Logger { get; }

        private readonly MqttDispatcher _dispatcher;
        private readonly IMqttClient _mqttClient;
        private readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(0);

        private readonly IDriverFactoryLoader _loader;
        private readonly IDriverNodesStore _nodeStore;

        private IDriver _driverInstance;
        private readonly IDriverStore _driverStore;

        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);

        public MqttConnection(ILogger logger, string host, string nodeId, string username, string password,
            IDriverFactory factory,
            IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            
            Logger = logger;
            MasterAddress = host;
            NodeId = nodeId;
            Username = username;
            Password = password;
            Factory = factory;

            _nodeTemplateFactory = serviceProvider.GetRequiredService<INodeTemplateFactory>();
            _loader = _serviceProvider.GetRequiredService<IDriverFactoryLoader>();
            _nodeStore = serviceProvider.GetRequiredService<IDriverNodesStore>();
            _driverStore = serviceProvider.GetRequiredService<IDriverStore>();

            _mqttClient = new MqttFactory().CreateMqttClient();
            _dispatcher = new MqttDispatcher(_mqttClient);
        }

        public IDriverFactory Factory { get; }

        public async Task<bool> Start()
        {
            try
            {
                var options = new MqttClientOptionsBuilder()
                    .WithClientId(NodeId)
                    .WithTcpServer(MasterAddress)
                    .WithCredentials(Username, Password)
                    .WithCleanSession()
                    .Build();
                _mqttClient.Disconnected += OnClientDisconnected;
                _mqttClient.ApplicationMessageReceived += OnMqttClientOnApplicationMessageReceived;

                await _mqttClient.ConnectAsync(options);

                Logger.LogInformation($"Connected to mqtt broker {MasterAddress} with clientId {NodeId}");

                Logger.LogDebug($"Subscribe to {RemoteTopicConstants.CONFIG_TOPIC}/{NodeId}");
                Logger.LogDebug($"Subscribe to {RemoteTopicConstants.DISPATCHER_TOPIC}/#");

                await _mqttClient.SubscribeAsync(
                    new TopicFilterBuilder().WithTopic($"{RemoteTopicConstants.CONFIG_TOPIC}/{NodeId}")
                        .WithExactlyOnceQoS().Build(),
                    new TopicFilterBuilder().WithTopic($"{RemoteTopicConstants.DISPATCHER_TOPIC}/#")
                        .WithAtLeastOnceQoS().Build());
            }
            catch (Exception e)
            {

                Logger.LogError(e, "Could not connect to broker");
                return false;
            }

            return true;
        }

        public Task Run()
        {
            return _semaphoreSlim.WaitAsync();
        }

        public async Task<bool> Send(string topic, object data)
        {
            if (_driverInstance == null)
            {
                return false;
            }

            var jsonMessage = JsonConvert.SerializeObject(data);

            var mqttMessage = new MqttApplicationMessage
            {
                Topic = $"{RemoteTopicConstants.ACTION_TOPIC_START}/{_driverInstance.Id}/{topic}",
                Payload = Encoding.UTF8.GetBytes(jsonMessage),
                QualityOfServiceLevel = MqttQualityOfServiceLevel.ExactlyOnce

            };

            await _mqttClient.PublishAsync(mqttMessage);

            return true;
        }


        private async void OnMqttClientOnApplicationMessageReceived(object sender, MqttApplicationMessageReceivedEventArgs e)
        {

            var json = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
            Logger.LogDebug($"received topic {e.ApplicationMessage.Topic} with data {json}...");

            if (e.ApplicationMessage.Topic == $"{RemoteTopicConstants.CONFIG_TOPIC}/{NodeId}")
            {
                if (!await _semaphore.WaitAsync(1000))
                {
                    Logger.LogWarning($"Instance already running, ignoring second call...");
                    return;
                }

                var dto = JsonConvert.DeserializeObject<NodeInstance>(json);

                var context = new DriverContext(dto, _dispatcher, _nodeTemplateFactory,
                    new RemoteTelegramMonitor(), new RemoteLicenseState(), Logger, new RemoteLearnMode(this),
                    new RemoteServerCloudApi(), new RemoteLicenseContract(), false);
                _driverInstance = Factory.CreateDriver(context);

                await _loader.LoadDriverFactory(dto, Factory, context);

                foreach (var driver in _driverStore.All())
                {
                    await driver.Start();
                }

                await _mqttClient.SubscribeAsync(new TopicFilterBuilder()
                        .WithTopic($"{RemoteTopicConstants.ACTION_TOPIC_START}/{_driverInstance.Id}/+")
                        .WithExactlyOnceQoS().Build());

            }
            else if (MqttTopicFilterComparer.IsMatch(e.ApplicationMessage.Topic, $"{RemoteTopicConstants.DISPATCHER_TOPIC}/#"))
            {
                _dispatcher.MqttDispatch(e.ApplicationMessage.Topic,  json);
            }
            else if (_driverInstance != null && MqttTopicFilterComparer.IsMatch(e.ApplicationMessage.Topic, $"{RemoteTopicConstants.ACTION_TOPIC_START}/{_driverInstance.Id}/#"))
            {
                var idObject = JsonConvert.DeserializeObject<IdObject>(json);

                var node = _nodeStore.Get(idObject.Id);

                if (node == null)
                {
                    return;
                }

                if (e.ApplicationMessage.Topic.EndsWith(DriverNodeRemoteAction.StartLearnMode.ToString()))
                {
                    await node.EnableLearnMode();
                }
                else if (e.ApplicationMessage.Topic.EndsWith(DriverNodeRemoteAction.StopLearnMode.ToString()))
                {
                    await node.DisableLearnMode();
                }
            }
        }

        private void OnClientDisconnected(object sender, MqttClientDisconnectedEventArgs e)
        {
            _mqttClient.Disconnected -= OnClientDisconnected;
            _mqttClient.ApplicationMessageReceived -= OnMqttClientOnApplicationMessageReceived;

            Logger.LogWarning(e.Exception, "Mqtt client disconnected");
            _semaphoreSlim.Release(1);
        }

        public Task<bool> Stop()
        {
            _semaphoreSlim.Release(1);
            return Task.FromResult(true);
        }
    }
}

