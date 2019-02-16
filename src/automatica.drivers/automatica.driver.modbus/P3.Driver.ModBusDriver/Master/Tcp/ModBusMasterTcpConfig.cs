using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace P3.Driver.ModBusDriver.Master.Tcp
{
    public class ModBusMasterTcpConfig : ModBusMasterConfig
    {
        public IPAddress IpAddress { get; set; }
        public short Port { get; set; }

    }
}
