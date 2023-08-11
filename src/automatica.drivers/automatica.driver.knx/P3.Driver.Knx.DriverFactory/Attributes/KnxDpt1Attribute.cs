using System;
using Automatica.Core.Driver;
using P3.Driver.Knx.DriverFactory.Factories.IpTunneling;
using P3.Driver.Knx.DriverFactory.ThreeLevel;

namespace P3.Driver.Knx.DriverFactory.Attributes
{
    public class KnxDpt1Attribute : KnxGroupAddress
    {
        private bool? _value;

        public KnxDpt1Attribute(IDriverContext driverContext, KnxDriver knxDriver) : base(driverContext, knxDriver)
        {
        }

        protected override object ConvertToDptValue(object value)
        {
            var newValue = Convert.ToBoolean(value);

            if (newValue != _value)
            {
                _value = newValue;
                DispatchValue(newValue);
                return newValue;
            }

            return null;
        }

        protected override bool ValueRead(object value)
        {
            return true; //always dispatch dpt1 values...
        }
    }
}
