using Automatica.Core.Driver;
using Microsoft.Extensions.Logging;
using P3.Driver.Blockchain.Ticker.Driver.Dia;
using P3.Driver.Blockchain.Ticker.Driver.Ethereum;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace P3.Driver.Blockchain.Ticker.Driver
{
    internal abstract class CoinValueNode(IDriverContext driverContext, CoinNode parent, string currency, bool addSymbol, string symbol)
        : DriverNotWriteableBase(driverContext)
    {
        public abstract DiaAssetQuotation? GetPriceValue(DiaAssetQuotation tickerValue);
        protected override async Task<bool> Read(IReadContext readContext, CancellationToken token = new CancellationToken())
        {
            await parent.Refresh(token);
            return true;
        }


        public void UpdateValue(DiaAssetQuotation tickerValue)
        {
            var ticker = GetPriceValue(tickerValue);

            if (ticker != null)
            {
                var tickerPrice = ticker.Price;

                if (currency.ToLowerInvariant().Contains("usd"))
                {
                    tickerPrice = ticker.Price;
                }

                var value = $"{tickerPrice.ToString(CultureInfo.InvariantCulture)}";

                if (addSymbol)
                {
                    value += symbol;
                }

                DispatchRead(value);

                DriverContext.Logger.LogDebug($"Read value {tickerPrice}{symbol}");
            }
        }
    }
}
