using System.Collections.Generic;

namespace P3.Driver.ModBus.SolarmanV5.DriverFactory.Devices
{
    internal class DeviceGroupMap
    {
        internal List<(int start, int end)> GroupRead { get; } = new List<(int start, int end)>();
    }
}
