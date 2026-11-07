using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Protocol;
using MQTTnet.Server;

namespace Automatica.Core.Satellite.Tests.Infrastructure
{
    internal sealed record ConnectAttempt(string ClientId, string UserName, string Password);

    internal sealed record SubscribedTopic(string ClientId, string Topic);

    internal sealed record PublishedMessage(string ClientId, string Topic, string Payload);

    /// <summary>
    /// A real MQTTnet 5 broker on a loopback ephemeral port (plain or TLS) that records what the clients do
    /// </summary>
    internal sealed class TestBroker : IAsyncDisposable
    {
        private readonly MqttServer _server;

        public int Port { get; }
        public bool Tls { get; }

        public ConcurrentQueue<ConnectAttempt> Connects { get; } = new();
        public ConcurrentQueue<SubscribedTopic> Subscriptions { get; } = new();
        public ConcurrentQueue<PublishedMessage> Published { get; } = new();

        private TestBroker(MqttServer server, int port, bool tls)
        {
            _server = server;
            Port = port;
            Tls = tls;

            _server.ValidatingConnectionAsync += e =>
            {
                Connects.Enqueue(new ConnectAttempt(e.ClientId, e.UserName, e.Password));
                return Task.CompletedTask;
            };
            _server.ClientSubscribedTopicAsync += e =>
            {
                Subscriptions.Enqueue(new SubscribedTopic(e.ClientId, e.TopicFilter.Topic));
                return Task.CompletedTask;
            };
            _server.InterceptingPublishAsync += e =>
            {
                Published.Enqueue(new PublishedMessage(e.ClientId, e.ApplicationMessage.Topic, e.ApplicationMessage.ConvertPayloadToString()));
                return Task.CompletedTask;
            };
        }

        public static async Task<TestBroker> StartAsync(X509Certificate2 tlsCertificate = null)
        {
            var port = TestPorts.Free();
            var builder = new MqttServerOptionsBuilder();
            if (tlsCertificate == null)
            {
                builder = builder
                    .WithDefaultEndpoint()
                    .WithDefaultEndpointBoundIPAddress(IPAddress.Loopback)
                    .WithDefaultEndpointBoundIPV6Address(IPAddress.None)
                    .WithDefaultEndpointPort(port);
            }
            else
            {
                builder = builder
                    .WithoutDefaultEndpoint()
                    .WithEncryptedEndpoint()
                    .WithEncryptedEndpointBoundIPAddress(IPAddress.Loopback)
                    .WithEncryptedEndpointBoundIPV6Address(IPAddress.None)
                    .WithEncryptedEndpointPort(port)
                    .WithEncryptionCertificate(tlsCertificate)
                    .WithEncryptionSslProtocol(SslProtocols.Tls12);
            }

            var server = new MqttServerFactory().CreateMqttServer(builder.Build());
            var broker = new TestBroker(server, port, tlsCertificate != null);
            await server.StartAsync();
            return broker;
        }

        public bool HasSubscription(string clientId, string topic) =>
            Subscriptions.Any(s => s.ClientId == clientId && s.Topic == topic);

        /// <summary>
        /// A client that plays the core
        /// </summary>
        public async Task<IMqttClient> ConnectClientAsync(string clientId = "core-test")
        {
            var client = new MqttClientFactory().CreateMqttClient();
            var options = new MqttClientOptionsBuilder()
                .WithTcpServer("127.0.0.1", Port)
                .WithClientId(clientId)
                .WithCleanSession()
                .Build();
            await client.ConnectAsync(options, CancellationTokens.Timeout());
            return client;
        }

        public Task InjectAsync(string topic, string payload)
        {
            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(Encoding.UTF8.GetBytes(payload ?? ""))
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                .Build();
            return _server.InjectApplicationMessage(new InjectedMqttApplicationMessage(message));
        }

        public async Task<int> ConnectedClientCountAsync() => (await _server.GetClientsAsync()).Count;

        /// <summary>
        /// The broker closes a client's connection (like a core restart would)
        /// </summary>
        public async Task DisconnectClientAsync(string clientId)
        {
            var client = (await _server.GetClientsAsync()).Single(c => c.Id == clientId);
            await client.DisconnectAsync(new MqttServerClientDisconnectOptions());
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                await _server.StopAsync();
            }
            catch (Exception)
            {
                // already stopped
            }
            _server.Dispose();
        }
    }
}
