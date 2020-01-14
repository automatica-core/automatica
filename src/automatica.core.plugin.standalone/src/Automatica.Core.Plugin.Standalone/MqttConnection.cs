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
using MQTTnet.Client.Options;
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

        public INodeTemplateFactory NodeTemplateFactory => _nodeTemplateFactory;
        private readonly INodeTemplateFactory _nodeTemplateFactory;
        public string MasterAddress { get; }
        public string NodeId { get; }
        public string Username { get; }
        public string Password { get; }
        public ILogger Logger { get; }

        internal MqttDispatcher Dispatcher => _dispatcher;
        private readonly MqttDispatcher _dispatcher;

        public IMqttClient MqttClient => _mqttClient;
        private readonly IMqttClient _mqttClient;
        private readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(0);

        public IDriverFactoryLoader Loader => _loader;
        private readonly IDriverFactoryLoader _loader;

        public IDriverNodesStore NodeStore => _nodeStore;
        private readonly IDriverNodesStore _nodeStore;

        internal IDriver DriverInstance
        {
            get { return _driverInstance; }
            set { _driverInstance = value; }
        }
        private IDriver _driverInstance;

        public IDriverStore DriverStore => _driverStore;
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

