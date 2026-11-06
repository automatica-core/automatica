using System;
using Automatica.Core.Runtime.Core;
using Xunit;

namespace Automatica.Core.Tests.Mqtt
{
    public class MqttAccessControlTests
    {
        private const string Sat = "sat1";
        private const string OtherSat = "sat2";

        private static readonly Guid Plugin = Guid.Parse("11111111-1111-1111-1111-111111111111");
        private static readonly Guid PluginChild = Guid.Parse("11111111-1111-1111-1111-222222222222");
        private static readonly Guid SiblingPlugin = Guid.Parse("33333333-3333-3333-3333-333333333333");
        private static readonly Guid SiblingChild = Guid.Parse("33333333-3333-3333-3333-444444444444");
        private static readonly Guid OtherSatPlugin = Guid.Parse("55555555-5555-5555-5555-555555555555");
        private static readonly Guid CoreNode = Guid.Parse("99999999-9999-9999-9999-999999999999");

        private readonly MqttNodeRegistry _registry = new();
        private readonly MqttClientIdentity _plugin = MqttClientIdentity.ForPlugin(Sat, Guid.NewGuid(), Plugin);
        private readonly MqttClientIdentity _satellite = MqttClientIdentity.ForSatellite(Sat, Guid.NewGuid());

        public MqttAccessControlTests()
        {
            _registry.Register(Sat, Plugin, new[] { PluginChild });
            _registry.Register(Sat, SiblingPlugin, new[] { SiblingChild });
            _registry.Register(OtherSat, OtherSatPlugin, Array.Empty<Guid>());
        }

        [Theory]
        [InlineData("slave/sat1/action")]
        [InlineData("slave/sat1/actions")]
        [InlineData("slave/sat2/action")]
        [InlineData("config/11111111-1111-1111-1111-111111111111")]
        [InlineData("action/11111111-1111-1111-1111-111111111111/Read")]
        [InlineData("action/33333333-3333-3333-3333-333333333333/notifyLearnMode")]
        [InlineData("dispatcher/NodeInstance/99999999-9999-9999-9999-999999999999")]
        [InlineData("dispatcher/NodeInstance/33333333-3333-3333-3333-444444444444")]
        [InlineData("dispatcher/RuleInstance/11111111-1111-1111-1111-222222222222")]
        [InlineData("dispatcher/+/11111111-1111-1111-1111-222222222222")]
        [InlineData("dispatcher/NodeInstance/#")]
        [InlineData("watchdog/sat1/33333333-3333-3333-3333-333333333333")]
        [InlineData("watchdog/sat2/11111111-1111-1111-1111-111111111111")]
        [InlineData("")]
        public void Plugin_CannotPublish(string topic)
        {
            Assert.False(MqttAccessControl.CanPublish(_plugin, topic, _registry));
        }

        [Theory]
        [InlineData("dispatcher/NodeInstance/11111111-1111-1111-1111-111111111111")]
        [InlineData("dispatcher/NodeInstance/11111111-1111-1111-1111-222222222222")]
        [InlineData("action/11111111-1111-1111-1111-111111111111/notifyLearnMode")]
        [InlineData("watchdog/sat1/11111111-1111-1111-1111-111111111111")]
        public void Plugin_CanPublishOwnTopics(string topic)
        {
            Assert.True(MqttAccessControl.CanPublish(_plugin, topic, _registry));
        }

        [Theory]
        [InlineData("config/11111111-1111-1111-1111-111111111111")]
        [InlineData("slave/11111111-1111-1111-1111-111111111111/reinit")]
        [InlineData("action/11111111-1111-1111-1111-111111111111/+")]
        [InlineData("action/11111111-1111-1111-1111-111111111111/#")]
        [InlineData("dispatcher/NodeInstance/11111111-1111-1111-1111-222222222222")]
        [InlineData("dispatcher/+/11111111-1111-1111-1111-222222222222")]
        public void Plugin_CanSubscribeOwnTopics(string filter)
        {
            Assert.True(MqttAccessControl.CanSubscribe(_plugin, filter, _registry));
        }

        [Theory]
        [InlineData("#")]
        [InlineData("+/+/+")]
        [InlineData("dispatcher/#")]
        [InlineData("dispatcher/+/+")]
        [InlineData("dispatcher/NodeInstance/+")]
        [InlineData("dispatcher/NodeInstance/33333333-3333-3333-3333-444444444444")]
        [InlineData("dispatcher/NodeInstance/99999999-9999-9999-9999-999999999999")]
        [InlineData("config/#")]
        [InlineData("config/+")]
        [InlineData("config/33333333-3333-3333-3333-333333333333")]
        [InlineData("slave/sat1/#")]
        [InlineData("slave/+/action")]
        [InlineData("slave/33333333-3333-3333-3333-333333333333/reinit")]
        [InlineData("action/+/Read")]
        [InlineData("action/#")]
        [InlineData("watchdog/#")]
        [InlineData("$share/g/config/11111111-1111-1111-1111-111111111111")]
        [InlineData("$SYS/#")]
        public void Plugin_CannotSubscribeForeignTopics(string filter)
        {
            Assert.False(MqttAccessControl.CanSubscribe(_plugin, filter, _registry));
        }

