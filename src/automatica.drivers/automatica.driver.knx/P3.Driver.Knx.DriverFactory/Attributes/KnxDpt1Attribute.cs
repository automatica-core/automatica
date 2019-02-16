using System;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.Base.Templates;
using Automatica.Core.Driver;
using P3.Driver.Knx.DriverFactory.ThreeLevel;
using P3.Knx.Core.Driver;

namespace P3.Driver.Knx.DriverFactory.Attributes
{
    public class KnxDpt1Attribute : KnxGroupAddress
    {
        private bool? _value;

        public KnxDpt1Attribute(IDriverContext driverContext, KnxDriver knxDriver) : base(driverContext, knxDriver)
        {
        }

        public override Task WriteValue(IDispatchable source, object value)
        {
            var newValue = Convert.ToBoolean(value);

            if (newValue != _value)
            {
                DispatchValue(newValue);
                _value = newValue;

                Tunneling.Write(GroupAddress, ConvertToBus(newValue));
            }


            return Task.CompletedTask;
        }

        protected override bool ValueRead(object value)
        {
            var newValue = Convert.ToBoolean(value);
            var ret = newValue != _value;

            _value = newValue;

            return ret;
        }

        protected override string GetDptString(int dpt)
        {
            return PropertyHelper.GetNameAttributeFromEnumValue((DptType)dpt).EnumValue;
        }
    }
}
