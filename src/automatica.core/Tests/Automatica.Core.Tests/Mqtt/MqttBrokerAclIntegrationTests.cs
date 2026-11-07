using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Automatica.Core.Base.Common;
using Automatica.Core.EF.Models;
using Automatica.Core.Runtime.Core;
using MQTTnet;
using MQTTnet.Formatter;
using MQTTnet.Protocol;
using Xunit;

namespace Automatica.Core.Tests.Mqtt
{
    /// <summary>
    /// Real broker + real MQTTnet clients: authentication, client id binding, key rehashing and the topic ACL
    /// </summary>
    public sealed class MqttBrokerAclIntegrationTests : IAsyncLifetime
    {
        private const string Sat1Key = "sat1-made-up-test-key-0001";
        private const string Sat2Key = "sat2-made-up-test-key-0002";

        private static readonly Guid P1 = Guid.Parse("10000000-0000-0000-0000-000000000001");
        private static readonly Guid P1Child = Guid.Parse("10000000-0000-0000-0000-0000000000c1");
        private static readonly Guid P2 = Guid.Parse("20000000-0000-0000-0000-000000000002");
        private static readonly Guid P2Child = Guid.Parse("20000000-0000-0000-0000-0000000000c2");
        private static readonly Guid Q1 = Guid.Parse("30000000-0000-0000-0000-000000000003");

        // PBKDF2 with the production iteration count is slow, hash once per class
        private static readonly string Sat1Hash = SatelliteClientKey.Hash(Sat1Key);
        private static readonly string Sat2Hash = SatelliteClientKey.Hash(Sat2Key);

        private readonly MqttBrokerHost _host = new();
        private readonly Slave _sat1 = new() { ObjId = Guid.NewGuid(), ClientId = "sat1", ClientKey = Sat1Hash };
        private readonly Slave _sat2 = new() { ObjId = Guid.NewGuid(), ClientId = "sat2", ClientKey = Sat2Hash };

        public async Task InitializeAsync()
        {
            _host.Store.Satellites[_sat1.ClientId] = _sat1;
            _host.Store.Satellites[_sat2.ClientId] = _sat2;
            _host.Registry.Register("sat1", P1, new[] { P1Child });
            _host.Registry.Register("sat1", P2, new[] { P2Child });
            _host.Registry.Register("sat2", Q1, Array.Empty<Guid>());
            await _host.StartAsync();
        }

        public async Task DisposeAsync() => await _host.DisposeAsync();

        private Task<MqttBrokerHost.Client> Satellite1() => _host.Connect("sat1", "sat1", Sat1Key);
        private Task<MqttBrokerHost.Client> Plugin(Guid node, string satellite = "sat1") =>
            _host.Connect(node.ToString(), satellite, _host.Credentials.GetPassword(satellite, node));

        // ---- authentication ----

        [Fact]
        public async Task Satellite_WithItsKey_Connects()
        {
            var sat = await Satellite1();

            Assert.Equal(MqttConnectReasonCode.Success, sat.Result);
            Assert.True(sat.Connected);
        }

        [Fact]
        public async Task Plugin_WithItsDerivedPassword_Connects()
        {
            var plugin = await Plugin(P1);

            Assert.Equal(MqttConnectReasonCode.Success, plugin.Result);
            Assert.True(plugin.Connected);
        }

        [Theory]
        [InlineData("sat1", "sat1", "wrong-key-wrong-key")]
        [InlineData("sat1", "sat1", Sat2Key)]
        [InlineData("sat2", "sat2", Sat1Key)]
        [InlineData("nobody", "nobody", Sat1Key)]
        public async Task Satellite_WithAWrongKey_IsRejected(string clientId, string user, string password)
        {
            var client = await _host.Connect(clientId, user, password);

            Assert.Equal(MqttConnectReasonCode.BadUserNameOrPassword, client.Result);
            Assert.False(client.Connected);
        }

