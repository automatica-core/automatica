using Automatica.Core.Driver;

namespace P3.Driver.Synology.DriverFactory
{
    internal class SynologyDriver : DriverNoneAttributeBase
    {
        public SynologyDriver(IDriverContext driverContext) : base(driverContext)
        {
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            return new SynologyDevice(ctx);
        }
    }
}
