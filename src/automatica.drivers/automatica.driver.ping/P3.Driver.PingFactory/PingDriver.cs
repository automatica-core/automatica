using Automatica.Core.Driver;

namespace P3.Driver.PingFactory
{
    public class PingDriver : DriverNoneAttributeBase
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