        [Fact]
        public async Task Mqtt311Client_IsAuthenticatedTheSameWay()
        {
            var bad = await _host.Connect("sat1", "sat1", "wrong-key-wrong-key", MqttProtocolVersion.V311);
            var good = await _host.Connect("sat1", "sat1", Sat1Key, MqttProtocolVersion.V311);

            Assert.Equal(MqttConnectReasonCode.BadUserNameOrPassword, bad.Result);
            Assert.False(bad.Connected);
            Assert.Equal(MqttConnectReasonCode.Success, good.Result);
            Assert.True(good.Connected);
        }

        [Fact]
        public async Task SelfSatelliteRow_WithAnyKey_IsRejected()
        {
            _host.Store.Satellites["local"] = new Slave { ObjId = ServerInfo.SelfSlaveGuid, ClientId = "local", ClientKey = SatelliteClientKey.Hash("self-made-up-key-12345") };

            var client = await _host.Connect("local", "local", "self-made-up-key-12345");

            Assert.Equal(MqttConnectReasonCode.BadUserNameOrPassword, client.Result);
        }

        [Fact]
        public async Task PlaintextLegacyKey_IsAccepted_AndRehashedOnConnect()
        {
            const string legacy = "legacy-plaintext-made-up-key";
            var sat3 = new Slave { ObjId = Guid.NewGuid(), ClientId = "sat3", ClientKey = legacy };
            _host.Store.Satellites["sat3"] = sat3;

            var first = await _host.Connect("sat3", "sat3", legacy);

            Assert.Equal(MqttConnectReasonCode.Success, first.Result);
            var update = Assert.Single(_host.Store.Updates);
            Assert.Equal(sat3.ObjId, update.Item1);
            Assert.True(SatelliteClientKey.IsHashed(sat3.ClientKey));
            Assert.DoesNotContain(legacy, sat3.ClientKey);
            Assert.True(SatelliteClientKey.Verify(legacy, sat3.ClientKey, out var needsRehash));
            Assert.False(needsRehash);

            await first.Mqtt.DisconnectAsync();
            var second = await _host.Connect("sat3", "sat3", legacy);
            Assert.Equal(MqttConnectReasonCode.Success, second.Result);
            Assert.Single(_host.Store.Updates);
        }

        [Fact]
        public async Task PlaintextLegacyKey_WrongPassword_IsNotRehashed()
        {
            const string legacy = "legacy-plaintext-made-up-key";
            _host.Store.Satellites["sat3"] = new Slave { ObjId = Guid.NewGuid(), ClientId = "sat3", ClientKey = legacy };

            var client = await _host.Connect("sat3", "sat3", "legacy-plaintext-made-up-kez");

            Assert.Equal(MqttConnectReasonCode.BadUserNameOrPassword, client.Result);
            Assert.Empty(_host.Store.Updates);
            Assert.Equal(legacy, _host.Store.Satellites["sat3"].ClientKey);
        }

        [Fact]
        public async Task Plugin_WithThePasswordOfAnotherNode_IsRejected()
        {
            var client = await _host.Connect(P1.ToString(), "sat1", _host.Credentials.GetPassword("sat1", P2));

            Assert.Equal(MqttConnectReasonCode.BadUserNameOrPassword, client.Result);
        }

        [Fact]
        public async Task Plugin_OfAnotherSatellite_GetsClientIdNotValid()
        {
            // the node runs on sat2, the session claims sat1
            var client = await _host.Connect(Q1.ToString(), "sat1", Sat1Key);

            Assert.Equal(MqttConnectReasonCode.ClientIdentifierNotValid, client.Result);
            Assert.False(client.Connected);
        }

        [Fact]
        public async Task Plugin_WithTheSatelliteKey_ConnectsWhileLegacyCredentialsAreAllowed()
        {
            var client = await _host.Connect(P1.ToString(), "sat1", Sat1Key);

            Assert.Equal(MqttConnectReasonCode.Success, client.Result);
        }

        [Fact]
        public async Task Plugin_WithTheSatelliteKey_IsRejectedWhenLegacyCredentialsAreOff()
        {
            await using var strict = new MqttBrokerHost(allowLegacyPluginCredentials: false);
            strict.Store.Satellites["sat1"] = _sat1;
            strict.Registry.Register("sat1", P1, Array.Empty<Guid>());
            await strict.StartAsync();

            var legacy = await strict.Connect(P1.ToString(), "sat1", Sat1Key);
            var perPlugin = await strict.Connect(P1.ToString(), "sat1", strict.Credentials.GetPassword("sat1", P1));

            Assert.Equal(MqttConnectReasonCode.BadUserNameOrPassword, legacy.Result);
            Assert.Equal(MqttConnectReasonCode.Success, perPlugin.Result);
        }

