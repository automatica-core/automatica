using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.Remote;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using Automatica.Core.Plugin.Standalone.Factories;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client.Receiving;
using Newtonsoft.Json;
using MQTTnet.Client;
using MQTTnet.Server.Internal;
using System;
using System.Linq;
using Automatica.Core.Plugin.Standalone.Dispatcher;
using RemoteDispatchValue = Automatica.Core.Base.Remote.RemoteDispatchValue;

namespace Automatica.Core.Plugin.Standalone
{
    internal class MqttMessageHandlers : IMqttApplicationMessageReceivedHandler
    {
        private readonly MqttConnection _connection;
        public ILogger Logger { get; }
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);

        public MqttMessageHandlers(ILogger logger, MqttConnection connection)
        {
            _connection = connection;
            Logger = logger;
        }

        public async Task HandleApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs e)
        {
            var json = "";

            if (e.ApplicationMessage.Payload.Length > 0)
            {
                json = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
            }

            if (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable("LOG_VERBOSE")))
            {
                Logger.LogTrace($"received topic {e.ApplicationMessage.Topic} with data {json}...");
            }

            if (e.ApplicationMessage.Topic == $"{RemoteTopicConstants.CONFIG_TOPIC}/{_connection.NodeId}")
            {
                Logger.LogDebug($"Received config event...");

                if (!await _semaphore.WaitAsync(1000))
                {
                    Logger.LogWarning($"Instance already running, ignoring second call...");
                    return;
                }

                Logger.LogInformation($"Init driver...");
                var dto = JsonConvert.DeserializeObject<NodeInstance>(json);
                var factory = _connection.Factories.First(a => a.DriverGuid == dto.This2NodeTemplate);
                var context = new DriverContext(dto, factory, _connection.Dispatcher, _connection.NodeTemplateFactory,
                    new RemoteTelegramMonitor(), new RemoteLicenseState(), Logger, new RemoteLearnMode(_connection),
                    new RemoteServerCloudApi(), new RemoteLicenseContract(), new ConsoleLoggerFactory(), false);

             

                _connection.DriverInstance = factory.CreateDriver(context);

                var driverInstance = await _connection.Loader.LoadDriverFactory(dto, factory, context);
                Logger.LogInformation($"Init driver...done");

                Logger.LogInformation($"Starting driver...");

                foreach (var driver in _connection.DriverStore.All())
                {
                    if (!await driver.Start())
                    {
                        Logger.LogError($"Could not start driver...");
                    }
                }

                Logger.LogInformation($"Starting driver...done");

                await _connection.MqttClient.SubscribeAsync(new TopicFilterBuilder()
                    .WithTopic($"{RemoteTopicConstants.ACTION_TOPIC_START}/{_connection.DriverInstance.Id}/+")
                    .WithExactlyOnceQoS()
                    .Build());

            }
            else if (MqttTopicFilterComparer.IsMatch(e.ApplicationMessage.Topic, $"{RemoteTopicConstants.DISPATCHER_TOPIC}/#"))
            {
                var remoteDispatchValue = JsonConvert.DeserializeObject<RemoteDispatchValue>(json);

                if (_connection.MqttClient.Options.ClientId == remoteDispatchValue.Source)
                {
                    return;
                }

                _connection.Dispatcher.MqttDispatch(e.ApplicationMessage.Topic, json);
            }
            else if (MqttTopicFilterComparer.IsMatch(e.ApplicationMessage.Topic, $"{RemoteTopicConstants.SLAVE_TOPIC}/*/reinit"))
            {
                Logger.LogDebug($"Received re-init event...");
                Environment.Exit(0);
            }
            else if (_connection.DriverInstance != null && MqttTopicFilterComparer.IsMatch(e.ApplicationMessage.Topic, $"{RemoteTopicConstants.ACTION_TOPIC_START}/{_connection.DriverInstance.Id}/#"))
            {
                var idObject = JsonConvert.DeserializeObject<IdObject>(json);

                var node = _connection.NodeStore.Get(idObject.Id);

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
                else if (e.ApplicationMessage.Topic.EndsWith(DriverNodeRemoteAction.Read.ToString()))
                {
                    await node.Read();
                }
            }
        }
    }
}
