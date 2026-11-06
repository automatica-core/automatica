using Microsoft.Extensions.Logging;
using P3.Driver.Matter.DataModel;
using P3.Driver.Matter.DataModel.Clusters;
using P3.Driver.Matter.Storage;

namespace P3.Driver.Matter
{
    /// <summary>A device behind the bridge, exposed on its own endpoint below the aggregator.</summary>
    public abstract class BridgedDevice
    {
        internal BridgedDevice(string uniqueId, string name)
        {
            UniqueId = uniqueId;
            Name = name;
        }

        public string UniqueId { get; }
        public string Name { get; private set; }
        public ushort EndpointId { get; internal set; }
        internal Endpoint Endpoint { get; set; }
        internal MatterBridge Bridge { get; set; }

        internal abstract DeviceType DeviceType { get; }

        internal abstract void AddClusters(Endpoint endpoint);

        protected void Locked(Action action)
        {
            var node = Endpoint?.Node;
            if (node == null)
            {
                action();
                return;
            }

            lock (node.SyncRoot)
            {
                action();
            }
        }

        public void SetName(string name)
        {
            Name = name;
            Locked(() => Endpoint?.GetCluster<BridgedDeviceBasicInformationCluster>()?.SetNodeLabel(name));
        }

        public void SetReachable(bool reachable)
        {
            Locked(() => Endpoint?.GetCluster<BridgedDeviceBasicInformationCluster>()?.SetReachable(reachable));
        }

        /// <summary>Raised when a controller asks the device to identify itself (seconds, 0 = stop).</summary>
        public event Action<ushort> IdentifyRequested;

        internal void AddCommonClusters(Endpoint endpoint)
        {
            endpoint.Add(new DescriptorCluster());
            endpoint.Add(new BridgedDeviceBasicInformationCluster(UniqueId, Name, Bridge.Node.Options.VendorName, ProductName));
            var identify = new IdentifyCluster();
            identify.IdentifyRequested += s => IdentifyRequested?.Invoke(s);
            endpoint.Add(identify);
        }

        protected virtual string ProductName => GetType().Name.Replace("Bridged", "");
    }

    public enum OnOffDeviceKind
    {
        Light,
        PlugInUnit
    }

    public sealed class BridgedOnOffDevice : BridgedDevice
    {
        private readonly OnOffDeviceKind _kind;
        private OnOffCluster _onOff;
        private bool _initial;

        public BridgedOnOffDevice(string uniqueId, string name, OnOffDeviceKind kind = OnOffDeviceKind.Light) : base(uniqueId, name)
        {
            _kind = kind;
        }

        internal override DeviceType DeviceType => _kind == OnOffDeviceKind.Light ? DeviceType.OnOffLight : DeviceType.OnOffPlugInUnit;

        protected override string ProductName => _kind == OnOffDeviceKind.Light ? "On/Off Light" : "On/Off Plug-in Unit";

        /// <summary>Called when a controller switches the device.</summary>
        public Func<bool, Task> SwitchRequested { get; set; }

        public bool IsOn => _onOff?.OnOff ?? _initial;

        internal override void AddClusters(Endpoint endpoint)
        {
            AddCommonClusters(endpoint);
            endpoint.Add(new GroupsCluster());
            _onOff = new OnOffCluster { OnOffRequested = on => SwitchRequested?.Invoke(on) ?? Task.CompletedTask };
            _onOff.SetOnOff(_initial);
            endpoint.Add(_onOff);
        }

        public void SetOnOff(bool on)
        {
            _initial = on;
            Locked(() => _onOff?.SetOnOff(on));
        }
    }

    public sealed class BridgedDimmableLight : BridgedDevice
    {
        private OnOffCluster _onOff;
        private LevelControlCluster _level;
        private bool _on;
        private int _percent = 100;

        public BridgedDimmableLight(string uniqueId, string name) : base(uniqueId, name)
        {
        }

        internal override DeviceType DeviceType => DeviceType.DimmableLight;

        protected override string ProductName => "Dimmable Light";

        public Func<bool, Task> SwitchRequested { get; set; }

        /// <summary>Called with the brightness in percent (1..100) when a controller dims.</summary>
        public Func<int, Task> LevelRequested { get; set; }

        internal override void AddClusters(Endpoint endpoint)
        {
            AddCommonClusters(endpoint);
            endpoint.Add(new GroupsCluster());
            _onOff = new OnOffCluster { OnOffRequested = on => SwitchRequested?.Invoke(on) ?? Task.CompletedTask };
            _level = new LevelControlCluster
            {
                LevelRequested = async (level, withOnOff) =>
                {
                    var percent = Math.Max(1, LevelControlCluster.LevelToPercent(level));
                    if (withOnOff && level <= LevelControlCluster.MinLevel)
                    {
                        if (SwitchRequested != null)
                        {
                            await SwitchRequested(false);
                        }

                        return;
                    }

                    if (LevelRequested != null)
                    {
                        await LevelRequested(percent);
                    }
                }
            };
            _onOff.SetOnOff(_on);
            _level.SetLevel(LevelControlCluster.PercentToLevel(_percent));
            endpoint.Add(_onOff);
            endpoint.Add(_level);
        }

        /// <summary>Updates the state from the real device (percent 0..100, 0 means off).</summary>
        public void SetState(bool on, int percent)
        {
            _on = on;
            if (percent > 0)
            {
                _percent = percent;
            }

            Locked(() =>
            {
                _onOff?.SetOnOff(on);
                if (percent > 0)
                {
                    _level?.SetLevel(LevelControlCluster.PercentToLevel(percent));
                }
            });
        }
    }

