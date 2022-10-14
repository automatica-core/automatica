using System;
using System.Collections.Generic;
using System.Text;

namespace P3.Driver.MBus.Config
{
    public class MBusConfig
    {
        public bool ResetBeforeRead { get; set; }
        public int Timeout { get; set; } = 5000;
    }
}
