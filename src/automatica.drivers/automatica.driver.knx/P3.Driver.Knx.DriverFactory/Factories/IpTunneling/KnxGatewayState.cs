using Automatica.Core.Driver;

namespace P3.Driver.Knx.DriverFactory.Factories.IpTunneling
{
    public class KnxGatewayState : DriverBase
    {
        public KnxGatewayState(IDriverContext driverContext) : base(driverContext)
        {
        }

        public void SetGatewayState(bool state)
        {
            DriverContext.Dispatcher.DispatchValue(this, state);
        }
        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            return null;
        }
    }
}
