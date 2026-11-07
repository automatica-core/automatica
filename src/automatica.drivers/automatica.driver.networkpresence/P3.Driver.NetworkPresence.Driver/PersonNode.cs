using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;

namespace P3.Driver.NetworkPresence.Driver
{
    /// <summary>
    /// A person with one or more phones. Home as long as one of the phones is connected.
    /// </summary>
    internal sealed class PersonNode : DriverNoneAttributeBase
    {
        private readonly NetworkPresenceDriver _driver;
        private readonly List<DeviceNode> _devices = new();
        private readonly List<ValueNode> _home = new();
        private readonly List<ValueNode> _lastSeen = new();
        private readonly List<ValueNode> _devicesHome = new();

        public PersonNode(IDriverContext driverContext, NetworkPresenceDriver driver) : base(driverContext)
        {
            _driver = driver;
        }

        internal IReadOnlyList<DeviceNode> Devices => _devices;

        internal bool IsHome { get; private set; }

        internal DateTimeOffset? LastSeen { get; private set; }

        /// <summary>
        /// Recomputes the person from the (already updated) phones, returns whether the person is home
        /// </summary>
        internal bool Update()
        {
            var connected = _devices.Count(d => d.Connected);
            IsHome = connected > 0;
            LastSeen = _devices.Where(d => d.LastSeen.HasValue).Select(d => d.LastSeen).DefaultIfEmpty(null).Max();

            foreach (var node in _home)
            {
                node.Set(IsHome);
            }
            foreach (var node in _devicesHome)
            {
                node.Set(connected);
            }
            if (LastSeen.HasValue)
            {
                foreach (var node in _lastSeen)
                {
                    node.Set(LastSeen.Value.UtcDateTime);
                }
            }
            return IsHome;
        }

        /// <summary>
        /// Lists the phones on the network that are not assigned to anybody yet, adding one assigns it to this person
        /// </summary>
        public override async Task<IList<NodeInstance>> Scan(CancellationToken token = default)
        {
            var result = new List<NodeInstance>();
            foreach (var (entry, hostName) in await _driver.UnassignedDevicesAsync(token))
            {
                var node = DriverContext.NodeTemplateFactory.CreateNodeInstance(NetworkPresenceDriverFactory.DeviceTemplate);
                node.Name = string.IsNullOrWhiteSpace(hostName) ? $"{entry.Address} ({entry.Mac})" : $"{hostName} ({entry.Address})";
                node.SetProperty(NetworkPresenceDriverFactory.MacKey, entry.Mac);
                foreach (var template in NetworkPresenceDriverFactory.DeviceValueTemplates)
                {
                    node.InverseThis2ParentNodeInstanceNavigation.Add(DriverContext.NodeTemplateFactory.CreateNodeInstance(template));
                }
                result.Add(node);
            }
            return result;
        }

        protected override Task<bool> Read(IReadContext readContext, CancellationToken token = default)
        {
            return _driver.Read(token);
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            switch (ctx.NodeInstance.This2NodeTemplateNavigation?.Key)
            {
                case "device":
                    var device = new DeviceNode(ctx, _driver);
                    _devices.Add(device);
                    return device;
                case "home":
                    return Track(_home, ctx);
                case "last_seen":
                    return Track(_lastSeen, ctx);
                case "devices_home":
                    return Track(_devicesHome, ctx);
            }
            return null;
        }

        private static ValueNode Track(List<ValueNode> list, IDriverContext ctx)
        {
            var node = new ValueNode(ctx);
            list.Add(node);
            return node;
        }
    }
}
