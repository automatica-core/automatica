using Automatica.Core.Driver;

namespace P3.Driver.Constants
{
    public class ConstantsDriver : DriverBase
    { 
        public ConstantsDriver(IDriverContext ctx) : base(ctx)
        {
        }
        
        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            return new Constant(ctx);
        }
    }
}
