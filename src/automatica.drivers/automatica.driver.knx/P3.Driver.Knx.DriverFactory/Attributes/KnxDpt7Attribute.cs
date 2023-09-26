using Automatica.Core.Driver;
using P3.Driver.Knx.DriverFactory.Factories.IpTunneling;
using P3.Driver.Knx.DriverFactory.ThreeLevel;

namespace P3.Driver.Knx.DriverFactory.Attributes
{
    public class KnxDpt7Attribute : KnxGroupAddress
    {
        public override int ImplementationDptType => (int)P3.Knx.Core.Driver.DptType.Dpt7;
        public KnxDpt7Attribute(IDriverContext driverContext, KnxDriver knxDriver) : base(driverContext, knxDriver)
        {
        }
        protected override object ConvertToDptValue(object value, IWriteContext writeContext)
        {
            return value;
        }
    }
}
