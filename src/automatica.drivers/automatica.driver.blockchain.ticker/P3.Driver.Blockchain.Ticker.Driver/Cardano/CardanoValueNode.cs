using Automatica.Core.Driver;
using P3.Driver.Blockchain.Ticker.Driver.Dia;

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

        public override DiaAssetQuotation? GetPriceValue(DiaAssetQuotation tickerValue)
        {
            return tickerValue;
        }
    }
}