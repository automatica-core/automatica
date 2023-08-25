using Automatica.Core.Driver;

namespace P3.Driver.Knx.DriverFactory.Factories.IpTunneling
{
    public class KnxGatewayState : DriverNoneAttributeBase
    {
        public KnxGatewayState(IDriverContext driverContext) : base(driverContext)
        {
        }

        public void SetGatewayState(bool state)
        {
            DispatchRead(state);
        }
        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            return null;
        }
    }
}
