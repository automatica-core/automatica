using Automatica.Core.Driver;

namespace P3.Driver.ZWave
{
    public class ZWaveDriver : DriverBase
    {
        public ZWaveDriver(IDriverContext driverContext) : base(driverContext)
        {
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            return null;
        }
    }
}
