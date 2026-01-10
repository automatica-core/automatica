using Automatica.Core.Driver;
using P3.Driver.Blockchain.Ticker.Driver.Dia;

namespace P3.Driver.Blockchain.Ticker.Driver.Ethereum
{
    internal class EthereumValueNode(
        IDriverContext driverContext,
        string currency,
        bool addSymbol,
        string symbol,
        EthereumNode ethNode)
        : CoinValueNode(driverContext, ethNode, currency, addSymbol, symbol)
    {
     
        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            return null;
        }

        public override DiaAssetQuotation? GetPriceValue(DiaAssetQuotation tickerValue)
        {
            return tickerValue;
        }
    }
}