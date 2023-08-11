using System;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.Driver;
using P3.Driver.Knx.DriverFactory.Factories.IpTunneling;
using P3.Driver.Knx.DriverFactory.ThreeLevel;

namespace P3.Driver.Knx.DriverFactory.Attributes
{
    public class KnxDpt9Attribute : KnxGroupAddress
    {
        private double? _value;

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

        public override Task WriteValue(IDispatchable source, object value)
        {
            var newValue = Convert.ToDouble(value);

            DispatchValue(newValue);
            _value = newValue;

            Driver.Write(this, GroupAddress, ConvertToBus(newValue));

            return Task.CompletedTask;
        }
    }
}
