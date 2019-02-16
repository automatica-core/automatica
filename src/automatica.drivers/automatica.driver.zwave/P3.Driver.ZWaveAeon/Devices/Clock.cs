using System;

namespace P3.Driver.ZWaveAeon.Devices
{
    public class Clock
    {
        public readonly DayOfWeek DayOfWeek;
        public readonly byte Hour;
        public readonly byte Minute;

        public Clock(DayOfWeek dayOfWeek, byte hour, byte minute)
        {
            DayOfWeek = dayOfWeek;
            Hour = hour;
            Minute = minute;
        }

        public Clock(DateTime date)
            : this(date.DayOfWeek, (byte)date.Hour, (byte)date.Minute)
        {
        }


        public override string ToString()
        {
            return $"{DayOfWeek} {Hour:D2}:{Minute:D2}";
        }

    }
}
