using System;
using System.Globalization;
using Automatica.Core.Driver;
using Knx.Falcon.ApplicationData;
using Microsoft.Extensions.Logging;
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

        public override int ImplementationDptType => (int)P3.Knx.Core.Driver.DptType.Dpt10;

        protected override bool ValueRead(object value)
        {
            if (value is KnxTime dpt10Value)
            {
                var timeOfDay = new TimeSpan(dpt10Value.TimeOfDay.Hours, dpt10Value.TimeOfDay.Minutes, dpt10Value.TimeOfDay.Seconds);
                var ret = !_value.HasValue || timeOfDay != _value.Value;

                _value = timeOfDay;

                return false;

            }
            return false;
        }

        protected override object ConvertToDptValue(object value)
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
                            _value = timeOfDay;
                        }

                        break;
                    }
                    case TimeOnly ts:
                    {
                        var timeOfDay = new TimeSpan(ts.Hour, ts.Minute, ts.Second);

                        if (timeOfDay != _value)
                        {

                            dpt10Value = new KnxTime(timeOfDay, 0);
                            _value = timeOfDay;
                        }

                        break;
                    }
                    case string str:
                        if (TimeOnly.TryParse(str, CultureInfo.InvariantCulture, out var timeOnly))
                        {
                            var parsedTimeSpan = new TimeSpan(timeOnly.Hour, timeOnly.Minute, timeOnly.Second);
                            if (parsedTimeSpan != _value)
                            {

                                dpt10Value = new KnxTime(parsedTimeSpan, 0);
                                _value = parsedTimeSpan;
                            }
                        }
                        else
                        {
                            DriverContext.Logger.LogError($"Could not parse time {str}");
                        }
                        break;
                    default:
                        throw new NotImplementedException();
                }


                return dpt10Value;
            }
        }
    }
}
