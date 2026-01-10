#nullable enable
using System.Runtime.CompilerServices;
using Automatica.Core.Driver;

[assembly: InternalsVisibleTo("P3.Driver.Blockchain.Ticker.Console")]

namespace P3.Driver.Blockchain.Ticker.Driver.Ethereum
{
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

        protected override string GetUrl()
        {
            return "https://api.diadata.org/v1/assetQuotation/Ethereum/0x0000000000000000000000000000000000000000";
        }
    }
}
