using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace P3.Driver.MBus.Config
{
    public class MBusUdpConfig : MBusConfig
    {
        public IPAddress IpAddress { get; set; }
        public int Port { get; set; }
    }
}
