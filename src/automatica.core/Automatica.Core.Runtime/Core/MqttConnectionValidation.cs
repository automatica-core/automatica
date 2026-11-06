using System;
using System.Collections;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.Common;
using Automatica.Core.EF.Models;
using Automatica.Core.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MQTTnet.Protocol;
using MQTTnet.Server;

namespace Automatica.Core.Runtime.Core
{
    /// <summary>
    /// Security of the MQTT broker: CONNECT authentication with ClientId binding, the topic ACL for publishes and
    /// subscriptions, and the address the plaintext listener binds to. Must be registered before AddHostedMqttServer so
    /// it runs before the server accepts connections.
    /// </summary>
    internal class MqttConnectionValidation : IHostedService
    {
        public const string BindConfigKey = "server:mqttBind";

        private readonly MqttServer _mqttServer;
        private readonly MqttServerOptions _serverOptions;
        private readonly IConfiguration _configuration;
        private readonly MqttNodeRegistry _registry;
        private readonly MqttBrokerAuthenticator _authenticator;
        private readonly ILogger _logger;
        private readonly SemaphoreSlim _rebindLock = new(1, 1);

        public MqttConnectionValidation(MqttServer mqttServer, MqttServerOptions serverOptions, IConfiguration configuration,
            MqttNodeRegistry registry, MqttBrokerAuthenticator authenticator, ILogger logger = null)
        {
            _mqttServer = mqttServer;
            _serverOptions = serverOptions;
            _configuration = configuration;
            _registry = registry;
            _authenticator = authenticator;
            _logger = logger ?? new CoreLogger(configuration, null);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _mqttServer.ValidatingConnectionAsync += Validate;
            _mqttServer.InterceptingSubscriptionAsync += InterceptSubscription;
            _mqttServer.InterceptingPublishAsync += InterceptPublish;

            ApplyBinding(ResolveBindAddress());
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _mqttServer.ValidatingConnectionAsync -= Validate;
            _mqttServer.InterceptingSubscriptionAsync -= InterceptSubscription;
            _mqttServer.InterceptingPublishAsync -= InterceptPublish;
            return Task.CompletedTask;
        }

        internal static MqttClientIdentity GetIdentity(IDictionary sessionItems)
        {
            return sessionItems?[MqttClientIdentity.SessionItemKey] as MqttClientIdentity;
        }

        private Task Validate(ValidatingConnectionEventArgs context)
        {
            var result = _authenticator.Authenticate(context.ClientId, context.UserName, context.Password, context.RemoteEndPoint);

            // MQTTnet maps the reason code to the MQTT 3.1.1 return code for older clients
            context.ReasonCode = result.ReasonCode;
            if (result.Identity != null)
            {
                context.SessionItems[MqttClientIdentity.SessionItemKey] = result.Identity;
            }

            _logger.LogInformation($"Validating connection from {context.RemoteEndPoint} clientId: {context.ClientId}, userName: {context.UserName}, " +
                                   $"tls: {context.IsSecureConnection}, reasonCode: {context.ReasonCode}{(result.Identity != null ? $" ({result.Identity})" : "")}");
            return Task.CompletedTask;
        }

        private Task InterceptSubscription(InterceptingSubscriptionEventArgs e)
        {
            // subscriptions the server creates itself have no client
            if (String.IsNullOrEmpty(e.ClientId))
            {
                return Task.CompletedTask;
            }

            var identity = GetIdentity(e.SessionItems);
            if (!MqttAccessControl.CanSubscribe(identity, e.TopicFilter.Topic, _registry))
            {
                e.ProcessSubscription = false;
                e.Response.ReasonCode = MqttSubscribeReasonCode.NotAuthorized;
                _logger.LogWarning($"Denied subscription of {e.ClientId} ({identity?.ToString() ?? "unknown"}) to {e.TopicFilter.Topic}");
            }
            return Task.CompletedTask;
        }

        private Task InterceptPublish(InterceptingPublishEventArgs e)
        {
            // messages the server injects itself have no client id
            if (String.IsNullOrEmpty(e.ClientId))
            {
                return Task.CompletedTask;
            }

            var identity = GetIdentity(e.SessionItems);
            if (!MqttAccessControl.CanPublish(identity, e.ApplicationMessage.Topic, _registry))
            {
                e.ProcessPublish = false;
                e.Response.ReasonCode = MqttPubAckReasonCode.NotAuthorized;
                _logger.LogWarning($"Denied publish of {e.ClientId} ({identity?.ToString() ?? "unknown"}) to {e.ApplicationMessage.Topic}");
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// <c>server:mqttBind</c>: <c>auto</c> (default) binds the plaintext and the TLS listener to localhost while no
        /// satellite is configured and to all addresses otherwise, <c>any</c> always binds to all addresses (the old
        /// behaviour), <c>localhost</c> or an IP address bind to that address.
        /// </summary>
        internal IPAddress ResolveBindAddress()
        {
            var mode = _configuration[BindConfigKey];

            if (String.IsNullOrWhiteSpace(mode) || mode.Equals("auto", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    using var db = new AutomaticaContext(_configuration);
                    var selfSlaveId = ServerInfo.SelfSlaveGuid;
                    return db.Slaves.Any(a => a.ObjId != selfSlaveId) ? IPAddress.Any : IPAddress.Loopback;
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e, "Could not check for satellites, the mqtt broker listens on all addresses");
                    return IPAddress.Any;
                }
            }

            if (mode.Equals("any", StringComparison.OrdinalIgnoreCase))
            {
                return IPAddress.Any;
            }
            if (mode.Equals("localhost", StringComparison.OrdinalIgnoreCase) || mode.Equals("loopback", StringComparison.OrdinalIgnoreCase))
            {
                return IPAddress.Loopback;
            }
            if (IPAddress.TryParse(mode, out var address))
            {
                return address;
            }

            _logger.LogError($"Invalid {BindConfigKey} value {mode}, the mqtt broker listens on localhost only");
            return IPAddress.Loopback;
        }

        private void ApplyBinding(IPAddress address)
        {
            _serverOptions.DefaultEndpointOptions.BoundInterNetworkAddress = address;
            _serverOptions.TlsEndpointOptions.BoundInterNetworkAddress = address;

            _logger.LogInformation($"MQTT broker listens on {address}:{_serverOptions.DefaultEndpointOptions.Port}" +
                                   (_serverOptions.TlsEndpointOptions.IsEnabled ? $" and tls {address}:{_serverOptions.TlsEndpointOptions.Port}" : ""));
        }

        /// <summary>
        /// Re-evaluates the bind address (a satellite was added or removed) and restarts the broker if it changed
        /// </summary>
        public async Task RefreshBindingAsync()
        {
            if (!await _rebindLock.WaitAsync(0))
            {
                return;
            }

            try
            {
                var address = ResolveBindAddress();
                if (address.Equals(_serverOptions.DefaultEndpointOptions.BoundInterNetworkAddress))
                {
                    return;
                }

                ApplyBinding(address);

                if (_mqttServer.IsStarted)
                {
                    _logger.LogInformation("Restarting the mqtt broker for the new bind address");
                    await _mqttServer.StopAsync(new MqttServerStopOptions());
                    await _mqttServer.StartAsync();
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Could not rebind the mqtt broker");
            }
            finally
            {
                _rebindLock.Release();
            }
        }
    }
}
