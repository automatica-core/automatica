using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Automatica.Core.EF.Models;
using Automatica.Core.Runtime;
using Automatica.Core.Runtime.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using MQTTnet.Protocol;
using Xunit;

namespace Automatica.Core.Tests.Mqtt
{
    public class SatelliteClientKeyTests
    {
        private static string HashWith(string key, int iterations, int saltSize = 16, int hashSize = 32)
        {
            var salt = RandomNumberGenerator.GetBytes(saltSize);
            var hash = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(key), salt, iterations, HashAlgorithmName.SHA512, hashSize);
            return $"{SatelliteClientKey.Prefix}{iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
        }

        [Fact]
        public void Hash_HasThePrefixedFormat_WithTheProductionIterations()
        {
            var parts = SatelliteClientKey.Hash("format-test-made-up-key").Split('$');

            Assert.Equal(4, parts.Length);
            Assert.Equal("pbkdf2-sha512", parts[0]);
            Assert.Equal(SatelliteClientKey.Iterations.ToString(), parts[1]);
            Assert.True(SatelliteClientKey.Iterations >= 210_000);
            Assert.Equal(16, Convert.FromBase64String(parts[2]).Length);
            Assert.Equal(32, Convert.FromBase64String(parts[3]).Length);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void Hash_EmptyKey_Throws(string key)
        {
            Assert.Throws<ArgumentException>(() => SatelliteClientKey.Hash(key));
        }

        [Fact]
        public void Verify_PlaintextStoredKey_MatchesAndAsksForRehash()
        {
            Assert.True(SatelliteClientKey.Verify("plain-made-up-key", "plain-made-up-key", out var rehash));
            Assert.True(rehash);
        }

        [Theory]
        [InlineData("plain-made-up-key", "plain-made-up-kez")]
        [InlineData("plain-made-up-key", "PLAIN-MADE-UP-KEY")]
        [InlineData("plain-made-up-key", "plain-made-up-key ")]
        public void Verify_PlaintextStoredKey_Mismatch_IsRejectedWithoutRehash(string key, string stored)
        {
            Assert.False(SatelliteClientKey.Verify(key, stored, out var rehash));
            Assert.False(rehash);
        }

        [Fact]
        public void Verify_HashWithFewerIterations_IsValidAndAsksForRehash()
        {
            var stored = HashWith("weak-made-up-key", 1000);

            Assert.True(SatelliteClientKey.Verify("weak-made-up-key", stored, out var rehash));
            Assert.True(rehash);
            Assert.False(SatelliteClientKey.Verify("weak-made-up-kez", stored, out rehash));
            Assert.False(rehash);
        }

        [Fact]
        public void Verify_HashWithMoreIterations_DoesNotAskForRehash()
        {
            var stored = HashWith("strong-made-up-key", SatelliteClientKey.Iterations + 1);

            Assert.True(SatelliteClientKey.Verify("strong-made-up-key", stored, out var rehash));
            Assert.False(rehash);
        }

        [Theory]
        [InlineData("pbkdf2-sha512$")]
        [InlineData("pbkdf2-sha512$1000$c2FsdA==")]
        [InlineData("pbkdf2-sha512$1000$c2FsdA==$aGFzaA==$extra")]
        [InlineData("pbkdf2-sha512$abc$c2FsdA==$aGFzaA==")]
        [InlineData("pbkdf2-sha512$0$c2FsdA==$aGFzaA==")]
        [InlineData("pbkdf2-sha512$-5$c2FsdA==$aGFzaA==")]
        [InlineData("pbkdf2-sha512$1000$not base64!$aGFzaA==")]
        [InlineData("pbkdf2-sha512$1000$c2FsdA==$not base64!")]
        public void Verify_MalformedHash_IsRejected_WithoutThrowing(string stored)
        {
            Assert.False(SatelliteClientKey.Verify("any-made-up-key", stored, out var rehash));
            Assert.False(rehash);
        }

        [Fact]
        public void Verify_AHashUsedAsPassword_IsRejected()
        {
            // a leaked hash must not work as the key itself
            var stored = SatelliteClientKey.Hash("the-made-up-key-123");

            Assert.False(SatelliteClientKey.Verify(stored, stored, out _));
        }

        [Theory]
        [InlineData(null, "x")]
        [InlineData("", "x")]
        [InlineData("x", null)]
        [InlineData("x", "")]
        public void Verify_EmptyKeyOrStoredValue_IsRejected(string key, string stored)
        {
            Assert.False(SatelliteClientKey.Verify(key, stored, out var rehash));
            Assert.False(rehash);
        }

        [Theory]
        [InlineData(null, false)]
        [InlineData("", false)]
        [InlineData("pbkdf2-sha512$1$a$b", true)]
        [InlineData("PBKDF2-SHA512$1$a$b", false)]
        [InlineData(" pbkdf2-sha512$1$a$b", false)]
        [InlineData("plaintext", false)]
        public void IsHashed_OnlyForThePrefix(string stored, bool expected)
        {
            Assert.Equal(expected, SatelliteClientKey.IsHashed(stored));
        }

        [Fact]
        public void Generate_IsBase64Url_With256Bits()
        {
            for (var i = 0; i < 20; i++)
            {
                var key = SatelliteClientKey.Generate();
                Assert.Equal(43, key.Length);
                Assert.DoesNotContain('=', key);
                Assert.DoesNotContain('+', key);
                Assert.DoesNotContain('/', key);
                Assert.True(key.Length >= SatelliteClientKey.MinimumKeyLength);
            }
        }

        [Theory]
        [InlineData("a", "a", true)]
        [InlineData("", "", true)]
        [InlineData("a", "b", false)]
        [InlineData("a", "aa", false)]
        [InlineData(null, "", false)]
        [InlineData("", null, false)]
        [InlineData(null, null, false)]
        public void FixedTimeEquals_ComparesValues_AndNeverMatchesNull(string a, string b, bool expected)
        {
            Assert.Equal(expected, SatelliteClientKey.FixedTimeEquals(a, b));
        }
    }

