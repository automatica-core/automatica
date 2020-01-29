using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

[assembly: InternalsVisibleTo("P3.Driver.Blockchain.Ticker.Console")]

namespace P3.Driver.Blockchain.Ticker.Driver.Bitcoin
{
    internal class BitcoinNode : CoinNode
    {
        private readonly List<BitcoinValueNode> _nodes = new List<BitcoinValueNode>();
        private readonly HttpClient _client = new HttpClient();

        public BitcoinNode(IDriverContext driverContext) : base(driverContext)
        {

        }

        public override async Task Refresh()
        {
            try
            {
                using (var response = await _client.GetAsync("https://blockchain.info/ticker"))
                {
                    response.EnsureSuccessStatusCode();

                    var res = await response.Content.ReadAsStringAsync();

                    var jsonToken = Newtonsoft.Json.JsonConvert.DeserializeObject<JObject>(res);

                    foreach (var node in _nodes)
                    {
                        node.UpdateValue(jsonToken);
                    }
                }
            }
            catch (Exception e)
            {
                DriverContext.Logger.LogError(e, "Could not refresh state");
            }
        }

        internal void AddNode(BitcoinValueNode node)
        {
            if (node != null)
            {
                _nodes.Add(node);
            }
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            BitcoinValueNode node = null;
            switch (ctx.NodeInstance.This2NodeTemplateNavigation.Key)
            {
                case "blockchain-btc-usd":
                    node = new BitcoinValueNode(ctx, "USD");
                    break;
                case "blockchain-btc-eur":
                    node = new BitcoinValueNode(ctx, "EUR");
                    break;
                case "blockchain-btc-usd-with-symbol":
                    node = new BitcoinValueNode(ctx, "USD", true);
                    break;
                case "blockchain-btc-eur-with-symbol":
                    node = new BitcoinValueNode(ctx, "EUR", true);
                    break;
            }

            AddNode(node);
          
            return node;
        }
    }
}
