using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using Microsoft.Extensions.Logging;
using P3.Driver.Blockchain.Ticker.Driver.Ethereum;

namespace P3.Driver.Blockchain.Ticker.Driver.Cardano
{
    internal class CardanoValueNode : DriverNotWriteableBase
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
        protected override async Task<bool> Read(IReadContext readContext, CancellationToken token = new CancellationToken())
        {
            await _cardanoNode.Refresh(token);
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