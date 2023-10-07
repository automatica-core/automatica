using System;
using Automatica.Core.Driver;

namespace P3.Driver.Synology.DriverFactory.Attributes
{
    internal class SynologyConnectedAttribute : DriverNoneAttributeBase
    {

        public SynologyConnectedAttribute(IDriverContext driverContext) : base(driverContext)
        {
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            throw new NotImplementedException();
        }
    }
}
