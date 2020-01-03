using System;

namespace P3.Driver.Sonos.Models
{
    public class SonosTrackNumber
    {
        private const int FirstTrackNumber = 1;

        public SonosTrackNumber() : this(FirstTrackNumber)
        {
        }

        public SonosTrackNumber(int value)
        {
            if (value < FirstTrackNumber)
            {
                throw new ArgumentOutOfRangeException(nameof(value), $"Track number must be {FirstTrackNumber} or greater.");
            }

            Value = value;
        }

        public int Value { get; }

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}
