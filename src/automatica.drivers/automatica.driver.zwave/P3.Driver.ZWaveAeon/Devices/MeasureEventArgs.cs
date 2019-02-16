using System;

namespace P3.Driver.ZWaveAeon.Devices
{
    public class MeasureEventArgs : EventArgs
    {
        public readonly Measure Meassure;

        public MeasureEventArgs(Measure meassure)
        {
            Meassure = meassure;
        }
    }
}
