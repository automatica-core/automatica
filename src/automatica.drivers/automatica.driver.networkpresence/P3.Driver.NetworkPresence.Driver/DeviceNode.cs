using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using Microsoft.Extensions.Logging;
using P3.Driver.NetworkPresence.Driver.Network;

namespace P3.Driver.NetworkPresence.Driver
{
    /// <summary>
    /// A phone, identified by its MAC address. Connected while it was seen within the away timeout.
    /// </summary>
    internal sealed class DeviceNode : DriverNoneAttributeBase
    {
        private readonly NetworkPresenceDriver _driver;
        private readonly List<ValueNode> _connected = new();
        private readonly List<ValueNode> _ip = new();
        private readonly List<ValueNode> _lastSeen = new();

        public DeviceNode(IDriverContext driverContext, NetworkPresenceDriver driver) : base(driverContext)
        {
            _driver = driver;
        }

        internal string Mac { get; private set; }

        internal IPAddress LastAddress { get; private set; }

        internal DateTimeOffset? LastSeen { get; private set; }

        internal bool Connected { get; private set; }

        public override Task<bool> Init(CancellationToken token = default)
        {
            var configured = GetPropertyValueString(NetworkPresenceDriverFactory.MacKey);
            Mac = NeighborEntry.NormalizeMac(configured);
            if (Mac == null)
            {
                DriverContext.Logger.LogWarning($"{FullName}: \"{configured}\" is not a MAC address, the device is never seen");
            }
            else if (NeighborEntry.IsRandomized(Mac))
            {
                DriverContext.Logger.LogInformation($"{FullName}: {Mac} is a private (randomized) address. It stays stable per Wi-Fi network unless the phone rotates it");
            }
            return base.Init(token);
        }

        internal void Update(NeighborEntry entry, DateTimeOffset now, TimeSpan awayTimeout)
        {
            if (entry != null)
            {
                LastSeen = now;
                LastAddress = entry.Address;
            }

            Connected = LastSeen.HasValue && now - LastSeen.Value <= awayTimeout;

            foreach (var node in _connected)
            {
                node.Set(Connected);
            }
            if (LastAddress != null)
            {
                foreach (var node in _ip)
                {
                    node.Set(LastAddress.ToString());
                }
            }
            if (LastSeen.HasValue)
            {
                foreach (var node in _lastSeen)
                {
                    node.Set(LastSeen.Value.UtcDateTime);
                }
            }
        }

        protected override Task<bool> Read(IReadContext readContext, CancellationToken token = default)
        {
            return _driver.Read(token);
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            var node = new ValueNode(ctx);
            switch (ctx.NodeInstance.This2NodeTemplateNavigation?.Key)
            {
                case "connected":
                    _connected.Add(node);
                    return node;
                case "ip":
                    _ip.Add(node);
                    return node;
                case "last_seen":
                    _lastSeen.Add(node);
                    return node;
            }
            return null;
        }
    }
}
