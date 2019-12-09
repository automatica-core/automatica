using Automatica.Core.Driver;

namespace P3.Driver.PingFactory
{
    public class PingDriver : DriverBase
    {
        public PingDriver(IDriverContext driverContext) : base(driverContext)
        {
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            return new PingDevice(ctx);
        }
    }
}
