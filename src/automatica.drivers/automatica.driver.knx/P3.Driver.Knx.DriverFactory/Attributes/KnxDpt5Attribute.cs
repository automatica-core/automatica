using System;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.Base.Templates;
using Automatica.Core.Driver;
using P3.Driver.Knx.DriverFactory.ThreeLevel;
using P3.Knx.Core.Abstractions;
using P3.Knx.Core.Driver;

namespace P3.Driver.Knx.DriverFactory.Attributes
{
    public class KnxDpt5Attribute : KnxGroupAddress
    {
        private int _value;

        public KnxDpt5Attribute(IDriverContext driverContext, IKnxDriver knxDriver) : base(driverContext, knxDriver)
        {
        }

        public override Task WriteValue(IDispatchable source, object value)
        {
            var newValue = Convert.ToInt32(value);

            if (newValue != _value)
            {
                DispatchValue(newValue);
                _value = newValue;

                Driver.Write(GroupAddress, ConvertToBus(newValue));
            }

            return Task.CompletedTask;
        }

        protected override string GetDptString(int dpt)
        {
            return PropertyHelper.GetNameAttributeFromEnumValue((Dpt5Type)dpt).EnumValue;
        }
    }
}
