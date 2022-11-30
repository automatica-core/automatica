using System.Collections.Generic;

namespace P3.Driver.ModBus.SolarmanV5.DriverFactory.Devices
{
    internal enum DeviceType
    {
        SG03LP3 = 0
    }
    internal abstract class DeviceMap
    {
        public DeviceType DeviceType { get; }

        public Dictionary<string, List<int>> NameRegisterMap { get; set; } = new();
        public Dictionary<string, List<int>> NameInputRegisterMap { get; set; } = new();

        internal DeviceMap(DeviceType deviceType)
        {
            DeviceType = deviceType;
        }

        public bool ContainsKey(string key)
        {
            return NameInputRegisterMap.ContainsKey(key) || NameRegisterMap.ContainsKey(key);
        }

        public List<int> this[string key]
        {
            get
            {
                if (NameRegisterMap.ContainsKey(key))
                {
                    return NameRegisterMap[key];
                }

                return NameInputRegisterMap[key];
            }
        }
    }
}
