using System;

namespace P3.Driver.Sonos.Models
{
    public class SonosVolume
    {
        public static readonly int MinVolume = 0;
        public static readonly int MaxVolume = 100;

        public SonosVolume() : this(MinVolume)
        {
        }

        public SonosVolume(int value)
        {
            if (value < 0 || value > 100)
            {
                throw new ArgumentOutOfRangeException(nameof(value), $"Volume must be between {MinVolume} and {MaxVolume}.");
            }

            Value = value;
        }

        public int Value { get; private set; }

        public void Increase(int amout)
        {
            if(amout < 0)
                throw new ArgumentOutOfRangeException(nameof(amout), "Increase amount cannot be less than zero.");

            if (Value + amout > MaxVolume)
                Value = MaxVolume;
            else
                Value = Value + amout;
        }

        public void Decrease(int amout)
        {
            if (amout < 0)
                throw new ArgumentOutOfRangeException(nameof(amout), "Decrease amount cannot be less than zero.");

            if (amout > Value)
                Value = MinVolume;
            else
                Value = Value - amout;
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}
