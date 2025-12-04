using Automatica.Core.Driver;

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

        public override PriceValue? GetPriceValue(TickerPriceValue tickerValue)
        {
            return tickerValue.Ethereum;
        }
    }
}