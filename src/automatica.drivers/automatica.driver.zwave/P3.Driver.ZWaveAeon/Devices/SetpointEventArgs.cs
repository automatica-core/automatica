using System;

namespace P3.Driver.ZWaveAeon.Devices
{
    public class SetpointEventArgs : EventArgs
    {
        public readonly Setpoint Setpoint;

        public SetpointEventArgs(Setpoint setpoint)
        {
            Setpoint = setpoint;
        }
    }
}