    public sealed class BridgedWindowCovering : BridgedDevice
    {
        private WindowCoveringCluster _covering;
        private int? _position;
        private CoveringMovement _movement;

        public BridgedWindowCovering(string uniqueId, string name) : base(uniqueId, name)
        {
        }

        internal override DeviceType DeviceType => DeviceType.WindowCovering;

        protected override string ProductName => "Window Covering";

        /// <summary>Called with the command and, for <see cref="CoveringCommand.GoToPosition"/>, the target in percent (0 = open, 100 = closed).</summary>
        public Func<CoveringCommand, int?, Task> CommandRequested { get; set; }

        internal override void AddClusters(Endpoint endpoint)
        {
            AddCommonClusters(endpoint);
            _covering = new WindowCoveringCluster
            {
                CommandRequested = (command, target) =>
                    CommandRequested?.Invoke(command, target.HasValue ? (int)Math.Round(target.Value / 100.0) : null) ?? Task.CompletedTask
            };
            _covering.SetState(_position.HasValue ? (ushort)(_position * 100) : null, _movement);
            endpoint.Add(_covering);
        }

        /// <summary>Updates the state: position in percent (0 = open, 100 = closed), or null if unknown.</summary>
        public void SetState(int? positionPercent, CoveringMovement movement)
        {
            _position = positionPercent.HasValue ? Math.Clamp(positionPercent.Value, 0, 100) : null;
            _movement = movement;
            Locked(() => _covering?.SetState(_position.HasValue ? (ushort)(_position.Value * 100) : null, movement));
        }
    }

    /// <summary>
    /// A Matter bridge (spec 9.12): the root node, an aggregator on endpoint 1 and one endpoint per bridged device.
    /// Endpoint numbers are kept stable per <see cref="BridgedDevice.UniqueId"/> across restarts.
    /// </summary>
    public sealed class MatterBridge : IAsyncDisposable
    {
        public const ushort AggregatorEndpoint = 1;

        private readonly Dictionary<string, BridgedDevice> _devices = new();
        private readonly object _lock = new();

        public MatterBridge(MatterNodeOptions options, IMatterStorage storage, ILogger logger = null)
        {
            Node = new MatterNode(options, storage, logger);
        }

        public MatterNode Node { get; }

        public IReadOnlyCollection<BridgedDevice> Devices
        {
            get
            {
                lock (_lock)
                {
                    return _devices.Values.ToList();
                }
            }
        }

        public async Task InitializeAsync(CancellationToken token = default)
        {
            if (Node.State != null)
            {
                return;
            }

            await Node.LoadAsync(token);
            var aggregator = new Endpoint(AggregatorEndpoint, 0, DeviceType.Aggregator);
            aggregator.Add(new DescriptorCluster());
            Node.DataModel.AddEndpoint(aggregator);
        }

        public async Task StartAsync(CancellationToken token = default)
        {
            await InitializeAsync(token);
            await Node.StartAsync(token);
        }

        public T Add<T>(T device) where T : BridgedDevice
        {
            if (Node.State == null)
            {
                throw new InvalidOperationException("Call InitializeAsync first");
            }

            lock (_lock)
            {
                if (_devices.ContainsKey(device.UniqueId))
                {
                    throw new ArgumentException($"A device with id {device.UniqueId} is already bridged");
                }

                if (!Node.State.Endpoints.TryGetValue(device.UniqueId, out var endpointId))
                {
                    endpointId = AllocateEndpointId();
                    Node.State.Endpoints[device.UniqueId] = endpointId;
                    Node.RequestPersist();
                }

                device.Bridge = this;
                device.EndpointId = endpointId;
                var endpoint = new Endpoint(endpointId, AggregatorEndpoint, device.DeviceType, DeviceType.BridgedNode);
                device.AddClusters(endpoint);
                device.Endpoint = endpoint;
                _devices[device.UniqueId] = device;
                Node.DataModel.AddEndpoint(endpoint);
            }

            return device;
        }

        public void Remove(string uniqueId, bool forgetEndpoint = false)
        {
            lock (_lock)
            {
                if (!_devices.Remove(uniqueId, out var device))
                {
                    return;
                }

                Node.DataModel.RemoveEndpoint(device.EndpointId);
                device.Endpoint = null;
                if (forgetEndpoint && Node.State.Endpoints.Remove(uniqueId))
                {
                    Node.RequestPersist();
                }
            }
        }

        /// <summary>Drops endpoint reservations of devices that no longer exist.</summary>
        public void ForgetUnusedEndpoints()
        {
            lock (_lock)
            {
                var stale = Node.State.Endpoints.Keys.Where(k => !_devices.ContainsKey(k)).ToList();
                foreach (var key in stale)
                {
                    Node.State.Endpoints.Remove(key);
                }

                if (stale.Count > 0)
                {
                    Node.RequestPersist();
                }
            }
        }

        private ushort AllocateEndpointId()
        {
            var used = Node.State.Endpoints.Values.ToHashSet();
            var id = Math.Max(Node.State.NextEndpointId, (ushort)2);
            while (used.Contains(id) || id < 2)
            {
                id = id == 0xFFFE ? (ushort)2 : (ushort)(id + 1);
            }

            Node.State.NextEndpointId = id == 0xFFFE ? (ushort)2 : (ushort)(id + 1);
            return id;
        }

        public ValueTask DisposeAsync() => Node.DisposeAsync();
    }
}
