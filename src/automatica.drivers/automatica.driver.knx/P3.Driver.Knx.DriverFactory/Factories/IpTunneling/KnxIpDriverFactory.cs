using System;
using System.Runtime.CompilerServices;
using Automatica.Core.Driver;


[assembly: InternalsVisibleTo("P3.Driver.Knx.Tests")]


namespace P3.Driver.Knx.DriverFactory.Factories.IpTunneling
{
    public class KnxIpDriverFactory : KnxFactory
    {
        public override string DriverName => "P3.Driver.Knx";
        public override Guid DriverGuid => KnxGatway;

        public override string ImageName => "plugin-p3.driver.knx";

        public override IDriver CreateDriver(IDriverContext config)
        {
            return new KnxDriver(config, false);
        }
    }
}
