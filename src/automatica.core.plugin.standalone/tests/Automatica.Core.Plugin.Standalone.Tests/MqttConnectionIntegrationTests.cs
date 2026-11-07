using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.Base.Remote;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using Automatica.Core.Plugin.Standalone.Tests.Infrastructure;
using Microsoft.Extensions.Logging;
using Moq;
using MQTTnet.Client;
using Newtonsoft.Json;
using Xunit;

namespace Automatica.Core.Plugin.Standalone.Tests
{
    /// <summary>
    /// The plugin host's MqttConnection, MqttMessageHandlers and MqttDispatcher against a real MQTTnet broker
    /// (TLS listener on a loopback ephemeral port, certificate pinned like the satellite passes it)
    /// </summary>
    [Collection(ProcessStateCollection.Name)]
    public sealed class MqttConnectionIntegrationTests : IAsyncLifetime
    {
        private const string NodeId = "5a7d1c00-0000-4000-8000-00000000c0de";
        private static readonly DateTime FixedTime = new DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Utc);

        private PluginTestBroker _broker;
        private PluginEnvironment _environment;
        private PluginServices _services;
        private ListLogger _logger;
        private MqttConnection _connection;
        private CoreClient _core;
        private readonly ConcurrentQueue<int> _exitCodes = new ConcurrentQueue<int>();
        private Action<int> _previousExit;

        public async Task InitializeAsync()
        {
            _broker = await PluginTestBroker.StartAsync();
            _environment = new PluginEnvironment(_broker.Port, _broker.Fingerprint);
            _services = new PluginServices();
            _logger = new ListLogger();
            _connection = _services.CreateConnection(_logger, nodeId: NodeId);
            _previousExit = MqttMessageHandlers.ExitProcess;
            MqttMessageHandlers.ExitProcess = code => _exitCodes.Enqueue(code);
            _core = await _broker.ConnectCoreAsync();
        }

        public async Task DisposeAsync()
        {
            try
            {
                // stops the 2 minute config timer
                _connection.ConfigReceived();
            }
            catch (NullReferenceException)
            {
                // Start failed before the timer was created
            }
            if (_connection.MqttClient.IsConnected)
            {
                await _connection.MqttClient.DisconnectAsync();
            }
            _connection.MqttClient.Dispose();
            await _core.DisposeAsync();
            _environment.Dispose();
            MqttMessageHandlers.ExitProcess = _previousExit;
            await _broker.DisposeAsync();
        }

        private sealed class LocalNode : IDispatchable
        {
            public DispatchableSource Source => DispatchableSource.NodeInstance;
            public DispatchableType Type => DispatchableType.NodeInstance;
            public string Name => "local";
            public Guid Id { get; set; } = Guid.NewGuid();
            public bool IsRemanent => false;
        }

        private static string RemoteValue(Guid id, object value, string source = "core") => JsonConvert.SerializeObject(new RemoteDispatchValue
        {
            Source = source,
            Value = new DispatchValue(id, DispatchableType.NodeInstance, value, FixedTime, DispatchValueSource.Write)
        });

        private async Task StartAndLoadDriver()
        {
            Assert.True(await _connection.Start());
            await _core.PublishAsync($"config/{NodeId}", _services.ConfigJson());
            await Eventually.Assert(() => _broker.HasSubscription(NodeId, $"action/{_services.DriverId}/+"), "driver was not loaded");
        }

        // ---- connection

        [Fact]
        public async Task Start_PinnedTls_ConnectsWithThePluginCredentialsAndWill()
        {
            Assert.True(await _connection.Start());

            var connect = Assert.Single(_broker.Connects, c => c.ClientId == NodeId);
            Assert.Equal(TestKeys.SatelliteUser, connect.Username);
            Assert.Equal(TestKeys.PluginPassword, connect.Password);
            Assert.Equal($"watchdog/{TestKeys.SatelliteUser}/{NodeId}", connect.WillTopic);
            Assert.True(_logger.Contains(LogLevel.Information, "Using TLS"));
        }

        [Fact]
        public async Task Start_SubscribesOnlyToItsOwnConfigAndReinitTopics()
        {
            Assert.True(await _connection.Start());

            var topics = _broker.Subscriptions.Where(s => s.ClientId == NodeId).Select(s => s.Topic).OrderBy(t => t).ToArray();
            Assert.Equal(new[] { $"config/{NodeId}", $"slave/{NodeId}/reinit" }, topics);
            Assert.DoesNotContain(topics, t => t.Contains('#') || t.Contains('+'));
        }

