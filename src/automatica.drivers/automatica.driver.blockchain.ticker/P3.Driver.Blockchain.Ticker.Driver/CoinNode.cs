using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver;

namespace P3.Driver.Blockchain.Ticker.Driver
{
    internal abstract class CoinNode : DriverNoneAttributeBase
    {
        protected CoinNode(IDriverContext driverContext) : base(driverContext)
        {
        }

        public virtual Task Refresh(CancellationToken token = default)
        {
            return Task.CompletedTask;
        }

    }
}