        [Theory]
        [InlineData("slave/sat1/action")]
        [InlineData("slave/sat1/actions")]
        [InlineData("slave/sat1/reinit")]
        [InlineData("slave/sat1/#")]
        [InlineData("watchdog/sat1/#")]
        [InlineData("config/11111111-1111-1111-1111-111111111111")]
        [InlineData("config/33333333-3333-3333-3333-333333333333")]
        [InlineData("dispatcher/NodeInstance/33333333-3333-3333-3333-444444444444")]
        public void Satellite_CanSubscribeOwnTopics(string filter)
        {
            Assert.True(MqttAccessControl.CanSubscribe(_satellite, filter, _registry));
        }

        [Theory]
        [InlineData("#")]
        [InlineData("slave/#")]
        [InlineData("slave/+/action")]
        [InlineData("slave/sat2/action")]
        [InlineData("watchdog/sat2/#")]
        [InlineData("config/#")]
        [InlineData("config/55555555-5555-5555-5555-555555555555")]
        [InlineData("dispatcher/#")]
        [InlineData("dispatcher/NodeInstance/99999999-9999-9999-9999-999999999999")]
        [InlineData("action/11111111-1111-1111-1111-111111111111/+")]
        public void Satellite_CannotSubscribeForeignTopics(string filter)
        {
            Assert.False(MqttAccessControl.CanSubscribe(_satellite, filter, _registry));
        }

        [Theory]
        [InlineData("slave/sat1/action")]
        [InlineData("slave/sat2/action")]
        [InlineData("config/11111111-1111-1111-1111-111111111111")]
        [InlineData("action/11111111-1111-1111-1111-111111111111/notifyLearnMode")]
        [InlineData("dispatcher/NodeInstance/99999999-9999-9999-9999-999999999999")]
        [InlineData("dispatcher/NodeInstance/55555555-5555-5555-5555-555555555555")]
        [InlineData("watchdog/sat1/11111111-1111-1111-1111-111111111111")]
        public void Satellite_CannotPublishServerTopics(string topic)
        {
            Assert.False(MqttAccessControl.CanPublish(_satellite, topic, _registry));
        }

        [Fact]
        public void Satellite_CanPublishDispatcherOfOwnNodes()
        {
            Assert.True(MqttAccessControl.CanPublish(_satellite, $"dispatcher/NodeInstance/{SiblingChild}", _registry));
        }

        [Fact]
        public void UnknownIdentity_IsDenied()
        {
            Assert.False(MqttAccessControl.CanPublish(null, $"dispatcher/NodeInstance/{Plugin}", _registry));
            Assert.False(MqttAccessControl.CanSubscribe(null, $"config/{Plugin}", _registry));
        }

        [Fact]
        public void Registry_Clear_RevokesNodeAccess()
        {
            _registry.Clear();
            Assert.False(MqttAccessControl.CanPublish(_plugin, $"dispatcher/NodeInstance/{Plugin}", _registry));
            Assert.False(MqttAccessControl.CanSubscribe(_satellite, $"config/{Plugin}", _registry));
            // own satellite topics don't depend on the registry
            Assert.True(MqttAccessControl.CanSubscribe(_satellite, "slave/sat1/action", _registry));
        }

        [Fact]
        public void Registry_RegisterNodeInstance_CollectsTree()
        {
            var registry = new MqttNodeRegistry();
            var child = new EF.Models.NodeInstance { ObjId = Guid.NewGuid() };
            var grandChild = new EF.Models.NodeInstance { ObjId = Guid.NewGuid() };
            child.InverseThis2ParentNodeInstanceNavigation = new() { grandChild };
            var root = new EF.Models.NodeInstance { ObjId = Guid.NewGuid(), InverseThis2ParentNodeInstanceNavigation = new() { child } };

            registry.Register(Sat, root);

            Assert.True(registry.IsPluginNode(Sat, root.ObjId));
            Assert.False(registry.IsPluginNode(OtherSat, root.ObjId));
            Assert.False(registry.IsPluginNode(Sat, child.ObjId));
            Assert.True(registry.IsInPluginTree(root.ObjId, grandChild.ObjId));
            Assert.True(registry.IsInSatelliteTree(Sat, grandChild.ObjId));
            Assert.False(registry.IsInSatelliteTree(OtherSat, grandChild.ObjId));
        }
    }
}
