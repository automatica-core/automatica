using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

[assembly: InternalsVisibleTo("P3.Driver.Blockchain.Ticker.Console")]

namespace P3.Driver.Blockchain.Ticker.Driver.Ethereum
{
    internal class TickerPriceValue
    {
        [JsonProperty("symbol")]
        public string Symbol { get; set; }

        [JsonProperty("last_trade_price")]
        public double LastTradePrice { get; set; }
    }

    internal class EthereumNode : CoinNode
    {
        private readonly List<EthereumValueNode> _nodes = new();
        private readonly HttpClient _client = new ();

        public EthereumNode(IDriverContext driverContext) : base(driverContext)
        {
        }

        public override async Task<bool> Read(CancellationToken token = default)
        {
            await Refresh(token);
            return true;
        }

        public override async Task Refresh(CancellationToken token = default)
        {
            try
            {
                using var response = await _client.GetAsync("https://api.blockchain.com/v3/exchange/tickers", token);
                response.EnsureSuccessStatusCode();

                var res = await response.Content.ReadAsStringAsync(token);

                var jsonToken = JsonConvert.DeserializeObject<List<TickerPriceValue>>(res);

                foreach (var node in _nodes)
                {
                    node.UpdateValue(jsonToken);
                }
            }
            catch (Exception e)
            {
                DriverContext.Logger.LogError(e, "Could not refresh state");
            }
        }

        internal void AddNode(EthereumValueNode node)
        {
            if (node != null)
            {
                _nodes.Add(node);
            }
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            EthereumValueNode node = null;
            switch (ctx.NodeInstance.This2NodeTemplateNavigation.Key)
            {
                case "blockchain-eth-usd":
                    node = new EthereumValueNode(ctx, "ETH-USD", false, "", this);
                    break;
                case "blockchain-eth-eur":
                    node = new EthereumValueNode(ctx, "ETH-EUR", false, "",this);
                    break;
                case "blockchain-eth-usd-with-symbol":
                    node = new EthereumValueNode(ctx, "ETH-USD", true, "USD", this);
                    break;
                case "blockchain-eth-eur-with-symbol":
                    node = new EthereumValueNode(ctx, "ETH-EUR", true, "EUR", this);
                    break;
            }

            AddNode(node);
          
            return node;
        }
    }
}
