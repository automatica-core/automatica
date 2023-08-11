using System;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.Driver;
using P3.Driver.Knx.DriverFactory.Factories.IpTunneling;
using P3.Driver.Knx.DriverFactory.ThreeLevel;

namespace P3.Driver.Knx.DriverFactory.Attributes
{
    public class KnxDpt5Attribute : KnxGroupAddress
    {
        private int _value;

        public KnxDpt5Attribute(IDriverContext driverContext, KnxDriver knxDriver) : base(driverContext, knxDriver)
        {
        }
        protected override object ConvertToDptValue(object value)
        {
            var newValue = Convert.ToInt32(value);

            if (newValue != _value)
            {
                DispatchValue(newValue);
                _value = newValue;
                return _value;
            }

            return null;
        }
    }
}
