using Automatica.Core.Driver;
using P3.Driver.EnOcean.DriverFactory.Driver.Simulated;

namespace P3.Driver.EnOcean.DriverFactory.Driver
{
    public class EnOceanSimulatedDevices : DriverBase
    {
        private readonly P3.Driver.EnOcean.Driver _driver;

        public EnOceanSimulatedDevices(IDriverContext driverContext, P3.Driver.EnOcean.Driver driver) : base(driverContext)
        {
            _driver = driver;
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            return new EnOceanSimulatedNode(ctx, _driver);
        }
    }
}
