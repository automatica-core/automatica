using System.Collections.Generic;

namespace P3.Driver.ModBus.SolarmanV5.DriverFactory.Devices.SG04LP3
{
    internal class SG043LP3DeviceGroupMap : DeviceGroupMap
    {
        public SG043LP3DeviceGroupMap()
        {
            GroupRead.Add((0x0003, 0x0059));
            GroupRead.Add((0x0202, 0x022E));
            GroupRead.Add((0x024A, 0x024F));
            GroupRead.Add((0x0256, 0x027C));
            GroupRead.Add((0x0284, 0x028D));
            GroupRead.Add((0x02A0, 0x02A7));
        }
    }
}
