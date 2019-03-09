using Automatica.Core.Base.Templates;
using Automatica.Core.Driver;
using P3.Driver.Knx.DriverFactory.ThreeLevel;
using P3.Knx.Core.Abstractions;
using P3.Knx.Core.Driver;

namespace P3.Driver.Knx.DriverFactory.Attributes
{
    public class KnxDpt4Attribute : KnxGroupAddress
    {
        public KnxDpt4Attribute(IDriverContext driverContext, IKnxDriver knxDriver) : base(driverContext, knxDriver)
        {
        }

        protected override string GetDptString(int dpt)
        {
            return PropertyHelper.GetNameAttributeFromEnumValue((DptType)dpt).EnumValue;
        }
    }
}
