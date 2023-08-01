using System;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver;

namespace P3.Driver.Synology.DriverFactory.Attributes
{
    internal class SynologyConnectedAttribute : DriverBase
    {

        public SynologyConnectedAttribute(IDriverContext driverContext) : base(driverContext)
        {
        }

        public override Task<bool> Start(CancellationToken token = new CancellationToken())
        {
            return base.Start(token);
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            throw new NotImplementedException();
        }
    }
}
