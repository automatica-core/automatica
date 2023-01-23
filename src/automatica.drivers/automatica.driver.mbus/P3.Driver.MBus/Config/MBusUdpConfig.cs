using System.Net;

namespace P3.Driver.MBus.Config
{
    public class MBusUdpConfig : MBusConfig
    {
        public IPAddress IpAddress { get; set; }
        public int Port { get; set; }
    }
}
