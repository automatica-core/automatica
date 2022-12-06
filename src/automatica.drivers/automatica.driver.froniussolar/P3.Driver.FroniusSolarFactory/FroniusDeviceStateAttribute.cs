using Automatica.Core.Driver;

namespace P3.Driver.FroniusSolarFactory
{
    internal class FroniusDeviceStateAttribute : DriverBase
    {
        public FroniusDeviceStateAttribute(IDriverContext driverContext) : base(driverContext)
        {
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            return null;
        }
    }
}
