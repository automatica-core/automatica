using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using MQTTnet.Client.Receiving;
using MQTTnet.Protocol;
using MQTTnet.Server;

namespace Automatica.Core.Plugin.Standalone.Tests.Infrastructure
{
    internal sealed class ConnectAttempt
    {
        public string ClientId { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string WillTopic { get; set; }
    }

    internal sealed class Received
    {
        public string Topic { get; set; }
        public string Payload { get; set; }
        public bool Retain { get; set; }
        public MqttQualityOfServiceLevel Qos { get; set; }
    }

    /// <summary>
    /// A real MQTTnet 3 broker (the version the plugin host uses) with a TLS listener on a loopback ephemeral port
    /// </summary>
    internal sealed class PluginTestBroker : IAsyncDisposable
    {
        private readonly IMqttServer _server;

        public int Port { get; }
        public X509Certificate2 Certificate { get; }
        public string Fingerprint { get; }

        public ConcurrentQueue<ConnectAttempt> Connects { get; } = new ConcurrentQueue<ConnectAttempt>();
        public ConcurrentQueue<(string ClientId, string Topic)> Subscriptions { get; } = new ConcurrentQueue<(string, string)>();
        public ConcurrentQueue<(string ClientId, string Topic, string Payload, bool Retain)> Published { get; } = new ConcurrentQueue<(string, string, string, bool)>();

        private PluginTestBroker(IMqttServer server, int port, X509Certificate2 certificate)
        {
            _server = server;
            Port = port;
            Certificate = certificate;
            Fingerprint = TestCertificates.Fingerprint(certificate);
        }

        public static async Task<PluginTestBroker> StartAsync()
        {
            var certificate = TestCertificates.SharedServerCertificate();
            var port = TestPorts.Free();
            var server = new MqttFactory().CreateMqttServer();
            var broker = new PluginTestBroker(server, port, certificate);

            var options = new MqttServerOptionsBuilder()
                .WithoutDefaultEndpoint()
                .WithEncryptedEndpoint()
                .WithEncryptedEndpointBoundIPAddress(IPAddress.Loopback)
                .WithEncryptedEndpointBoundIPV6Address(IPAddress.None)
                .WithEncryptedEndpointPort(port)
                .WithEncryptionCertificate(certificate)
                .WithEncryptionSslProtocol(SslProtocols.Tls12)
                .WithConnectionValidator(c =>
                {
                    broker.Connects.Enqueue(new ConnectAttempt { ClientId = c.ClientId, Username = c.Username, Password = c.Password, WillTopic = c.WillMessage?.Topic });
                    c.ReasonCode = MqttConnectReasonCode.Success;
                })
                .WithSubscriptionInterceptor(c =>
                {
                    broker.Subscriptions.Enqueue((c.ClientId, c.TopicFilter.Topic));
                    c.AcceptSubscription = true;
                })
                .WithApplicationMessageInterceptor(c =>
                {
                    broker.Published.Enqueue((c.ClientId, c.ApplicationMessage.Topic,
                        c.ApplicationMessage.Payload == null ? null : Encoding.UTF8.GetString(c.ApplicationMessage.Payload), c.ApplicationMessage.Retain));
                    c.AcceptPublish = true;
                })
                .Build();

            await server.StartAsync(options);
            return broker;
        }

        public bool HasSubscription(string clientId, string topic) => Subscriptions.Any(s => s.ClientId == clientId && s.Topic == topic);

        /// <summary>
        /// A client that plays the core, connected over TLS (it trusts the test certificate)
        /// </summary>
        public async Task<CoreClient> ConnectCoreAsync()
        {
            var options = new MqttClientOptionsBuilder()
                .WithClientId("core-test-" + Guid.NewGuid().ToString("N"))
                .WithTcpServer("127.0.0.1", Port)
                .WithCleanSession()
                .WithTls(new MqttClientOptionsBuilderTlsParameters
                {
                    UseTls = true,
                    SslProtocol = SslProtocols.Tls12,
                    CertificateValidationHandler = ctx => ctx.Certificate != null && ctx.Certificate.GetCertHashString() == Certificate.GetCertHashString()
                })
                .Build();
            // the test side only: a TLS handshake on the Windows test host occasionally stalls, so the core
            // client gets a short timeout and a couple of fresh attempts; the plugin under test is not retried
            for (var attempt = 1; ; attempt++)
            {
                var core = new CoreClient(new MqttFactory().CreateMqttClient());
                try
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(4));
                    await core.Client.ConnectAsync(options, cts.Token);
                    return core;
                }
                catch (Exception) when (attempt < 3)
                {
                    await core.DisposeAsync();
                }
            }
        }

        public async Task<int> ConnectedClientCountAsync() => (await _server.GetClientStatusAsync()).Count;

        public async ValueTask DisposeAsync()
        {
            await _server.StopAsync();
            _server.Dispose();
        }
    }

    internal sealed class CoreClient : IAsyncDisposable
    {
        public IMqttClient Client { get; }
        public ConcurrentQueue<Received> Received { get; } = new ConcurrentQueue<Received>();

        public CoreClient(IMqttClient client)
        {
            Client = client;
            Client.ApplicationMessageReceivedHandler = new MqttApplicationMessageReceivedHandlerDelegate(e =>
            {
                Received.Enqueue(new Received
                {
                    Topic = e.ApplicationMessage.Topic,
                    Payload = e.ApplicationMessage.Payload == null ? null : Encoding.UTF8.GetString(e.ApplicationMessage.Payload),
                    Retain = e.ApplicationMessage.Retain,
                    Qos = e.ApplicationMessage.QualityOfServiceLevel
                });
            });
        }

        public Task SubscribeAsync(string topic) =>
            Client.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic(topic).WithAtLeastOnceQoS().Build());

        public Task PublishAsync(string topic, string payload, bool retain = false) =>
            Client.PublishAsync(new MqttApplicationMessage
            {
                Topic = topic,
                Payload = payload == null ? null : Encoding.UTF8.GetBytes(payload),
                QualityOfServiceLevel = MqttQualityOfServiceLevel.AtLeastOnce,
                Retain = retain
            }, CancellationToken.None);

        public async ValueTask DisposeAsync()
        {
            if (Client.IsConnected)
            {
                await Client.DisconnectAsync();
            }
            Client.Dispose();
        }
    }
}
