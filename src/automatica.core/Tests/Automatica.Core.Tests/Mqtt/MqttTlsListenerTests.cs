using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.EF.Models;
using Automatica.Core.Runtime.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using MQTTnet;
using MQTTnet.Server;
using Xunit;

namespace Automatica.Core.Tests.Mqtt
{
    /// <summary>
    /// The TLS listener with the core's authentication, and clients that pin the certificate fingerprint like satellites do
    /// </summary>
    public sealed class MqttTlsListenerTests : IAsyncLifetime
    {
        private const string Key = "tls-satellite-made-up-key-1";
        private static readonly string KeyHash = SatelliteClientKey.Hash(Key);

        private readonly int _port = MqttBrokerHost.FreePort();
        private readonly FakeMqttSatelliteStore _store = new();
        private readonly MqttNodeRegistry _registry = new();
        private readonly List<bool> _secure = new();
        private X509Certificate2 _certificate;
        private MqttServer _server;
        private MqttConnectionValidation _security;

        // like the core's provider: the loaded certificate, not the raw pfx bytes
        private sealed class CertificateProvider : MQTTnet.Certificates.ICertificateProvider
        {
            private readonly X509Certificate2 _certificate;
            public CertificateProvider(X509Certificate2 certificate) => _certificate = certificate;
            public X509Certificate2 GetCertificate() => _certificate;
        }

        private static (X509Certificate2 Certificate, byte[] Pfx) CreateCertificate()
        {
            using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var request = new CertificateRequest("CN=automatica-mqtt-broker-test", ecdsa, HashAlgorithmName.SHA256);
            request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(new OidCollection { new Oid("1.3.6.1.5.5.7.3.1") }, false));
            // fixed validity, the clients pin the fingerprint and don't check the dates
            using var certificate = request.CreateSelfSigned(new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2099, 1, 1, 0, 0, 0, TimeSpan.Zero));
            // exported and loaded again: an ephemeral key does not work with SslStream on windows
            var pfx = certificate.Export(X509ContentType.Pkcs12);
            return (X509CertificateLoader.LoadPkcs12(pfx, null), pfx);
        }

        public async Task InitializeAsync()
        {
            (_certificate, _) = CreateCertificate();
            _store.Satellites["sat1"] = new Slave { ObjId = Guid.NewGuid(), ClientId = "sat1", ClientKey = KeyHash };

            var options = new MqttServerOptionsBuilder()
                .WithoutDefaultEndpoint()
                .WithEncryptedEndpoint()
                .WithEncryptedEndpointPort(_port)
                .WithEncryptionSslProtocol(SslProtocols.Tls12 | SslProtocols.Tls13)
                .WithEncryptionCertificate(new CertificateProvider(_certificate))
                .Build();
            _server = new MqttServerFactory().CreateMqttServer(options);
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string> { { MqttConnectionValidation.BindConfigKey, "localhost" } }).Build();
            var authenticator = new MqttBrokerAuthenticator(_store, _registry, new MqttPluginCredentials(RandomNumberGenerator.GetBytes(32)), new MqttConnectThrottle(), true, NullLogger.Instance);
            _security = new MqttConnectionValidation(_server, options, config, _registry, authenticator, NullLogger.Instance);
            await _security.StartAsync(CancellationToken.None);
            _server.ValidatingConnectionAsync += e =>
            {
                lock (_secure)
                {
                    _secure.Add(e.IsSecureConnection);
                }
                return Task.CompletedTask;
            };
            await _server.StartAsync();
        }

        public async Task DisposeAsync()
        {
            await _security.StopAsync(CancellationToken.None);
            await _server.StopAsync(new MqttServerStopOptions());
            _server.Dispose();
            _certificate.Dispose();
        }

        private Exception LastError;

        private async Task<(IMqttClient Client, bool Connected)> Connect(string pinnedFingerprint, string password)
        {
            var client = new MqttClientFactory().CreateMqttClient();
            var options = new MqttClientOptionsBuilder()
                .WithTcpServer("127.0.0.1", _port)
                .WithClientId("sat1")
                .WithCredentials("sat1", password)
                .WithTimeout(TimeSpan.FromSeconds(5))
                .WithTlsOptions(o => o.UseTls().WithSslProtocols(SslProtocols.Tls12 | SslProtocols.Tls13).WithCertificateValidationHandler(e =>
                    // the name is not checked, the fingerprint is pinned
                    e.Certificate != null && MqttBrokerCertificate.Fingerprint(X509CertificateLoader.LoadCertificate(e.Certificate.GetRawCertData())) == pinnedFingerprint))
                .Build();
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                await client.ConnectAsync(options, cts.Token);
            }
            catch (Exception e)
            {
                // tls handshake or authentication failed
                LastError = e;
            }
            return (client, client.IsConnected);
        }

        [Fact]
        public void Fingerprint_IsTheLowercaseSha256OfTheDerCertificate()
        {
            var fingerprint = MqttBrokerCertificate.Fingerprint(_certificate);

            Assert.Equal(64, fingerprint.Length);
            Assert.Equal(Convert.ToHexString(SHA256.HashData(_certificate.RawData)).ToLowerInvariant(), fingerprint);
            Assert.Equal(fingerprint.ToLowerInvariant(), fingerprint);
        }

        [Fact]
        public async Task PinnedClient_ConnectsOverTls_AndIsAuthenticated()
        {
            var (client, connected) = await Connect(MqttBrokerCertificate.Fingerprint(_certificate), Key);

            Assert.True(connected, LastError?.ToString());
            lock (_secure)
            {
                Assert.Contains(true, _secure);
            }
            await client.DisconnectAsync();
            client.Dispose();
        }

        [Fact]
        public async Task WrongPinnedFingerprint_IsRefused()
        {
            var (client, connected) = await Connect(new string('0', 64), Key);

            Assert.False(connected);
            client.Dispose();
        }

        [Fact]
        public async Task Tls_DoesNotReplaceAuthentication()
        {
            var (client, connected) = await Connect(MqttBrokerCertificate.Fingerprint(_certificate), "wrong-made-up-key-12345");

            Assert.False(connected);
            client.Dispose();
        }

        [Fact]
        public async Task PlaintextClient_OnTheTlsPort_IsRefused()
        {
            var client = new MqttClientFactory().CreateMqttClient();
            var options = new MqttClientOptionsBuilder().WithTcpServer("127.0.0.1", _port).WithClientId("sat1")
                .WithCredentials("sat1", Key).WithTimeout(TimeSpan.FromSeconds(3)).Build();

            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                await client.ConnectAsync(options, cts.Token);
            }
            catch (Exception)
            {
                // expected
            }

            Assert.False(client.IsConnected);
            client.Dispose();
        }
    }
}