        [Fact]
        public async Task Start_FingerprintInColonFormat_Connects()
        {
            var colon = String.Join(":", Enumerable.Range(0, 32).Select(i => _broker.Fingerprint.Substring(i * 2, 2).ToUpperInvariant()));
            using (new PluginEnvironment(_broker.Port, colon))
            {
                Assert.True(await _connection.Start());
            }
        }

        [Fact]
        public async Task Start_WrongPinnedFingerprint_DoesNotConnectOrSendThePassword()
        {
            using (new PluginEnvironment(_broker.Port, new string('0', 64)))
            {
                Assert.False(await _connection.Start());
            }

            Assert.DoesNotContain(_broker.Connects, c => c.ClientId == NodeId);
            Assert.True(_logger.Contains(LogLevel.Error, "does not match the pinned fingerprint"));
            Assert.False(_connection.MqttClient.IsConnected);
        }

        // ---- config

        [Fact]
        public async Task Config_LoadsAndStartsTheDriverAndSubscribesToItsActions()
        {
            Assert.True(await _connection.Start());
            var nodeId = Guid.NewGuid();

            await _core.PublishAsync($"config/{NodeId}", _services.ConfigJson(nodeId));

            await Eventually.Assert(() => _broker.HasSubscription(NodeId, $"action/{_services.DriverId}/+"), "no subscription to the driver's actions");
            _services.Factory.Verify(f => f.CreateDriver(It.IsAny<IDriverContext>()), Times.Once);
            _services.Driver.Verify(d => d.Start(It.IsAny<CancellationToken>()), Times.Once);
            var loaded = Assert.Single(_services.LoadedNodes);
            Assert.Equal(nodeId, loaded.ObjId);
            Assert.Equal(_services.DriverGuid, loaded.This2NodeTemplate);
            Assert.Same(_services.Driver.Object, _connection.DriverInstance);

            var context = Assert.Single(_services.CreatedContexts);
            Assert.Same(_connection.Dispatcher, context.Dispatcher);
            Assert.Equal(nodeId, context.NodeInstance.ObjId);
        }

        [Fact]
        public async Task Config_SecondConfig_IsIgnored()
        {
            await StartAndLoadDriver();

            await _core.PublishAsync($"config/{NodeId}", _services.ConfigJson());

            await Eventually.Assert(() => _logger.Contains(LogLevel.Warning, "Instance already running"), "second config was not rejected");
            _services.Factory.Verify(f => f.CreateDriver(It.IsAny<IDriverContext>()), Times.Once);
        }

        [Fact]
        public async Task Config_ForAnotherNode_IsNotReceived()
        {
            Assert.True(await _connection.Start());

            await _core.PublishAsync($"config/{Guid.NewGuid()}", _services.ConfigJson());
            // sentinel on a topic of this node, sent after the foreign config
            await _core.PublishAsync($"slave/{NodeId}/reinit", "{}");

            await Eventually.Assert(() => _exitCodes.Count == 1, "sentinel was not received");
            _services.Factory.Verify(f => f.CreateDriver(It.IsAny<IDriverContext>()), Times.Never);
        }

        // ---- actions

        [Theory]
        [InlineData("StartLearnMode")]
        [InlineData("StopLearnMode")]
        [InlineData("Read")]
        public async Task Action_FromTheCore_ReachesTheNode(string action)
        {
            await StartAndLoadDriver();
            var id = Guid.NewGuid();
            var node = _services.AddNode(id);
            var called = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            node.Setup(n => n.EnableLearnMode(It.IsAny<CancellationToken>())).Callback(() => called.TrySetResult("StartLearnMode")).ReturnsAsync(true);
            node.Setup(n => n.DisableLearnMode(It.IsAny<CancellationToken>())).Callback(() => called.TrySetResult("StopLearnMode")).ReturnsAsync(true);
            node.Setup(n => n.Read(It.IsAny<CancellationToken>())).Callback(() => called.TrySetResult("Read")).ReturnsAsync(true);

            await _core.PublishAsync($"action/{_services.DriverId}/{action}", JsonConvert.SerializeObject(new { Id = id }));

            Assert.Equal(action, await called.Task.WaitAsync(TimeSpan.FromSeconds(10)));
        }

        [Fact]
        public async Task Send_BeforeTheDriverIsLoaded_ReturnsFalse()
        {
            Assert.True(await _connection.Start());

            Assert.False(await _connection.Send("notifyLearnMode", new { Name = "x" }));
        }

        [Fact]
        public async Task Send_AfterTheDriverIsLoaded_PublishesOnTheDriversActionTopic()
        {
            await StartAndLoadDriver();
            await _core.SubscribeAsync($"action/{_services.DriverId}/#");

            Assert.True(await _connection.Send("notifyLearnMode", new { Name = "learned" }));

            await Eventually.Assert(() => _core.Received.Any(r => r.Topic == $"action/{_services.DriverId}/notifyLearnMode"), "learn mode notification not received");
            Assert.Contains("learned", _core.Received.First(r => r.Topic.EndsWith("notifyLearnMode")).Payload);
        }

