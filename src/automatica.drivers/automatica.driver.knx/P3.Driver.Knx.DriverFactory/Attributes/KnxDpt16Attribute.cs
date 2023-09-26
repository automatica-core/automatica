using System;
using Automatica.Core.Driver;
using P3.Driver.Knx.DriverFactory.Factories.IpTunneling;
using P3.Driver.Knx.DriverFactory.ThreeLevel;

namespace P3.Driver.Knx.DriverFactory.Attributes
{
    public class KnxDpt16Attribute : KnxGroupAddress
    {

        public override int ImplementationDptType => (int)P3.Knx.Core.Driver.DptType.Dpt16;
        public KnxDpt16Attribute(IDriverContext driverContext, KnxDriver knxDriver) : base(driverContext, knxDriver)
        {
        }

        protected override object ConvertToDptValue(object value, IWriteContext writeContext)
        {
            if (value is string dpt16Value)
            {
                return dpt16Value;
            }

            throw new NotImplementedException();
        }

        protected override bool ValueRead(object value)
        {
            if (value is string dpt16Value)
            {
                DispatchRead(dpt16Value);
                return false;
            }

            return false;
        }
    }
}