    public sealed class MqttPluginCredentialsTests : IDisposable
    {
        private readonly string _dir = Path.Combine(Path.GetTempPath(), "automatica-tests-" + Guid.NewGuid().ToString("N"));
        private static readonly byte[] Secret = Enumerable(32, 7);

        public MqttPluginCredentialsTests()
        {
            Directory.CreateDirectory(_dir);
        }

        public void Dispose()
        {
            if (Directory.Exists(_dir))
            {
                Directory.Delete(_dir, true);
            }
        }

        private static byte[] Enumerable(int length, byte value)
        {
            var b = new byte[length];
            Array.Fill(b, value);
            return b;
        }

        [Fact]
        public void Constructor_RejectsMissingOrShortSecrets()
        {
            Assert.Throws<ArgumentException>(() => new MqttPluginCredentials(null));
            Assert.Throws<ArgumentException>(() => new MqttPluginCredentials(new byte[31]));
            _ = new MqttPluginCredentials(new byte[32]);
        }

        [Fact]
        public void Password_IsVersionedBase64Url()
        {
            var password = new MqttPluginCredentials(Secret).GetPassword("sat1", Guid.Parse("11111111-1111-1111-1111-111111111111"));

            Assert.StartsWith("p1.", password);
            Assert.Equal(3 + 43, password.Length);
            Assert.DoesNotContain('=', password);
            Assert.DoesNotContain('+', password);
            Assert.DoesNotContain('/', password);
        }

        [Fact]
        public void Password_IsAnHmacOfSatelliteAndNode()
        {
            var node = Guid.Parse("11111111-1111-1111-1111-111111111111");
            using var hmac = new HMACSHA256(Secret);
            var expected = "p1." + Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes($"automatica-mqtt-plugin\nsat1\n{node:D}")))
                .TrimEnd('=').Replace('+', '-').Replace('/', '_');

