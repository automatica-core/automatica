using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using Microsoft.Extensions.Logging;

namespace P3.Driver.Blockchain.Ticker.Driver.Ethereum
{
    internal class EthereumValueNode : DriverBase
    {
        private readonly string _currency;
        private readonly bool _addSymbol;
        private readonly string _symbol;
        private readonly EthereumNode _ethNode;

        public EthereumValueNode(IDriverContext driverContext, string currency, bool addSymbol, string symbol,
            EthereumNode bitcoinNode) : base(driverContext)
        {
            _currency = currency;
            _addSymbol = addSymbol;
            _symbol = symbol;
            _ethNode = bitcoinNode;
        }

        public override async Task<bool> Read()
        {
            await _ethNode.Refresh();

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