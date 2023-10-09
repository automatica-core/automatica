namespace Automatica.Driver.ShellyFactory.Discovery
{
    public enum ShellyDeviceType
    {
        Shelly1Pm,
        Shelly25
    }

    public class ShellyDevice
    {
        public ShellyDevice(string id, string name, ShellyDeviceType type, string ipAddress)
        {
            Id = id;
            Type = type;
            IpAddress = ipAddress;
            Name = name;
        }

        public string Id { get;  }
        public string Name { get;  }
        public ShellyDeviceType Type { get;  }
        public string IpAddress { get;  }

    }
}
