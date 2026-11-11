using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using Microsoft.Extensions.Logging;
using P3.Driver.EnOcean.DriverFactory.Driver.Learned;

namespace P3.Driver.EnOcean.DriverFactory.Driver
{
    public class EnOceanLearnedDevices : EnOceanBaseNode<IEnOceanBaseNode>
    {
        public EnOceanLearnedDevices(IDriverContext driverContext, ITeachInManager teachInManager) : base(driverContext, teachInManager)
        {
        }

        public override Task<bool> EnableLearnMode(CancellationToken token = default)
        {
            DriverContext.Logger.LogDebug("Enable learn mode...");
            return Parent.EnableLearnMode(token);
        }

        public override Task<bool> DisableLearnMode(CancellationToken token = default)
        {
            DriverContext.Logger.LogDebug("Disable learn mode...");
            return Parent.DisableLearnMode(token);
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            // learn mode adds the device directly below "Learned", without RORG and function nodes
            if (EnOceanTypeNode.IsTypeNode(ctx.NodeInstance))
            {
                return new EnOceanTypeNode(ctx, TeachInManager);
            }
            return new EnOceanRorgNode(ctx, TeachInManager);
        }
    }
}
