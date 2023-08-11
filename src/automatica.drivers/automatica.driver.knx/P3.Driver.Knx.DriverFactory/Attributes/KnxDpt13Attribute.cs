using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.Driver;
using P3.Driver.Knx.DriverFactory.Factories.IpTunneling;
using P3.Driver.Knx.DriverFactory.ThreeLevel;

namespace P3.Driver.Knx.DriverFactory.Attributes
{
    public class KnxDpt13Attribute : KnxGroupAddress
    {
        private int? _value;

        public KnxDpt13Attribute(IDriverContext driverContext, KnxDriver knxDriver) : base(driverContext, knxDriver)
        {
        }

        protected override bool ValueRead(object value)
        {
            if (value is int dpt13Value)
            {
                var ret = !_value.HasValue || dpt13Value != _value.Value;

                _value = dpt13Value;

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
            int? intValue = null;

            if (value is int dt)
            {
                DispatchValue(dt);
                _value = dt;
                intValue = dt;
            }

            if (intValue != null)
            {
                Driver.Write(this, GroupAddress, ConvertToBus(intValue));
            }

            return Task.CompletedTask;
        }
    }
}
