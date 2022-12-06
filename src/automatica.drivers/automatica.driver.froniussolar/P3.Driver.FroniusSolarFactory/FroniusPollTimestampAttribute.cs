using System;
using Automatica.Core.Driver;

namespace P3.Driver.FroniusSolarFactory
{
    internal class FroniusPollTimestampAttribute : DriverBase
    {
        public FroniusPollTimestampAttribute(IDriverContext driverContext) : base(driverContext)
        {
        }

        public void DispatchTimestamp()
        {
            DriverContext.Dispatcher.DispatchValue(this, DateTime.Now);
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            return null;
        }
    }
}
