using System;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.Base.Templates;
using Automatica.Core.Driver;
using P3.Driver.Knx.DriverFactory.ThreeLevel;
using P3.Knx.Core.Abstractions;

namespace P3.Driver.Knx.DriverFactory.Attributes
{
    public class KnxDpt16Attribute : KnxGroupAddress
    {
        private string _value;

        public KnxDpt16Attribute(IDriverContext driverContext, IKnxDriver knxDriver) : base(driverContext, knxDriver)
        {
        }

        protected override bool ValueRead(object value)
        {
            if (value is string dpt16Value)
            {
                DispatchValue(dpt16Value);

                return false;
            }

            return false;
        }

        public override Task WriteValue(IDispatchable source, object value)
        {
            string writeValue = null;

            if (value is string str)
            {
                DispatchValue(str);
                _value = str;
                writeValue = str;
            }

            if (writeValue != null)
            {
                Driver.Write(GroupAddress, ConvertToBus(writeValue));
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
