using System;
using System.Collections.Generic;
using System.Text;

namespace P3.Driver.ModBusDriver.Slave.Tcp
{
    public class ModBusSlaveTcpConfig : ModBusSlaveConfig
    {
        public ushort Port { get; set; }

        public bool IgnoreDeviceId { get; set; } = true;

        public List<int> DeviceIds { get; set; }
    }
}
