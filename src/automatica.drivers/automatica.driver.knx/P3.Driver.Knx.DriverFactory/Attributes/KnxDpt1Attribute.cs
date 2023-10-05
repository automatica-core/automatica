using System;
using Automatica.Core.Driver;
using P3.Driver.Knx.DriverFactory.Factories.IpTunneling;
using P3.Driver.Knx.DriverFactory.ThreeLevel;

namespace P3.Driver.Knx.DriverFactory.Attributes
{
    public class KnxDpt1Attribute : KnxGroupAddress
    {
        private bool? _value;

        public override int ImplementationDptType => (int)P3.Knx.Core.Driver.DptType.Dpt1;
        public KnxDpt1Attribute(IDriverContext driverContext, KnxDriver knxDriver) : base(driverContext, knxDriver)
        {
        }

        protected override object GetCurrentValue()
        {
            return _value;
        }

        protected override object ConvertToDptValue(object value)
        {
            var newValue = Convert.ToBoolean(value);
            var oldValue = _value;

            _value = newValue;
            if (WriteOnlyIfChanged)
            {
                if (oldValue != newValue)
                {
                    return newValue;
                }

                return null;
            }

            return newValue;
        }

        protected override bool ValueRead(object value)
        {
            DispatchRead(value);
            return true; //always dispatch dpt1 values...
        }
    }
}
