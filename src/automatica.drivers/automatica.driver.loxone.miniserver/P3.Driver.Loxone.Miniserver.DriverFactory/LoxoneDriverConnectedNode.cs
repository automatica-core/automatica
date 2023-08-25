using Automatica.Core.Driver;

namespace P3.Driver.Loxone.Miniserver.DriverFactory
{
    public class LoxoneDriverConnectedNode : DriverNoneAttributeBase
    {
        public LoxoneDriverConnectedNode(IDriverContext driverContext) : base(driverContext)
        {

        }

        public void StateChanged(bool value)
        {
            DispatchRead(value);
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            return null;
        }
    }
}
