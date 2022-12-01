using System.Threading.Tasks;
using Automatica.Core.Driver;
using P3.Driver.ModBusDriver.Master;

namespace P3.Driver.ModBus.SolarmanV5.DriverFactory.Attributes
{
    internal class SolarmanBooleanAttribute : SolarmanAttrribute
    {
        public SolarmanBooleanAttribute(IDriverContext driverContext, SolarmanGroupAttribute parent) : base(driverContext, parent)
        {
        }

        public override async Task<object> ConvertValue(ModBusRegisterValueReturn modbusReturn)
        {
            await Task.CompletedTask;

            return modbusReturn.Data[0] == 1;
        }
    }
}
