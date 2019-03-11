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
    public class KnxDpt9Attribute : KnxGroupAddress
    {
        private double? _value;

        public KnxDpt9Attribute(IDriverContext driverContext, IKnxDriver knxDriver) : base(driverContext, knxDriver)
        {
        }

        protected override bool ValueRead(object value)
        {
            var newValue = Convert.ToDouble(value);
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            var ret = !_value.HasValue || newValue != _value.Value;

            _value = newValue;

            return ret;
        }

        public override Task WriteValue(IDispatchable source, object value)
        {
            var newValue = Convert.ToDouble(value);

            DispatchValue(newValue);
            _value = newValue;

            Driver.Write(GroupAddress, ConvertToBus(newValue));

            return Task.CompletedTask;
        }

        protected override string GetDptString(int dpt)
        {
            return PropertyHelper.GetNameAttributeFromEnumValue((Dpt9Type)dpt).EnumValue;
        }

        
    }
}
