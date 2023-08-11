using System;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.Driver;
using Knx.Falcon.ApplicationData;
using P3.Driver.Knx.DriverFactory.Factories.IpTunneling;
using P3.Driver.Knx.DriverFactory.ThreeLevel;

namespace P3.Driver.Knx.DriverFactory.Attributes
{
    public class KnxDpt11Attribute : KnxGroupAddress
    {
        private DateTime? _value;

        public KnxDpt11Attribute(IDriverContext driverContext, KnxDriver knxDriver) : base(driverContext, knxDriver)
        {
        }

        protected override bool ValueRead(object value)
        {
            if (value is KnxDate dpt11Value)
            {
                var ret = !_value.HasValue || dpt11Value != _value.Value;

                _value = dpt11Value;

                if (ret)
                {
                    DispatchValue(_value);
                }

                return false;

            }
            return false;
        }

        protected override object ConvertToDptValue(object value)
        {
            if (value is DateTime vlDt)
            {
                DispatchValue(vlDt);
                return new KnxDate(vlDt);
            }
            else if (value is DateOnly dtOnly)
            {
                DispatchValue(dtOnly);
                return new KnxDate(new DateTime(dtOnly.Year, dtOnly.Month, dtOnly.Day));
            }

            return null;
        }

        public override Task WriteValue(IDispatchable source, object value)
        {
            DateTime? dateTime = null;
            var lastValue = _value;

            if (value is DateTime dt)
            {
                DispatchValue(dt);
                _value = dt;
                dateTime = dt;
            }

            if (dateTime != null && lastValue.HasValue && lastValue != dateTime)
            {
                Driver.Write(this, GroupAddress, ConvertToBus(dateTime));
            }

            return Task.CompletedTask;
        }
    }
}
