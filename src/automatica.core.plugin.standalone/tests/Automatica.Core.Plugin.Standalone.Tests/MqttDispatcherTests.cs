using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.Base.Remote;
using Automatica.Core.Plugin.Standalone.Dispatcher;
using Automatica.Core.Plugin.Standalone.Tests.Infrastructure;
using Moq;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using MQTTnet.Client.Publishing;
using MQTTnet.Client.Subscribing;
using MQTTnet.Client.Unsubscribing;
using MQTTnet.Protocol;
using Newtonsoft.Json;
using Xunit;

namespace Automatica.Core.Plugin.Standalone.Tests
{
    public class MqttDispatcherTests
    {
        private const string ClientId = "11111111-2222-3333-4444-555555555555";
        private static readonly DateTime FixedTime = new DateTime(2024, 5, 6, 7, 8, 9, DateTimeKind.Utc);

        private readonly Mock<IMqttClient> _client = new Mock<IMqttClient>();
        private readonly List<MqttApplicationMessage> _published = new List<MqttApplicationMessage>();
        private readonly List<MqttClientSubscribeOptions> _subscribed = new List<MqttClientSubscribeOptions>();
        private readonly List<MqttClientUnsubscribeOptions> _unsubscribed = new List<MqttClientUnsubscribeOptions>();
        private readonly MqttDispatcher _dispatcher;

        public MqttDispatcherTests()
        {
            var options = new Mock<IMqttClientOptions>();
            options.SetupGet(o => o.ClientId).Returns(ClientId);
            _client.SetupGet(c => c.Options).Returns(options.Object);
            _client.Setup(c => c.PublishAsync(It.IsAny<MqttApplicationMessage>(), It.IsAny<CancellationToken>()))
                .Callback<MqttApplicationMessage, CancellationToken>((m, _) => { lock (_published) { _published.Add(m); } })
                .ReturnsAsync(new MqttClientPublishResult());
            _client.Setup(c => c.SubscribeAsync(It.IsAny<MqttClientSubscribeOptions>(), It.IsAny<CancellationToken>()))
                .Callback<MqttClientSubscribeOptions, CancellationToken>((o, _) => _subscribed.Add(o))
                .ReturnsAsync(new MqttClientSubscribeResult());
            _client.Setup(c => c.UnsubscribeAsync(It.IsAny<MqttClientUnsubscribeOptions>(), It.IsAny<CancellationToken>()))
                .Callback<MqttClientUnsubscribeOptions, CancellationToken>((o, _) => _unsubscribed.Add(o))
                .ReturnsAsync(new MqttClientUnsubscribeResult());

            _dispatcher = new MqttDispatcher(_client.Object);
        }

        private sealed class Dispatchable : IDispatchable
        {
            public DispatchableSource Source { get; set; } = DispatchableSource.NodeInstance;
            public DispatchableType Type { get; set; } = DispatchableType.NodeInstance;
            public string Name { get; set; } = "node";
            public Guid Id { get; set; } = Guid.NewGuid();
            public bool IsRemanent => false;
        }

        private static DispatchValue Value(IDispatchable d, object value) => new DispatchValue(d.Id, d.Type, value, FixedTime, DispatchValueSource.Read);

        [Fact]
        public async Task RegisterDispatch_SubscribesToTheNodesDispatcherTopic()
        {
            var id = Guid.NewGuid();

            await _dispatcher.RegisterDispatch(DispatchableType.NodeInstance, id, (_, _) => { });

            var filter = Assert.Single(Assert.Single(_subscribed).TopicFilters);
            Assert.Equal($"dispatcher/NodeInstance/{id}", filter.Topic);
            Assert.Equal(MqttQualityOfServiceLevel.AtLeastOnce, filter.QualityOfServiceLevel);
        }

        [Theory]
        [InlineData(DispatchableType.RuleInstance)]
        [InlineData(DispatchableType.Visualization)]
        [InlineData(DispatchableType.Control)]
        public async Task RegisterDispatch_TopicUsesTheTypeName(DispatchableType type)
        {
            var id = Guid.NewGuid();

            await _dispatcher.RegisterDispatch(type, id, (_, _) => { });

            Assert.Equal($"dispatcher/{type}/{id}", Assert.Single(Assert.Single(_subscribed).TopicFilters).Topic);
        }

