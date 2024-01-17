using System;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver;

namespace Automatica.Driver.ShellyFactory.Types.Relay
{
    internal class RelayTimerNode : DriverBase
    {
        public RelayTimerNode(IDriverContext driverContext) : base(driverContext)
        {
        }

        protected override Task Write(object value, IWriteContext writeContext, CancellationToken token = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        protected override Task<bool> Read(IReadContext readContext, CancellationToken token = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            throw new NotImplementedException();
        }
    }
}
