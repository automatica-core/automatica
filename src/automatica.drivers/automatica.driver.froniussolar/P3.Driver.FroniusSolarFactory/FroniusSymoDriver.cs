using Automatica.Core.Driver;

namespace P3.Driver.FroniusSymoFactory
{
    public class FroniusSymoDriver : DriverBase
    {
        public FroniusSymoDriver(IDriverContext driverContext) : base(driverContext)
        {
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            return null;
        }
    }
}
