using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography;
using Automatica.Core.EF.Models;
using Automatica.Core.Runtime.Core;
using Microsoft.Extensions.Logging.Abstractions;
using MQTTnet.Protocol;
using Xunit;

namespace Automatica.Core.Tests.Mqtt
{
    public class MqttBrokerAuthenticatorTests
    {
        private const string Key = "a-long-satellite-key-1234";
        private static readonly EndPoint Remote = new IPEndPoint(IPAddress.Parse("192.168.1.20"), 50000);

        private readonly FakeMqttSatelliteStore _store = new();
        private readonly MqttNodeRegistry _registry = new();
        private readonly MqttPluginCredentials _credentials = new(RandomNumberGenerator.GetBytes(32));
        private readonly Slave _sat = new() { ObjId = Guid.NewGuid(), ClientId = "sat1", ClientKey = SatelliteClientKey.Hash(Key) };
        private readonly Slave _sat2 = new() { ObjId = Guid.NewGuid(), ClientId = "sat2", ClientKey = SatelliteClientKey.Hash("other-satellite-key-5678") };
        private readonly Guid _node = Guid.NewGuid();
        private readonly Guid _nodeOfSat2 = Guid.NewGuid();

        public MqttBrokerAuthenticatorTests()
        {
            _store.Satellites[_sat.ClientId] = _sat;
            _store.Satellites[_sat2.ClientId] = _sat2;
            _registry.Register(_sat.ClientId, _node, Array.Empty<Guid>());
            _store.NodeSatellite[_nodeOfSat2] = _sat2.ObjId;
        }

        private MqttBrokerAuthenticator Create(bool legacy = true, MqttConnectThrottle throttle = null) =>
            new(_store, _registry, _credentials, throttle ?? new MqttConnectThrottle(), legacy, NullLogger.Instance);

        [Fact]
        public void Satellite_WithKey_IsAccepted()
        {
            var result = Create().Authenticate("sat1", "sat1", Key, Remote);

            Assert.Equal(MqttConnectReasonCode.Success, result.ReasonCode);
            Assert.Equal(MqttClientRole.Satellite, result.Identity.Role);
            Assert.Equal("sat1", result.Identity.SatelliteClientId);
        }

        [Theory]
        [InlineData("sat1", "sat1", "wrong")]
        [InlineData("sat1", "sat1", "")]
        [InlineData("sat1", "", "a-long-satellite-key-1234")]
        [InlineData("", "sat1", "a-long-satellite-key-1234")]
        [InlineData("unknown", "unknown", "a-long-satellite-key-1234")]
        public void Satellite_WithWrongCredentials_IsRejected(string clientId, string user, string password)
        {
            var result = Create().Authenticate(clientId, user, password, Remote);

            Assert.NotEqual(MqttConnectReasonCode.Success, result.ReasonCode);
            Assert.Null(result.Identity);
        }

        [Fact]
        public void ClientId_OfAnotherSatellite_IsRejected()
        {
            // the key of sat1 must not allow taking over sat2's session (or kicking it)
            var result = Create().Authenticate("sat2", "sat1", Key, Remote);

            Assert.Equal(MqttConnectReasonCode.ClientIdentifierNotValid, result.ReasonCode);
        }

        [Fact]
        public void PlainTextKey_IsMigratedToHash()
        {
            _sat.ClientKey = Key;

            var result = Create().Authenticate("sat1", "sat1", Key, Remote);

            Assert.Equal(MqttConnectReasonCode.Success, result.ReasonCode);
            Assert.Single(_store.Updates);
            Assert.True(SatelliteClientKey.IsHashed(_sat.ClientKey));
            Assert.True(SatelliteClientKey.Verify(Key, _sat.ClientKey, out var rehash));
            Assert.False(rehash);
        }

        [Fact]
        public void Plugin_WithOwnPassword_IsAccepted()
        {
            var password = _credentials.GetPassword("sat1", _node);

            var result = Create(legacy: false).Authenticate(_node.ToString(), "sat1", password, Remote);

            Assert.Equal(MqttConnectReasonCode.Success, result.ReasonCode);
            Assert.Equal(MqttClientRole.Plugin, result.Identity.Role);
            Assert.Equal(_node, result.Identity.NodeId);
            Assert.False(result.Identity.LegacyCredentials);
        }