        [Fact]
        public async Task DispatchValue_LocalSource_CallsSubscribersAndPublishesRetained()
        {
            var node = new Dispatchable();
            var received = new List<DispatchValue>();
            await _dispatcher.RegisterDispatch(node.Type, node.Id, (_, v) => received.Add(v));
            var value = Value(node, 42);

            await _dispatcher.DispatchValue(node, value);

            Assert.Same(value, Assert.Single(received));
            var message = Assert.Single(_published);
            Assert.Equal($"dispatcher/NodeInstance/{node.Id}", message.Topic);
            Assert.True(message.Retain);
            Assert.Equal(MqttQualityOfServiceLevel.AtLeastOnce, message.QualityOfServiceLevel);
        }

        [Fact]
        public async Task DispatchValue_PublishedPayload_RoundTripsAsRemoteDispatchValue()
        {
            var node = new Dispatchable();

            await _dispatcher.DispatchValue(node, Value(node, 21.5));

            var payload = JsonConvert.DeserializeObject<RemoteDispatchValue>(Encoding.UTF8.GetString(Assert.Single(_published).Payload));
            Assert.Equal(ClientId, payload.Source);
            Assert.Equal(node.Id, payload.Value.Id);
            Assert.Equal(DispatchableType.NodeInstance, payload.Value.Type);
            Assert.Equal(21.5, Convert.ToDouble(payload.Value.Value));
            Assert.Equal(DispatchValueSource.Read, payload.Value.ValueSource);
            Assert.Equal(FixedTime, payload.Value.Timestamp.ToUniversalTime());
        }

        [Fact]
        public async Task DispatchValue_RemoteSource_CallsSubscribersButIsNotPublishedBack()
        {
            var node = new Dispatchable { Source = DispatchableSource.Remote };
            var calls = 0;
            await _dispatcher.RegisterDispatch(node.Type, node.Id, (_, _) => calls++);

            await _dispatcher.DispatchValue(node, Value(node, true));

            Assert.Equal(1, calls);
            Assert.Empty(_published);
            Assert.True((bool)_dispatcher.GetValue(node.Type, node.Id).Value);
        }

        [Fact]
        public async Task DispatchValue_SameValueInstanceTwice_IsPublishedOnce()
        {
            var node = new Dispatchable();
            var value = Value(node, 1);

            await _dispatcher.DispatchValue(node, value);
            await _dispatcher.DispatchValue(node, value);

            Assert.Single(_published);
        }

        [Fact]
        public async Task DispatchValue_NewValue_IsPublishedAgainAndStored()
        {
            var node = new Dispatchable();

            await _dispatcher.DispatchValue(node, Value(node, 1));
            await _dispatcher.DispatchValue(node, Value(node, 2));

            Assert.Equal(2, _published.Count);
            Assert.Equal(2, _dispatcher.GetValue(node.Type, node.Id).Value);
        }

        [Fact]
        public async Task DispatchValue_ObjectOverload_BuildsTheValue()
        {
            var node = new Dispatchable();

            await _dispatcher.DispatchValue(node, "on", DispatchValueSource.User);

            var stored = _dispatcher.GetValue(node.Type, node.Id);
            Assert.Equal("on", stored.Value);
            Assert.Equal(node.Id, stored.Id);
            Assert.Equal(DispatchValueSource.User, stored.ValueSource);
        }

        [Fact]
        public async Task DispatchValue_FailingSubscriber_DoesNotStopTheOthers()
        {
            var node = new Dispatchable();
            var calls = 0;
            await _dispatcher.RegisterDispatch(node.Type, node.Id, (_, _) => throw new InvalidOperationException("subscriber failure"));
            await _dispatcher.RegisterDispatch(node.Type, node.Id, (_, _) => calls++);

            await _dispatcher.DispatchValue(node, Value(node, 5));

            Assert.Equal(1, calls);
            Assert.Single(_published);
        }

