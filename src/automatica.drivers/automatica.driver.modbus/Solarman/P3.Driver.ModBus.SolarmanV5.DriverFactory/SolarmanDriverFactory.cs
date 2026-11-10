using System;

namespace P3.Driver.ModBus.SolarmanV5.DriverFactory
{
    public abstract class SolarmanDriverFactory : Automatica.Core.Driver.DriverFactory
    {
        public override string DriverName => "SolarmanV5";
        public override Guid DriverGuid => Guid.Parse("20e6210f-dbc8-4ea0-a788-a2bebb5d6add");
        // 0.1: time of use group (writable grid charge flags, slot SOC), signed defaults for the power/current registers
        // 0.1.0.1: only the device is a driver interface, so adding a group reloads the driver
        public override Version DriverVersion => new Version(0, 1, 0, 1);

        public override string ImageName => "p3.driver.modbus.solarman";

    }
}
