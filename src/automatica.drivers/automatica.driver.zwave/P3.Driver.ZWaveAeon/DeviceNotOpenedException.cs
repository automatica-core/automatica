using System;

namespace P3.Driver.ZWaveAeon
{
    public class DeviceNotOpenedException : Exception
    {
        public DeviceNotOpenedException() : base($"Device is not opened, you need to call {nameof(ZWaveController.Open)} first")
        {
        }
    }
}