        // ---- re-init

        [Fact]
        public async Task Reinit_WithAPayload_ExitsTheProcess()
        {
            Assert.True(await _connection.Start());

            await _core.PublishAsync($"slave/{NodeId}/reinit", "{}");

            await Eventually.Assert(() => _exitCodes.Count == 1, "re-init did not exit");
            Assert.Equal(0, _exitCodes.Single());
        }

        [Fact(Skip = "BUG: MqttMessageHandlers.cs:46 drops messages with an empty payload (MQTTnet 3 delivers them with Payload == null) before the re-init check, and MqttService.ReInit sends slave/<node>/reinit with an empty payload, so plugins never re-init")]
        public async Task Reinit_EmptyPayloadAsTheCoreSendsIt_ExitsTheProcess()
        {
            Assert.True(await _connection.Start());

            // MqttService.ReInit: Encoding.UTF8.GetBytes("")
            await _core.PublishAsync($"slave/{NodeId}/reinit", "");

            await Eventually.Assert(() => _exitCodes.Count == 1, "re-init did not exit");
        }

        // ---- dispatcher

        [Fact]
        public async Task Dispatch_RemoteValue_ReachesTheRegisteredCallback()
        {
            Assert.True(await _connection.Start());
            var id = Guid.NewGuid();
            var got = new TaskCompletionSource<(IDispatchable, DispatchValue)>(TaskCreationOptions.RunContinuationsAsynchronously);
            await _connection.Dispatcher.RegisterDispatch(DispatchableType.NodeInstance, id, (d, v) => got.TrySetResult((d, v)));

            await _core.PublishAsync($"dispatcher/NodeInstance/{id}", RemoteValue(id, 42));

            var (from, value) = await got.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.Equal(id, from.Id);
            Assert.Equal(DispatchableSource.Remote, from.Source);
            Assert.Equal(42L, Convert.ToInt64(value.Value));
            Assert.Equal(FixedTime, value.Timestamp.ToUniversalTime());
            Assert.True(_broker.HasSubscription(NodeId, $"dispatcher/NodeInstance/{id}"));
        }

        [Fact(Skip = "BUG: Automatica.Core.Base/IO/IDispatcher.cs:44 DispatchValue.ValueSource has no setter, so Newtonsoft drops it: every remote value arrives as Read")]
        public async Task Dispatch_RemoteValue_KeepsItsValueSource()
        {
            Assert.True(await _connection.Start());
            var id = Guid.NewGuid();
            var got = new TaskCompletionSource<DispatchValue>(TaskCreationOptions.RunContinuationsAsynchronously);
            await _connection.Dispatcher.RegisterDispatch(DispatchableType.NodeInstance, id, (_, v) => got.TrySetResult(v));

            await _core.PublishAsync($"dispatcher/NodeInstance/{id}", RemoteValue(id, 42));

            Assert.Equal(DispatchValueSource.Write, (await got.Task.WaitAsync(TimeSpan.FromSeconds(10))).ValueSource);
        }

        [Fact]
        public async Task Dispatch_RemoteValue_IsNotPublishedBackByThePlugin()
        {
            Assert.True(await _connection.Start());
            var id = Guid.NewGuid();
            var calls = 0;
            await _connection.Dispatcher.RegisterDispatch(DispatchableType.NodeInstance, id, (_, _) => Interlocked.Increment(ref calls));

            await _core.PublishAsync($"dispatcher/NodeInstance/{id}", RemoteValue(id, 1));
            await Eventually.Assert(() => Volatile.Read(ref calls) == 1, "remote value not dispatched");

            Assert.DoesNotContain(_broker.Published, p => p.ClientId == NodeId && p.Topic.StartsWith("dispatcher/"));
        }

        [Fact]
        public async Task Dispatch_LocalValue_IsPublishedRetainedWithThePluginAsSource()
        {
            Assert.True(await _connection.Start());
            var node = new LocalNode();

            await _connection.Dispatcher.DispatchValue(node, new DispatchValue(node.Id, node.Type, 21, FixedTime, DispatchValueSource.Read));
            // subscribing afterwards: the value is retained
            await _core.SubscribeAsync("dispatcher/#");

            await Eventually.Assert(() => _core.Received.Any(r => r.Topic == $"dispatcher/NodeInstance/{node.Id}"), "dispatched value not received by the core");
            var received = _core.Received.First(r => r.Topic == $"dispatcher/NodeInstance/{node.Id}");
            Assert.True(received.Retain);
            var payload = JsonConvert.DeserializeObject<RemoteDispatchValue>(received.Payload);
            Assert.Equal(NodeId, payload.Source);
            Assert.Equal(node.Id, payload.Value.Id);
            Assert.Equal(21L, Convert.ToInt64(payload.Value.Value));
        }

