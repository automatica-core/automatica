using Automatica.Core.Driver;

namespace P3.Driver.FroniusSolarFactory
{
    public class FroniusSolarDriver : DriverNoneAttributeBase
    {
       
        public FroniusSolarDriver(IDriverContext driverContext) : base(driverContext)
        {
        }


        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            return new FroniusDeviceAttribute(ctx);
        }
    }
}
