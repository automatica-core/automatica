using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using Microsoft.Extensions.Logging;
using P3.Driver.EnOcean.DriverFactory.Driver.Learned;

namespace P3.Driver.EnOcean.DriverFactory.Driver
{
    public class EnOceanLearnedDevices : EnOceanBaseNode<EnOceanRorgNode>
    {
        public EnOceanLearnedDevices(IDriverContext driverContext, ITeachInManager teachInManager) : base(driverContext, teachInManager)
        {
        }

        public override Task<bool> EnableLearnMode()
        {
            DriverContext.Logger.LogDebug("Enable learn mode...");
            return Parent.EnableLearnMode();
        }

        public override Task<bool> DisableLearnMode()
        {
            DriverContext.Logger.LogDebug("Disable learn mode...");
            return Parent.DisableLearnMode();
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            return new EnOceanRorgNode(ctx, TeachInManager);
        }
    }
}
