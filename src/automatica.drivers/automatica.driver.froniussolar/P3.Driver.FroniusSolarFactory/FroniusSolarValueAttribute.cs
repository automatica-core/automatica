using Automatica.Core.Driver;

namespace P3.Driver.FroniusSolarFactory
{
    internal class FroniusSolarValueAttribute : DriverNoneAttributeBase
    {
        public string Key => DriverContext.NodeInstance.This2NodeTemplateNavigation.Key;

        public FroniusSolarValueAttribute(IDriverContext driverContext) : base(driverContext)
        {
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            return null;
        }
    }
}
