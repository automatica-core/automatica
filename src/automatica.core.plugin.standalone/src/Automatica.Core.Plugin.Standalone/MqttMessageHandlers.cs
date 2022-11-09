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
using MQTTnet.Server;
using Newtonsoft.Json;
using MQTTnet.Client;
using MQTTnet.Server.Internal;
using System;

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
            var json = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
            Logger.LogDebug($"received topic {e.ApplicationMessage.Topic} with data {json}...");

            if (e.ApplicationMessage.Topic == $"{RemoteTopicConstants.CONFIG_TOPIC}/{_connection.NodeId}")
            {
                if (!await _semaphore.WaitAsync(1000))
                {
                    Logger.LogWarning($"Instance already running, ignoring second call...");
                    return;
                }

                var dto = JsonConvert.DeserializeObject<NodeInstance>(json);

                var context = new DriverContext(dto, _connection.Dispatcher, _connection.NodeTemplateFactory,
                    new RemoteTelegramMonitor(), new RemoteLicenseState(), Logger, new RemoteLearnMode(_connection),
                    new RemoteServerCloudApi(), new RemoteLicenseContract(), false);
                _connection.DriverInstance = _connection.Factory.CreateDriver(context);

                await _connection.Loader.LoadDriverFactory(dto, _connection.Factory, context);

                foreach (var driver in _connection.DriverStore.All())
                {
                    await driver.Start();
                }

                await _connection.MqttClient.SubscribeAsync(new TopicFilterBuilder()
                    .WithTopic($"{RemoteTopicConstants.ACTION_TOPIC_START}/{_connection.DriverInstance.Id}/+")
                    .WithExactlyOnceQoS()
                    .Build());

            }
            else if (MqttTopicFilterComparer.IsMatch(e.ApplicationMessage.Topic, $"{RemoteTopicConstants.DISPATCHER_TOPIC}/#"))
            {
                _connection.Dispatcher.MqttDispatch(e.ApplicationMessage.Topic, json);
            }
            else if (MqttTopicFilterComparer.IsMatch(e.ApplicationMessage.Topic, $"{RemoteTopicConstants.SLAVE_TOPIC}/*/reinit"))
            {
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
            }
        }
    }
}