            Assert.Equal(expected, new MqttPluginCredentials(Secret).GetPassword("sat1", node));
        }

        [Fact]
        public void Password_DependsOnSecretSatelliteAndNode()
        {
            var node = Guid.NewGuid();
            var a = new MqttPluginCredentials(Secret);
            var b = new MqttPluginCredentials(Enumerable(32, 8));

            Assert.Equal(a.GetPassword("sat1", node), new MqttPluginCredentials((byte[])Secret.Clone()).GetPassword("sat1", node));
            Assert.NotEqual(a.GetPassword("sat1", node), b.GetPassword("sat1", node));
            Assert.NotEqual(a.GetPassword("sat1", node), a.GetPassword("sat2", node));
            Assert.NotEqual(a.GetPassword("sat1", node), a.GetPassword("sat1", Guid.NewGuid()));
            // the separator keeps "sat1"+"1..." and "sat11"+"..." apart
            Assert.NotEqual(a.GetPassword("sat", node), a.GetPassword("sat\n", node));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("p1.")]
        public void Verify_EmptyOrTruncatedPassword_IsRejected(string password)
        {
            Assert.False(new MqttPluginCredentials(Secret).Verify("sat1", Guid.NewGuid(), password));
        }

        [Fact]
        public void Verify_AcceptsOnlyTheExactPassword()
        {
            var credentials = new MqttPluginCredentials(Secret);
            var node = Guid.NewGuid();
            var password = credentials.GetPassword("sat1", node);

            Assert.True(credentials.Verify("sat1", node, password));
            Assert.False(credentials.Verify("sat1", node, password.ToUpperInvariant()));
            Assert.False(credentials.Verify("sat1", node, password + "x"));
            Assert.False(credentials.Verify("SAT1", node, password));
        }

        [Fact]
        public void LoadOrCreateSecret_CreatesAndThenReusesTheKeyFile()
        {
            var path = Path.Combine(_dir, MqttPluginCredentials.FileName);

            var created = MqttPluginCredentials.LoadOrCreateSecret(path);
            var loaded = MqttPluginCredentials.LoadOrCreateSecret(path);

            Assert.Equal(32, created.Length);
            Assert.Equal(created, File.ReadAllBytes(path));
            Assert.Equal(created, loaded);
        }

        [Fact]
        public void LoadOrCreateSecret_ReplacesATooShortKeyFile()
        {
            var path = Path.Combine(_dir, MqttPluginCredentials.FileName);
            File.WriteAllBytes(path, new byte[] { 1, 2, 3 });

            var key = MqttPluginCredentials.LoadOrCreateSecret(path);

            Assert.Equal(32, key.Length);
            Assert.Equal(key, File.ReadAllBytes(path));
        }

        [Fact]
        public void LoadOrCreateSecret_UnwritableLocation_StillReturnsARandomKey()
        {
            var path = Path.Combine(_dir, "missing-directory", MqttPluginCredentials.FileName);

            var a = MqttPluginCredentials.LoadOrCreateSecret(path);
            var b = MqttPluginCredentials.LoadOrCreateSecret(path);

            Assert.Equal(32, a.Length);
            Assert.NotEqual(a, b);
            Assert.False(File.Exists(path));
        }

        [Fact]
        public void LoadOrCreateSecret_IsPrivateOnUnix()
        {
            if (OperatingSystem.IsWindows())
            {
                return;
            }

            var path = Path.Combine(_dir, MqttPluginCredentials.FileName);
            MqttPluginCredentials.LoadOrCreateSecret(path);

            Assert.Equal(UnixFileMode.UserRead | UnixFileMode.UserWrite, File.GetUnixFileMode(path));
        }
    }

    public class MqttConnectThrottleTests
    {
        private DateTime _now = new(2026, 3, 1, 8, 0, 0, DateTimeKind.Utc);

        private MqttConnectThrottle Create(int max = 3) => new(max, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(10), () => _now);

        [Fact]
        public void Defaults_AreTenFailuresInFiveMinutes()
        {
            var throttle = new MqttConnectThrottle();

            Assert.Equal(10, throttle.MaxFailures);
            Assert.Equal(TimeSpan.FromMinutes(5), throttle.Window);
            Assert.Equal(TimeSpan.FromMinutes(5), throttle.BlockTime);
        }

        [Fact]
        public void BelowTheLimit_IsNotBlocked()
        {
            var throttle = Create();
            throttle.Failed("a");
            throttle.Failed("a");

            Assert.False(throttle.IsBlocked("a"));
        }

        [Fact]
        public void AtTheLimit_IsBlockedForTheBlockTime()
        {
            var throttle = Create();
            for (var i = 0; i < 3; i++)
            {
                throttle.Failed("a");
            }

            Assert.True(throttle.IsBlocked("a"));
            Assert.False(throttle.IsBlocked("b"));
            _now = _now.AddMinutes(9);
            Assert.True(throttle.IsBlocked("a"));
            _now = _now.AddMinutes(2);
            Assert.False(throttle.IsBlocked("a"));
        }

        [Fact]
        public void FailuresOutsideTheWindow_AreForgotten()
        {
            var throttle = Create();
            throttle.Failed("a");
            throttle.Failed("a");
            _now = _now.AddMinutes(6);
            throttle.Failed("a");
            throttle.Failed("a");

            Assert.False(throttle.IsBlocked("a"));
        }

        [Fact]
        public void Success_ResetsTheFailures()
        {
            var throttle = Create();
            throttle.Failed("a");
            throttle.Failed("a");
            throttle.Succeeded("a");
            throttle.Failed("a");
            throttle.Failed("a");

            Assert.False(throttle.IsBlocked("a"));
        }

        [Fact]
        public void AfterABlock_TheCountStartsAgain()
        {
            var throttle = Create();
            for (var i = 0; i < 3; i++)
            {
                throttle.Failed("a");
            }
            _now = _now.AddMinutes(11);

            throttle.Failed("a");

            Assert.False(throttle.IsBlocked("a"));
        }

        [Fact]
        public void KeyOf_MapsIpv4MappedIpv6_AndHandlesOtherEndPoints()
        {
            Assert.Equal("192.168.1.5", MqttConnectThrottle.KeyOf(new IPEndPoint(IPAddress.Parse("::ffff:192.168.1.5"), 1)));
            Assert.Equal("192.168.1.5", MqttConnectThrottle.KeyOf(new IPEndPoint(IPAddress.Parse("192.168.1.5"), 2)));
            Assert.Equal("fe80::1", MqttConnectThrottle.KeyOf(new IPEndPoint(IPAddress.Parse("fe80::1"), 3)));
            Assert.Equal("unknown", MqttConnectThrottle.KeyOf(null));
            Assert.Equal(new DnsEndPoint("host", 1).ToString(), MqttConnectThrottle.KeyOf(new DnsEndPoint("host", 1)));
        }

        [Fact]
        public void PortIsNotPartOfTheKey_SoReconnectingFromANewPortStaysBlocked()
        {
            var throttle = Create();
            for (var port = 1; port <= 3; port++)
            {
                throttle.Failed(MqttConnectThrottle.KeyOf(new IPEndPoint(IPAddress.Loopback, port)));
            }

            Assert.True(throttle.IsBlocked(MqttConnectThrottle.KeyOf(new IPEndPoint(IPAddress.Loopback, 9999))));
        }
    }

    public class MqttBrokerAuthenticatorEdgeTests
    {
        private const string Key = "edge-made-up-satellite-key";
        private static readonly string KeyHash = SatelliteClientKey.Hash(Key);
        private static readonly EndPoint Remote = new IPEndPoint(IPAddress.Parse("10.0.0.9"), 1234);

        private sealed class ThrowingStore : IMqttSatelliteStore
        {
            public bool ThrowOnFind { get; init; }
            public bool ThrowOnNode { get; init; }
            public bool ThrowOnUpdate { get; init; }
            public Slave Satellite { get; init; }
            public int Updates;

            public Slave FindSatellite(string clientId) => ThrowOnFind ? throw new InvalidOperationException("db down") : Satellite;
            public Guid? GetNodeSatellite(Guid nodeId) => ThrowOnNode ? throw new InvalidOperationException("db down") : null;

            public void UpdateClientKey(Guid satelliteObjId, string storedKey)
            {
                Updates++;
                if (ThrowOnUpdate)
                {
                    throw new InvalidOperationException("read only");
                }
            }
        }

        private static MqttBrokerAuthenticator Create(IMqttSatelliteStore store, MqttConnectThrottle throttle = null) =>
            new(store, new MqttNodeRegistry(), new MqttPluginCredentials(RandomNumberGenerator.GetBytes(32)), throttle ?? new MqttConnectThrottle(), true, NullLogger.Instance);

        [Fact]
        public void DatabaseError_OnTheSatellite_IsServerUnavailable()
        {
            var result = Create(new ThrowingStore { ThrowOnFind = true }).Authenticate("sat1", "sat1", Key, Remote);

            Assert.Equal(MqttConnectReasonCode.ServerUnavailable, result.ReasonCode);
            Assert.Null(result.Identity);
        }

        [Fact]
        public void DatabaseError_OnTheNode_RejectsThePlugin()
        {
            var store = new ThrowingStore { ThrowOnNode = true, Satellite = new Slave { ObjId = Guid.NewGuid(), ClientId = "sat1", ClientKey = KeyHash } };

            var result = Create(store).Authenticate(Guid.NewGuid().ToString(), "sat1", Key, Remote);

            Assert.Equal(MqttConnectReasonCode.ClientIdentifierNotValid, result.ReasonCode);
        }

        [Fact]
        public void FailingRehash_StillAcceptsTheSatellite()
        {
            var store = new ThrowingStore { ThrowOnUpdate = true, Satellite = new Slave { ObjId = Guid.NewGuid(), ClientId = "sat1", ClientKey = "plain-made-up-key-1" } };

            var result = Create(store).Authenticate("sat1", "sat1", "plain-made-up-key-1", Remote);

            Assert.Equal(MqttConnectReasonCode.Success, result.ReasonCode);
            Assert.Equal(1, store.Updates);
        }

        [Fact]
        public void PlaintextKey_IsRehashedOnEverySuccessUntilStored()
        {
            // the plaintext check result is never cached: a failed store must not leave the plaintext accepted from the cache only
            var store = new ThrowingStore { ThrowOnUpdate = true, Satellite = new Slave { ObjId = Guid.NewGuid(), ClientId = "sat1", ClientKey = "plain-made-up-key-1" } };
            var auth = Create(store);

            auth.Authenticate("sat1", "sat1", "plain-made-up-key-1", Remote);
            auth.Authenticate("sat1", "sat1", "plain-made-up-key-1", Remote);

            Assert.Equal(2, store.Updates);
        }

        [Fact]
        public void SatelliteWithoutAKey_IsRejected()
        {
            var store = new ThrowingStore { Satellite = new Slave { ObjId = Guid.NewGuid(), ClientId = "sat1", ClientKey = null } };

            Assert.Equal(MqttConnectReasonCode.BadUserNameOrPassword, Create(store).Authenticate("sat1", "sat1", Key, Remote).ReasonCode);
        }

        [Fact]
        public void Success_ClearsEarlierFailuresOfTheAddress()
        {
            var store = new ThrowingStore { Satellite = new Slave { ObjId = Guid.NewGuid(), ClientId = "sat1", ClientKey = KeyHash } };
            var throttle = new MqttConnectThrottle(3, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5), () => new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            var auth = Create(store, throttle);

            auth.Authenticate("sat1", "sat1", "wrong-1", Remote);
            auth.Authenticate("sat1", "sat1", "wrong-2", Remote);
            Assert.Equal(MqttConnectReasonCode.Success, auth.Authenticate("sat1", "sat1", Key, Remote).ReasonCode);
            auth.Authenticate("sat1", "sat1", "wrong-3", Remote);
            auth.Authenticate("sat1", "sat1", "wrong-4", Remote);

            Assert.Equal(MqttConnectReasonCode.Success, auth.Authenticate("sat1", "sat1", Key, Remote).ReasonCode);
        }

        [Fact]
        public void ServerUnavailable_CountsAsAFailedAttempt()
        {
            var throttle = new MqttConnectThrottle(2, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5), () => new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            var auth = Create(new ThrowingStore { ThrowOnFind = true }, throttle);

            auth.Authenticate("sat1", "sat1", Key, Remote);
            auth.Authenticate("sat1", "sat1", Key, Remote);

            Assert.Equal(MqttConnectReasonCode.ConnectionRateExceeded, auth.Authenticate("sat1", "sat1", Key, Remote).ReasonCode);
        }

        [Fact]
        public void PluginIdentity_CarriesSatelliteAndNode()
        {
            var sat = new Slave { ObjId = Guid.NewGuid(), ClientId = "sat1", ClientKey = KeyHash };
            var registry = new MqttNodeRegistry();
            var node = Guid.NewGuid();
            registry.Register("sat1", node, Array.Empty<Guid>());
            var credentials = new MqttPluginCredentials(RandomNumberGenerator.GetBytes(32));
            var auth = new MqttBrokerAuthenticator(new ThrowingStore { Satellite = sat }, registry, credentials, new MqttConnectThrottle(), false, NullLogger.Instance);

            var result = auth.Authenticate(node.ToString(), "sat1", credentials.GetPassword("sat1", node), Remote);

            Assert.Equal(MqttConnectReasonCode.Success, result.ReasonCode);
            Assert.Equal(MqttClientRole.Plugin, result.Identity.Role);
            Assert.Equal(node, result.Identity.NodeId);
            Assert.Equal(sat.ObjId, result.Identity.SatelliteObjId);
            Assert.Equal("sat1", result.Identity.SatelliteClientId);
            Assert.False(result.Identity.LegacyCredentials);
            Assert.Equal($"plugin {node} on satellite sat1", result.Identity.ToString());
        }

        [Fact]
        public void LegacyPlugin_IsMarkedAsLegacy()
        {
            var sat = new Slave { ObjId = Guid.NewGuid(), ClientId = "sat1", ClientKey = KeyHash };
            var registry = new MqttNodeRegistry();
            var node = Guid.NewGuid();
            registry.Register("sat1", node, Array.Empty<Guid>());
            var auth = new MqttBrokerAuthenticator(new ThrowingStore { Satellite = sat }, registry, new MqttPluginCredentials(RandomNumberGenerator.GetBytes(32)),
                new MqttConnectThrottle(), true, NullLogger.Instance);

            var result = auth.Authenticate(node.ToString(), "sat1", Key, Remote);

            Assert.True(result.Identity.LegacyCredentials);
            Assert.Equal("satellite sat1", MqttClientIdentity.ForSatellite("sat1", Guid.Empty).ToString());
        }

        [Fact]
        public void GetIdentity_ReadsTheSessionItem()
        {
            var identity = MqttClientIdentity.ForSatellite("sat1", Guid.NewGuid());
            IDictionary items = new Hashtable { { MqttClientIdentity.SessionItemKey, identity } };

            Assert.Same(identity, MqttConnectionValidation.GetIdentity(items));
            Assert.Null(MqttConnectionValidation.GetIdentity(new Hashtable()));
            Assert.Null(MqttConnectionValidation.GetIdentity(new Hashtable { { MqttClientIdentity.SessionItemKey, "forged" } }));
            Assert.Null(MqttConnectionValidation.GetIdentity(null));
        }
    }

    public class MqttNodeRegistryTests
    {
        [Fact]
        public void CyclicNodeGraph_DoesNotHang()
        {
            var a = new NodeInstance { ObjId = Guid.NewGuid() };
            var b = new NodeInstance { ObjId = Guid.NewGuid() };
            a.InverseThis2ParentNodeInstanceNavigation.Add(b);
            b.InverseThis2ParentNodeInstanceNavigation.Add(a);
            var registry = new MqttNodeRegistry();

            registry.Register("sat1", a);

            Assert.True(registry.IsInPluginTree(a.ObjId, b.ObjId));
        }

        [Fact]
        public void VeryDeepTree_IsCutAtTheDepthLimit()
        {
            var root = new NodeInstance { ObjId = Guid.NewGuid() };
            var current = root;
            var all = new List<NodeInstance> { root };
            for (var i = 0; i < 100; i++)
            {
                var child = new NodeInstance { ObjId = Guid.NewGuid() };
                current.InverseThis2ParentNodeInstanceNavigation.Add(child);
                all.Add(child);
                current = child;
            }
            var registry = new MqttNodeRegistry();

            registry.Register("sat1", root);

            Assert.True(registry.IsInPluginTree(root.ObjId, all[64].ObjId));
            Assert.False(registry.IsInPluginTree(root.ObjId, all[66].ObjId));
        }

        [Fact]
        public void InvalidRegistrations_AreIgnored()
        {
            var registry = new MqttNodeRegistry();

            registry.Register(null, new NodeInstance { ObjId = Guid.NewGuid() });
            registry.Register("", new NodeInstance { ObjId = Guid.NewGuid() });
            registry.Register("sat1", (NodeInstance)null);

            Assert.Equal(0, registry.SatelliteCount);
        }

        [Fact]
        public void ReRegisteringAPlugin_OnAnotherSatellite_MovesIt()
        {
            var registry = new MqttNodeRegistry();
            var node = Guid.NewGuid();
            var child = Guid.NewGuid();
            registry.Register("sat1", node, new[] { child });

            registry.Register("sat2", node, new[] { child });

            Assert.False(registry.IsPluginNode("sat1", node));
            Assert.False(registry.IsInSatelliteTree("sat1", child));
            Assert.True(registry.IsPluginNode("sat2", node));
            Assert.True(registry.IsInSatelliteTree("sat2", child));
            Assert.Equal(1, registry.SatelliteCount);
        }

        [Fact]
        public void SatelliteTree_IsTheUnionOfItsPlugins_ButPluginTreesStaySeparate()
        {
            var registry = new MqttNodeRegistry();
            Guid p1 = Guid.NewGuid(), c1 = Guid.NewGuid(), p2 = Guid.NewGuid(), c2 = Guid.NewGuid();
            registry.Register("sat1", p1, new[] { c1 });
            registry.Register("sat1", p2, new[] { c2 });

            Assert.True(registry.IsInSatelliteTree("sat1", c1));
            Assert.True(registry.IsInSatelliteTree("sat1", c2));
            Assert.True(registry.IsInPluginTree(p1, p1));
            Assert.False(registry.IsInPluginTree(p1, c2));
            Assert.False(registry.IsInPluginTree(p2, c1));
        }

        [Fact]
        public void SatelliteIds_AreCaseSensitive()
        {
            var registry = new MqttNodeRegistry();
            var node = Guid.NewGuid();
            registry.Register("sat1", node, Array.Empty<Guid>());

            Assert.False(registry.IsPluginNode("SAT1", node));
            Assert.False(registry.IsInSatelliteTree("SAT1", node));
        }
    }

    public class MqttServerOptionsTests
    {
        private static IConfiguration Config(params (string Key, string Value)[] values)
        {
            var dict = new Dictionary<string, string>();
            foreach (var (k, v) in values)
            {
                dict[k] = v;
            }
            return new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
        }

        [Fact]
        public void Defaults_ListenOnLoopbackOnly_WithTls()
        {
            var options = ServiceCollectionExtensions.BuildMqttServerOptions(Config());

            Assert.True(options.DefaultEndpointOptions.IsEnabled);
            Assert.Equal(1883, options.DefaultEndpointOptions.Port);
            Assert.Equal(IPAddress.Loopback, options.DefaultEndpointOptions.BoundInterNetworkAddress);
            Assert.Equal(IPAddress.None, options.DefaultEndpointOptions.BoundInterNetworkV6Address);
            Assert.True(options.TlsEndpointOptions.IsEnabled);
            Assert.Equal(8883, options.TlsEndpointOptions.Port);
            Assert.Equal(IPAddress.Loopback, options.TlsEndpointOptions.BoundInterNetworkAddress);
            Assert.Equal(System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13, options.TlsEndpointOptions.SslProtocol);
        }

        [Fact]
        public void Ports_AreConfigurable()
        {
            var options = ServiceCollectionExtensions.BuildMqttServerOptions(Config(("server:mqttPort", "11883"), ("server:mqttTlsPort", "18883")));

            Assert.Equal(11883, options.DefaultEndpointOptions.Port);
            Assert.Equal(18883, options.TlsEndpointOptions.Port);
        }

        [Theory]
        [InlineData("false")]
        [InlineData("FALSE")]
        public void Tls_CanBeTurnedOff(string value)
        {
            var options = ServiceCollectionExtensions.BuildMqttServerOptions(Config(("server:mqttTls", value)));

            Assert.False(options.TlsEndpointOptions.IsEnabled);
        }
    }
}
