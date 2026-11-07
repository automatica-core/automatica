using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.EF.Models;
using Automatica.Core.Runtime.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using MQTTnet;
using MQTTnet.Formatter;
using MQTTnet.Protocol;
using MQTTnet.Server;
using Xunit;

namespace Automatica.Core.Tests.Mqtt
{
    /// <summary>
    /// Runs the real MQTTnet broker with the core's connection validation and topic ACL
    /// </summary>
    public sealed class MqttBrokerSecurityIntegrationTests : IAsyncLifetime
    {
        private const string SatKey = "integration-satellite-key-1";
        private readonly Guid _node = Guid.NewGuid();
        private readonly Guid _otherNode = Guid.NewGuid();

        private readonly FakeMqttSatelliteStore _store = new();
        private readonly MqttNodeRegistry _registry = new();
        private readonly MqttPluginCredentials _credentials = new(RandomNumberGenerator.GetBytes(32));
        private readonly ConcurrentQueue<string> _serverReceived = new();
        private readonly ConcurrentDictionary<string, MqttConnectReasonCode> _connectResults = new();
        private readonly int _port = FreePort();

        private MqttServer _server;
        private MqttConnectionValidation _security;
        private MqttServerOptions _options;

        public async Task InitializeAsync()
        {
            _store.Satellites["sat1"] = new Slave { ObjId = Guid.NewGuid(), ClientId = "sat1", ClientKey = SatelliteClientKey.Hash(SatKey) };
            _store.Satellites["sat2"] = new Slave { ObjId = Guid.NewGuid(), ClientId = "sat2", ClientKey = SatelliteClientKey.Hash("integration-satellite-key-2") };
            _registry.Register("sat1", _node, Array.Empty<Guid>());
            _registry.Register("sat1", _otherNode, Array.Empty<Guid>());

            _options = new MqttServerOptionsBuilder()
                .WithDefaultEndpoint()
                .WithDefaultEndpointPort(_port)
                .Build();
            _server = new MqttServerFactory().CreateMqttServer(_options);

            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string> { { MqttConnectionValidation.BindConfigKey, "localhost" } }).Build();
            var authenticator = new MqttBrokerAuthenticator(_store, _registry, _credentials, new MqttConnectThrottle(), true, NullLogger.Instance);
            _security = new MqttConnectionValidation(_server, _options, config, _registry, authenticator, NullLogger.Instance);
            await _security.StartAsync(CancellationToken.None);

            // attached after the validation, sees its result
            _server.ValidatingConnectionAsync += e =>
            {
                _connectResults[$"{e.ClientId}|{e.UserName}|{e.Password}"] = e.ReasonCode;
                return Task.CompletedTask;
            };

            _server.InterceptingPublishAsync += e =>
            {
                if (e.ProcessPublish && !String.IsNullOrEmpty(e.ClientId))
                {
                    _serverReceived.Enqueue(e.ApplicationMessage.Topic);
                }
                return Task.CompletedTask;
            };

            // like MqttService: the config is sent when the plugin subscribes, not retained
            _server.ClientSubscribedTopicAsync += async e =>
            {
                if (e.TopicFilter.Topic.StartsWith("config/"))
                {
                    await _server.InjectApplicationMessage(new InjectedMqttApplicationMessage(
                        MqttService.BuildMessage(e.TopicFilter.Topic, Encoding.UTF8.GetBytes("{\"cfg\":1}"), MqttQualityOfServiceLevel.ExactlyOnce)), CancellationToken.None);
                }
            };

            await _server.StartAsync();
        }

        public async Task DisposeAsync()
        {
            await _security.StopAsync(CancellationToken.None);
            await _server.StopAsync(new MqttServerStopOptions());
            _server.Dispose();
        }

        private static int FreePort()
        {
            var l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();
            var port = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();
            return port;
        }

        private async Task<(IMqttClient Client, MqttConnectReasonCode Result, ConcurrentQueue<string> Received)> Connect(string clientId, string user, string password,
            string willTopic = null)
        {
            var client = new MqttClientFactory().CreateMqttClient();
            var received = new ConcurrentQueue<string>();
            client.ApplicationMessageReceivedAsync += e =>
            {
                received.Enqueue(e.ApplicationMessage.Topic);
                return Task.CompletedTask;
            };

            var builder = new MqttClientOptionsBuilder()
                .WithTcpServer("127.0.0.1", _port)
                .WithClientId(clientId)
                .WithCredentials(user, password)
                .WithProtocolVersion(MqttProtocolVersion.V500)
                .WithCleanSession();
            if (willTopic != null)
            {
                builder = builder.WithWillTopic(willTopic);
            }

            try
            {
                await client.ConnectAsync(builder.Build());
            }
            catch (MQTTnet.Adapter.MqttConnectingFailedException)
            {
                // the reason code is taken from the server side below
            }
            return (client, _connectResults.TryGetValue($"{clientId}|{user}|{password}", out var code) ? code : MqttConnectReasonCode.UnspecifiedError, received);
        }

