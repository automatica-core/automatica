using System;
using System.Collections.Generic;
using System.Text;
using Automatica.Core.Driver;

namespace P3.Driver.WakeOnLan
{
    public class WakeOnLanDriver : DriverBase
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
