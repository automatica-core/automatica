using System;
using Automatica.Core.Driver;
using P3.Driver.Knx.DriverFactory.Factories.IpTunneling;
using P3.Driver.Knx.DriverFactory.ThreeLevel;

namespace P3.Driver.Knx.DriverFactory.Attributes
{
    public class KnxDpt13Attribute : KnxGroupAddress
    {
        private int? _value;

        public override int ImplementationDptType => (int)P3.Knx.Core.Driver.DptType.Dpt13;
        public KnxDpt13Attribute(IDriverContext driverContext, KnxDriver knxDriver) : base(driverContext, knxDriver)
        {
        }

        protected override bool ValueRead(object value)
        {
            if (value is int dpt13Value)
            {
                var ret = !_value.HasValue || dpt13Value != _value.Value;

                _value = dpt13Value;

                return ret;

            }

            return false;
        }

        protected override object ConvertToDptValue(object value)
        {
            if (value is int vlInt)
            {
                return vlInt;
            }

            throw new NotImplementedException();
        }
    }
}