        [Fact]
        public async Task RepeatedFailures_BlockTheAddress_UntilTheBlockTimeIsOver()
        {
            var now = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            await using var throttled = new MqttBrokerHost(throttle: new MqttConnectThrottle(3, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5), () => now));
            throttled.Store.Satellites["sat1"] = _sat1;
            await throttled.StartAsync();

            for (var i = 0; i < 3; i++)
            {
                Assert.Equal(MqttConnectReasonCode.BadUserNameOrPassword, (await throttled.Connect("sat1", "sat1", $"wrong-{i}-made-up-key")).Result);
            }

            var blocked = await throttled.Connect("sat1", "sat1", Sat1Key);
            Assert.Equal(MqttConnectReasonCode.ConnectionRateExceeded, blocked.Result);

            now = now.AddMinutes(6);
            var allowed = await throttled.Connect("sat1", "sat1", Sat1Key);
            Assert.Equal(MqttConnectReasonCode.Success, allowed.Result);
        }

        // ---- topic ACL: satellites ----

        [Fact]
        public async Task Satellite_PublishesDispatcherValuesOfItsOwnNodes_Only()
        {
            var sat = await Satellite1();

            Assert.Equal(MqttClientPublishReasonCode.Success, await sat.Publish($"dispatcher/NodeInstance/{P1Child}"));
            Assert.Equal(MqttClientPublishReasonCode.Success, await sat.Publish($"dispatcher/NodeInstance/{P2}"));
            Assert.Equal(MqttClientPublishReasonCode.NotAuthorized, await sat.Publish($"dispatcher/NodeInstance/{Q1}"));
            Assert.Equal(MqttClientPublishReasonCode.NotAuthorized, await sat.Publish($"dispatcher/RuleInstance/{P1Child}"));

            await MqttBrokerHost.WaitFor(() => _host.Processed.Count >= 2);
            Assert.Contains(("sat1", $"dispatcher/NodeInstance/{P1Child}"), _host.Processed);
            Assert.DoesNotContain(("sat1", $"dispatcher/NodeInstance/{Q1}"), _host.Processed);
        }

        [Theory]
        [InlineData("slave/sat1/action")]
        [InlineData("slave/sat2/action")]
        [InlineData("config/10000000-0000-0000-0000-000000000001")]
        [InlineData("action/10000000-0000-0000-0000-000000000001/Read")]
        [InlineData("watchdog/sat1/10000000-0000-0000-0000-000000000001")]
        public async Task Satellite_CannotPublishServerTopics(string topic)
        {
            var sat = await Satellite1();

            Assert.Equal(MqttClientPublishReasonCode.NotAuthorized, await sat.Publish(topic));
            await Task.Delay(30);
            Assert.DoesNotContain(("sat1", topic), _host.Processed);
        }

        [Theory]
        [InlineData("slave/sat1/action", true)]
        [InlineData("slave/sat1/actions", true)]
        [InlineData("watchdog/sat1/#", true)]
        [InlineData("config/10000000-0000-0000-0000-000000000001", true)]
        [InlineData("dispatcher/+/10000000-0000-0000-0000-0000000000c1", true)]
        [InlineData("dispatcher/NodeInstance/20000000-0000-0000-0000-0000000000c2", true)]
        [InlineData("config/30000000-0000-0000-0000-000000000003", false)]
        [InlineData("dispatcher/NodeInstance/30000000-0000-0000-0000-000000000003", false)]
        [InlineData("slave/sat2/#", false)]
        [InlineData("watchdog/+/#", false)]
        [InlineData("+/sat1/action", false)]
        [InlineData("#", false)]
        public async Task Satellite_Subscriptions_AreLimitedToItsOwnTopics(string filter, bool granted)
        {
            var sat = await Satellite1();

            var result = await sat.Subscribe(filter);

            Assert.Equal(granted ? MqttClientSubscribeResultCode.GrantedQoS1 : MqttClientSubscribeResultCode.NotAuthorized, result);
        }

