namespace P3.Driver.ModBusDriver.Master.Rtu
{
    public class ModBusMasterRtuConfig : ModBusMasterConfig
    {
        public string Port { get; set; }

        public int Baud { get; set; }
        public int DataBits { get; set; }
        public string Parity { get; set; }

        public double StopBits { get; set; }

    }
}
