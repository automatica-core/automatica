using System;

namespace P3.Driver.Sonos.Upnp.Services.Models
{
    public class TransportState
    {
        public TransportStateType Type { get; set; }

        public string Value { get; }

        public TransportState(TransportStateType type)
        {
            Value = ConvertToValue(type);
            Type = type;
        }

        public TransportState(string value)
        {
            Type = ConvertToType(value);
            Value = value;
        }

        public bool IsPlaying => Type == TransportStateType.Playing;

        private static TransportStateType ConvertToType(string value)
        {
            switch (value)
            {
                case "STOPPED":
                    return TransportStateType.Stopped;
                case "PLAYING":
                    return TransportStateType.Playing;
                case "PAUSED_PLAYING":
                case "PAUSED_PLAYBACK":
                    return TransportStateType.Paused;
                case "TRANSITIONING"
                    return TransportStateType.Transitioning;
                default:
                    throw new ArgumentException($"Value unit: '{value}' is not valid.");
            }
        }

        private static string ConvertToValue(TransportStateType type)
        {
            switch (type)
            {
                case TransportStateType.Stopped:
                    return "STOPPED";
                case TransportStateType.Playing:
                    return "PLAYING";
                case TransportStateType.Paused:
                    return "PAUSED_PLAYBACK";
                default:
                    throw new ArgumentException($"{typeof(TransportStateType).Name} type value: {type} is not valid.");
            }
        }

        public override string ToString()
        {
            return Value;
        }
    }

    public enum TransportStateType
    {
        Stopped,
        Playing,
        Paused,
        Transitioning
    }
}