using System;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver;

namespace P3.Driver.FroniusSolarFactory
{
    internal class FroniusPollTimestampAttribute : DriverBase
    {
        public FroniusPollTimestampAttribute(IDriverContext driverContext) : base(driverContext)
        {
        }

        public void DispatchTimestamp()
        {
            DriverContext.Dispatcher.DispatchValue(this, DateTime.Now);
        }

        protected override Task Write(object value, IWriteContext writeContext, CancellationToken token = new CancellationToken())
        {
            return Task.CompletedTask;
        }

        protected override async Task<bool> Read(IReadContext readContext, CancellationToken token = new CancellationToken())
        {
            await readContext.DispatchValue(DateTime.Now, token);
            return true;
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            return null;
        }
    }
}
