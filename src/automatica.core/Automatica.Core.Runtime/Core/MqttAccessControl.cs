using System;
using System.Collections.Generic;
using Automatica.Core.Base.IO;
using Automatica.Core.Base.Remote;
using Automatica.Core.EF.Models;

namespace Automatica.Core.Runtime.Core
{
    internal enum MqttClientRole
    {
        /// <summary>
        /// The satellite process itself: ClientId == user name == <c>Slave.ClientId</c>
        /// </summary>
        Satellite,

        /// <summary>
        /// A plugin container on a satellite: ClientId == the driver node id, user name == the satellite's ClientId
        /// </summary>
        Plugin
    }

    /// <summary>
    /// The authenticated identity of an MQTT session, stored in the session items at CONNECT
    /// </summary>
    internal sealed class MqttClientIdentity
    {
        public const string SessionItemKey = "automatica.mqtt.identity";

        public MqttClientRole Role { get; init; }

        /// <summary>
        /// <c>Slave.ClientId</c> of the satellite the session belongs to
        /// </summary>
        public string SatelliteClientId { get; init; }

        public Guid SatelliteObjId { get; init; }

        /// <summary>
        /// Driver node id of a <see cref="MqttClientRole.Plugin"/> session
        /// </summary>
        public Guid NodeId { get; init; }

        /// <summary>
        /// Plugin that authenticated with the satellite's key (satellites before per-plugin credentials)
        /// </summary>
        public bool LegacyCredentials { get; init; }

        public static MqttClientIdentity ForSatellite(string clientId, Guid objId) => new() { Role = MqttClientRole.Satellite, SatelliteClientId = clientId, SatelliteObjId = objId };

        public static MqttClientIdentity ForPlugin(string satelliteClientId, Guid satelliteObjId, Guid nodeId, bool legacy = false) => new()
        {
            Role = MqttClientRole.Plugin, SatelliteClientId = satelliteClientId, SatelliteObjId = satelliteObjId, NodeId = nodeId, LegacyCredentials = legacy
        };

        public override string ToString() => Role == MqttClientRole.Satellite
            ? $"satellite {SatelliteClientId}"
            : $"plugin {NodeId} on satellite {SatelliteClientId}";
    }

    /// <summary>
    /// The driver nodes the core runs on satellites: which satellite runs which driver node, and which node ids
    /// (the driver node and all its children) belong to it. Filled by <see cref="MqttService"/>.
    /// </summary>
    internal sealed class MqttNodeRegistry
    {
        private readonly object _lock = new();
        private readonly Dictionary<Guid, (string Satellite, HashSet<Guid> Tree)> _plugins = new();
        private readonly Dictionary<string, HashSet<Guid>> _satelliteTrees = new(StringComparer.Ordinal);

        public void Register(string satelliteClientId, NodeInstance driverNode)
        {
            if (String.IsNullOrEmpty(satelliteClientId) || driverNode == null)
            {
                return;
            }

            var tree = new HashSet<Guid>();
            Collect(driverNode, tree, 0);

            lock (_lock)
            {
                _plugins[driverNode.ObjId] = (satelliteClientId, tree);
                RebuildSatelliteTrees();
            }
        }

        public void Register(string satelliteClientId, Guid driverNodeId, IEnumerable<Guid> tree)
        {
            var set = new HashSet<Guid>(tree) { driverNodeId };
            lock (_lock)
            {
                _plugins[driverNodeId] = (satelliteClientId, set);
                RebuildSatelliteTrees();
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _plugins.Clear();
                _satelliteTrees.Clear();
            }
        }

        public bool IsPluginNode(string satelliteClientId, Guid nodeId)
        {
            lock (_lock)
            {
                return _plugins.TryGetValue(nodeId, out var p) && String.Equals(p.Satellite, satelliteClientId, StringComparison.Ordinal);
            }
        }

        public bool IsInPluginTree(Guid pluginNodeId, Guid id)
        {
            lock (_lock)
            {
                return _plugins.TryGetValue(pluginNodeId, out var p) && p.Tree.Contains(id);
            }
        }

        public bool IsInSatelliteTree(string satelliteClientId, Guid id)
        {
            lock (_lock)
            {
                return _satelliteTrees.TryGetValue(satelliteClientId, out var tree) && tree.Contains(id);
            }
        }

        public int SatelliteCount
        {
            get
            {
                lock (_lock)
                {
                    return _satelliteTrees.Count;
                }
            }
        }

        private void RebuildSatelliteTrees()
        {
            _satelliteTrees.Clear();
            foreach (var plugin in _plugins.Values)
            {
                if (!_satelliteTrees.TryGetValue(plugin.Satellite, out var set))
                {
                    set = new HashSet<Guid>();
                    _satelliteTrees.Add(plugin.Satellite, set);
                }
                set.UnionWith(plugin.Tree);
            }
        }

        private static void Collect(NodeInstance node, HashSet<Guid> tree, int depth)
        {
            // the tree comes from the db, the depth limit only guards against a corrupt (cyclic) graph
            if (node == null || depth > 64 || !tree.Add(node.ObjId) || node.InverseThis2ParentNodeInstanceNavigation == null)
            {
                return;
            }
            foreach (var child in node.InverseThis2ParentNodeInstanceNavigation)
            {
                Collect(child, tree, depth + 1);
            }
        }
    }

