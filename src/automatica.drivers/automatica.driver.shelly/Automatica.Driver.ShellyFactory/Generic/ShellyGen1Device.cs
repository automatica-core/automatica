#nullable enable annotations
using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.TelegramMonitor;
using Automatica.Core.Driver;
using Automatica.Driver.Shelly.CoIoT;
using Automatica.Driver.Shelly.Gen1;
using Automatica.Driver.Shelly.Rpc;
using Automatica.Driver.ShellyFactory.Catalog;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Automatica.Driver.ShellyFactory.Generic
{
    /// <summary>
    /// A Gen1 device: GET /status polls and the control URLs, CoIoT packets for push updates
    /// (the only updates of the sleeping battery devices).
    /// </summary>
    internal sealed class ShellyGen1Device : ShellyGenericDevice
    {
        private readonly object _clientLock = new();
        private int _wakeupFetchRunning;

        public ShellyGen1HttpClient? Client { get; private set; }
        public string? DeviceType { get; private set; }
        public string? Mode { get; private set; }
        public CoIoTDescription? Description { get; private set; }

        protected override Func<Guid, ShellyComponentType?> Catalog => ShellyGen1Catalog.Get;

        public ShellyGen1Device(IDriverContext driverContext, ITelegramMonitorInstance monitor) : base(driverContext, monitor)
        {
            DeviceType = GetPropertyValueString(ShellyGenericTemplates.ModelKey);
            Mode = GetPropertyValueString(ShellyGenericTemplates.ModeKey);
            Description = CoIoTDescription.TryParse(GetPropertyValueString(ShellyGenericTemplates.CoIoTDescriptionKey));
        }

        public override async Task<bool> Start(CancellationToken token = default)
        {
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
                    DriverContext.Logger.LogInformation($"Sleeping shelly {Mac} has no address yet, waiting for its CoIoT packets");
                    return true;
                }

                DriverContext.Logger.LogError($"Could not find shelly {Mac} in the local network, set its IP address");
                return false;
            }

            var client = CreateClient(Address);
            lock (_clientLock)
            {
                Client = client;
            }

            try
            {
                var info = await client.GetJsonAsync("/shelly", token);
                var mac = NormalizeMac(info.Value<string>("mac"));
                if (mac != Mac)
                {
                    DriverContext.Logger.LogError($"Invalid shelly id, expected {Mac} but device at {Address} reports {mac}");
                    ReleaseClient();
                    return false;
                }

                DeviceType = info.Value<string>("type") ?? DeviceType;

                if (Description == null)
                {
                    await TryLoadDescription(client, token);
                }

                await PollAsync(token);
            }
            catch (ShellyRpcException e)
            {
                if (Sleeping)
                {
                    DriverContext.Logger.LogInformation($"Sleeping shelly {Mac} at {Address} is not awake ({e.Message}), waiting for its CoIoT packets");
                    return true;
                }

                DriverContext.Logger.LogError($"Could not read shelly {Mac} at {Address}, device offline or not reachable: {e.Message}");
                ReleaseClient();
                return false;
            }

            Online = true;
            if (!Sleeping)
            {
                StartPolling();
            }

            return true;
        }

        private ShellyGen1HttpClient CreateClient(string address)
        {
            return new ShellyGen1HttpClient(address, GetPropertyValueString(ShellyGenericTemplates.UserNameKey),
                GetPropertyValueString(ShellyGenericTemplates.PasswordKey), Monitor, TimeSpan.FromSeconds(5));
        }

        private async Task TryLoadDescription(ShellyGen1HttpClient client, CancellationToken token)
        {
            try
            {
                if (await client.GetJsonAsync("/cit/d", token, TimeSpan.FromSeconds(3)) is JObject json)
                {
                    Description = CoIoTDescription.Parse(json);
                }
            }
            catch (ShellyRpcException e)
            {
                DriverContext.Logger.LogDebug($"No CoIoT description of shelly {Mac}: {e.Message}");
            }
        }

        protected override async Task PollAsync(CancellationToken token)
        {
            var client = Client;
            if (client == null)
            {
                return;
            }

            if (await client.GetJsonAsync("/status", token) is JObject status)
            {
                Online = true;
                foreach (var component in Components)
                {
                    component.ApplyFullStatus(status);
                }
            }
        }

        /// <summary>
        /// A CoIoT status packet of this device: apply the values (by the CoIoT description) and read /status while the device is awake.
        /// </summary>
        public void HandleCoIoT(CoIoTMessage message, IPAddress sender)
        {
            Online = true;
            if (string.IsNullOrWhiteSpace(GetPropertyValueString(ShellyFactory.DeviceIpPropertyKey)) && !IPAddress.None.Equals(sender) &&
                Address != sender.ToString())
            {
                Address = sender.ToString();
                ReleaseClient();
                lock (_clientLock)
                {
                    Client = CreateClient(Address);
                }
            }

            if (Description != null)
            {
                foreach (var value in message.Values)
                {
                    var sensor = Description.Get(value.SensorId);
                    if (sensor == null)
                    {
                        continue;
                    }

                    var (block, index) = sensor.BlockNameAndIndex;
                    foreach (var component in Components)
                    {
                        var blocks = component.ComponentType.CoIoTBlocks;
                        if (blocks.Length > 0 && (!blocks.Contains(block) || component.ComponentId != index && block != "device"))
                        {
                            continue;
                        }

                        component.ApplyCoIoT(sensor.Description, value.Value);
                    }
                }
            }

            if (Sleeping)
            {
                _ = FetchWhileAwake();
            }
            else
            {
                RequestPoll();
            }
        }

        private async Task FetchWhileAwake()
        {
            if (Interlocked.Exchange(ref _wakeupFetchRunning, 1) == 1)
            {
                return;
            }

            try
            {
                var client = Client;
                if (client == null && Address != null)
                {
                    lock (_clientLock)
                    {
                        Client ??= CreateClient(Address);
                        client = Client;
                    }
                }

                if (client == null)
                {
                    return;
                }

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(4));
                if (Description == null)
                {
                    await TryLoadDescription(client, cts.Token);
                }
                await PollAsync(cts.Token);
            }
            catch (Exception e)
            {
                DriverContext.Logger.LogDebug($"Sleeping shelly {Mac} went back to sleep before /status was read: {e.Message}");
            }
            finally
            {
                Interlocked.Exchange(ref _wakeupFetchRunning, 0);
            }
        }

        public override async Task WriteAsync(ShellyComponentNode component, ShellyDataPoint dataPoint, object value, CancellationToken token)
        {
            var url = dataPoint.Gen1Writer?.Invoke(value, component.ComponentId, new Gen1WriteContext(DeviceType, Mode, component.Status));
            if (url == null)
            {
                return;
            }

            var client = Client;
            if (client == null)
            {
                throw new InvalidOperationException($"shelly {Mac} is not reachable, cannot request {url}");
            }

            var answer = await client.GetJsonAsync(url, token);
            if (answer is JObject state && component.ComponentType.Layout == ShellyComponentLayout.Array)
            {
                // /relay/0, /roller/0, /light/0... answer with the new state of the component
                component.ApplyComponentStatus(state);
            }
        }

        private void ReleaseClient()
        {
            ShellyGen1HttpClient? client;
            lock (_clientLock)
            {
                client = Client;
                Client = null;
            }
            client?.Dispose();
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
