using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Runtime.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using MQTTnet;
using MQTTnet.Formatter;
using MQTTnet.Protocol;
using MQTTnet.Server;

namespace Automatica.Core.Tests.Mqtt
{
    /// <summary>
    /// A real MQTTnet broker on a loopback ephemeral port with the core's authenticator, client id binding and topic ACL
    /// </summary>
    internal sealed class MqttBrokerHost : IAsyncDisposable
    {
        public FakeMqttSatelliteStore Store { get; } = new();
        public MqttNodeRegistry Registry { get; } = new();
        public MqttPluginCredentials Credentials { get; } = new(RandomNumberGenerator.GetBytes(32));
        public MqttConnectThrottle Throttle { get; }
        public MqttServer Server { get; }
        public MqttServerOptions Options { get; }
        public MqttConnectionValidation Security { get; }
        public MqttBrokerAuthenticator Authenticator { get; }
        public IConfiguration Configuration { get; }
        public int Port { get; }

        /// <summary>
        /// Topics of client publishes the server processed (after the ACL)
        /// </summary>
        public ConcurrentQueue<(string ClientId, string Topic)> Processed { get; } = new();

        private readonly ConcurrentDictionary<string, MqttConnectReasonCode> _connectResults = new();
        private readonly List<IMqttClient> _clients = new();

        public MqttBrokerHost(bool allowLegacyPluginCredentials = true, MqttConnectThrottle throttle = null, IDictionary<string, string> config = null)
        {
            Port = FreePort();
            Throttle = throttle ?? new MqttConnectThrottle();
            Options = new MqttServerOptionsBuilder().WithDefaultEndpoint().WithDefaultEndpointPort(Port).Build();
            Server = new MqttServerFactory().CreateMqttServer(Options);

            var settings = new Dictionary<string, string> { { MqttConnectionValidation.BindConfigKey, "localhost" }, { "server:mqttTls", "false" } };
            if (config != null)
            {
                foreach (var c in config)
                {
                    settings[c.Key] = c.Value;
                }
            }
            Configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

            Authenticator = new MqttBrokerAuthenticator(Store, Registry, Credentials, Throttle, allowLegacyPluginCredentials, NullLogger.Instance);
            Security = new MqttConnectionValidation(Server, Options, Configuration, Registry, Authenticator, NullLogger.Instance);
        }

        public async Task StartAsync()
        {
            await Security.StartAsync(CancellationToken.None);

            // attached after the validation: sees its result
            Server.ValidatingConnectionAsync += e =>
            {
                _connectResults[Key(e.ClientId, e.UserName, e.Password)] = e.ReasonCode;
                return Task.CompletedTask;
            };
            Server.InterceptingPublishAsync += e =>
            {
                if (e.ProcessPublish && !String.IsNullOrEmpty(e.ClientId))
                {
                    Processed.Enqueue((e.ClientId, e.ApplicationMessage.Topic));
                }
                return Task.CompletedTask;
            };

            await Server.StartAsync();
        }

        private static string Key(string clientId, string user, string password) => $"{clientId}|{user}|{password}";

        public static int FreePort()
        {
            var l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();
            var port = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();
            return port;
        }

        public sealed class Client
        {
            public IMqttClient Mqtt { get; init; }
            public MqttConnectReasonCode Result { get; set; }
            public ConcurrentQueue<MqttApplicationMessage> Received { get; } = new();
            public bool Connected => Mqtt.IsConnected;

            public async Task<MqttClientSubscribeResultCode> Subscribe(string filter, MqttQualityOfServiceLevel qos = MqttQualityOfServiceLevel.AtLeastOnce)
            {
                var result = await Mqtt.SubscribeAsync(filter, qos);
                return System.Linq.Enumerable.First(result.Items).ResultCode;
            }

            public async Task<MqttClientPublishReasonCode> Publish(string topic, string payload = "{}")
            {
                var result = await Mqtt.PublishStringAsync(topic, payload, MqttQualityOfServiceLevel.AtLeastOnce);
                // accepted by the ACL, just nobody listening
                return result.ReasonCode == MqttClientPublishReasonCode.NoMatchingSubscribers ? MqttClientPublishReasonCode.Success : result.ReasonCode;
            }
        }

        public async Task<Client> Connect(string clientId, string user, string password, MqttProtocolVersion version = MqttProtocolVersion.V500, string willTopic = null)
        {
            var mqtt = new MqttClientFactory().CreateMqttClient();
            var client = new Client { Mqtt = mqtt };
            mqtt.ApplicationMessageReceivedAsync += e =>
            {
                client.Received.Enqueue(e.ApplicationMessage);
                return Task.CompletedTask;
            };

            var builder = new MqttClientOptionsBuilder()
                .WithTcpServer("127.0.0.1", Port)
                .WithClientId(clientId)
                .WithCredentials(user, password)
                .WithProtocolVersion(version)
                .WithTimeout(TimeSpan.FromSeconds(5))
                .WithCleanSession();
            if (willTopic != null)
            {
                builder = builder.WithWillTopic(willTopic);
            }

            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                await mqtt.ConnectAsync(builder.Build(), cts.Token);
            }
            catch (MQTTnet.Adapter.MqttConnectingFailedException)
            {
                // the reason code is taken from the server side
            }

            lock (_clients)
            {
                _clients.Add(mqtt);
            }

            client.Result = _connectResults.TryGetValue(Key(clientId, user, password), out var code) ? code : MqttConnectReasonCode.UnspecifiedError;
            return client;
        }

        public static async Task WaitFor(Func<bool> condition, int timeoutMs = 5000)
        {
            var until = Environment.TickCount64 + timeoutMs;
            while (!condition() && Environment.TickCount64 < until)
            {
                await Task.Delay(10);
            }
        }

        public async ValueTask DisposeAsync()
        {
            List<IMqttClient> clients;
            lock (_clients)
            {
                clients = new List<IMqttClient>(_clients);
            }
            foreach (var c in clients)
            {
                try
                {
                    if (c.IsConnected)
                    {
                        await c.DisconnectAsync();
                    }
                }
                catch (Exception)
                {
                    // already gone
                }
                c.Dispose();
            }

            await Security.StopAsync(CancellationToken.None);
            if (Server.IsStarted)
            {
                await Server.StopAsync(new MqttServerStopOptions());
            }
            Server.Dispose();
        }
    }
}
