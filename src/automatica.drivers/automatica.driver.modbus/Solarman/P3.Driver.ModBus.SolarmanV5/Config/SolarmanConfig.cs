using P3.Driver.ModBusDriver.Master.Tcp;

namespace P3.Driver.ModBus.SolarmanV5.Config
{
    public class SolarmanConfig : ModBusMasterTcpConfig
    {
        public uint SolarmanSerialNumber { get; set; }
    }
}
