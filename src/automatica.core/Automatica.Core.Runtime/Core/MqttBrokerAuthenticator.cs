using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Automatica.Core.Base.Common;
using Automatica.Core.EF.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MQTTnet.Protocol;

namespace Automatica.Core.Runtime.Core
{
    /// <summary>
    /// Satellite data the broker needs to authenticate a CONNECT
    /// </summary>
    internal interface IMqttSatelliteStore
    {
        /// <summary>
        /// The satellite with this ClientId, never the core's own (local) satellite row
        /// </summary>
        Slave FindSatellite(string clientId);

        /// <summary>
        /// <c>NodeInstance.This2Slave</c> of a node, null if unknown
        /// </summary>
        Guid? GetNodeSatellite(Guid nodeId);

        void UpdateClientKey(Guid satelliteObjId, string storedKey);
    }

    internal sealed class DatabaseMqttSatelliteStore : IMqttSatelliteStore
    {
        private readonly IConfiguration _configuration;

        public DatabaseMqttSatelliteStore(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public Slave FindSatellite(string clientId)
        {
            using var db = new AutomaticaContext(_configuration);
            var selfSlaveId = ServerInfo.SelfSlaveGuid;
            return db.Slaves.FirstOrDefault(a => a.ObjId != selfSlaveId && a.ClientId == clientId);
        }

        public Guid? GetNodeSatellite(Guid nodeId)
        {
            using var db = new AutomaticaContext(_configuration);
            return db.NodeInstances.Where(a => a.ObjId == nodeId).Select(a => a.This2Slave).FirstOrDefault();
        }

        public void UpdateClientKey(Guid satelliteObjId, string storedKey)
        {
            using var db = new AutomaticaContext(_configuration);
            var slave = db.Slaves.FirstOrDefault(a => a.ObjId == satelliteObjId);
            if (slave == null)
            {
                return;
            }
            slave.ClientKey = storedKey;
            db.SaveChanges();
        }
    }

    internal readonly record struct MqttAuthResult(MqttConnectReasonCode ReasonCode, MqttClientIdentity Identity);

    /// <summary>
    /// Authenticates broker CONNECTs and binds the ClientId to the authenticated identity:
    /// <list type="bullet">
    /// <item>satellite: ClientId == user name == <c>Slave.ClientId</c>, password == the satellite key (hashed in the db)</item>
    /// <item>plugin: ClientId == a driver node assigned to that satellite, user name == <c>Slave.ClientId</c>, password ==
    /// the per-plugin password (<see cref="MqttPluginCredentials"/>), or the satellite key while legacy plugin
    /// credentials are allowed (satellites that don't know per-plugin passwords yet)</item>
    /// </list>
    /// Failed CONNECTs are throttled per remote address.
    /// </summary>
    internal sealed class MqttBrokerAuthenticator
    {
        private readonly IMqttSatelliteStore _store;
        private readonly MqttNodeRegistry _registry;
        private readonly MqttPluginCredentials _pluginCredentials;
        private readonly MqttConnectThrottle _throttle;
        private readonly ILogger _logger;

        // PBKDF2 is slow on purpose; remember successful checks (hash of stored value + password) for a while so plugin
        // reconnects with legacy credentials don't cost a PBKDF2 run each
        private readonly ConcurrentDictionary<string, DateTime> _verified = new();
        private static readonly TimeSpan VerifiedCacheTime = TimeSpan.FromMinutes(10);

        public bool AllowLegacyPluginCredentials { get; }

        public MqttBrokerAuthenticator(IMqttSatelliteStore store, MqttNodeRegistry registry, MqttPluginCredentials pluginCredentials,
            MqttConnectThrottle throttle, bool allowLegacyPluginCredentials, ILogger logger)
        {
            _store = store;
            _registry = registry;
            _pluginCredentials = pluginCredentials;
            _throttle = throttle;
            AllowLegacyPluginCredentials = allowLegacyPluginCredentials;
            _logger = logger;
        }

        public MqttPluginCredentials PluginCredentials => _pluginCredentials;

        public MqttAuthResult Authenticate(string clientId, string userName, string password, EndPoint remoteEndPoint)
        {
            var remote = MqttConnectThrottle.KeyOf(remoteEndPoint);

            if (_throttle.IsBlocked(remote))
            {
                _logger.LogWarning($"Rejected mqtt connection from {remote} (clientId {clientId}): too many failed attempts");
                return new MqttAuthResult(MqttConnectReasonCode.ConnectionRateExceeded, null);
            }

            var result = AuthenticateInternal(clientId, userName, password);

            if (result.ReasonCode == MqttConnectReasonCode.Success)
            {
                _throttle.Succeeded(remote);
            }
            else
            {
                _throttle.Failed(remote);
            }
            return result;
        }

