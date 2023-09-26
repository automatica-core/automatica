using System;
using Automatica.Core.Driver;
using Knx.Falcon.ApplicationData;
using P3.Driver.Knx.DriverFactory.Factories.IpTunneling;
using P3.Driver.Knx.DriverFactory.ThreeLevel;

namespace P3.Driver.Knx.DriverFactory.Attributes
{
    public class KnxDpt11Attribute : KnxGroupAddress
    {
        private DateTime? _value;

        public override int ImplementationDptType => (int)P3.Knx.Core.Driver.DptType.Dpt11;
        public KnxDpt11Attribute(IDriverContext driverContext, KnxDriver knxDriver) : base(driverContext, knxDriver)
        {
        }

        protected override bool ValueRead(object value)
        {
            if (value is KnxDate dpt11Value)
            {
                var ret = !_value.HasValue || dpt11Value != _value.Value;

                _value = dpt11Value;

                return ret;

            }
            return false;
        }

        protected override object ConvertToDptValue(object value, IWriteContext writeContext)
        {
            if (value is DateTime vlDt)
            {
                return new KnxDate(vlDt);
            }

            if (value is DateOnly dtOnly)
            {
                return new KnxDate(new DateTime(dtOnly.Year, dtOnly.Month, dtOnly.Day));
            }

            throw new NotImplementedException();
        }
    }
}