    /// <summary>
    /// Topic ACL of the core broker. Messages the server injects itself are not checked (no client id).
    /// <list type="bullet">
    /// <item>Only the server publishes <c>slave/*</c>, <c>config/*</c> and <c>action/*</c>; the one exception is a
    /// plugin's learn mode notification <c>action/&lt;ownNode&gt;/notifyLearnMode</c>.</item>
    /// <item>A satellite subscribes to <c>slave/&lt;own&gt;/#</c>, <c>watchdog/&lt;own&gt;/#</c> and the config and
    /// dispatcher topics of the nodes assigned to it.</item>
    /// <item>A plugin subscribes to <c>config/&lt;own&gt;</c>, <c>slave/&lt;own&gt;/#</c>, <c>action/&lt;own&gt;/#</c>
    /// and the dispatcher topics of its own node tree.</item>
    /// <item>A client publishes to <c>dispatcher/NodeInstance/&lt;id&gt;</c> only for nodes of its own tree; a plugin
    /// also publishes its will message <c>watchdog/&lt;satellite&gt;/&lt;ownNode&gt;</c>.</item>
    /// </list>
    /// </summary>
    internal static class MqttAccessControl
    {
        /// <summary>
        /// Will message topic of the plugins: <c>watchdog/&lt;satellite&gt;/&lt;node&gt;</c>
        /// </summary>
        public const string WatchdogTopic = "watchdog";

        public static bool CanPublish(MqttClientIdentity identity, string topic, MqttNodeRegistry registry)
        {
            if (identity == null || String.IsNullOrEmpty(topic) || topic.Contains('+') || topic.Contains('#'))
            {
                return false;
            }

            var s = topic.Split('/');

            if (identity.Role == MqttClientRole.Plugin)
            {
                if (IsDispatcherTopic(s, false, id => registry.IsInPluginTree(identity.NodeId, id)))
                {
                    return true;
                }
                if (s.Length == 3 && s[0] == RemoteTopicConstants.ACTION_TOPIC_START && IsGuid(s[1], identity.NodeId) &&
                    s[2] == RemoteTopicConstants.NOTIFY_LEARN_MODE)
                {
                    return true;
                }
                if (s.Length == 3 && s[0] == WatchdogTopic && s[1] == identity.SatelliteClientId && IsGuid(s[2], identity.NodeId))
                {
                    return true;
                }
                return false;
            }

            return IsDispatcherTopic(s, false, id => registry.IsInSatelliteTree(identity.SatelliteClientId, id));
        }

        public static bool CanSubscribe(MqttClientIdentity identity, string filter, MqttNodeRegistry registry)
        {
            if (identity == null || String.IsNullOrEmpty(filter))
            {
                return false;
            }

            var s = filter.Split('/');

            if (identity.Role == MqttClientRole.Plugin)
            {
                if (s.Length == 2 && s[0] == RemoteTopicConstants.CONFIG_TOPIC && IsGuid(s[1], identity.NodeId))
                {
                    return true;
                }
                if (s.Length >= 2 && (s[0] == RemoteTopicConstants.SLAVE_TOPIC || s[0] == RemoteTopicConstants.ACTION_TOPIC_START) && IsGuid(s[1], identity.NodeId))
                {
                    return true;
                }
                return IsDispatcherTopic(s, true, id => registry.IsInPluginTree(identity.NodeId, id));
            }

            var satellite = identity.SatelliteClientId;
            if (s.Length >= 2 && (s[0] == RemoteTopicConstants.SLAVE_TOPIC || s[0] == WatchdogTopic) && s[1] == satellite)
            {
                return true;
            }
            if (s.Length == 2 && s[0] == RemoteTopicConstants.CONFIG_TOPIC && Guid.TryParse(s[1], out var nodeId) && registry.IsPluginNode(satellite, nodeId))
            {
                return true;
            }
            return IsDispatcherTopic(s, true, id => registry.IsInSatelliteTree(satellite, id));
        }

        /// <summary>
        /// <c>dispatcher/&lt;type&gt;/&lt;id&gt;</c> with type NodeInstance and an id the predicate allows; for a
        /// subscription the type may be <c>+</c>
        /// </summary>
        private static bool IsDispatcherTopic(string[] s, bool allowTypeWildcard, Func<Guid, bool> allowed)
        {
            if (s.Length != 3 || s[0] != RemoteTopicConstants.DISPATCHER_TOPIC)
            {
                return false;
            }

            var typeOk = (allowTypeWildcard && s[1] == "+") ||
                         (!s[1].Contains('+') && !s[1].Contains('#') &&
                          Enum.TryParse(s[1], out DispatchableType type) && type == DispatchableType.NodeInstance);

            return typeOk && Guid.TryParse(s[2], out var id) && allowed(id);
        }

        private static bool IsGuid(string segment, Guid expected)
        {
            return Guid.TryParse(segment, out var g) && g == expected;
        }
    }
}
