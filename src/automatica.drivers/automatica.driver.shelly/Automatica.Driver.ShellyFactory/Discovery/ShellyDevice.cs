namespace Automatica.Driver.ShellyFactory.Discovery
{
    public enum ShellyDeviceType
    {
        Shelly1Pm,
        Shelly25,
        ShellyPlus2Pm,
        ShellyPlus1Pm,
    }

    public enum ShellyGeneration
    {
        Gen1,
        Gen2
    }

    public class ShellyDevice
    {
        public ShellyDevice(string id, string name, ShellyDeviceType type, string ipAddress, ShellyGeneration generation)
        {
            Id = id;
            Type = type;
            IpAddress = ipAddress;
            Generation = generation;
            Name = name;
        }

        public string Id { get;  }
        public string Name { get;  }
        public ShellyDeviceType Type { get;  }
        public ShellyGeneration Generation { get; set; }
        public string IpAddress { get;  }

    }
}
