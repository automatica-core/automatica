using System;
using System.Buffers;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Formatter;

namespace P3.Driver.BmwCarData.Driver
{
    internal sealed record BmwStreamOptions(string Host, int Port, bool Tls, string ClientId, string UserName, string Password, IReadOnlyCollection<string> Topics);

    /// <summary>
    /// The CarData stream (MQTT over TLS, QoS 0), replaceable in tests
    /// </summary>
    internal interface IBmwStream : IAsyncDisposable
    {
        /// <summary>
        /// Completes when the broker closes the connection (BMW closes it when the id token expires)
        /// </summary>
        Task Disconnected { get; }

        Task ConnectAsync(BmwStreamOptions options, Func<string, byte[], Task> onMessage, CancellationToken token);

        Task DisconnectAsync();
    }

    internal sealed class MqttNetStream : IBmwStream
    {
        private IMqttClient _client;
        private readonly TaskCompletionSource _disconnected = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task Disconnected => _disconnected.Task;

        /// <summary>
        /// "host", "host:port" or "scheme://host:port", TLS unless the scheme says otherwise
        /// </summary>
        internal static (string Host, int Port, bool Tls) ParseHost(string host, int defaultPort)
        {
            var value = host?.Trim() ?? "";
            var tls = true;
            var schemeEnd = value.IndexOf("://", StringComparison.Ordinal);
            if (schemeEnd >= 0)
            {
                var scheme = value[..schemeEnd].ToLowerInvariant();
                tls = scheme is "ssl" or "mqtts" or "tls";
                value = value[(schemeEnd + 3)..];
            }
            value = value.TrimEnd('/');
            var port = defaultPort;
            var colon = value.LastIndexOf(':');
            if (colon > 0 && int.TryParse(value[(colon + 1)..], out var p))
            {
                port = p;
                value = value[..colon];
            }
            return (value, port, tls);
        }

        public async Task ConnectAsync(BmwStreamOptions options, Func<string, byte[], Task> onMessage, CancellationToken token)
        {
            var factory = new MqttClientFactory();
            var client = factory.CreateMqttClient();
            client.ApplicationMessageReceivedAsync += e => onMessage(e.ApplicationMessage.Topic, e.ApplicationMessage.Payload.ToArray());
            client.DisconnectedAsync += _ =>
            {
                _disconnected.TrySetResult();
                return Task.CompletedTask;
            };

            var builder = new MqttClientOptionsBuilder()
                .WithTcpServer(options.Host, options.Port)
                .WithClientId(options.ClientId)
                .WithCredentials(options.UserName, options.Password)
                .WithProtocolVersion(MqttProtocolVersion.V311)
                .WithCleanSession()
                .WithKeepAlivePeriod(TimeSpan.FromSeconds(30));
            if (options.Tls)
            {
                builder = builder.WithTlsOptions(o => o.UseTls());
            }

            await client.ConnectAsync(builder.Build(), token);
            var subscribe = factory.CreateSubscribeOptionsBuilder();
            foreach (var topic in options.Topics)
            {
                subscribe.WithTopicFilter(topic, MQTTnet.Protocol.MqttQualityOfServiceLevel.AtMostOnce);
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
            _disconnected.TrySetResult();
        }

        public async ValueTask DisposeAsync()
        {
            await DisconnectAsync();
        }
    }
}
