using System;
using System.Collections.Generic;
using System.Text;
using Automatica.Core.Driver;

namespace P3.Driver.MachineFlags
{
    public class MachineFlagsDriver : DriverBase
    {
        
        public MachineFlagsDriver(IDriverContext ctx)
            : base(ctx)
        {
        }
        

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            return new MachineFlags(ctx);
        }
    }
}