        private MqttAuthResult AuthenticateInternal(string clientId, string userName, string password)
        {
            // empty credentials would match the seeded self-slave row ("local", ClientId/ClientKey "")
            if (String.IsNullOrEmpty(userName) || String.IsNullOrEmpty(password) || String.IsNullOrEmpty(clientId))
            {
                return Fail();
            }

            Slave satellite;
            try
            {
                satellite = _store.FindSatellite(userName);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Could not load the satellite for an mqtt connection");
                return new MqttAuthResult(MqttConnectReasonCode.ServerUnavailable, null);
            }

            if (satellite == null || satellite.ObjId == ServerInfo.SelfSlaveGuid)
            {
                return Fail();
            }

            if (String.Equals(clientId, userName, StringComparison.Ordinal))
            {
                return VerifySatelliteKey(satellite, password)
                    ? new MqttAuthResult(MqttConnectReasonCode.Success, MqttClientIdentity.ForSatellite(satellite.ClientId, satellite.ObjId))
                    : Fail();
            }

            // any other client id must be a driver node that runs on this satellite
            if (!Guid.TryParse(clientId, out var nodeId) || !IsNodeOfSatellite(satellite, nodeId))
            {
                _logger.LogWarning($"Rejected mqtt client id {clientId} for satellite {userName}: not a node of this satellite");
                return new MqttAuthResult(MqttConnectReasonCode.ClientIdentifierNotValid, null);
            }

            if (_pluginCredentials.Verify(satellite.ClientId, nodeId, password))
            {
                return new MqttAuthResult(MqttConnectReasonCode.Success, MqttClientIdentity.ForPlugin(satellite.ClientId, satellite.ObjId, nodeId));
            }

            if (AllowLegacyPluginCredentials && VerifySatelliteKey(satellite, password))
            {
                _logger.LogWarning($"Plugin {nodeId} on satellite {satellite.ClientId} uses the satellite key, update the satellite to get per-plugin credentials");
                return new MqttAuthResult(MqttConnectReasonCode.Success, MqttClientIdentity.ForPlugin(satellite.ClientId, satellite.ObjId, nodeId, true));
            }

            return Fail();
        }

        private bool IsNodeOfSatellite(Slave satellite, Guid nodeId)
        {
            if (_registry.IsPluginNode(satellite.ClientId, nodeId))
            {
                return true;
            }

            try
            {
                return _store.GetNodeSatellite(nodeId) == satellite.ObjId;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Could not load the node of an mqtt connection");
                return false;
            }
        }

        private bool VerifySatelliteKey(Slave satellite, string password)
        {
            var stored = satellite.ClientKey;
            if (String.IsNullOrEmpty(stored))
            {
                return false;
            }

            var cacheKey = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes($"{satellite.ObjId}\n{stored}\n{password}")));
            if (_verified.TryGetValue(cacheKey, out var until) && until > DateTime.UtcNow)
            {
                return true;
            }

            if (!SatelliteClientKey.Verify(password, stored, out var needsRehash))
            {
                return false;
            }

            if (needsRehash)
            {
                try
                {
                    _store.UpdateClientKey(satellite.ObjId, SatelliteClientKey.Hash(password));
                    _logger.LogInformation($"Stored the key of satellite {satellite.ClientId} hashed");
                }
                catch (Exception e)
                {
                    _logger.LogError(e, $"Could not store the hashed key of satellite {satellite.ClientId}");
                }
            }
            else
            {
                if (_verified.Count > 1000)
                {
                    _verified.Clear();
                }
                _verified[cacheKey] = DateTime.UtcNow.Add(VerifiedCacheTime);
            }

            return true;
        }

        private static MqttAuthResult Fail() => new(MqttConnectReasonCode.BadUserNameOrPassword, null);
    }

    /// <summary>
    /// Blocks a remote address for a while after too many failed CONNECTs
    /// </summary>
    internal sealed class MqttConnectThrottle
    {
        private sealed class Entry
        {
            public int Failures;
            public DateTime WindowStart;
            public DateTime BlockedUntil;
        }

        private readonly ConcurrentDictionary<string, Entry> _entries = new();
        private readonly Func<DateTime> _now;

        public int MaxFailures { get; }
        public TimeSpan Window { get; }
        public TimeSpan BlockTime { get; }

        public MqttConnectThrottle(int maxFailures = 10, TimeSpan? window = null, TimeSpan? blockTime = null, Func<DateTime> now = null)
        {
            MaxFailures = maxFailures;
            Window = window ?? TimeSpan.FromMinutes(5);
            BlockTime = blockTime ?? TimeSpan.FromMinutes(5);
            _now = now ?? (() => DateTime.UtcNow);
        }

        public static string KeyOf(EndPoint endPoint)
        {
            return endPoint switch
            {
                IPEndPoint ip => (ip.Address.IsIPv4MappedToIPv6 ? ip.Address.MapToIPv4() : ip.Address).ToString(),
                null => "unknown",
                _ => endPoint.ToString()
            };
        }

        public bool IsBlocked(string remote)
        {
            return _entries.TryGetValue(remote, out var e) && e.BlockedUntil > _now();
        }

        public void Failed(string remote)
        {
            var now = _now();
            if (_entries.Count > 10_000)
            {
                foreach (var stale in _entries.Where(a => a.Value.BlockedUntil < now && a.Value.WindowStart + Window < now).Select(a => a.Key).ToList())
                {
                    _entries.TryRemove(stale, out _);
                }
            }

            var entry = _entries.GetOrAdd(remote, _ => new Entry { WindowStart = now });
            lock (entry)
            {
                if (entry.WindowStart + Window < now)
                {
                    entry.WindowStart = now;
                    entry.Failures = 0;
                }
                entry.Failures++;
                if (entry.Failures >= MaxFailures)
                {
                    entry.BlockedUntil = now + BlockTime;
                    entry.Failures = 0;
                    entry.WindowStart = now;
                }
            }
        }

        public void Succeeded(string remote)
        {
            _entries.TryRemove(remote, out _);
        }
    }
}
