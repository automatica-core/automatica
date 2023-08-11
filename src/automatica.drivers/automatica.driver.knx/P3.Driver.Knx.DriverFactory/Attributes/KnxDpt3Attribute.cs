using Automatica.Core.Driver;
using P3.Driver.Knx.DriverFactory.Factories.IpTunneling;
using P3.Driver.Knx.DriverFactory.ThreeLevel;

namespace P3.Driver.Knx.DriverFactory.Attributes
{
    public class KnxDpt3Attribute : KnxGroupAddress
    {
        public override int ImplementationDptType => (int)P3.Knx.Core.Driver.DptType.Dpt3;
        public KnxDpt3Attribute(IDriverContext driverContext, KnxDriver knxDriver) : base(driverContext, knxDriver)
        {
        }

        protected override object ConvertToDptValue(object value)
        {
            return value;
        }
    }
}
