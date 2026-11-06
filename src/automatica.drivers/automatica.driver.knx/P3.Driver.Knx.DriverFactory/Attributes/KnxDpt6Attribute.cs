using System;
using Automatica.Core.Driver;
using Microsoft.Extensions.Logging;
using P3.Driver.Knx.DriverFactory.Factories.IpTunneling;
using P3.Driver.Knx.DriverFactory.ThreeLevel;

namespace P3.Driver.Knx.DriverFactory.Attributes
{
    public class KnxDpt6Attribute : KnxGroupAddress
    {
        private sbyte? _value;

        public override int ImplementationDptType => (int)P3.Knx.Core.Driver.DptType.Dpt6;
        public KnxDpt6Attribute(IDriverContext driverContext, KnxDriver knxDriver) : base(driverContext, knxDriver)
        {
        }

        protected override object GetCurrentValue()
        {
            return _value;
        }

        protected override object ConvertToDptValue(object value)
        {
            int intValue;
            try
            {
                intValue = Convert.ToInt32(value);
            }
            catch (Exception e) when (e is FormatException or InvalidCastException or OverflowException)
            {
                throw new NotImplementedException($"{value} is not a number", e);
            }

            if (intValue is < sbyte.MinValue or > sbyte.MaxValue)
            {
                DriverContext.Logger.LogWarning($"{GroupAddress}: {value} is out of range for DPT 6 ({sbyte.MinValue}..{sbyte.MaxValue})");
                throw new NotImplementedException($"{value} is out of range for DPT 6");
            }

            var newValue = (sbyte)intValue;
            var oldValue = _value;
            _value = newValue;

            if (WriteOnlyIfChanged && oldValue == newValue)
            {
                return null;
            }

            return newValue;
        }

        protected override bool ValueRead(object value)
        {
            try
            {
                var intValue = Convert.ToInt32(value);
                // WriteOnlyIfChanged compares against the current bus state
                _value = intValue is < sbyte.MinValue or > sbyte.MaxValue ? null : (sbyte)intValue;
            }
            catch (Exception e) when (e is FormatException or InvalidCastException or OverflowException)
            {
                _value = null;
            }

            return true;
        }
    }
}
