using Automatica.Core.Driver;

namespace P3.Driver.FroniusSolarFactory
{
    public class FroniusSolarDriver : DriverBase
    {
        public FroniusSolarDriver(IDriverContext driverContext) : base(driverContext)
        {
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            return null;
        }
    }
}