        [Fact]
        public async Task Dispatch_OwnValueComingBack_IsNotDispatchedTwice()
        {
            Assert.True(await _connection.Start());
            var node = new LocalNode();
            var calls = 0;
            await _connection.Dispatcher.RegisterDispatch(node.Type, node.Id, (_, _) => Interlocked.Increment(ref calls));

            await _connection.Dispatcher.DispatchValue(node, new DispatchValue(node.Id, node.Type, "local", FixedTime, DispatchValueSource.Read));
            Assert.Equal(1, Volatile.Read(ref calls));

            // the broker sends the plugin's own (retained) value back; sentinels from the core come after it
            await _core.PublishAsync($"dispatcher/NodeInstance/{node.Id}", RemoteValue(node.Id, "s1"));
            await Eventually.Assert(() => Volatile.Read(ref calls) >= 2, "first sentinel not received");
            await _core.PublishAsync($"dispatcher/NodeInstance/{node.Id}", RemoteValue(node.Id, "s2"));
            await Eventually.Assert(() => Volatile.Read(ref calls) >= 3, "second sentinel not received");

            Assert.Equal(3, Volatile.Read(ref calls));
        }

        [Fact]
        public async Task Dispatch_ValueWithThePluginsSourceFromOutside_IsIgnored()
        {
            Assert.True(await _connection.Start());
            var id = Guid.NewGuid();
            var values = new ConcurrentQueue<object>();
            await _connection.Dispatcher.RegisterDispatch(DispatchableType.NodeInstance, id, (_, v) => values.Enqueue(v.Value));

            await _core.PublishAsync($"dispatcher/NodeInstance/{id}", RemoteValue(id, "spoofed", source: NodeId));
            await _core.PublishAsync($"dispatcher/NodeInstance/{id}", RemoteValue(id, "real"));

            await Eventually.Assert(() => values.Count >= 1, "real value not dispatched");
            Assert.Equal(new object[] { "real" }, values.ToArray());
        }

        [Theory]
        [InlineData("")]
        [InlineData("{\"Value\":null,\"Source\":\"core\"}")]
        public async Task Dispatch_EmptyPayloadOrNullValue_DoesNotBreakTheConnection(string payload)
        {
            await DispatchBadPayloadThenGood(payload);
        }

        [Theory(Skip = "BUG: MqttMessageHandlers.cs:108-110 - a 'null' or invalid JSON payload on a dispatcher topic throws out of the handler; the following message did not reach the plugin within 10 s (the throwing message is not acked)")]
        [InlineData("{ not json")]
        [InlineData("null")]
        public async Task Dispatch_InvalidJson_DoesNotBreakTheConnection(string payload)
        {
            await DispatchBadPayloadThenGood(payload);
        }

        private async Task DispatchBadPayloadThenGood(string payload)
        {
            Assert.True(await _connection.Start());
            var id = Guid.NewGuid();
            var values = new ConcurrentQueue<object>();
            await _connection.Dispatcher.RegisterDispatch(DispatchableType.NodeInstance, id, (_, v) => values.Enqueue(v?.Value));

            await _core.PublishAsync($"dispatcher/NodeInstance/{id}", payload);
            await _core.PublishAsync($"dispatcher/NodeInstance/{id}", RemoteValue(id, "after"));

            await Eventually.Assert(() => values.Contains("after"), "value after the bad payload was not dispatched");
            Assert.True(_connection.MqttClient.IsConnected);
        }

        [Fact]
        public async Task Dispatch_UnregisteredNode_IsNotReceived()
        {
            Assert.True(await _connection.Start());
            var registered = Guid.NewGuid();
            var values = new ConcurrentQueue<Guid>();
            await _connection.Dispatcher.RegisterDispatch(DispatchableType.NodeInstance, registered, (d, _) => values.Enqueue(d.Id));

            await _core.PublishAsync($"dispatcher/NodeInstance/{Guid.NewGuid()}", RemoteValue(Guid.NewGuid(), 1));
            await _core.PublishAsync($"dispatcher/NodeInstance/{registered}", RemoteValue(registered, 2));

            await Eventually.Assert(() => values.Count >= 1, "registered value not dispatched");
            Assert.Equal(new[] { registered }, values.ToArray());
            Assert.Null(_connection.Dispatcher.GetValue(DispatchableType.NodeInstance, Guid.Empty));
        }
    }
}
