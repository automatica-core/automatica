using System;
using Automatica.Core.Driver;
using P3.Driver.Knx.DriverFactory.Factories.IpTunneling;
using P3.Driver.Knx.DriverFactory.ThreeLevel;

namespace P3.Driver.Knx.DriverFactory.Attributes
{
    public class KnxDpt5Attribute : KnxGroupAddress
    {
        private int _value;

        public override int ImplementationDptType => (int)P3.Knx.Core.Driver.DptType.Dpt5;
        public KnxDpt5Attribute(IDriverContext driverContext, KnxDriver knxDriver) : base(driverContext, knxDriver)
        {
        }
        protected override object ConvertToDptValue(object value, IWriteContext writeContext)
        {
            var newValue = Convert.ToInt32(value);
            _value = newValue;
            if (writeContext.WriteOnlyIfChanged)
            {
                if (newValue != _value)
                {
                    return _value;
                }

                return null;
            }

            return _value;
        }
    }
}
