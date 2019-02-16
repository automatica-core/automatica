using Automatica.Core.Driver;

namespace P3.Driver.Loxone.Miniserver.DriverFactory
{
    public class LoxoneDriverConnectedNode : DriverBase
    {
        public LoxoneDriverConnectedNode(IDriverContext driverContext) : base(driverContext)
        {

        }

        public void StateChanged(bool value)
        {
            DispatchValue(value);
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            return null;
        }
    }
}