        [Fact]
        public async Task DispatchValue_OtherNode_DoesNotCallTheSubscriber()
        {
            var node = new Dispatchable();
            var other = new Dispatchable();
            var calls = 0;
            await _dispatcher.RegisterDispatch(node.Type, node.Id, (_, _) => calls++);

            await _dispatcher.DispatchValue(other, Value(other, 5));

            Assert.Equal(0, calls);
        }

        [Fact]
        public async Task DispatchValue_SameIdOtherType_DoesNotCallTheSubscriber()
        {
            var id = Guid.NewGuid();
            var calls = 0;
            await _dispatcher.RegisterDispatch(DispatchableType.NodeInstance, id, (_, _) => calls++);
            var rule = new Dispatchable { Id = id, Type = DispatchableType.RuleInstance };

            await _dispatcher.DispatchValue(rule, Value(rule, 5));

            Assert.Equal(0, calls);
            Assert.Null(_dispatcher.GetValue(DispatchableType.NodeInstance, id));
            Assert.NotNull(_dispatcher.GetValue(DispatchableType.RuleInstance, id));
        }

        [Fact]
        public async Task GetValues_ByType_ReturnsTheStoredValues()
        {
            var a = new Dispatchable();
            var b = new Dispatchable();
            await _dispatcher.DispatchValue(a, Value(a, 1));
            await _dispatcher.DispatchValue(b, Value(b, 2));

            var values = _dispatcher.GetValues(DispatchableType.NodeInstance);

            Assert.Equal(2, values.Count);
            Assert.Empty(_dispatcher.GetValues(DispatchableType.Control));
        }

        [Fact]
        public void GetValue_Unknown_ReturnsNull()
        {
            Assert.Null(_dispatcher.GetValue(DispatchableType.NodeInstance, Guid.NewGuid()));
            Assert.Null(_dispatcher.GetValue(Guid.NewGuid()));
            Assert.Null(_dispatcher.GetValues());
        }

        [Fact]
        public async Task ClearValues_RemovesTheStoredValues()
        {
            var node = new Dispatchable();
            await _dispatcher.DispatchValue(node, Value(node, 1));

            await _dispatcher.ClearValues();

            Assert.Null(_dispatcher.GetValue(node.Type, node.Id));
        }

        [Fact]
        public async Task ClearRegistrations_UnsubscribesAllTopicsAndDropsTheCallbacks()
        {
            var a = new Dispatchable();
            var b = new Dispatchable();
            var calls = 0;
            await _dispatcher.RegisterDispatch(a.Type, a.Id, (_, _) => calls++);
            await _dispatcher.RegisterDispatch(b.Type, b.Id, (_, _) => calls++);

            await _dispatcher.ClearRegistrations();
            await _dispatcher.DispatchValue(a, Value(a, 1));

            Assert.Equal(0, calls);
            Assert.Equal(new HashSet<string> { $"dispatcher/NodeInstance/{a.Id}", $"dispatcher/NodeInstance/{b.Id}" },
                new HashSet<string>(Assert.Single(_unsubscribed).TopicFilters));
        }

        [Fact]
        public async Task MqttDispatch_RemoteMessage_ReachesTheSubscriberAsRemoteSource()
        {
            var id = Guid.NewGuid();
            IDispatchable from = null;
            DispatchValue got = null;
            await _dispatcher.RegisterDispatch(DispatchableType.NodeInstance, id, (d, v) => { from = d; got = v; });
            var json = JsonConvert.SerializeObject(new RemoteDispatchValue
            {
                Source = "core",
                Value = new DispatchValue(id, DispatchableType.NodeInstance, 7, FixedTime, DispatchValueSource.Write)
            });

            _dispatcher.MqttDispatch($"dispatcher/NodeInstance/{id}", json);

            await Eventually.Assert(() => got != null, "remote value was not dispatched");
            Assert.Equal(id, from.Id);
            Assert.Equal(DispatchableSource.Remote, from.Source);
            Assert.Equal(7L, Convert.ToInt64(got.Value));
            Assert.Empty(_published);
        }

        [Fact]
        public void MqttDispatch_UnknownType_IsIgnored()
        {
            _dispatcher.MqttDispatch($"dispatcher/Nope/{Guid.NewGuid()}", "{}");

            Assert.Empty(_published);
        }
    }
}
