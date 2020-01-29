using System;

namespace P3.Driver.Sonos.Upnp.Services.Models
{
    public class PlayMode
    {
        public PlayModeType Type { get; set; }

        public string Value { get; }

        public PlayMode() : this(PlayModeType.Normal)
        {
        }

        public PlayMode(PlayModeType type)
        {
            Value = ConvertToValue(type);
            Type = type;
        }

        public PlayMode(string value)
        {
            Type = ConvertToType(value);
            Value = value;
        }

        private static PlayModeType ConvertToType(string value)
        {
            switch (value)
            {
                case "NORMAL":
                    return PlayModeType.Normal;
                case "REPEAT_ALL":
                    return PlayModeType.RepeatAll;
                case "SHUFFLE":
                    return PlayModeType.Shuffle;
                case "SHUFFLE_NOREPEAT":
                    return PlayModeType.ShuffleNoRepeat;
                default:
                    throw new ArgumentException($"Value unit: '{value}' is not valid.");
            }
        }

        private static string ConvertToValue(PlayModeType type)
        {
            switch (type)
            {
                case PlayModeType.Normal:
                    return "NORMAL";
                case PlayModeType.RepeatAll:
                    return "REPEAT_ALL";
                case PlayModeType.Shuffle:
                    return "SHUFFLE";
                case PlayModeType.ShuffleNoRepeat:
                    return "SHUFFLE_NOREPEAT";
                default:
                    throw new ArgumentException($"{typeof(PlayModeType).Name} type value: {type} is not valid.");
            }
        }

        public override string ToString()
        {
            return Value;
        }
    }

    public enum PlayModeType
    {
        Normal,
        Shuffle,
        ShuffleNoRepeat,
        RepeatAll
    }
}