        private static async Task WaitFor(Func<bool> condition, int timeoutMs = 3000)
        {
            var until = DateTime.UtcNow.AddMilliseconds(timeoutMs);
            while (!condition() && DateTime.UtcNow < until)
            {
                await Task.Delay(20);
            }
        }

        [Fact]
        public async Task Satellite_CanSubscribeOwnTopics_ButNotOthers()
        {
            var (sat, result, _) = await Connect("sat1", "sat1", SatKey);
            Assert.Equal(MqttConnectReasonCode.Success, result);

            var own = await sat.SubscribeAsync("slave/sat1/action", MqttQualityOfServiceLevel.ExactlyOnce);
            Assert.Equal(MqttClientSubscribeResultCode.GrantedQoS2, own.Items.First().ResultCode);

            foreach (var topic in new[] { "#", "slave/sat2/action", "config/#", "dispatcher/#" })
            {
                var denied = await sat.SubscribeAsync(topic);
                Assert.Equal(MqttClientSubscribeResultCode.NotAuthorized, denied.Items.First().ResultCode);
            }
            await sat.DisconnectAsync();
        }

        [Fact]
        public async Task ForgedServerTopics_AreNotDelivered()
        {
            var (victim, _, victimReceived) = await Connect("sat2", "sat2", "integration-satellite-key-2");
            await victim.SubscribeAsync("slave/sat2/action", MqttQualityOfServiceLevel.AtLeastOnce);

            var (attacker, result, _) = await Connect("sat1", "sat1", SatKey);
            Assert.Equal(MqttConnectReasonCode.Success, result);

            var publish = await attacker.PublishStringAsync("slave/sat2/action", "{\"Action\":0,\"ImageName\":\"evil/img\"}", MqttQualityOfServiceLevel.AtLeastOnce);
            Assert.Equal(MqttClientPublishReasonCode.NotAuthorized, publish.ReasonCode);

            var dispatch = await attacker.PublishStringAsync($"dispatcher/NodeInstance/{Guid.NewGuid()}", "{}", MqttQualityOfServiceLevel.AtLeastOnce);
            Assert.Equal(MqttClientPublishReasonCode.NotAuthorized, dispatch.ReasonCode);

            await Task.Delay(200);
            Assert.Empty(victimReceived);
            Assert.DoesNotContain("slave/sat2/action", _serverReceived);

            await attacker.DisconnectAsync();
            await victim.DisconnectAsync();
        }

        [Fact]
        public async Task ClientId_OfAnotherSatellite_CannotBeTakenOver()
        {
            var (victim, _, _) = await Connect("sat2", "sat2", "integration-satellite-key-2");

            var (_, result, _) = await Connect("sat2", "sat1", SatKey);

            Assert.Equal(MqttConnectReasonCode.ClientIdentifierNotValid, result);
            Assert.True(victim.IsConnected);
            await victim.DisconnectAsync();
        }

        [Fact]
        public async Task Plugin_GetsItsConfigOnSubscribe_AndCannotReadOtherConfigs()
        {
            var (plugin, result, received) = await Connect(_node.ToString(), "sat1", _credentials.GetPassword("sat1", _node));
            Assert.Equal(MqttConnectReasonCode.Success, result);

            var own = await plugin.SubscribeAsync($"config/{_node}", MqttQualityOfServiceLevel.ExactlyOnce);
            Assert.Equal(MqttClientSubscribeResultCode.GrantedQoS2, own.Items.First().ResultCode);

            var other = await plugin.SubscribeAsync($"config/{_otherNode}", MqttQualityOfServiceLevel.ExactlyOnce);
            Assert.Equal(MqttClientSubscribeResultCode.NotAuthorized, other.Items.First().ResultCode);

            await WaitFor(() => received.Contains($"config/{_node}"));
            Assert.Contains($"config/{_node}", received);
            Assert.DoesNotContain($"config/{_otherNode}", received);
            await plugin.DisconnectAsync();
        }

        [Fact]
        public async Task Plugin_WillMessage_ReachesItsSatellite()
        {
            var (sat, _, satReceived) = await Connect("sat1", "sat1", SatKey);
            await sat.SubscribeAsync("watchdog/sat1/#", MqttQualityOfServiceLevel.AtLeastOnce);

            var (plugin, result, _) = await Connect(_node.ToString(), "sat1", _credentials.GetPassword("sat1", _node), $"watchdog/sat1/{_node}");
            Assert.Equal(MqttConnectReasonCode.Success, result);

            // an unclean disconnect sends the will message
            plugin.Dispose();

            await WaitFor(() => satReceived.Contains($"watchdog/sat1/{_node}"), 5000);
            Assert.Contains($"watchdog/sat1/{_node}", satReceived);
            await sat.DisconnectAsync();
        }

        [Fact]
        public async Task WrongPassword_IsRejected()
        {
            var (_, result, _) = await Connect("sat1", "sat1", "wrong");
            Assert.Equal(MqttConnectReasonCode.BadUserNameOrPassword, result);

            var (_, pluginResult, _) = await Connect(_node.ToString(), "sat1", _credentials.GetPassword("sat1", _otherNode));
            Assert.Equal(MqttConnectReasonCode.BadUserNameOrPassword, pluginResult);
        }
    }
}
