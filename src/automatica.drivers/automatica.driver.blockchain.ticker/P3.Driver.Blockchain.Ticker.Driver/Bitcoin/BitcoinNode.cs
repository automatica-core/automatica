using Automatica.Core.Driver;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("P3.Driver.Blockchain.Ticker.Console")]

namespace P3.Driver.Blockchain.Ticker.Driver.Bitcoin
{
    internal class BitcoinNode : CoinNode<BitcoinValueNode>
    {
        private readonly List<BitcoinValueNode> _nodes = new List<BitcoinValueNode>();
        private readonly HttpClient _client;

        public BitcoinNode(IDriverContext driverContext) : base(driverContext)
        {
            _client = new HttpClient();
            _client.Timeout = TimeSpan.FromSeconds(5);
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            BitcoinValueNode node = null;
            switch (ctx.NodeInstance.This2NodeTemplateNavigation.Key)
            {
                case "blockchain-btc-usd":
                    node = new BitcoinValueNode(ctx, "USD", false, "",this);
                    break;
                case "blockchain-btc-eur":
                    node = new BitcoinValueNode(ctx, "EUR", false, "", this);
                    break;
                case "blockchain-btc-usd-with-symbol":
                    node = new BitcoinValueNode(ctx, "USD", true, "USD",this);
                    break;
                case "blockchain-btc-eur-with-symbol":
                    node = new BitcoinValueNode(ctx, "EUR", true, "EUR",this);
                    break;
            }

            AddNode(node);
          
            return node;
        }

        protected override string GetUrl()
        {
            return "https://api.diadata.org/v1/assetQuotation/Bitcoin/0x0000000000000000000000000000000000000000";
        }
    }
}
