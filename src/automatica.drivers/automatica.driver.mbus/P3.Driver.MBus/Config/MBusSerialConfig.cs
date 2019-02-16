using System;
using System.Collections.Generic;
using System.Text;

namespace P3.Driver.MBus.Config
{
    public class MBusSerialConfig : MBusConfig
    {
        public string Port { get; set; }
        public int Baudrate { get; set; }

        public bool WaitForShortFrame { get; set; }
    }
}
