using System.Net;

namespace P3.Driver.ModBusDriver.Master.Tcp
{
    public class ModBusMasterTcpConfig : ModBusMasterConfig
    {
        public IPAddress IpAddress { get; set; }
        public short Port { get; set; }

    }
}
