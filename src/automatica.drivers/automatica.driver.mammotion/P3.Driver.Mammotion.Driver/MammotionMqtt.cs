using System;
using System.Buffers;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Formatter;

namespace P3.Driver.Mammotion.Driver
{
    /// <summary>
    /// The MQTT connection to the Mammotion broker, replaceable in tests
    /// </summary>
    internal interface IMammotionMqtt : IAsyncDisposable
    {
        bool IsConnected { get; }

        Task ConnectAsync(MqttCredentials credentials, IReadOnlyCollection<string> topics, Func<string, byte[], Task> onMessage, CancellationToken token);

        Task DisconnectAsync();
    }

    internal sealed class MqttNetConnection : IMammotionMqtt
    {
        private IMqttClient _client;

        public bool IsConnected => _client?.IsConnected == true;

        /// <summary>
        /// "host", "host:port" or "scheme://host:port", TLS on 8883 unless given otherwise
        /// </summary>
        internal static (string Host, int Port, bool Tls) ParseHost(string host)
        {
            var value = host?.Trim() ?? "";
            var tls = true;
            var schemeEnd = value.IndexOf("://", StringComparison.Ordinal);
            if (schemeEnd >= 0)
            {
                var scheme = value[..schemeEnd].ToLowerInvariant();
                tls = scheme is "ssl" or "mqtts" or "tls" or "wss";
                value = value[(schemeEnd + 3)..];
            }
            value = value.TrimEnd('/');
            var port = tls ? 8883 : 1883;
            var colon = value.LastIndexOf(':');
            if (colon > 0 && int.TryParse(value[(colon + 1)..], out var p))
            {
                port = p;
                value = value[..colon];
            }
            return (value, port, tls);
        }

        public async Task ConnectAsync(MqttCredentials credentials, IReadOnlyCollection<string> topics, Func<string, byte[], Task> onMessage, CancellationToken token)
        {
            await DisconnectAsync();

            var factory = new MqttClientFactory();
            var client = factory.CreateMqttClient();
            client.ApplicationMessageReceivedAsync += e => onMessage(e.ApplicationMessage.Topic, e.ApplicationMessage.Payload.ToArray());

            var (host, port, tls) = ParseHost(credentials.Host);
            var builder = new MqttClientOptionsBuilder()
                .WithTcpServer(host, port)
                .WithClientId(credentials.ClientId)
                .WithCredentials(credentials.UserName, credentials.Password)
                .WithProtocolVersion(MqttProtocolVersion.V311)
                .WithCleanSession()
                .WithKeepAlivePeriod(TimeSpan.FromSeconds(60));
            if (tls)
            {
                builder = builder.WithTlsOptions(o => o.UseTls());
            }

            await client.ConnectAsync(builder.Build(), token);
            var subscribe = factory.CreateSubscribeOptionsBuilder();
            foreach (var topic in topics)
            {
                subscribe.WithTopicFilter(topic);
            }
            await client.SubscribeAsync(subscribe.Build(), token);
            _client = client;
        }

        public async Task DisconnectAsync()
        {
            var client = _client;
            _client = null;
            if (client == null)
            {
                return;
            }
            try
            {
                if (client.IsConnected)
                {
                    await client.DisconnectAsync();
                }
            }
            catch
            {
                // already gone
            }
            client.Dispose();
        }

        public async ValueTask DisposeAsync()
        {
            await DisconnectAsync();
        }
    }
}
