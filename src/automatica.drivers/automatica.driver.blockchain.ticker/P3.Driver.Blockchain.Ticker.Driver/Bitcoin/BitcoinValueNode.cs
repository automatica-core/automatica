using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace P3.Driver.Blockchain.Ticker.Driver.Bitcoin
{
    internal class BlockchainValue
    {
        [JsonProperty("last")]
        public double Last { get; set; }

        [JsonProperty("symbol")]
        public string Symbol { get; set; }
    }


    internal class BitcoinValueNode : DriverNotWriteableBase
    {
        private readonly string _currency;
        private readonly bool _addSymbol;
        private readonly BitcoinNode _bitcoinNode;

        public BitcoinValueNode(IDriverContext driverContext, string currency, bool addSymbol,
            BitcoinNode bitcoinNode) : base(driverContext)
        {
            _currency = currency;
            _addSymbol = addSymbol;
            _bitcoinNode = bitcoinNode;
        }
        protected override async Task<bool> Read(IReadContext readContext, CancellationToken token = new CancellationToken())
        {
            await _bitcoinNode.Refresh(token);
            return true;
        }


        public void UpdateValue(JObject jObject)
        {
            if(jObject.ContainsKey(_currency))
            {
                var valueEntry = jObject[_currency].ToString();

                var blockChainValue = JsonConvert.DeserializeObject<BlockchainValue>(valueEntry);

                var value = $"{blockChainValue!.Last.ToString(CultureInfo.InvariantCulture)}";

                if (_addSymbol)
                {
                    value += blockChainValue.Symbol;
                }

                DispatchRead(value);

                DriverContext.Logger.LogDebug($"Read value {blockChainValue.Last}{blockChainValue.Symbol}");
            }
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            return null;
        }
    }
}