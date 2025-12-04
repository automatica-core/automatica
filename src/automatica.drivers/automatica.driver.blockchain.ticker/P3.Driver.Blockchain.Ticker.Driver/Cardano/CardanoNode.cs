using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using P3.Driver.Blockchain.Ticker.Driver.Ethereum;

[assembly: InternalsVisibleTo("P3.Driver.Blockchain.Ticker.Console")]

namespace P3.Driver.Blockchain.Ticker.Driver.Cardano
{
    internal class CardanoNode : CoinNode<CardanoValueNode>
    {
        private readonly List<CardanoValueNode> _nodes = new();
        private readonly HttpClient _client;

        public CardanoNode(IDriverContext driverContext) : base(driverContext)
        {
            _client = new HttpClient();
            _client.Timeout = TimeSpan.FromSeconds(5);
        }

        protected override async Task<bool> Read(IReadContext readContext, CancellationToken token = new CancellationToken())
        {
            await Refresh(token);
            return true;
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            CardanoValueNode node = null;
            switch (ctx.NodeInstance.This2NodeTemplateNavigation.Key)
            {
                case "blockchain-ada-usd":
                    node = new CardanoValueNode(ctx, "ADA-USD", false, "", this);
                    break;
                case "blockchain-ada-usd-with-symbol":
                    node = new CardanoValueNode(ctx, "ADA-USD", true, "USD", this);
                    break;
                
            }

            AddNode(node);
          
            return node;
        }
    }
}
