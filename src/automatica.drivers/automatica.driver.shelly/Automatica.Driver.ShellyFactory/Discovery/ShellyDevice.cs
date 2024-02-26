namespace Automatica.Driver.ShellyFactory.Discovery
{
    public enum ShellyDeviceType
    {
        Shelly1Pm,
        Shelly25,
        ShellyPlus2Pm,
        ShellyPlus1Pm,
        ShellyPlugS
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
}
