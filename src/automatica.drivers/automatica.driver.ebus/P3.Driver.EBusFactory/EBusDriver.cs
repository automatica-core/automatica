using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver;

namespace P3.Driver.EBusFactory
{
    public class EBusDriver : DriverBase
    {
        public EBusDriver(IDriverContext driverContext) : base(driverContext)
        {
        }

        protected override Task Write(object value, IWriteContext writeContext, CancellationToken token = default)
        {
            return Task.CompletedTask;
        }

        protected override Task<bool> Read(IReadContext readContext, CancellationToken token = default)
        {
            return Task.FromResult(false);
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            return null;
        }
    }
}
