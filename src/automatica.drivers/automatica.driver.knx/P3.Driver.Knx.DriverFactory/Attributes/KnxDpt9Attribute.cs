using System;
using Automatica.Core.Driver;
using P3.Driver.Knx.DriverFactory.Factories.IpTunneling;
using P3.Driver.Knx.DriverFactory.ThreeLevel;

namespace P3.Driver.Knx.DriverFactory.Attributes
{
    public class KnxDpt9Attribute : KnxGroupAddress
    {
        private double? _value;

        public override int ImplementationDptType => (int)P3.Knx.Core.Driver.DptType.Dpt9;
        public KnxDpt9Attribute(IDriverContext driverContext, KnxDriver knxDriver) : base(driverContext, knxDriver)
        {
        }

        protected override bool ValueRead(object value)
        {
            var newValue = Convert.ToDouble(value);
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            var ret = !_value.HasValue || newValue != _value.Value;

            _value = newValue;

            return ret;
        }
        protected override object ConvertToDptValue(object value)
        {
            var newValue = Convert.ToDouble(value);
            var oldValue = _value;
            _value = newValue;

            if (WriteOnlyIfChanged)
            {
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (newValue != oldValue)
                {
                    return _value;
                }

                return null;
            }
            return _value;
        }
    }
}
