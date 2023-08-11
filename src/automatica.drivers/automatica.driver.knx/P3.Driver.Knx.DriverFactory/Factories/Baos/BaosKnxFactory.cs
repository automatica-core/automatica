using Automatica.Core.Driver;
using System;

namespace P3.Driver.Knx.DriverFactory.Factories.Baos
{
    public class BaosKnxFactory : KnxFactory
    {
        public override string DriverName => "P3.Driver.Knx.Baos";
        public override Guid DriverGuid => KnxBaos;

        public override string ImageName => "plugin-p3.driver.knx";

        public override IDriver CreateDriver(IDriverContext config)
        {
            return new KnxBaosDriver(config);
        }
    }
}
