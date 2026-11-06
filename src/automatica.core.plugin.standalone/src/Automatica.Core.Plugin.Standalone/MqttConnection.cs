using System;
using System.Collections.Generic;
using System.Security.Authentication;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.Remote;
using Automatica.Core.Base.Templates;
using Automatica.Core.Driver;
using Automatica.Core.Driver.Loader;
using Automatica.Core.Plugin.Standalone.Abstraction;
using Automatica.Core.Plugin.Standalone.Dispatcher;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Options;
using MQTTnet.Diagnostics.Logger;
using MQTTnet.Protocol;
using Newtonsoft.Json;

namespace Automatica.Core.Plugin.Standalone
{
    internal class IdObject
    {
        public Guid Id { get; set; }
    }

    internal class MqttConnection : IDriverConnection
    {
        private readonly IServiceProvider _serviceProvider;

        public INodeTemplateFactory NodeTemplateFactory { get; }

        public string MasterAddress { get; }
        public string NodeId { get; }
        public string Username { get; }
        public string Password { get; }
        public ILogger Logger { get; }

        internal MqttDispatcher Dispatcher { get; }

        public IMqttClient MqttClient => _mqttClient;
        private readonly IMqttClient _mqttClient;
        private readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(0);

        public IDriverFactoryLoader Loader { get; }

        public IDriverNodesStore NodeStore { get; }

        internal IDriver DriverInstance
        {
            get => _driverInstance;
            set => _driverInstance = value;
        }
        private IDriver _driverInstance;

        public IDriverStore DriverStore { get; }

        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);
        private Timer _configTimer;

        public MqttConnection(ILogger logger, string host, string nodeId, string username, string password,
            IList<IDriverFactory> factories,
            IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            
            Logger = logger;
            MasterAddress = host;
            NodeId = nodeId;
            Username = username;
            Password = password;
            Factories = factories;


            NodeTemplateFactory = serviceProvider.GetRequiredService<INodeTemplateFactory>();
            Loader = _serviceProvider.GetRequiredService<IDriverFactoryLoader>();
            NodeStore = serviceProvider.GetRequiredService<IDriverNodesStore>();
            DriverStore = serviceProvider.GetRequiredService<IDriverStore>();

            _mqttClient = new MqttFactory().CreateMqttClient(new MqttLogger(logger));
            Dispatcher = new MqttDispatcher(_mqttClient);
        }

        public IList<IDriverFactory> Factories { get; }

        public async Task<bool> Start()
        {
            try
            {
                var builder = new MqttClientOptionsBuilder()
                    .WithClientId($"{NodeId}") //must be unique, if we have several factories in one dll, this crashed otherwise!
                    .WithCredentials(Username, Password)
                    .WithCleanSession()
                    .WithWillMessage(new MqttApplicationMessage { Topic = $"watchdog/{Username}/{NodeId}" });

                // the satellite passes the core's TLS port and the pinned certificate fingerprint when it uses TLS
                var tlsFingerprint = BrokerCertificatePin.Normalize(Environment.GetEnvironmentVariable("AUTOMATICA_SLAVE_TLS_FINGERPRINT"));
                int tlsPort;
                if (tlsFingerprint != null && Int32.TryParse(Environment.GetEnvironmentVariable("AUTOMATICA_SLAVE_TLS_PORT"), out tlsPort) && tlsPort > 0)
                {
                    builder = builder
                        .WithTcpServer(MasterAddress, tlsPort)
                        .WithTls(new MqttClientOptionsBuilderTlsParameters
                        {
                            UseTls = true,
                            SslProtocol = SslProtocols.Tls12 | SslProtocols.Tls13,
                            CertificateValidationHandler = context => BrokerCertificatePin.Matches(context.Certificate, context.Chain, tlsFingerprint, Logger)
                        });
                    Logger.LogInformation($"Using TLS to {MasterAddress}:{tlsPort}");
                }
                else
                {
                    builder = builder.WithTcpServer(MasterAddress);
                }

                var options = builder.Build();
                _mqttClient.DisconnectedHandler = new MqttDisconnectedHandler(this);

                _mqttClient.ApplicationMessageReceivedHandler = new MqttMessageHandlers(Logger, this, _serviceProvider);
                await _mqttClient.ConnectAsync(options);

                Logger.LogInformation($"Connected to mqtt broker {MasterAddress} with clientId {NodeId}");

                Logger.LogDebug($"Subscribe to {RemoteTopicConstants.CONFIG_TOPIC}/{NodeId}");
                // no dispatcher/# any more: the broker only allows the dispatcher topics of the own nodes, the driver
                // subscribes to those when it registers them (MqttDispatcher.RegisterDispatch)
                await _mqttClient.SubscribeAsync(
                    new MqttTopicFilterBuilder().WithTopic($"{RemoteTopicConstants.CONFIG_TOPIC}/{NodeId}").WithExactlyOnceQoS().Build(),
                    new MqttTopicFilterBuilder().WithTopic($"{RemoteTopicConstants.SLAVE_TOPIC}/{NodeId}/reinit").WithAtLeastOnceQoS().Build());

                _configTimer = new Timer(async state =>
                {
                    await _mqttClient.DisconnectAsync(new MqttClientDisconnectOptions()
                    {
                        ReasonCode = MqttClientDisconnectReason.SessionTakenOver,
                        ReasonString = "Did not receive a config topic within the timeout range..."
                    });
                }, null, TimeSpan.FromMinutes(2), TimeSpan.FromSeconds(0));
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

        internal void OnClientDisconnected(MqttClientDisconnectedEventArgs e)
        {
          
            Logger.LogWarning(e.Exception, "Mqtt client disconnected");
            _semaphoreSlim.Release(1);
        }

        public Task<bool> Stop()
        {
            _semaphoreSlim.Release(1);
            return Task.FromResult(true);
        }

        public void ConfigReceived()
        {
            _configTimer.Dispose();
        }
    }
}

