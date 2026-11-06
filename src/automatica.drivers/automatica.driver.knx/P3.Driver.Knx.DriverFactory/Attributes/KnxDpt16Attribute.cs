using System;
using Automatica.Core.Driver;
using P3.Driver.Knx.DriverFactory.Factories.IpTunneling;
using P3.Driver.Knx.DriverFactory.ThreeLevel;

namespace P3.Driver.Knx.DriverFactory.Attributes
{
    public class KnxDpt16Attribute : KnxGroupAddress
    {

        public override int ImplementationDptType => (int)P3.Knx.Core.Driver.DptType.Dpt16;

        /// <summary>
        /// The sub type selection stores 16.000 as 1 and 16.001 as 2 (existing configurations depend on it), Falcon numbers them 0 and 1.
        /// Nodes without a selection fall back to <see cref="KnxGroupAddress.DefaultDptSubType"/> (1), which is 16.000 here as well.
        /// </summary>
        protected internal override int CodecSubType => DptSubType == (int)P3.Knx.Core.Driver.Dpt16Type.Dpt16_001 ? 1 : 0;

        public KnxDpt16Attribute(IDriverContext driverContext, KnxDriver knxDriver) : base(driverContext, knxDriver)
        {
        }

        protected override object ConvertToDptValue(object value)
        {
            if (value is string dpt16Value)
            {
                return dpt16Value;
            }

            throw new NotImplementedException();
        }

        protected override bool ValueRead(object value)
        {
            if (value is string dpt16Value)
            {
                DispatchRead(dpt16Value);
                return false;
            }

            return false;
        }
    }
}
