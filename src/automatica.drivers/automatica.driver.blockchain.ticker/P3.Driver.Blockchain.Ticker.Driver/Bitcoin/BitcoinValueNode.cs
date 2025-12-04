using Automatica.Core.Driver;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using P3.Driver.Blockchain.Ticker.Driver.Ethereum;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace P3.Driver.Blockchain.Ticker.Driver.Bitcoin
{
    internal class BlockchainValue
    {
        [JsonProperty("last")]
        public double Last { get; set; }

        [JsonProperty("symbol")]
        public string Symbol { get; set; }
    }


    internal class BitcoinValueNode(
        IDriverContext driverContext,
        string currency,
        bool addSymbol,
        string symbol,
        BitcoinNode bitcoinNode)
        : CoinValueNode(driverContext, bitcoinNode, currency, addSymbol, symbol)
    {
       

      
        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            return null;
        }

        public override PriceValue GetPriceValue(TickerPriceValue tickerValue)
        {
            return tickerValue.Bitcoin;
        }
    }
}