using System.Collections.Generic;

namespace P3.Driver.ModBusDriver.Slave.Tcp
{
    public class ModBusSlaveTcpConfig : ModBusSlaveConfig
    {
        public ushort Port { get; set; }

        public bool IgnoreDeviceId { get; set; } = true;

        public List<int> DeviceIds { get; set; }
    }
}
