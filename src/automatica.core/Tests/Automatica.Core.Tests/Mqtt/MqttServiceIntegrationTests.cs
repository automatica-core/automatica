using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.Base.Remote;
using Automatica.Core.Driver.LeanMode;
using Automatica.Core.EF.Models;
using Automatica.Core.Runtime.Core;
using Automatica.Core.Tests.Dispatcher.Utils;
using Automatica.Core.UnitTests.Base.Logics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MQTTnet;
using MQTTnet.Protocol;
using MQTTnet.Server;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
using CoreDispatcher = Automatica.Core.Base.IO.Dispatcher;

namespace Automatica.Core.Tests.Mqtt
{
    /// <summary>
    /// <see cref="MqttService"/> on the real broker: start actions with per-plugin passwords, configs, values and learn mode
    /// from satellites and plugins
    /// </summary>
    public sealed class MqttServiceIntegrationTests : IAsyncLifetime
    {
        private const string SatKey = "service-sat-made-up-key-001";
        private static readonly string SatHash = SatelliteClientKey.Hash(SatKey);

        private readonly MqttBrokerHost _host = new();
        private readonly Mock<ILearnMode> _learnMode = new();
        private readonly IDispatcher _dispatcher;
        private readonly MqttService _service;

        private readonly NodeInstance _driverNode;
        private readonly NodeInstance _childNode;
        private readonly NodeInstance _otherDriverNode;

        public MqttServiceIntegrationTests()
        {
            _dispatcher = new CoreDispatcher(NullLogger<CoreDispatcher>.Instance, new Mock<IDataBroadcast>().Object, new Mock<IRemoteSender>().Object,
                new Mock<IRemanentHandler>().Object, new LogicInstanceVisuNotifyMock());

            _childNode = new NodeInstance { ObjId = Guid.NewGuid(), Name = "child" };
            _driverNode = new NodeInstance { ObjId = Guid.NewGuid(), Name = "driver" };
            _driverNode.InverseThis2ParentNodeInstanceNavigation.Add(_childNode);
            _otherDriverNode = new NodeInstance { ObjId = Guid.NewGuid(), Name = "other driver" };

            _host.Store.Satellites["sat1"] = new Slave { ObjId = Guid.NewGuid(), ClientId = "sat1", ClientKey = SatHash };

            var services = new ServiceCollection();
            services.AddSingleton<ILogger<MqttService>>(NullLogger<MqttService>.Instance);
            services.AddSingleton(_host.Server);
            services.AddSingleton(_learnMode.Object);
            services.AddSingleton(_dispatcher);
            services.AddSingleton(_host.Registry);
            services.AddSingleton(_host.Authenticator);
            services.AddSingleton(_host.Security);
            services.AddSingleton(_host.Configuration);
            _service = new MqttService(services.BuildServiceProvider());
        }

        public Task InitializeAsync() => _host.StartAsync();

        public async Task DisposeAsync()
        {
            await _host.DisposeAsync();
            await _dispatcher.ClearRegistrations();
        }

        private Task<MqttBrokerHost.Client> Satellite() => _host.Connect("sat1", "sat1", SatKey);

        private static IEnumerable<JObject> Actions(MqttBrokerHost.Client client) => client.Received
            .Where(a => a.Topic == "slave/sat1/action")
            .Select(a => JObject.Parse(a.ConvertPayloadToString()));

        [Fact]
        public async Task SatelliteSubscribingItsActions_GetsTheStartAction_WithThePluginPassword()
        {
            await _service.AddSlave("sat1", new DriverFactoryMock(), _driverNode);
            var sat = await Satellite();
            await sat.Subscribe("slave/sat1/action", MqttQualityOfServiceLevel.ExactlyOnce);

            await sat.Subscribe("slave/sat1/actions", MqttQualityOfServiceLevel.ExactlyOnce);

            await MqttBrokerHost.WaitFor(() => Actions(sat).Any());
            var action = Assert.Single(Actions(sat));
            Assert.Equal((int)SlaveAction.Start, action.Value<int>("Action"));
            Assert.Equal(_driverNode.ObjId, Guid.Parse(action.Value<string>("Id")));
            Assert.Equal(_host.Credentials.GetPassword("sat1", _driverNode.ObjId), action.Value<string>("ClientKey"));
            // satellites only accept registry images, the image source is never sent
            Assert.True(action["ImageSource"] == null || action["ImageSource"].Type == JTokenType.Null);
            Assert.Equal(0, action.Value<int>("BrokerTlsPort"));
            Assert.False(sat.Received.First(a => a.Topic == "slave/sat1/action").Retain);
        }

        [Fact]
        public async Task PluginPassword_FromTheStartAction_OpensThePluginSession()
        {
            var sat = await Satellite();
            await sat.Subscribe("slave/sat1/action", MqttQualityOfServiceLevel.ExactlyOnce);
            await MqttBrokerHost.WaitFor(() => _service.GetConnectedClients().Contains("sat1"));

            // already connected: started right away
            await _service.AddSlave("sat1", new DriverFactoryMock(), _driverNode);

            await MqttBrokerHost.WaitFor(() => Actions(sat).Any());
            var password = Actions(sat).Single().Value<string>("ClientKey");
            var plugin = await _host.Connect(_driverNode.ObjId.ToString(), "sat1", password);
            Assert.Equal(MqttConnectReasonCode.Success, plugin.Result);

            var otherPlugin = await _host.Connect(_otherDriverNode.ObjId.ToString(), "sat1", password);
            Assert.NotEqual(MqttConnectReasonCode.Success, otherPlugin.Result);
        }

