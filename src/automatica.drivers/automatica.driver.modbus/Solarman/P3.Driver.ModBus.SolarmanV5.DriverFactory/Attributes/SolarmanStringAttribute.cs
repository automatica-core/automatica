using System;
using System.Threading.Tasks;
using Automatica.Core.Driver;
using P3.Driver.ModBusDriver.Master;

namespace P3.Driver.ModBus.SolarmanV5.DriverFactory.Attributes
{
    internal class SolarmanStringAttribute : SolarmanAttrribute
    {
        public SolarmanStringAttribute(IDriverContext driverContext, SolarmanDriver parent) : base(driverContext, parent)
        {
        }

        public override Task<object> ConvertValue(ModBusRegisterValueReturn modbusReturn)
        {
            return Task.FromResult((object)0);
        }
    }
}
