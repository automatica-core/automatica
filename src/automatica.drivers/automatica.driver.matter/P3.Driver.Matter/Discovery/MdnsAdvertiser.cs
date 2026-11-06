using Makaretu.Dns;
using Microsoft.Extensions.Logging;

namespace P3.Driver.Matter.Discovery
{
    public sealed class CommissionableInfo
    {
        public ushort Discriminator { get; init; }
        public ushort VendorId { get; init; }
        public ushort ProductId { get; init; }
        public string DeviceName { get; init; }
        public uint DeviceType { get; init; }

        /// <summary>1 = basic window (default passcode), 2 = enhanced window (verifier set by an admin).</summary>
        public byte CommissioningMode { get; init; } = 1;
    }

    /// <summary>DNS-SD advertisement of the commissionable (_matterc._udp) and operational (_matter._tcp) services (spec 4.3).</summary>
    public sealed class MdnsAdvertiser : IDisposable
    {
        private readonly ILogger _logger;
        private readonly object _lock = new();
        private readonly Dictionary<string, ServiceProfile> _operational = new();
        private MulticastService _mdns;
        private ServiceDiscovery _discovery;
        private ServiceProfile _commissionable;
        private int _port;

        public MdnsAdvertiser(ILogger logger)
        {
            _logger = logger;
        }

        public void Start(int port)
        {
            _port = port;
            _mdns = new MulticastService();
            _discovery = new ServiceDiscovery(_mdns);
            _mdns.Start();
        }

        public void AdvertiseCommissionable(CommissionableInfo info)
        {
            lock (_lock)
            {
                StopCommissionableLocked();
                var instance = Convert.ToHexString(Crypto.MatterCrypto.RandomBytes(8));
                var profile = new ServiceProfile(instance, "_matterc._udp", (ushort)_port);
                profile.Subtypes.Add($"_L{info.Discriminator}");
                profile.Subtypes.Add($"_S{info.Discriminator >> 8}");
                profile.Subtypes.Add($"_V{info.VendorId}");
                profile.Subtypes.Add($"_T{info.DeviceType}");
                profile.Subtypes.Add("_CM");
                profile.AddProperty("D", info.Discriminator.ToString());
                profile.AddProperty("VP", $"{info.VendorId}+{info.ProductId}");
                profile.AddProperty("CM", info.CommissioningMode.ToString());
                profile.AddProperty("DT", info.DeviceType.ToString());
                if (!string.IsNullOrEmpty(info.DeviceName))
                {
                    profile.AddProperty("DN", info.DeviceName.Length > 32 ? info.DeviceName[..32] : info.DeviceName);
                }

                AddMrpProperties(profile);
                profile.AddProperty("PH", "33");
                _commissionable = profile;
                Advertise(profile);
                _logger.LogInformation("Advertising commissionable node {Instance} (discriminator {Discriminator})", instance, info.Discriminator);
            }
        }

        public void StopCommissionable()
        {
            lock (_lock)
            {
                StopCommissionableLocked();
            }
        }

        private void StopCommissionableLocked()
        {
            if (_commissionable != null)
            {
                Unadvertise(_commissionable);
                _commissionable = null;
            }
        }

        /// <summary>Makes the advertised operational instances match <paramref name="instances"/> (instance name, compressed fabric id).</summary>
        public void SetOperational(IEnumerable<(string Instance, string CompressedFabricId)> instances)
        {
            lock (_lock)
            {
                var wanted = instances.ToDictionary(i => i.Instance, i => i.CompressedFabricId);
                foreach (var stale in _operational.Keys.Where(k => !wanted.ContainsKey(k)).ToList())
                {
                    Unadvertise(_operational[stale]);
                    _operational.Remove(stale);
                }

                foreach (var (instance, cfid) in wanted.Where(w => !_operational.ContainsKey(w.Key)))
                {
                    var profile = new ServiceProfile(instance, "_matter._tcp", (ushort)_port);
                    profile.Subtypes.Add($"_I{cfid}");
                    AddMrpProperties(profile);
                    _operational[instance] = profile;
                    Advertise(profile);
                    _logger.LogInformation("Advertising operational instance {Instance}", instance);
                }
            }
        }

        private static void AddMrpProperties(ServiceProfile profile)
        {
            profile.AddProperty("SII", "500");
            profile.AddProperty("SAI", "300");
            profile.AddProperty("SAT", "4000");
            profile.AddProperty("T", "0");
        }

        private void Advertise(ServiceProfile profile)
        {
            if (_discovery == null)
            {
                return;
            }

            try
            {
                _discovery.Advertise(profile);
                _discovery.Announce(profile);
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "mDNS advertisement of {Name} failed", profile.FullyQualifiedName);
            }
        }

        private void Unadvertise(ServiceProfile profile)
        {
            if (_discovery == null)
            {
                return;
            }

            try
            {
                _discovery.Unadvertise(profile);
            }
            catch (Exception e)
            {
                _logger.LogDebug(e, "mDNS goodbye for {Name} failed", profile.FullyQualifiedName);
            }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                StopCommissionableLocked();
                foreach (var p in _operational.Values)
                {
                    Unadvertise(p);
                }

                _operational.Clear();
            }

            _discovery?.Dispose();
            _mdns?.Stop();
            _mdns?.Dispose();
            _discovery = null;
            _mdns = null;
        }
    }
}
