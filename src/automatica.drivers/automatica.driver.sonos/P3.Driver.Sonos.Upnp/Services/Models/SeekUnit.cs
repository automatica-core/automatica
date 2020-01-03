using System;

namespace P3.Driver.Sonos.Upnp.Services.Models
{
    public class SeekUnit
    {
        public SeekUnitType Type { get; set; }

        public string Value { get; }

        public SeekUnit(SeekUnitType type)
        {
            Value = ConvertToValue(type);
            Type = type;
        }

        public SeekUnit(string value)
        {
            Type = ConvertToType(value);
            Value = value;
        }

        private static SeekUnitType ConvertToType(string value)
        {
            switch (value)
            {
                case "REL_TIME":
                    return SeekUnitType.Time;
                case "TRACK_NR":
                    return SeekUnitType.TrackNumber;
                default:
                    throw new ArgumentException($"Value unit: '{value}' is not valid.");
            }
        }

        private static string ConvertToValue(SeekUnitType type)
        {
            switch (type)
            {
                case SeekUnitType.Time:
                    return "REL_TIME";
                case SeekUnitType.TrackNumber:
                    return "TRACK_NR";
                default:
                    throw new ArgumentException($"{typeof(SeekUnitType).Name} type value: {type} is not valid.");
            }
        }

        public override string ToString()
        {
            return Value;
        }
    }
}
