using Automatica.Core.Driver;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using P3.Driver.Blockchain.Ticker.Driver.Ethereum;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace P3.Driver.Blockchain.Ticker.Driver
{
    internal abstract class CoinNode(IDriverContext driverContext) : DriverNoneAttributeBase(driverContext)
    {
        internal abstract Task Refresh(CancellationToken token = default);
    }


    internal abstract class CoinNode<T> : CoinNode where  T : CoinValueNode
    {
        private readonly HttpClient _client;
        private readonly List<T> _nodes = new();

        protected CoinNode(IDriverContext driverContext) : base(driverContext)
        {
            _client = new HttpClient();
            _client.Timeout = TimeSpan.FromSeconds(5);
        }
        internal void AddNode(T node)
        {
            if (node != null)
            {
                _nodes.Add(node);
            }
        }

        internal override async Task Refresh(CancellationToken token = default)
        {
            try
            {
                using var response = await _client.GetAsync("https://api.coingecko.com/api/v3/simple/price?ids=cardano,ethereum,bitcoin&vs_currencies=usd,eur,btc", token);
                response.EnsureSuccessStatusCode();

                var res = await response.Content.ReadAsStringAsync(token);

                var jsonToken = JsonConvert.DeserializeObject<TickerPriceValue>(res);

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

    }
}
