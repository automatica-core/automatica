using System;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.Base.Templates;
using Automatica.Core.Driver;
using P3.Driver.Knx.DriverFactory.ThreeLevel;
using P3.Knx.Core.Abstractions;
using P3.Knx.Core.Driver.DPT;

namespace P3.Driver.Knx.DriverFactory.Attributes
{
    public class KnxDpt10Attribute : KnxGroupAddress
    {
        private TimeOnly? _value;
        private readonly object _lock = new object();

        public KnxDpt10Attribute(IDriverContext driverContext, IKnxDriver knxDriver) : base(driverContext, knxDriver)
        {
        }

        protected override bool ValueRead(object value)
        {
            if (value is Dpt10Value dpt10Value)
            {
                var timeOfDay = new TimeOnly(dpt10Value.TimeOfDay.Hour, dpt10Value.TimeOfDay.Minute, dpt10Value.TimeOfDay.Second);
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
                Dpt10Value dpt10Value = null;

                switch (value)
                {
                    case DateTime dt:
                    {
                        var timeOfDay = new TimeOnly(dt.TimeOfDay.Hours, dt.TimeOfDay.Minutes, dt.TimeOfDay.Seconds);

                        if (timeOfDay != _value)
                        {
                            dpt10Value = new Dpt10Value(dt);
                            DispatchValue(timeOfDay);
                            _value = timeOfDay;
                        }

                        break;
                    }
                    case TimeSpan ts:
                    {
                        var timeOfDay = new TimeOnly(ts.Hours, ts.Minutes, ts.Seconds);

                        if (timeOfDay != _value)
                        {

                            dpt10Value = new Dpt10Value(timeOfDay, 0);
                            DispatchValue(timeOfDay);
                            _value = timeOfDay;
                        }

                        break;
                    }
                }


                if (dpt10Value != null)
                {
                    Driver.Write(GroupAddress, ConvertToBus(dpt10Value));
                }
            }
            return Task.CompletedTask;
        }
        

        protected override string GetDptString(int dpt)
        {
            var dpt10 = P3.Knx.Core.Driver.DptType.Dpt10;
            return PropertyHelper.GetNameAttributeFromEnumValue(dpt10).EnumValue;
        }

        
    }
}
