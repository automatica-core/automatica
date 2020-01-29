using System.Threading.Tasks;
using Automatica.Core.Driver;

namespace P3.Driver.Blockchain.Ticker.Driver
{
    internal abstract class CoinNode : DriverBase
    {
        protected CoinNode(IDriverContext driverContext) : base(driverContext)
        {
        }

        public virtual Task Refresh()
        {
            return Task.CompletedTask;
        }

    }
}
