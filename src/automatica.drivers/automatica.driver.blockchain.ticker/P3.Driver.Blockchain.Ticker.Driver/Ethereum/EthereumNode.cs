#nullable enable
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

[assembly: InternalsVisibleTo("P3.Driver.Blockchain.Ticker.Console")]

namespace P3.Driver.Blockchain.Ticker.Driver.Ethereum
{
    public class PriceValue
    {
        [JsonProperty("usd")]
        public double Usd { get; set; }

        [JsonProperty("eur")]
        public double Eur { get; set; }
    }

    public class TickerPriceValue
    {
        [JsonProperty("ethereum")]
        public PriceValue? Ethereum { get; set; }

        [JsonProperty("cardano")]
        public PriceValue? Cardano { get; set; }

        [JsonProperty("bitcoin")]
        public PriceValue? Bitcoin { get; set; }
    }

    internal class EthereumNode(IDriverContext driverContext) : CoinNode<EthereumValueNode>(driverContext)
    {
        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            EthereumValueNode? node = null;
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
