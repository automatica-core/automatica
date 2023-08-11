using System;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.Driver;
using P3.Driver.Knx.DriverFactory.Factories.IpTunneling;
using P3.Driver.Knx.DriverFactory.ThreeLevel;

namespace P3.Driver.Knx.DriverFactory.Attributes
{
    public class KnxDpt1Attribute : KnxGroupAddress
    {
        private bool? _value;

        public override int SizeInBits => 1;

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

                Driver.Write(this, GroupAddress, ConvertToBus(newValue));
                //Tunneling.Write(GroupAddress, ConvertToBus(newValue));
            }


            return Task.CompletedTask;
        }

        protected override bool ValueRead(object value)
        {
            return true; //always dispatch dpt1 values...
        }
    }
}
