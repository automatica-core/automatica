using System.Threading.Tasks;
using Automatica.Core.Driver;

namespace P3.Driver.ModBus.SolarmanV5.DriverFactory.Attributes
{
    internal class SolarmanStringAttribute : SolarmanAttrribute
    {
        public SolarmanStringAttribute(IDriverContext driverContext, SolarmanGroupAttribute parent) : base(driverContext, parent)
        {
        }

        public override Task<object> ConvertValue(ushort[] data)
        {
            return Task.FromResult((object)0);
        }
    }
}
