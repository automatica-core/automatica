using Automatica.Core.Driver;
using P3.Driver.Knx.DriverFactory.Factories.IpTunneling;
using System;
using System.Collections.Generic;
using System.Text;

namespace P3.Driver.Knx.DriverFactory.Factories.Baos
{
    public class BaosKnxFactory : KnxFactory
    {
        public override string DriverName => "P3.Driver.Knx.Baos";
        public override Guid DriverGuid => new Guid("3bef31e4-11f2-42dc-8dd8-819cb190c4f5");

        public override IDriver CreateDriver(IDriverContext config)
        {
            return new KnxDriver(config, false);
        }
    }
}
