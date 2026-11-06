namespace P3.Driver.Matter.DataModel
{
    public readonly record struct DeviceType(uint Id, ushort Revision)
    {
        public static readonly DeviceType RootNode = new(0x0016, 2);
        public static readonly DeviceType Aggregator = new(0x000E, 1);
        public static readonly DeviceType BridgedNode = new(0x0013, 2);
        public static readonly DeviceType OnOffLight = new(0x0100, 3);
        public static readonly DeviceType DimmableLight = new(0x0101, 3);
        public static readonly DeviceType OnOffPlugInUnit = new(0x010A, 3);
        public static readonly DeviceType WindowCovering = new(0x0202, 3);
        public static readonly DeviceType ContactSensor = new(0x0015, 1);
        public static readonly DeviceType TemperatureSensor = new(0x0302, 2);
    }

    public sealed class Endpoint
    {
        private readonly SortedDictionary<uint, ClusterServer> _clusters = new();

        public Endpoint(ushort id, ushort? parentId, params DeviceType[] deviceTypes)
        {
            Id = id;
            ParentId = parentId;
            DeviceTypes = deviceTypes.ToList();
        }

        public ushort Id { get; }
        public ushort? ParentId { get; }
        public List<DeviceType> DeviceTypes { get; }
        public DataModelNode Node { get; internal set; }

        public IEnumerable<ClusterServer> Clusters => _clusters.Values;

        public ClusterServer GetCluster(uint id) => _clusters.TryGetValue(id, out var c) ? c : null;

        public T GetCluster<T>() where T : ClusterServer => _clusters.Values.OfType<T>().FirstOrDefault();

        public Endpoint Add(ClusterServer cluster)
        {
            cluster.Endpoint = this;
            _clusters[cluster.ClusterId] = cluster;
            return this;
        }
    }

    /// <summary>The node's endpoint tree. All data model access happens under <see cref="SyncRoot"/>.</summary>
    public sealed class DataModelNode
    {
        private readonly SortedDictionary<ushort, Endpoint> _endpoints = new();

        public object SyncRoot { get; } = new();

        public event Action<ushort, uint, uint> AttributeChanged;

        /// <summary>Raised when the endpoint structure changes (parts lists).</summary>
        public event Action StructureChanged;

        public IEnumerable<Endpoint> Endpoints
        {
            get
            {
                lock (SyncRoot)
                {
                    return _endpoints.Values.ToList();
                }
            }
        }

        public Endpoint GetEndpoint(ushort id)
        {
            lock (SyncRoot)
            {
                return _endpoints.TryGetValue(id, out var e) ? e : null;
            }
        }

        public void AddEndpoint(Endpoint endpoint)
        {
            lock (SyncRoot)
            {
                endpoint.Node = this;
                _endpoints[endpoint.Id] = endpoint;
            }

            OnStructureChanged(endpoint.ParentId);
        }

        public void RemoveEndpoint(ushort id)
        {
            Endpoint removed;
            lock (SyncRoot)
            {
                if (!_endpoints.Remove(id, out removed))
                {
                    return;
                }

                removed.Node = null;
            }

            OnStructureChanged(removed.ParentId);
        }

        /// <summary>All endpoints below <paramref name="endpointId"/> (for the Descriptor PartsList).</summary>
        public IEnumerable<ushort> Descendants(ushort endpointId)
        {
            lock (SyncRoot)
            {
                if (endpointId == 0)
                {
                    return _endpoints.Keys.Where(k => k != 0).ToList();
                }

                var result = new List<ushort>();
                var frontier = new Queue<ushort>(new[] { endpointId });
                while (frontier.Count > 0)
                {
                    var parent = frontier.Dequeue();
                    foreach (var child in _endpoints.Values.Where(e => e.ParentId == parent))
                    {
                        result.Add(child.Id);
                        frontier.Enqueue(child.Id);
                    }
                }

                result.Sort();
                return result;
            }
        }

        private void OnStructureChanged(ushort? parentId)
        {
            var root = GetEndpoint(0)?.GetCluster(Clusters.DescriptorCluster.Id);
            root?.MarkChanged(Clusters.DescriptorCluster.AttributePartsList);
            if (parentId is { } p and not 0)
            {
                var parent = GetEndpoint(p)?.GetCluster(Clusters.DescriptorCluster.Id);
                parent?.MarkChanged(Clusters.DescriptorCluster.AttributePartsList);
            }

            StructureChanged?.Invoke();
        }

        internal void RaiseAttributeChanged(ushort endpoint, uint cluster, uint attribute)
        {
            AttributeChanged?.Invoke(endpoint, cluster, attribute);
        }
    }
}
