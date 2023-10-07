using Automatica.Core.Driver;

namespace P3.Driver.WakeOnLan
{
    public class WakeOnLanDriver : DriverNoneAttributeBase
    {
        
        public WakeOnLanDriver(IDriverContext ctx)
            : base(ctx)
        {
        }
        

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            return new WakeOnLan(ctx);
        }
    }
}
