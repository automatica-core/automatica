using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Plugin.Standalone.Tests.Infrastructure;
using Moq;
using MQTTnet;
using Newtonsoft.Json;
using Xunit;

namespace Automatica.Core.Plugin.Standalone.Tests
{
    /// <summary>
    /// MqttMessageHandlers called directly (no broker): routing by topic, payload handling, the re-init exit
    /// </summary>
    [Collection(ProcessStateCollection.Name)]
    public sealed class MqttMessageHandlersTests : IDisposable
    {
        private const string NodeId = "8d3f6a2e-0000-4000-8000-000000000001";

        private readonly PluginServices _services = new PluginServices();
        private readonly ListLogger _logger = new ListLogger();
        private readonly MqttConnection _connection;
        private readonly MqttMessageHandlers _handler;
        private readonly List<int> _exitCodes = new List<int>();
        private readonly Action<int> _previousExit;

        public MqttMessageHandlersTests()
        {
            _connection = _services.CreateConnection(_logger, nodeId: NodeId);
            _handler = new MqttMessageHandlers(_logger, _connection, _services.Provider);
            _previousExit = MqttMessageHandlers.ExitProcess;
            MqttMessageHandlers.ExitProcess = code => _exitCodes.Add(code);
        }

        public void Dispose()
        {
            MqttMessageHandlers.ExitProcess = _previousExit;
            _connection.MqttClient.Dispose();
        }

        private static MqttApplicationMessageReceivedEventArgs Message(string topic, string payload) =>
            new MqttApplicationMessageReceivedEventArgs("broker", new MqttApplicationMessage
            {
                Topic = topic,
                Payload = payload == null ? null : Encoding.UTF8.GetBytes(payload)
            }, null, null);

        private static string IdJson(Guid id) => JsonConvert.SerializeObject(new { Id = id });

        private void VerifyNoNodeCalls() => _services.NodeStore.Verify(s => s.Get(It.IsAny<Guid>()), Times.Never);

        [Fact]
        public async Task NullEventArgs_AreIgnored()
        {
            await _handler.HandleApplicationMessageReceivedAsync(null);

            VerifyNoNodeCalls();
            Assert.Empty(_exitCodes);
        }

        [Theory]
        [InlineData("slave/" + NodeId + "/reinit")]
        [InlineData("dispatcher/NodeInstance/8d3f6a2e-0000-4000-8000-000000000002")]
        [InlineData("config/" + NodeId)]
        public async Task NullPayload_IsIgnoredOnEveryTopic(string topic)
        {
            await _handler.HandleApplicationMessageReceivedAsync(Message(topic, null));

            Assert.Empty(_exitCodes);
            _services.Factory.Verify(f => f.CreateDriver(It.IsAny<Automatica.Core.Driver.IDriverContext>()), Times.Never);
            Assert.True(_logger.Contains(Microsoft.Extensions.Logging.LogLevel.Information, "Application message is empty"));
        }

        [Fact]
        public async Task ReinitForThisNode_ExitsWithCodeZero()
        {
            await _handler.HandleApplicationMessageReceivedAsync(Message($"slave/{NodeId}/reinit", ""));

            Assert.Equal(new[] { 0 }, _exitCodes);
        }

        [Theory]
        [InlineData("slave/8d3f6a2e-0000-4000-8000-0000000000ff/reinit")]
        [InlineData("slave/" + NodeId + "/reinit/x")]
        [InlineData("slave/" + NodeId)]
        [InlineData("slave/+/reinit")]
        public async Task ReinitForOtherTopics_DoesNotExit(string topic)
        {
            await _handler.HandleApplicationMessageReceivedAsync(Message(topic, ""));

            Assert.Empty(_exitCodes);
        }

        [Fact]
        public async Task UnknownTopic_IsIgnored()
        {
            await _handler.HandleApplicationMessageReceivedAsync(Message("something/else", "{}"));

            VerifyNoNodeCalls();
            Assert.Empty(_exitCodes);
        }

