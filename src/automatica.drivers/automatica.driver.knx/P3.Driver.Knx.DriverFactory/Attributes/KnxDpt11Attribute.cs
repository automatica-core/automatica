using System;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.Base.Templates;
using Automatica.Core.Driver;
using P3.Driver.Knx.DriverFactory.ThreeLevel;
using P3.Knx.Core.Driver;

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
            if (value is DateTime dpt10Value)
            {
                var ret = !_value.HasValue || dpt10Value != _value.Value;

                _value = dpt10Value;

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
            DateTime? dateTime = null;

            if (value is DateTime dt)
            {
                DispatchValue(dt);
                _value = dt;
                dateTime = dt;
            }

            if (dateTime != null)
            {
                Tunneling.Write(GroupAddress, ConvertToBus(dateTime));
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
