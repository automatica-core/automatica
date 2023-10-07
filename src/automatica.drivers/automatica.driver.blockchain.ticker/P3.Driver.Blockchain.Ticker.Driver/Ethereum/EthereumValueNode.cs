using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using Microsoft.Extensions.Logging;

namespace P3.Driver.Blockchain.Ticker.Driver.Ethereum
{
    internal class EthereumValueNode : DriverNotWriteableBase
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
        protected override async Task<bool> Read(IReadContext readContext, CancellationToken token = new CancellationToken())
        {
            await _ethNode.Refresh(token);
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

                DispatchRead(value);

                DriverContext.Logger.LogDebug($"Read value {tickerPrice.LastTradePrice}{_symbol}");
            }
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            return null;
        }
    }
}