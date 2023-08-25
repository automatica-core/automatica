using System;
using Automatica.Core.Driver;

namespace P3.Driver.VkingBms.DriverFactory.Nodes
{
    internal class VkingBatteryValueNode : DriverNoneAttributeBase
    {
       

        public VkingBatteryValueNode(IDriverContext driverContext) : base(driverContext)
        {
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            throw new NotImplementedException();
        }
    }
}
