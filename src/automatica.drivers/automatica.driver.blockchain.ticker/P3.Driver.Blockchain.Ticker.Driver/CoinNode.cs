using Automatica.Core.Driver;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using P3.Driver.Blockchain.Ticker.Driver.Dia;
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
        // one client for all coin nodes and driver instances, so reloads don't leak sockets
        protected static readonly HttpClient SharedClient = new HttpClient(new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(5)
        })
        {
            Timeout = TimeSpan.FromSeconds(5)
        };

        internal abstract Task Refresh(CancellationToken token = default);
    }


    internal abstract class CoinNode<T> : CoinNode where  T : CoinValueNode
    {
        private readonly List<T> _nodes = new();

        protected CoinNode(IDriverContext driverContext) : base(driverContext)
        {
        }
        internal void AddNode(T node)
        {
            if (node != null)
            {
                _nodes.Add(node);
            }
        }

        protected abstract string GetUrl();

        internal override async Task Refresh(CancellationToken token = default)
        {
            try
            {
                using var response = await SharedClient.GetAsync(GetUrl(), token);
                response.EnsureSuccessStatusCode();

                var res = await response.Content.ReadAsStringAsync(token);

                var jsonToken = JsonConvert.DeserializeObject<DiaAssetQuotation>(res);

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
