using System.Threading.Tasks;
using Automatica.Core.Driver;

namespace P3.Driver.ModBus.SolarmanV5.DriverFactory.Attributes
{
    internal class SolarmanBooleanAttribute : SolarmanAttrribute
    {
        public SolarmanBooleanAttribute(IDriverContext driverContext, SolarmanGroupAttribute parent) : base(driverContext, parent)
        {
        }

        public override async Task<object> ConvertValue(ushort[] data)
        {
            await Task.CompletedTask;

            return data[0] == 1;
        }
    }
}
