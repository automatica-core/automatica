﻿using System;
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
    internal class CardanoNode : CoinNode
    {
        private readonly List<CardanoValueNode> _nodes = new();
        private readonly HttpClient _client = new ();

        public CardanoNode(IDriverContext driverContext) : base(driverContext)
        {
        }

        protected override async Task<bool> Read(IReadContext readContext, CancellationToken token = new CancellationToken())
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

        internal void AddNode(CardanoValueNode node)
        {
            if (node != null)
            {
                _nodes.Add(node);
            }
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
