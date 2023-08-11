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

        protected override object ConvertToDptValue(object value)
        {
            if (value is int vlInt)
            {
                DispatchValue(vlInt);
                return vlInt;
            }

            return null;
        }
    }
}
