namespace Automatica.Driver.ShellyFactory.Discovery
{
    public enum ShellyDeviceType
    {
        Shelly1Pm,
        Shelly25,
        ShellyPlus2Pm,
        ShellyPlus1Pm,
        ShellyPlugS,
        Shelly1
    }

    public enum ShellyGeneration
    {
        Gen1,
        Gen2
    }

    public class ShellyDevice
    {
        public ShellyDevice(string id, string name, string deviceId, ShellyDeviceType type, string ipAddress, ShellyGeneration generation)
        {
            Id = id;
            Type = type;
            IpAddress = ipAddress;
            Generation = generation;
            DeviceId = deviceId;
            Name = name;
        }

        public string Id { get;  }
        public string Name { get;  }
        public string DeviceId { get; set; }
        public ShellyDeviceType Type { get;  }
        public ShellyGeneration Generation { get; set; }
        public string IpAddress { get;  }

    }

    /// <summary>
    /// A Shelly found by mDNS: <see cref="Id"/> is the announced name (shellyplus1pm-a8032ab12345, shelly1-A4CF12F3D2B1), it ends with the MAC.
    /// </summary>
    public sealed record ShellyDiscoveredHost(string Id, string IpAddress, int? Generation);
}
