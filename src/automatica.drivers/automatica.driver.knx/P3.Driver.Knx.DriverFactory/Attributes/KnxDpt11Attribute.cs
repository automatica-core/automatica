using System;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.Base.Templates;
using Automatica.Core.Driver;
using P3.Driver.Knx.DriverFactory.ThreeLevel;
using P3.Knx.Core.Abstractions;

namespace P3.Driver.Knx.DriverFactory.Attributes
{
    public class KnxDpt11Attribute : KnxGroupAddress
    {
        private DateOnly? _value;

        public KnxDpt11Attribute(IDriverContext driverContext, IKnxDriver knxDriver) : base(driverContext, knxDriver)
        {
        }

        protected override bool ValueRead(object value)
        {
            if (value is DateOnly dpt11Value)
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

        public override Task WriteValue(IDispatchable source, object value)
        {
            DateOnly? dateTime = null;
            var lastValue = _value;

            if (value is DateOnly dt)
            {
                DispatchValue(dt);
                _value = dt;
                dateTime = dt;
            }

            if (dateTime != null && lastValue.HasValue && lastValue != dateTime)
            {
                Driver.Write(GroupAddress, ConvertToBus(dateTime));
            }

            return Task.CompletedTask;
        }

        protected override string GetDptString(int dpt)
        {
            var dpt11 = P3.Knx.Core.Driver.DptType.Dpt11;
            return PropertyHelper.GetNameAttributeFromEnumValue(dpt11).EnumValue;
        }

        
    }
}
