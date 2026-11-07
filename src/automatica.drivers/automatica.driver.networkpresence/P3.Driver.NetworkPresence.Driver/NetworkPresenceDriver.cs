using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using Microsoft.Extensions.Logging;
using P3.Driver.NetworkPresence.Driver.Network;

namespace P3.Driver.NetworkPresence.Driver
{
    /// <summary>
    /// Sweeps the local network, reads the neighbor (ARP) table and marks phones and their owners as home or away.
    /// A quick sweep probes only the last known addresses of the tracked phones, a full sweep probes every host of the networks.
    /// </summary>
    internal sealed class NetworkPresenceDriver : DriverNoneAttributeBase
    {
        // the kernel resolves ARP within milliseconds on a LAN, give slow Wi-Fi clients a moment
        private static readonly TimeSpan DefaultSettleTime = TimeSpan.FromMilliseconds(1500);

        private readonly SemaphoreSlim _sweepLock = new(1, 1);
        private readonly List<PersonNode> _persons = new();
        private readonly List<ValueNode> _anyoneHome = new();
        private readonly List<ValueNode> _peopleHome = new();

        private Timer _timer;
        private IReadOnlyList<Subnet> _subnets = Array.Empty<Subnet>();
        private TimeSpan _poll;
        private TimeSpan _fullScanInterval;
        private DateTimeOffset _lastFullSweep = DateTimeOffset.MinValue;

        internal INeighborSource NeighborSource { get; set; } = new SystemNeighborSource();
        internal IProber Prober { get; set; } = new ArpProber();
        internal Func<IReadOnlyList<Subnet>> LocalSubnets { get; set; } = Subnet.FromLocalInterfaces;
        internal Func<IPAddress, CancellationToken, Task<string>> ResolveHostName { get; set; } = ReverseLookupAsync;
        internal TimeSpan SettleTime { get; set; } = DefaultSettleTime;

        internal TimeSpan AwayTimeout { get; private set; }
        internal IReadOnlyList<NeighborEntry> LastNeighbors { get; private set; } = Array.Empty<NeighborEntry>();
        internal IReadOnlyList<PersonNode> Persons => _persons;

        public NetworkPresenceDriver(IDriverContext driverContext) : base(driverContext)
        {
        }

        private DateTimeOffset Now => DriverContext.TimeProvider.GetUtcNow();

        public override Task<bool> Init(CancellationToken token = default)
        {
            _poll = TimeSpan.FromSeconds(Math.Max(10, GetPropertyValueInt(NetworkPresenceDriverFactory.PollKey)));
            AwayTimeout = TimeSpan.FromMinutes(Math.Max(1, GetPropertyValueInt(NetworkPresenceDriverFactory.AwayTimeoutKey)));
            _fullScanInterval = TimeSpan.FromMinutes(Math.Max(1, GetPropertyValueInt(NetworkPresenceDriverFactory.FullScanKey)));

            var configured = GetPropertyValueString(NetworkPresenceDriverFactory.SubnetsKey);
            _subnets = Subnet.ParseList(configured);
            if (!string.IsNullOrWhiteSpace(configured) && _subnets.Count == 0)
            {
                DriverContext.Logger.LogWarning($"No valid network in \"{configured}\", using the networks of the local interfaces");
            }

            return base.Init(token);
        }

        public override async Task<bool> Start(CancellationToken token = default)
        {
            _timer?.Dispose();
            _timer = null;

            if (_subnets.Count == 0)
            {
                _subnets = LocalSubnets();
            }
            DriverContext.Logger.LogInformation($"Tracking presence in {string.Join(", ", _subnets)} every {_poll.TotalSeconds}s, away after {AwayTimeout.TotalMinutes} min");

            try
            {
                await SweepAsync(true, token);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                DriverContext.Logger.LogError(ex, "First network sweep failed, retrying with the next poll");
            }

            _timer = new Timer(TimerElapsed, null, _poll, _poll);
            return await base.Start(token);
        }

        public override Task<bool> Stop(CancellationToken token = default)
        {
            _timer?.Dispose();
            _timer = null;
            return base.Stop(token);
        }

        private async void TimerElapsed(object state)
        {
            try
            {
                await SweepAsync(false);
            }
            catch (Exception ex)
            {
                DriverContext.Logger.LogError(ex, "Network sweep failed");
            }
        }

        protected override async Task<bool> Read(IReadContext readContext, CancellationToken token = default)
        {
            await SweepAsync(false, token);
            return true;
        }

