using System;
using Automatica.Core.Driver;

namespace P3.Driver.ModBus.SolarmanV5.DriverFactory
{
    internal class SolarmanPollTimestampAttribute : DriverNoneAttributeBase
    {
        public SolarmanPollTimestampAttribute(IDriverContext driverContext) : base(driverContext)
        {
        }

        public void DispatchTimestamp()
        {
            DispatchRead(DateTime.Now);
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            return null;
        }
    }
}
