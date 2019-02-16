using System;
using System.Collections.Generic;
using System.Text;
using P3.Knx.Core.DPT.Base;

namespace P3.Knx.Core.DPT
{
    public class Dpt10Value
    {
        public TimeSpan TimeOfDay { get; }
        public int Weekday { get; }

        public Dpt10Value(TimeSpan timeOfDay, int weekday)
        {
            TimeOfDay = timeOfDay;
            Weekday = weekday;
        }

        public Dpt10Value(DateTime dt)
        {
            TimeOfDay = dt.TimeOfDay;

            switch (dt.DayOfWeek)
            {
                case DayOfWeek.Friday:
                    Weekday = 5;
                    break;
                case DayOfWeek.Monday:
                    Weekday = 1;
                    break;
                case DayOfWeek.Saturday:
                    Weekday = 6;
                    break;
                case DayOfWeek.Sunday:
                    Weekday = 7;
                    break;
                case DayOfWeek.Thursday:
                    Weekday = 4;
                    break;
                case DayOfWeek.Tuesday:
                    Weekday = 2;
                    break;
                case DayOfWeek.Wednesday:
                    Weekday = 3;
                    break;
            }
        }

        public byte[] ToByteArray()
        {
            var data = new byte[4];

            var b0 = (byte)(Weekday << 5);
            var hour = (byte)TimeOfDay.Hours;
            b0 += hour;

            data[1] = b0;
            data[2] = (byte) TimeOfDay.Minutes;
            data[3] = (byte) TimeOfDay.Seconds;
            
            return data;
        }
    }
    internal class Dpt10Translator : Dpt
    {
        public override string[] Ids => new[] {"10.001"};
        public override object FromDataPoint(byte[] data)
        {
            if (data == null || data.Length != 3)
            {
                throw new FromDataPointException("data is invalid");
            }

            var seconds = data[2];
            var minutes = data[1];
            var hour = data[0] & 0x1F;
            var day = ((data[0] & 0xE0) >> 5);

            var dt = new TimeSpan(hour, minutes, seconds);

            return new Dpt10Value(dt, day);
        }

        public override byte[] ToDataPoint(object value)
        {
            if (value is Dpt10Value dpt10Value)
            {
                return dpt10Value.ToByteArray();
            }
            throw new ToDataPointException($"{nameof(value)} must be of type {nameof(Dpt10Value)}");
        }
    }
}