        /// <summary>
        /// Probes, reads the neighbor table and updates every data point. Concurrent calls wait for the running sweep.
        /// </summary>
        internal async Task SweepAsync(bool forceFull, CancellationToken token = default)
        {
            await _sweepLock.WaitAsync(token);
            try
            {
                var now = Now;
                var devices = _persons.SelectMany(p => p.Devices).ToList();
                var full = forceFull || now - _lastFullSweep >= _fullScanInterval || devices.Any(d => d.LastAddress == null);

                var targets = full
                    ? _subnets.SelectMany(s => s.Hosts()).Distinct().ToList()
                    : devices.Where(d => d.LastAddress != null).Select(d => d.LastAddress).Distinct().ToList();

                if (targets.Count > 0)
                {
                    await Prober.ProbeAsync(targets, token);
                    if (SettleTime > TimeSpan.Zero)
                    {
                        await Task.Delay(SettleTime, token);
                    }
                }
                if (full)
                {
                    _lastFullSweep = now;
                }

                var neighbors = await NeighborSource.ReadAsync(token);
                LastNeighbors = neighbors;
                Apply(neighbors, Now);
            }
            finally
            {
                _sweepLock.Release();
            }
        }

        /// <summary>
        /// Maps the resolved neighbor entries onto the phones and recomputes who is home
        /// </summary>
        internal void Apply(IReadOnlyList<NeighborEntry> neighbors, DateTimeOffset now)
        {
            var byMac = neighbors.Where(n => n.Complete && n.Mac != null)
                .GroupBy(n => n.Mac)
                .ToDictionary(g => g.Key, g => g.First());

            var peopleHome = 0;
            foreach (var person in _persons)
            {
                foreach (var device in person.Devices)
                {
                    device.Update(device.Mac != null && byMac.TryGetValue(device.Mac, out var entry) ? entry : null, now, AwayTimeout);
                }

                if (person.Update())
                {
                    peopleHome++;
                }
            }

            foreach (var node in _anyoneHome)
            {
                node.Set(peopleHome > 0);
            }
            foreach (var node in _peopleHome)
            {
                node.Set(peopleHome);
            }
        }

        /// <summary>
        /// Phones on the network that no person has yet, used by the scan of a person
        /// </summary>
        internal async Task<IReadOnlyList<(NeighborEntry Entry, string HostName)>> UnassignedDevicesAsync(CancellationToken token = default)
        {
            await SweepAsync(true, token);

            var assigned = _persons.SelectMany(p => p.Devices).Select(d => d.Mac).Where(m => m != null).ToHashSet();
            var candidates = LastNeighbors
                .Where(n => n.Complete && n.Mac != null && !assigned.Contains(n.Mac))
                .Where(n => _subnets.Count == 0 || _subnets.Any(s => s.Contains(n.Address)))
                .GroupBy(n => n.Mac)
                .Select(g => g.First())
                .OrderBy(n => n.Address.GetAddressBytes(), ByteArrayComparer.Instance)
                .ToList();

            var names = await Task.WhenAll(candidates.Select(async c => (c, await SafeResolveAsync(c.Address, token))));
            return names.Select(n => (n.c, n.Item2)).ToList();
        }

        private async Task<string> SafeResolveAsync(IPAddress address, CancellationToken token)
        {
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
                cts.CancelAfter(TimeSpan.FromSeconds(2));
                return await ResolveHostName(address, cts.Token);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static async Task<string> ReverseLookupAsync(IPAddress address, CancellationToken token)
        {
            var entry = await Dns.GetHostEntryAsync(address.ToString(), token);
            var name = entry.HostName;
            return string.IsNullOrWhiteSpace(name) || name == address.ToString() ? null : name.Split('.')[0];
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            switch (ctx.NodeInstance.This2NodeTemplateNavigation?.Key)
            {
                case "person":
                    var person = new PersonNode(ctx, this);
                    _persons.Add(person);
                    return person;
                case "anyone_home":
                    var anyone = new ValueNode(ctx);
                    _anyoneHome.Add(anyone);
                    return anyone;
                case "people_home":
                    var people = new ValueNode(ctx);
                    _peopleHome.Add(people);
                    return people;
            }
            return null;
        }

        private sealed class ByteArrayComparer : IComparer<byte[]>
        {
            public static readonly ByteArrayComparer Instance = new();

            public int Compare(byte[] x, byte[] y)
            {
                for (var i = 0; i < Math.Min(x!.Length, y!.Length); i++)
                {
                    var c = x[i].CompareTo(y[i]);
                    if (c != 0)
                    {
                        return c;
                    }
                }
                return x.Length.CompareTo(y.Length);
            }
        }
    }
}
