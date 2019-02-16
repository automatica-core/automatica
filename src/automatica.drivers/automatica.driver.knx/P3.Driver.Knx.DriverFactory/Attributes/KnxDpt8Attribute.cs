using Automatica.Core.Base.Templates;
using Automatica.Core.Driver;
using P3.Driver.Knx.DriverFactory.ThreeLevel;
using P3.Knx.Core.Driver;

namespace P3.Driver.Knx.DriverFactory.Attributes
{
    public class KnxDpt8Attribute : KnxGroupAddress
    {
        public KnxDpt8Attribute(IDriverContext driverContext, KnxDriver knxDriver) : base(driverContext, knxDriver)
        {
        }

        protected override string GetDptString(int dpt)
        {
            return PropertyHelper.GetNameAttributeFromEnumValue((Dpt8Type)dpt).EnumValue;
        }
    }
}