        [Fact]
        public async Task Plugin_GetsItsConfigWhenItSubscribes_NotRetained()
        {
            await _service.AddSlave("sat1", new DriverFactoryMock(), _driverNode);
            await _service.AddNode(_driverNode.ObjId.ToString(), _driverNode);
            var plugin = await _host.Connect(_driverNode.ObjId.ToString(), "sat1", _host.Credentials.GetPassword("sat1", _driverNode.ObjId));

            await plugin.Subscribe($"config/{_driverNode.ObjId}", MqttQualityOfServiceLevel.ExactlyOnce);

            await MqttBrokerHost.WaitFor(() => plugin.Received.Any(a => a.Topic == $"config/{_driverNode.ObjId}"));
            var config = plugin.Received.First(a => a.Topic == $"config/{_driverNode.ObjId}");
            Assert.False(config.Retain);
            var node = JsonConvert.DeserializeObject<NodeInstance>(config.ConvertPayloadToString());
            Assert.Equal(_driverNode.ObjId, node.ObjId);
            Assert.Equal(_childNode.ObjId, node.InverseThis2ParentNodeInstanceNavigation.Single().ObjId);
        }

        private static string DispatchPayload(Guid id, object value) => JsonConvert.SerializeObject(new RemoteDispatchValue
        {
            Value = new DispatchValue(id, DispatchableType.NodeInstance, value, new DateTime(2026, 1, 1, 12, 0, 0), DispatchValueSource.Read),
            Source = "test"
        });

        [Fact]
        public async Task PluginValue_IsDispatchedIntoTheCore()
        {
            await _service.AddSlave("sat1", new DriverFactoryMock(), _driverNode);
            var plugin = await _host.Connect(_driverNode.ObjId.ToString(), "sat1", _host.Credentials.GetPassword("sat1", _driverNode.ObjId));

            Assert.Equal(MqttClientPublishReasonCode.Success, await plugin.Publish($"dispatcher/NodeInstance/{_childNode.ObjId}", DispatchPayload(_childNode.ObjId, 21.5)));

            await MqttBrokerHost.WaitFor(() => _dispatcher.GetValue(DispatchableType.NodeInstance, _childNode.ObjId) != null);
            Assert.Equal(21.5, Convert.ToDouble(_dispatcher.GetValue(DispatchableType.NodeInstance, _childNode.ObjId).Value));
        }

        [Fact]
        public async Task ForgedValue_ForANodeOfAnotherPlugin_IsNotDispatched()
        {
            await _service.AddSlave("sat1", new DriverFactoryMock(), _driverNode);
            await _service.AddSlave("sat1", new DriverFactoryMock(), _otherDriverNode);
            var plugin = await _host.Connect(_driverNode.ObjId.ToString(), "sat1", _host.Credentials.GetPassword("sat1", _driverNode.ObjId));

            Assert.Equal(MqttClientPublishReasonCode.NotAuthorized, await plugin.Publish($"dispatcher/NodeInstance/{_otherDriverNode.ObjId}", DispatchPayload(_otherDriverNode.ObjId, true)));
            Assert.Equal(MqttClientPublishReasonCode.Success, await plugin.Publish($"dispatcher/NodeInstance/{_childNode.ObjId}", DispatchPayload(_childNode.ObjId, true)));

            await MqttBrokerHost.WaitFor(() => _dispatcher.GetValue(DispatchableType.NodeInstance, _childNode.ObjId) != null);
            Assert.Null(_dispatcher.GetValue(DispatchableType.NodeInstance, _otherDriverNode.ObjId));
        }

        private static string LearnPayload(NodeInstance self) => JsonConvert.SerializeObject(new LearnModeDto
        {
            Name = "learned",
            Description = "learned node",
            Self = self,
            Templates = new List<NodeTemplate>(),
            PropertyInstances = new List<PropertyInstance>()
        });

        [Fact]
        public async Task LearnMode_ForANodeOfThePlugin_IsForwarded()
        {
            await _service.AddSlave("sat1", new DriverFactoryMock(), _driverNode);
            var plugin = await _host.Connect(_driverNode.ObjId.ToString(), "sat1", _host.Credentials.GetPassword("sat1", _driverNode.ObjId));

            await plugin.Publish($"action/{_driverNode.ObjId}/notifyLearnMode", LearnPayload(new NodeInstance { ObjId = _childNode.ObjId, Name = "child" }));

            await MqttBrokerHost.WaitFor(() => _learnMode.Invocations.Count > 0);
            _learnMode.Verify(a => a.NotifyLearnNode("learned", "learned node", It.Is<NodeInstance>(n => n.ObjId == _childNode.ObjId),
                It.IsAny<IList<NodeTemplate>>(), It.IsAny<IList<PropertyInstance>>()), Times.Once);
        }

