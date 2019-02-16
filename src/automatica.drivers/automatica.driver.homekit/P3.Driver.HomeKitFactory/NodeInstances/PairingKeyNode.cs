using System;
using System.Collections.Generic;
using System.Text;
using Automatica.Core.Driver;

namespace P3.Driver.HomeKitFactory.NodeInstances
{
    internal class PairingKeyNode : DriverBase
    {
        public PairingKeyNode(IDriverContext driverContext) : base(driverContext)
        {
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            return null;
        }
    }
}