        // ---- topic ACL: plugins ----

        [Theory]
        [InlineData("config/10000000-0000-0000-0000-000000000001", true)]
        [InlineData("slave/10000000-0000-0000-0000-000000000001/#", true)]
        [InlineData("action/10000000-0000-0000-0000-000000000001/#", true)]
        [InlineData("dispatcher/+/10000000-0000-0000-0000-0000000000c1", true)]
        [InlineData("dispatcher/NodeInstance/10000000-0000-0000-0000-000000000001", true)]
        // the satellite's action topic carries the per-plugin passwords of all its plugins
        [InlineData("slave/sat1/action", false)]
        [InlineData("slave/sat1/#", false)]
        [InlineData("config/20000000-0000-0000-0000-000000000002", false)]
        [InlineData("action/20000000-0000-0000-0000-000000000002/#", false)]
        [InlineData("dispatcher/NodeInstance/20000000-0000-0000-0000-0000000000c2", false)]
        [InlineData("watchdog/sat1/#", false)]
        [InlineData("#", false)]
        public async Task Plugin_Subscriptions_AreLimitedToItsOwnNode(string filter, bool granted)
        {
            var plugin = await Plugin(P1);

            var result = await plugin.Subscribe(filter);

            Assert.Equal(granted ? MqttClientSubscribeResultCode.GrantedQoS1 : MqttClientSubscribeResultCode.NotAuthorized, result);
        }

        [Theory]
        [InlineData("dispatcher/NodeInstance/10000000-0000-0000-0000-0000000000c1", true)]
        [InlineData("dispatcher/NodeInstance/10000000-0000-0000-0000-000000000001", true)]
        [InlineData("action/10000000-0000-0000-0000-000000000001/notifyLearnMode", true)]
        [InlineData("watchdog/sat1/10000000-0000-0000-0000-000000000001", true)]
        [InlineData("dispatcher/NodeInstance/20000000-0000-0000-0000-0000000000c2", false)]
        [InlineData("action/20000000-0000-0000-0000-000000000002/notifyLearnMode", false)]
        [InlineData("action/10000000-0000-0000-0000-000000000001/Read", false)]
        [InlineData("watchdog/sat1/20000000-0000-0000-0000-000000000002", false)]
        [InlineData("watchdog/sat2/10000000-0000-0000-0000-000000000001", false)]
        [InlineData("slave/sat1/action", false)]
        [InlineData("config/10000000-0000-0000-0000-000000000001", false)]
        public async Task Plugin_Publishes_OnlyToItsOwnNodeTopics(string topic, bool allowed)
        {
            var plugin = await Plugin(P1);

            var result = await plugin.Publish(topic);

            Assert.Equal(allowed ? MqttClientPublishReasonCode.Success : MqttClientPublishReasonCode.NotAuthorized, result);
        }

        [Fact]
        public async Task PluginValues_ReachTheSatellite_ButForgedOnesDoNot()
        {
            var sat = await Satellite1();
            Assert.Equal(MqttClientSubscribeResultCode.GrantedQoS1, await sat.Subscribe($"dispatcher/+/{P1Child}"));
            Assert.Equal(MqttClientSubscribeResultCode.GrantedQoS1, await sat.Subscribe($"dispatcher/+/{P2Child}"));
            var p1 = await Plugin(P1);

            await p1.Publish($"dispatcher/NodeInstance/{P2Child}", "{\"forged\":true}");
            await p1.Publish($"dispatcher/NodeInstance/{P1Child}", "{\"own\":true}");

            await MqttBrokerHost.WaitFor(() => sat.Received.Any());
            await Task.Delay(50);
            var topic = Assert.Single(sat.Received).Topic;
            Assert.Equal($"dispatcher/NodeInstance/{P1Child}", topic);
        }

        [Fact]
        public async Task ServerInjectedMessages_PassTheAcl()
        {
            var sat = await Satellite1();
            await sat.Subscribe("slave/sat1/action");

            await _host.Server.InjectApplicationMessage(new MQTTnet.Server.InjectedMqttApplicationMessage(
                MqttService.BuildMessage("slave/sat1/action", System.Text.Encoding.UTF8.GetBytes("{}"), MqttQualityOfServiceLevel.AtLeastOnce)));

            await MqttBrokerHost.WaitFor(() => sat.Received.Any());
            Assert.Equal("slave/sat1/action", Assert.Single(sat.Received).Topic);
        }

