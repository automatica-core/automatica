using System;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.Driver;
using Knx.Falcon.ApplicationData;
using P3.Driver.Knx.DriverFactory.Factories.IpTunneling;
using P3.Driver.Knx.DriverFactory.ThreeLevel;

namespace P3.Driver.Knx.DriverFactory.Attributes
{
    public class KnxDpt10Attribute : KnxGroupAddress
    {
        private TimeSpan? _value;
        private readonly object _lock = new object();

        public KnxDpt10Attribute(IDriverContext driverContext, KnxDriver knxDriver) : base(driverContext, knxDriver)
        {
        }

        protected override bool ValueRead(object value)
        {
            if (value is KnxTime dpt10Value)
            {
                var timeOfDay = new TimeSpan(dpt10Value.TimeOfDay.Hours, dpt10Value.TimeOfDay.Minutes, dpt10Value.TimeOfDay.Seconds);
                var ret = !_value.HasValue || timeOfDay != _value.Value;

                _value = timeOfDay;

                if (ret)
                {
                    DispatchValue(_value);
                }
                return false;

            }
            return false;
        }

        public override Task WriteValue(IDispatchable source, object value)
        {
            lock (_lock)
            {
                KnxTime dpt10Value = null;

                switch (value)
                {
                    case DateTime dt:
                    {
                        var timeOfDay = new TimeSpan(dt.TimeOfDay.Hours, dt.TimeOfDay.Minutes, dt.TimeOfDay.Seconds);

                        if (timeOfDay != _value)
                        {
                            dpt10Value = new KnxTime(timeOfDay);
                            DispatchValue(timeOfDay);
                            _value = timeOfDay;
                        }

                        break;
                    }
                    case TimeSpan ts:
                    {
                        var timeOfDay = new TimeSpan(ts.Hours, ts.Minutes, ts.Seconds);

                        if (timeOfDay != _value)
                        {

                            dpt10Value = new KnxTime(timeOfDay, 0);
                            DispatchValue(timeOfDay);
                            _value = timeOfDay;
                        }

                        break;
                    }
                }


                if (dpt10Value != null)
                {
                    Driver.Write(this, GroupAddress, ConvertToBus(dpt10Value));
                }
            }
            return Task.CompletedTask;
        }
    }
}
