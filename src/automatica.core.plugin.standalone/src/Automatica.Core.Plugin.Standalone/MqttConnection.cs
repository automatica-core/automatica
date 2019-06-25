using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.Mqtt;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using Automatica.Core.Plugin.Standalone.Dispatcher;
using Automatica.Core.Plugin.Standalone.Factories;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Server;
using Newtonsoft.Json;
using MqttClientDisconnectedEventArgs = MQTTnet.Client.MqttClientDisconnectedEventArgs;

namespace Automatica.Core.Plugin.Standalone
{
    internal class MqttConnection
    {
        private readonly ILogger _logger;
        private readonly string _host;
        private readonly string _username;
        private readonly string _password;
        private readonly IDriverFactory _factory;

        private readonly RemoteNodeTemplatesFactory _remoteNodeTemplatesFactory = new RemoteNodeTemplatesFactory();
        private readonly MqttDispatcher _dispatcher;
        private readonly IMqttClient _mqttClient;
        private readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(0);

        public MqttConnection(ILogger logger, string host, string username, string password, IDriverFactory factory)
        {
            _logger = logger;
            _host = host;
            _username = username;
            _password = password;
            _factory = factory;

            _mqttClient = new MqttFactory().CreateMqttClient();
            _dispatcher = new MqttDispatcher(_mqttClient);
        }

        public async Task<bool> Start()
        {
            try
            {
                var options = new MqttClientOptionsBuilder()
                    .WithClientId(_factory.FactoryGuid.ToString())
                    .WithTcpServer(_host)
                    .WithCredentials(_username, _password)
                    .WithCleanSession()
                    .Build();
                _mqttClient.Disconnected += OnClientDisconnected;
                _mqttClient.ApplicationMessageReceived += OnMqttClientOnApplicationMessageReceived;

                await _mqttClient.ConnectAsync(options);
                await _mqttClient.SubscribeAsync(
                    new TopicFilterBuilder().WithTopic($"{MqttTopicConstants.CONFIG_TOPIC}/{_factory.DriverGuid}")
                        .WithExactlyOnceQoS().Build(),
                    new TopicFilterBuilder().WithTopic($"{MqttTopicConstants.DISPATCHER_TOPIC}/#")
                        .WithAtLeastOnceQoS().Build());
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Could not connect to broker");
                return false;
            }

            return true;
        }

        public Task Run()
        {
            return _semaphoreSlim.WaitAsync();
        }


        private async void OnMqttClientOnApplicationMessageReceived(object sender, MqttApplicationMessageReceivedEventArgs e)
        {
            Console.WriteLine($"received topic {e.ApplicationMessage.Topic}...");

            if (e.ApplicationMessage.Topic == $"{MqttTopicConstants.CONFIG_TOPIC}/{_factory.DriverGuid}")
            {
                var json = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
                var dto = JsonConvert.DeserializeObject<NodeInstance>(json);

                var context = new DriverContext(dto, _dispatcher, _remoteNodeTemplatesFactory, null, null, _logger, null, null, null, false);
                var driver = _factory.CreateDriver(context);

                if (driver.BeforeInit())
                {
                    driver.Configure();
                    await driver.Start();
                }
            }
            else if (MqttTopicFilterComparer.IsMatch(e.ApplicationMessage.Topic, $"{MqttTopicConstants.DISPATCHER_TOPIC}/#"))
            {
                _dispatcher.MqttDispatch(e.ApplicationMessage.Topic, e.ApplicationMessage.Payload);
            }
        }

        private void OnClientDisconnected(object sender, MqttClientDisconnectedEventArgs e)
        {
            _mqttClient.Disconnected -= OnClientDisconnected;
            _mqttClient.ApplicationMessageReceived -= OnMqttClientOnApplicationMessageReceived;

            _logger.LogWarning(e.Exception, "Mqtt client disconnected");
            _semaphoreSlim.Release(1);
        }

        public Task Stop()
        {
            _semaphoreSlim.Release(1);
            return Task.CompletedTask;
        }
    }
}
