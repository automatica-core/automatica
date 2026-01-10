using Automatica.Core.Driver;
using P3.Driver.Blockchain.Ticker.Driver.Dia;

namespace P3.Driver.Blockchain.Ticker.Driver.Bitcoin
{
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

        public override DiaAssetQuotation GetPriceValue(DiaAssetQuotation tickerValue)
        {
            return tickerValue;
        }
    }
}