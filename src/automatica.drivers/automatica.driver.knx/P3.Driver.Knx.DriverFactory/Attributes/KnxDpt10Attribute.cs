﻿using System;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.Base.Templates;
using Automatica.Core.Driver;
using P3.Driver.Knx.DriverFactory.ThreeLevel;
using P3.Knx.Core.DPT;
using P3.Knx.Core.Driver;

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
            if (value is Dpt10Value dpt10Value)
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
                Dpt10Value dpt10Value = null;

                switch (value)
                {
                    case DateTime dt:
                    {
                        var timeOfDay = new TimeSpan(dt.TimeOfDay.Hours, dt.TimeOfDay.Minutes, dt.TimeOfDay.Seconds);

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
                        var timeOfDay = new TimeSpan(ts.Hours, ts.Minutes, ts.Seconds);

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
                    Tunneling.Write(GroupAddress, ConvertToBus(dpt10Value));
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
