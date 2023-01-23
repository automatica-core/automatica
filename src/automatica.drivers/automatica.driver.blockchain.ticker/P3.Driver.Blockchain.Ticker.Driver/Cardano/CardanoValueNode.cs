using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using Microsoft.Extensions.Logging;
using P3.Driver.Blockchain.Ticker.Driver.Ethereum;

namespace P3.Driver.Blockchain.Ticker.Driver.Cardano
{
    internal class CardanoValueNode : DriverBase
    {
        private readonly string _currency;
        private readonly bool _addSymbol;
        private readonly string _symbol;
        private readonly CardanoNode _cardanoNode;

        public CardanoValueNode(IDriverContext driverContext, string currency, bool addSymbol, string symbol,
            CardanoNode bitcoinNode) : base(driverContext)
        {
            _currency = currency;
            _addSymbol = addSymbol;
            _symbol = symbol;
            _cardanoNode = bitcoinNode;
        }

        public override async Task<bool> Read()
        {
            await _cardanoNode.Refresh();

            return true;
        }

        public void UpdateValue(List<TickerPriceValue> values)
        {


            if(values.Any(a => a.Symbol == _currency))
            {
                
                var tickerPrice = values.First(a => a.Symbol == _currency);
                
                var value = $"{tickerPrice.LastTradePrice.ToString(CultureInfo.InvariantCulture)}";

                if (_addSymbol)
                {
                    value += _symbol;
                }

                DispatchValue(value);

                DriverContext.Logger.LogDebug($"Read value {tickerPrice.LastTradePrice}{_symbol}");
            }
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            return null;
        }
    }
}