#nullable enable annotations
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Automatica.Core.Base.TelegramMonitor;
using Automatica.Core.Driver;
using Automatica.Driver.ShellyFactory.Catalog;
using Microsoft.Extensions.Logging;

namespace Automatica.Driver.ShellyFactory.Generic
{
    /// <summary>
    /// A Shelly device of the generic templates: the components below it come from the scan of the device.
    /// Address: the "shelly-ip" property, else the mDNS discovery by MAC.
    /// </summary>
    internal abstract class ShellyGenericDevice : DriverBase
    {
        private readonly Channel<bool> _pollNow = Channel.CreateBounded<bool>(new BoundedChannelOptions(1) { FullMode = BoundedChannelFullMode.DropWrite });
        private CancellationTokenSource? _pollCts;
        private Task? _pollLoop;

        protected ITelegramMonitorInstance Monitor { get; }

        public string Mac { get; }
        public string? Address { get; protected set; }
        public int PollingInterval { get; }
        public bool Sleeping { get; }
        public bool Online { get; protected set; }
        public List<ShellyComponentNode> Components { get; } = new();

        protected abstract Func<Guid, ShellyComponentType?> Catalog { get; }

        protected ShellyGenericDevice(IDriverContext driverContext, ITelegramMonitorInstance monitor) : base(driverContext)
        {
            Monitor = monitor ?? new EmptyTelegramMonitorInstance();
            Mac = NormalizeMac(GetPropertyValueString(ShellyFactory.DeviceIdPropertyKey));
            PollingInterval = GetPropertyValueInt(ShellyGenericTemplates.PollingIntervalKey);
            Sleeping = GetPropertyValueBool(ShellyGenericTemplates.SleepingKey) ?? false;
        }

        /// <summary>
        /// "A8:03:2A:B1:23:45", "a8032ab12345" and the Gen2+ device id "shellyplus1pm-a8032ab12345" all become "A8032AB12345".
        /// </summary>
        public static string NormalizeMac(string? mac)
        {
            var value = (mac ?? "").Trim();
            var dash = value.LastIndexOf('-');
            if (dash >= 0 && value.Length - dash - 1 == 12)
            {
                value = value.Substring(dash + 1);
            }

            return value.Replace(":", "").Replace("-", "").ToUpperInvariant();
        }

        /// <summary>
        /// Whether a device id of a notification / CoIoT packet is this device: Gen2+ ids end with the MAC (shellyplus1pm-a8032ab12345),
        /// Gen1 CoIoT ids are the MAC or its last 6 hex digits.
        /// </summary>
        public bool Matches(string? deviceId)
        {
            if (string.IsNullOrEmpty(deviceId) || Mac.Length == 0)
            {
                return false;
            }

            var id = NormalizeMac(deviceId);
            return id.EndsWith(Mac, StringComparison.Ordinal) || id.Length >= 6 && Mac.EndsWith(id, StringComparison.Ordinal);
        }

        protected string? ResolveAddress()
        {
            var ip = GetPropertyValueString(ShellyFactory.DeviceIpPropertyKey);
            if (!string.IsNullOrWhiteSpace(ip))
            {
                return ip.Trim();
            }

            if (Parent is ShellyDriver driver)
            {
                var found = driver.DiscoveredHosts.FirstOrDefault(a => Matches(a.Id));
                if (found != null)
                {
                    return found.IpAddress;
                }
            }

            return null;
        }

        protected void StartPolling()
        {
            StopPolling();
            if (PollingInterval <= 0)
            {
                return;
            }

            _pollCts = new CancellationTokenSource();
            var token = _pollCts.Token;
            _pollLoop = Task.Run(() => PollLoop(token), CancellationToken.None);
        }

        /// <summary>
        /// Wakes the poll loop now (e.g. a CoIoT packet says the device changed), no-op while a poll is pending.
        /// </summary>
        public void RequestPoll() => _pollNow.Writer.TryWrite(true);

        private async Task PollLoop(CancellationToken token)
        {
            var failures = 0;
            while (!token.IsCancellationRequested)
            {
                try
                {
                    using var wait = CancellationTokenSource.CreateLinkedTokenSource(token);
                    var delay = Task.Delay(PollingInterval, wait.Token);
                    var wake = _pollNow.Reader.WaitToReadAsync(wait.Token).AsTask();
                    await Task.WhenAny(delay, wake);
                    await wait.CancelAsync();
                    _pollNow.Reader.TryRead(out _);
                    token.ThrowIfCancellationRequested();

                    await PollAsync(token);
                    if (failures > 0)
                    {
                        DriverContext.Logger.LogInformation($"Shelly {Mac} at {Address} is reachable again");
                    }
                    failures = 0;
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception e)
                {
                    failures++;
                    // sleeping devices are unreachable most of the time, log only the first failure of a series
                    if (failures == 1 && !Sleeping)
                    {
                        DriverContext.Logger.LogWarning($"Could not poll shelly {Mac} at {Address}: {e.Message}");
                    }
                    else
                    {
                        DriverContext.Logger.LogDebug($"Could not poll shelly {Mac} at {Address}: {e.Message}");
                    }
                }
            }
        }

        protected void StopPolling()
        {
            var cts = _pollCts;
            _pollCts = null;
            if (cts == null)
            {
                return;
            }

            cts.Cancel();
            try
            {
                _pollLoop?.Wait(TimeSpan.FromSeconds(5));
            }
            catch
            {
                // the loop ends with the cancellation
            }
            cts.Dispose();
            _pollLoop = null;
        }

        protected abstract Task PollAsync(CancellationToken token);

        public abstract Task WriteAsync(ShellyComponentNode component, ShellyDataPoint dataPoint, object value, CancellationToken token);

        protected override Task Write(object value, IWriteContext writeContext, CancellationToken token = default)
        {
            return Task.CompletedTask;
        }

        protected override Task<bool> Read(IReadContext readContext, CancellationToken token = default)
        {
            RequestPoll();
            return Task.FromResult(true);
        }

        public override Task OnDelete(Core.EF.Models.NodeInstance instance, CancellationToken token = default)
        {
            return Parent?.OnDelete(instance, token) ?? Task.CompletedTask;
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            var type = Catalog(ctx.NodeInstance.This2NodeTemplate ?? Guid.Empty);
            if (type == null)
            {
                DriverContext.Logger.LogWarning($"Unknown shelly component {ctx.NodeInstance.Name}");
                return null!;
            }

            var node = new ShellyComponentNode(ctx, this, type);
            Components.Add(node);
            return node;
        }
    }
}
