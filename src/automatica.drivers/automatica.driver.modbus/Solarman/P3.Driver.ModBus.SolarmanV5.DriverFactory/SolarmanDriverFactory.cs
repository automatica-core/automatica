using System;

namespace P3.Driver.ModBus.SolarmanV5.DriverFactory
{
    public abstract class SolarmanDriverFactory : Automatica.Core.Driver.DriverFactory
    {
        public override string DriverName => "SolarmanV5";
        public override Guid DriverGuid => Guid.Parse("20e6210f-dbc8-4ea0-a788-a2bebb5d6add");
        public override Version DriverVersion => new Version(0, 0, 0, 6);

        public override string ImageName => "p3.driver.modbus.solarman";

    }
}
