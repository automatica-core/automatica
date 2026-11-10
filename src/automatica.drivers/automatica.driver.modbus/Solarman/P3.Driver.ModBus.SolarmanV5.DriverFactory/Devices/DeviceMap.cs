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

        /// <summary>
        /// Keys of registers that hold two's complement values (rule 2/4 in the ha-solarman register maps).
        /// </summary>
        public HashSet<string> SignedKeys { get; } = new();

        /// <summary>
        /// Keys of the writable time of use grid charge flags (one register per slot, or all slots).
        /// </summary>
        public HashSet<string> GridChargeKeys { get; } = new();

        /// <summary>
        /// The only registers the driver may ever write to.
        /// </summary>
        public HashSet<int> WritableRegisters { get; } = new();

        /// <summary>
        /// First register and number of the time of use charge registers (one per slot).
        /// </summary>
        public (int start, int count) TimeOfUseChargeRegisters { get; protected set; }

        public bool IsSigned(string key) => SignedKeys.Contains(key);

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