        [Fact]
        public void Plugin_Password_DoesNotWorkForAnotherNodeOrTheSatellite()
        {
            var other = Guid.NewGuid();
            _registry.Register("sat1", other, Array.Empty<Guid>());
            var password = _credentials.GetPassword("sat1", _node);
            var auth = Create();

            Assert.Equal(MqttConnectReasonCode.BadUserNameOrPassword, auth.Authenticate(other.ToString(), "sat1", password, Remote).ReasonCode);
            Assert.Equal(MqttConnectReasonCode.BadUserNameOrPassword, auth.Authenticate("sat1", "sat1", password, Remote).ReasonCode);
        }

        [Fact]
        public void Plugin_WithSatelliteKey_OnlyWhileLegacyIsAllowed()
        {
            var legacy = Create(legacy: true).Authenticate(_node.ToString(), "sat1", Key, Remote);
            Assert.Equal(MqttConnectReasonCode.Success, legacy.ReasonCode);
            Assert.True(legacy.Identity.LegacyCredentials);
            Assert.Equal(MqttClientRole.Plugin, legacy.Identity.Role);

            var strict = Create(legacy: false).Authenticate(_node.ToString(), "sat1", Key, Remote);
            Assert.Equal(MqttConnectReasonCode.BadUserNameOrPassword, strict.ReasonCode);
        }

        [Fact]
        public void Plugin_ClientIdMustBeANodeOfTheSatellite()
        {
            var auth = Create();

            Assert.Equal(MqttConnectReasonCode.ClientIdentifierNotValid, auth.Authenticate(Guid.NewGuid().ToString(), "sat1", Key, Remote).ReasonCode);
            Assert.Equal(MqttConnectReasonCode.ClientIdentifierNotValid, auth.Authenticate(_nodeOfSat2.ToString(), "sat1", Key, Remote).ReasonCode);
            Assert.Equal(MqttConnectReasonCode.ClientIdentifierNotValid, auth.Authenticate("not-a-guid", "sat1", Key, Remote).ReasonCode);
        }

        [Fact]
        public void Plugin_NodeKnownOnlyInTheDatabase_IsAccepted()
        {
            var password = _credentials.GetPassword("sat2", _nodeOfSat2);

            var result = Create().Authenticate(_nodeOfSat2.ToString(), "sat2", password, Remote);

            Assert.Equal(MqttConnectReasonCode.Success, result.ReasonCode);
        }

        [Fact]
        public void SelfSatelliteRow_IsNeverAccepted()
        {
            _store.Satellites["local"] = new Slave { ObjId = Base.Common.ServerInfo.SelfSlaveGuid, ClientId = "local", ClientKey = "local" };

            Assert.NotEqual(MqttConnectReasonCode.Success, Create().Authenticate("local", "local", "local", Remote).ReasonCode);
        }

        [Fact]
        public void FailedConnects_BlockTheAddress()
        {
            var now = new DateTime(2026, 11, 6, 12, 0, 0, DateTimeKind.Utc);
            var throttle = new MqttConnectThrottle(3, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5), () => now);
            var auth = Create(throttle: throttle);

            for (var i = 0; i < 3; i++)
            {
                Assert.Equal(MqttConnectReasonCode.BadUserNameOrPassword, auth.Authenticate("sat1", "sat1", "wrong", Remote).ReasonCode);
            }

            // blocked even with the right key
            Assert.Equal(MqttConnectReasonCode.ConnectionRateExceeded, auth.Authenticate("sat1", "sat1", Key, Remote).ReasonCode);
            // other addresses are not affected
            Assert.Equal(MqttConnectReasonCode.Success, auth.Authenticate("sat1", "sat1", Key, new IPEndPoint(IPAddress.Parse("192.168.1.21"), 1)).ReasonCode);

            now = now.AddMinutes(6);
            Assert.Equal(MqttConnectReasonCode.Success, auth.Authenticate("sat1", "sat1", Key, Remote).ReasonCode);
        }

        [Fact]
        public void ClientKey_HashAndVerify()
        {
            var hash = SatelliteClientKey.Hash("secret-key-123456");

            Assert.StartsWith(SatelliteClientKey.Prefix, hash);
            Assert.True(SatelliteClientKey.Verify("secret-key-123456", hash, out var rehash));
            Assert.False(rehash);
            Assert.False(SatelliteClientKey.Verify("secret-key-123457", hash, out _));
            Assert.False(SatelliteClientKey.Verify("x", "pbkdf2-sha512$broken", out _));
            Assert.NotEqual(hash, SatelliteClientKey.Hash("secret-key-123456"));
        }

        [Fact]
        public void ClientKey_Generate_IsLongAndRandom()
        {
            var a = SatelliteClientKey.Generate();
            var b = SatelliteClientKey.Generate();

            Assert.True(a.Length >= 43);
            Assert.NotEqual(a, b);
        }
    }
}
