using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Automatica.Core.Driver;
using Microsoft.Extensions.Logging;
using P3.Driver.Blockchain.Ticker.Driver.Bitcoin;
using P3.Driver.Blockchain.Ticker.Driver.Cardano;
using P3.Driver.Blockchain.Ticker.Driver.Ethereum;
using Timer = System.Timers.Timer;

namespace P3.Driver.Blockchain.Ticker.Driver
{
    internal class BlockchainDriver : DriverNoneAttributeBase
    {
        private Timer _timer = new Timer();
      
        private List<CoinNode> _nodes = new List<CoinNode>();

        private ILogger _logger;

        public BlockchainDriver(IDriverContext driverContext) : base(driverContext)
        {
            _logger = driverContext.Logger;
        }

        public override Task<bool> Init(CancellationToken token = default)
        {
            var pollTime = GetPropertyValueInt("poll");
       
            _timer.Elapsed += _timer_Elapsed;
            _timer.Interval = pollTime * 1000;

            _logger.LogInformation($"Start polling every {pollTime}s");


            return base.Init(token);
        }

        public override async Task<bool> Start(CancellationToken token = default)
        {
            _timer.Start();

            await ReadValues(token);

            return await base.Start(token);
        }

        private async void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            await ReadValues();

        }

        public override async Task<bool> Read(CancellationToken token = default)
        {
            await ReadValues(token);

            return true;
        }

        private async Task ReadValues(CancellationToken token = default)
        {
            _logger.LogDebug($"Poll values...");

            foreach (var node in _nodes)
            {
                await node.Refresh();
            }
        }

        public override Task<bool> Stop(CancellationToken token = default)
        {
            _timer.Stop();
            _timer.Elapsed -= _timer_Elapsed;
            return base.Stop(token);
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            CoinNode node = null;
            switch(ctx.NodeInstance.This2NodeTemplateNavigation.Key)
            {
                case "blockchain-btc":
                    node = new BitcoinNode(ctx);
                    break;
                case "blockchain-eth":
                    node = new EthereumNode(ctx);
                    break;
                case "blockchain-ada":
                    node = new CardanoNode(ctx);
                    break;
            }

            if(node != null)
            {
                _nodes.Add(node);
            }

            return node;
        }
    }
}
