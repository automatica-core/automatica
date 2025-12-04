using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using Microsoft.Extensions.Logging;
using P3.Driver.Blockchain.Ticker.Driver.Ethereum;

namespace P3.Driver.Blockchain.Ticker.Driver.Cardano
{
    internal class CardanoValueNode(
        IDriverContext driverContext,
        string currency,
        bool addSymbol,
        string symbol,
        CardanoNode node)
        : CoinValueNode(driverContext, node, currency, addSymbol, symbol)
    {
       
        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            return null;
        }

        public override PriceValue? GetPriceValue(TickerPriceValue tickerValue)
        {
            return tickerValue.Cardano;
        }
    }
}