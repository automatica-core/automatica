using Automatica.Core.Driver;
using P3.Driver.EnOcean.DriverFactory.Driver.Learned;

namespace P3.Driver.EnOcean.DriverFactory.Driver
{
    public class EnOceanFunctionNode : EnOceanBaseNode<EnOceanTypeNode>
    {
        public EnOceanFunctionNode(IDriverContext driverContext, ITeachInManager teachInManager) : base(driverContext, teachInManager)
        {
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            return new EnOceanTypeNode(ctx, TeachInManager);
        }
    }
}
