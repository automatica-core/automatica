using Automatica.Core.Driver;
using P3.Driver.EnOcean.DriverFactory.Driver.Learned;

namespace P3.Driver.EnOcean.DriverFactory.Driver
{
    public class EnOceanRorgNode : EnOceanBaseNode<IEnOceanBaseNode>
    {
        public EnOceanRorgNode(IDriverContext driverContext, ITeachInManager teachInManager) : base(driverContext, teachInManager)
        {
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            if (EnOceanTypeNode.IsTypeNode(ctx.NodeInstance))
            {
                return new EnOceanTypeNode(ctx, TeachInManager);
            }
            return new EnOceanFunctionNode(ctx, TeachInManager);
        }
    }
}
