using System;
using System.Collections.Generic;
using System.Text;
using Automatica.Core.Driver;

namespace P3.Driver.Knx.DriverFactory.ThreeLevel
{
    public class KnxMainGroup : KnxLevelBase
    {
        public KnxMainGroup(IDriverContext driverContext, KnxDriver knxDriver) : base(driverContext, knxDriver)
        {
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            var m = new KnxMiddleGroup(ctx, Driver);

            AddChild(m);
            return m;
        }
    }
}