        [Fact]
        public async Task RegistryClear_RevokesNodeAccessOfConnectedSessions()
        {
            var sat = await Satellite1();
            Assert.Equal(MqttClientPublishReasonCode.Success, await sat.Publish($"dispatcher/NodeInstance/{P1Child}"));

            _host.Registry.Clear();

            Assert.Equal(MqttClientPublishReasonCode.NotAuthorized, await sat.Publish($"dispatcher/NodeInstance/{P1Child}"));
            Assert.Equal(MqttClientSubscribeResultCode.NotAuthorized, await sat.Subscribe($"config/{P1}"));
            // topics bound to the identity itself stay
            Assert.Equal(MqttClientSubscribeResultCode.GrantedQoS1, await sat.Subscribe("slave/sat1/action"));
        }

        [Fact]
        public async Task SecondSession_WithTheSameClientId_TakesOverOnlyWithValidCredentials()
        {
            var original = await Satellite1();

            var forged = await _host.Connect("sat1", "sat1", "wrong-key-wrong-key");
            await Task.Delay(50);
            Assert.Equal(MqttConnectReasonCode.BadUserNameOrPassword, forged.Result);
            Assert.True(original.Connected);

            // a plugin password does not open the satellite's own session
            var plugin = await _host.Connect("sat1", "sat1", _host.Credentials.GetPassword("sat1", P1));
            await Task.Delay(50);
            Assert.NotEqual(MqttConnectReasonCode.Success, plugin.Result);
            Assert.True(original.Connected);
        }

        // ---- bind address ----

        [Theory]
        [InlineData("any", "0.0.0.0")]
        [InlineData("ANY", "0.0.0.0")]
        [InlineData("localhost", "127.0.0.1")]
        [InlineData("loopback", "127.0.0.1")]
        [InlineData("192.168.10.20", "192.168.10.20")]
        [InlineData("not-an-address", "127.0.0.1")]
        public async Task ResolveBindAddress_HonoursTheConfiguration(string mode, string expected)
        {
            await using var host = new MqttBrokerHost(config: new System.Collections.Generic.Dictionary<string, string> { { MqttConnectionValidation.BindConfigKey, mode } });

            Assert.Equal(IPAddress.Parse(expected), host.Security.ResolveBindAddress());
        }

        [Fact]
        public void StartAsync_BindsBothListenersToTheResolvedAddress()
        {
            // the host started with server:mqttBind=localhost
            Assert.Equal(IPAddress.Loopback, _host.Options.DefaultEndpointOptions.BoundInterNetworkAddress);
            Assert.Equal(IPAddress.Loopback, _host.Options.TlsEndpointOptions.BoundInterNetworkAddress);
        }

        [Fact]
        public async Task RefreshBinding_RestartsTheBrokerOnTheNewAddress()
        {
            var before = await Satellite1();
            Assert.Equal(MqttConnectReasonCode.Success, before.Result);

            _host.Configuration[MqttConnectionValidation.BindConfigKey] = "127.0.0.2";
            await _host.Security.RefreshBindingAsync();

            Assert.Equal(IPAddress.Parse("127.0.0.2"), _host.Options.DefaultEndpointOptions.BoundInterNetworkAddress);
            Assert.True(_host.Server.IsStarted);
            await MqttBrokerHost.WaitFor(() => !before.Connected);
            Assert.False(before.Connected);

            var client = new MqttClientFactory().CreateMqttClient();
            var options = new MqttClientOptionsBuilder().WithTcpServer("127.0.0.2", _host.Port).WithClientId("sat1")
                .WithCredentials("sat1", Sat1Key).WithTimeout(TimeSpan.FromSeconds(5)).Build();
            var result = await client.ConnectAsync(options);
            Assert.Equal(MqttClientConnectResultCode.Success, result.ResultCode);
            await client.DisconnectAsync();
            client.Dispose();
        }
    }
}