        [Fact]
        public async Task LearnMode_ForANodeOutsideThePluginTree_IsIgnored()
        {
            await _service.AddSlave("sat1", new DriverFactoryMock(), _driverNode);
            await _service.AddSlave("sat1", new DriverFactoryMock(), _otherDriverNode);
            var plugin = await _host.Connect(_driverNode.ObjId.ToString(), "sat1", _host.Credentials.GetPassword("sat1", _driverNode.ObjId));

            // allowed topic (own node), but the learned node belongs to another plugin
            Assert.Equal(MqttClientPublishReasonCode.Success,
                await plugin.Publish($"action/{_driverNode.ObjId}/notifyLearnMode", LearnPayload(new NodeInstance { ObjId = _otherDriverNode.ObjId, Name = "x" })));
            Assert.Equal(MqttClientPublishReasonCode.Success,
                await plugin.Publish($"action/{_driverNode.ObjId}/notifyLearnMode", LearnPayload(null)));
            await MqttBrokerHost.WaitFor(() => _host.Processed.Count(a => a.Topic.EndsWith("notifyLearnMode")) >= 2);
            await Task.Delay(50);

            Assert.Empty(_learnMode.Invocations);
        }

        [Fact]
        public async Task Init_ClearsTheRegistry_SoTheSatelliteLosesNodeAccess()
        {
            await _service.AddSlave("sat1", new DriverFactoryMock(), _driverNode);
            var sat = await Satellite();
            Assert.Equal(MqttClientPublishReasonCode.Success, await sat.Publish($"dispatcher/NodeInstance/{_childNode.ObjId}", DispatchPayload(_childNode.ObjId, 1)));

            await _service.Init();

            Assert.Equal(MqttClientPublishReasonCode.NotAuthorized, await sat.Publish($"dispatcher/NodeInstance/{_childNode.ObjId}", DispatchPayload(_childNode.ObjId, 2)));
            Assert.Equal(0, _host.Registry.SatelliteCount);
        }

        [Fact]
        public async Task ReInit_SendsReinitToEveryConnectedClient()
        {
            var sat = await Satellite();
            await sat.Subscribe("slave/sat1/#", MqttQualityOfServiceLevel.ExactlyOnce);
            await MqttBrokerHost.WaitFor(() => _service.GetConnectedClients().Contains("sat1"));

            await _service.ReInit();

            await MqttBrokerHost.WaitFor(() => sat.Received.Any(a => a.Topic == "slave/sat1/reinit"));
            Assert.Contains(sat.Received, a => a.Topic == "slave/sat1/reinit");
        }

        [Fact]
        public async Task Stop_SendsAStopActionPerPlugin()
        {
            await _service.AddSlave("sat1", new DriverFactoryMock(), _driverNode);
            var sat = await Satellite();
            await sat.Subscribe("slave/sat1/action", MqttQualityOfServiceLevel.ExactlyOnce);

            await _service.Stop();

            await MqttBrokerHost.WaitFor(() => Actions(sat).Any(a => a.Value<int>("Action") == (int)SlaveAction.Stop));
            var stop = Actions(sat).Single(a => a.Value<int>("Action") == (int)SlaveAction.Stop);
            Assert.Equal(_driverNode.ObjId, Guid.Parse(stop.Value<string>("Id")));
            Assert.Null(stop["ClientKey"]);
        }

        [Fact]
        public async Task Disconnect_RemovesTheClientFromTheConnectedClients()
        {
            var sat = await Satellite();
            await MqttBrokerHost.WaitFor(() => _service.GetConnectedClients().Contains("sat1"));

            await sat.Mqtt.DisconnectAsync();

            await MqttBrokerHost.WaitFor(() => !_service.GetConnectedClients().Contains("sat1"));
            Assert.DoesNotContain("sat1", _service.GetConnectedClients());
        }

        [Fact]
        public async Task RejectedClient_IsNeverListedAsConnected()
        {
            await _host.Connect("sat1", "sat1", "wrong-made-up-key-xyz");
            await Task.Delay(50);

            Assert.DoesNotContain("sat1", _service.GetConnectedClients());
        }

        [Fact]
        public void BuildMessage_IsNotRetainedByDefault_AndKeepsTopicQosAndPayload()
        {
            var message = MqttService.BuildMessage("config/x", Encoding.UTF8.GetBytes("abc"), MqttQualityOfServiceLevel.ExactlyOnce);

            Assert.Equal("config/x", message.Topic);
            Assert.False(message.Retain);
            Assert.Equal(MqttQualityOfServiceLevel.ExactlyOnce, message.QualityOfServiceLevel);
            Assert.Equal("abc", message.ConvertPayloadToString());
        }

        [Fact]
        public void BuildMessage_WithoutPayload_HasAnEmptyPayload()
        {
            var message = MqttService.BuildMessage("slave/x/reinit", null, MqttQualityOfServiceLevel.AtMostOnce, retain: true);

            Assert.True(message.Retain);
            Assert.True(message.Payload.IsEmpty);
        }
    }
}
