using Automatica.Core.Driver;
using P3.Driver.HomeKit.Hap.Model;

namespace P3.Driver.HomeKitFactory.NodeInstances
{
    public class AttributeNodeInstance : DriverNoneAttributeBase
    {
        private readonly Characteristic _characteristic;

        public AttributeNodeInstance(IDriverContext driverContext, Characteristic characteristic) : base(driverContext)
        {
            _characteristic = characteristic;
        }

        public override IDriverNode CreateDriverNode(IDriverContext ctx)
        {
            return null; //nothing more to come
        }
    }
}
