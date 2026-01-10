using System;

namespace P3.Driver.Blockchain.Ticker.Driver.Dia
{
    public class DiaAssetQuotation
    {
        public string Symbol { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public string Blockchain { get; set; }

        public double Price { get; set; }
        public double PriceYesterday { get; set; }
        public double VolumeYesterdayUSD { get; set; }

        public DateTime Time { get; set; }

        public string Source { get; set; }
        public string Signature { get; set; }
    }

}