        [Fact]
        public async Task ActionTopic_BeforeTheDriverIsLoaded_IsIgnored()
        {
            var node = Guid.NewGuid();
            _services.AddNode(node);

            await _handler.HandleApplicationMessageReceivedAsync(Message($"action/{_services.DriverId}/StartLearnMode", IdJson(node)));

            VerifyNoNodeCalls();
        }

        [Fact]
        public async Task ActionTopic_StartLearnMode_EnablesLearnModeOnTheNode()
        {
            _connection.DriverInstance = _services.Driver.Object;
            var id = Guid.NewGuid();
            var node = _services.AddNode(id);

            await _handler.HandleApplicationMessageReceivedAsync(Message($"action/{_services.DriverId}/StartLearnMode", IdJson(id)));

            node.Verify(n => n.EnableLearnMode(It.IsAny<CancellationToken>()), Times.Once);
            node.Verify(n => n.DisableLearnMode(It.IsAny<CancellationToken>()), Times.Never);
            node.Verify(n => n.Read(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ActionTopic_StopLearnMode_DisablesLearnModeOnTheNode()
        {
            _connection.DriverInstance = _services.Driver.Object;
            var id = Guid.NewGuid();
            var node = _services.AddNode(id);

            await _handler.HandleApplicationMessageReceivedAsync(Message($"action/{_services.DriverId}/StopLearnMode", IdJson(id)));

            node.Verify(n => n.DisableLearnMode(It.IsAny<CancellationToken>()), Times.Once);
            node.Verify(n => n.EnableLearnMode(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ActionTopic_Read_ReadsTheNode()
        {
            _connection.DriverInstance = _services.Driver.Object;
            var id = Guid.NewGuid();
            var node = _services.AddNode(id);

            await _handler.HandleApplicationMessageReceivedAsync(Message($"action/{_services.DriverId}/Read", IdJson(id)));

            node.Verify(n => n.Read(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ActionTopic_UnknownAction_CallsNothingOnTheNode()
        {
            _connection.DriverInstance = _services.Driver.Object;
            var id = Guid.NewGuid();
            var node = _services.AddNode(id);

            await _handler.HandleApplicationMessageReceivedAsync(Message($"action/{_services.DriverId}/Explode", IdJson(id)));

            node.Verify(n => n.EnableLearnMode(It.IsAny<CancellationToken>()), Times.Never);
            node.Verify(n => n.DisableLearnMode(It.IsAny<CancellationToken>()), Times.Never);
            node.Verify(n => n.Read(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ActionTopic_UnknownNode_IsIgnored()
        {
            _connection.DriverInstance = _services.Driver.Object;

            var e = await Record.ExceptionAsync(() => _handler.HandleApplicationMessageReceivedAsync(Message($"action/{_services.DriverId}/Read", IdJson(Guid.NewGuid()))));

            Assert.Null(e);
        }

        [Fact]
        public async Task ActionTopic_OfAnotherDriver_IsIgnored()
        {
            _connection.DriverInstance = _services.Driver.Object;
            var id = Guid.NewGuid();
            var node = _services.AddNode(id);

            await _handler.HandleApplicationMessageReceivedAsync(Message($"action/{Guid.NewGuid()}/Read", IdJson(id)));

            node.Verify(n => n.Read(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact(Skip = "BUG: MqttMessageHandlers.cs:124-126 - an empty, null or invalid JSON payload on action/<driver>/... throws (NullReferenceException / JsonReaderException) out of the handler instead of being ignored")]
        public async Task ActionTopic_EmptyOrInvalidPayload_IsIgnored()
        {
            _connection.DriverInstance = _services.Driver.Object;

            foreach (var payload in new[] { "", "null", "{ not json" })
            {
                var e = await Record.ExceptionAsync(() => _handler.HandleApplicationMessageReceivedAsync(Message($"action/{_services.DriverId}/Read", payload)));
                Assert.Null(e);
            }
            VerifyNoNodeCalls();
        }
    }
}
