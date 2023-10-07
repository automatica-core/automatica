﻿using Automatica.Core.Driver;

namespace P3.Driver.HomeKitFactory.NodeInstances
{
    internal class PairingKeyNode : DriverNoneAttributeBase
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
