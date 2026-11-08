#nullable enable annotations
using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.TelegramMonitor;
using Automatica.Core.Driver;
using Automatica.Driver.Shelly.Rpc;
using Automatica.Driver.ShellyFactory.Catalog;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Automatica.Driver.ShellyFactory.Generic
{
    /// <summary>
    /// A Gen2 / Gen3 / Gen4 device: JSON-RPC calls, live updates over the websocket, a slow poll as fallback.
    /// Sleeping (battery) devices cannot be reached most of the time: they report through the outbound websocket server of the driver.
    /// </summary>
    internal sealed class ShellyRpcDevice : ShellyGenericDevice
    {
        private readonly object _clientLock = new();

        public ShellyRpcClient? Client { get; private set; }

        protected override Func<Guid, ShellyComponentType?> Catalog => ShellyRpcCatalog.Get;

        public ShellyRpcDevice(IDriverContext driverContext, ITelegramMonitorInstance monitor) : base(driverContext, monitor)
        {
        }

        public override async Task<bool> Start(CancellationToken token = default)
        {
            // Start is retried on the same instance: release what a previous attempt created
            StopPolling();
            ReleaseClient();

            if (!await base.Start(token))
            {
                return false;
            }

            Address = ResolveAddress();
            if (Address == null)
            {
                if (Sleeping)
                {
                    DriverContext.Logger.LogInformation($"Sleeping shelly {Mac} has no address yet, waiting for it to connect to the outbound websocket");
                    return true;
                }

                DriverContext.Logger.LogError($"Could not find shelly {Mac} in the local network, set its IP address");
                return false;
            }

            var client = CreateClient(Address);

            JObject status;
            try
            {
                var info = await client.CallAsync("Shelly.GetDeviceInfo", null, token) as JObject;
                var mac = NormalizeMac(info?.Value<string>("mac"));
                if (mac.Length == 0)
                {
                    throw new ShellyRpcException(500, "no mac in Shelly.GetDeviceInfo");
                }

                if (mac != Mac)
                {
                    DriverContext.Logger.LogError($"Invalid shelly id, expected {Mac} but device at {Address} reports {mac}");
                    client.Dispose();
                    return false;
                }

                status = await client.CallAsync("Shelly.GetStatus", null, token) as JObject ?? new JObject();
            }
            catch (ShellyRpcException e)
            {
                client.Dispose();
                if (Sleeping)
                {
                    DriverContext.Logger.LogInformation($"Sleeping shelly {Mac} at {Address} is not awake ({e.Message}), waiting for it to connect to the outbound websocket");
                    return true;
                }

                DriverContext.Logger.LogError($"Could not read shelly {Mac} at {Address}, device offline or not reachable: {e.Message}");
                return false;
            }

            lock (_clientLock)
            {
                Client = client;
            }
            Online = true;
            client.Notification += OnNotification;
            ApplyFullStatus(status);

            if (!Sleeping)
            {
                if (!await client.ConnectAsync(token))
                {
                    DriverContext.Logger.LogWarning($"Websocket of shelly {Mac} at {Address} is not connected yet, it keeps retrying; values are polled meanwhile");
                }

                StartPolling();
            }

            return true;
        }

        private ShellyRpcClient CreateClient(string address)
        {
            return new ShellyRpcClient(address, GetPropertyValueString(ShellyGenericTemplates.PasswordKey), DriverContext.Logger, Monitor,
                TimeSpan.FromSeconds(5), $"automatica-{Id:N}");
        }

        private void OnNotification(string method, JObject parameters)
        {
            HandleNotification(method, parameters);
        }

        /// <summary>
        /// A notification of the device: over the own websocket or over the outbound websocket server of the driver.
        /// </summary>
        public void HandleNotification(string method, JObject parameters, IPAddress? remoteAddress = null)
        {
            Online = true;
            if (remoteAddress != null && !IPAddress.None.Equals(remoteAddress) && string.IsNullOrWhiteSpace(GetPropertyValueString(ShellyFactory.DeviceIpPropertyKey)))
            {
                Address = remoteAddress.ToString();
            }

            switch (method)
            {
                case "NotifyFullStatus":
                    ApplyFullStatus(parameters);
                    break;
                case "NotifyStatus":
                    foreach (var component in Components)
                    {
                        component.ApplyPartialStatus(parameters);
                    }
                    break;
                case "NotifyEvent":
                    if (parameters["events"] is JArray events)
                    {
                        foreach (var e in events.OfType<JObject>())
                        {
                            var key = e.Value<string>("component");
                            var name = e.Value<string>("event");
                            if (key == null || name == null)
                            {
                                continue;
                            }

                            foreach (var component in Components.Where(a => a.Key == key))
                            {
                                component.ApplyEvent(name);
                            }
                        }
                    }
                    break;
            }
        }

        private void ApplyFullStatus(JObject status)
        {
            foreach (var component in Components)
            {
                component.ApplyFullStatus(status);
            }
        }

        protected override async Task PollAsync(CancellationToken token)
        {
            var client = Client;
            if (client == null)
            {
                return;
            }

            if (await client.CallAsync("Shelly.GetStatus", null, token) is JObject status)
            {
                Online = true;
                ApplyFullStatus(status);
            }
        }

        public override async Task WriteAsync(ShellyComponentNode component, ShellyDataPoint dataPoint, object value, CancellationToken token)
        {
            var request = dataPoint.RpcWriter?.Invoke(value, component.ComponentId, component.Status as JObject);
            if (request == null)
            {
                return;
            }

            var client = Client;
            if (client == null)
            {
                if (Sleeping || Address == null)
                {
                    throw new InvalidOperationException($"shelly {Mac} is asleep or not reachable, cannot call {request.Value.Method}");
                }

                client = CreateClient(Address);
                lock (_clientLock)
                {
                    Client ??= client;
                }
            }

            await client.CallAsync(request.Value.Method, request.Value.Params, token);
        }

        private void ReleaseClient()
        {
            ShellyRpcClient? client;
            lock (_clientLock)
            {
                client = Client;
                Client = null;
            }

            if (client != null)
            {
                client.Notification -= OnNotification;
                client.Dispose();
            }
        }

        public override async Task<bool> Stop(CancellationToken token = default)
        {
            StopPolling();
            ReleaseClient();
            Online = false;
            return await base.Stop(token);
        }
    }
}
