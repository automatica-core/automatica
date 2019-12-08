using Automatica.Core.Driver;

namespace P3.Driver.EBusFactory
{
    public class EBusDriver : DriverBase
    {
        public EBusDriver(IDriverContext driverContext) : base(driverContext)
        {
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            return null;
        }
    }
}
