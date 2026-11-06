using System;
using Automatica.Core.Driver;
using P3.Driver.Knx.DriverFactory.Factories.IpTunneling;
using P3.Driver.Knx.DriverFactory.ThreeLevel;

namespace P3.Driver.Knx.DriverFactory.Attributes
{
    public class KnxDpt5Attribute : KnxGroupAddress
    {
        private int? _value; // null = unknown, the first write always goes out

        public override int ImplementationDptType => (int)P3.Knx.Core.Driver.DptType.Dpt5;
        public KnxDpt5Attribute(IDriverContext driverContext, KnxDriver knxDriver) : base(driverContext, knxDriver)
        {
        }

        protected override object GetCurrentValue()
        {
            return _value;
        }

        protected override object ConvertToDptValue(object value)
        {
            var newValue = Convert.ToInt32(value);
            var oldValue = _value;

            _value = newValue;

            if (WriteOnlyIfChanged)
            {
                if (newValue != oldValue)
                {
                    return newValue;
                }

                return null;
            }

            return newValue;
        }

        protected override bool ValueRead(object value)
        {
            try
            {
                _value = Convert.ToInt32(value); // WriteOnlyIfChanged compares against the current bus state
            }
            catch (Exception e) when (e is FormatException or InvalidCastException or OverflowException)
            {
                _value = null;
            }

            return true;
        }
    }
}
