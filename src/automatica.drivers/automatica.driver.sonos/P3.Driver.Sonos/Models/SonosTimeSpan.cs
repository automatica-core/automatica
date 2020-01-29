using System;

namespace P3.Driver.Sonos.Models
{
    public class SonosTimeSpan
    {
        private const int MaxHours = 99;
        private const int MaxMinutes = 59;
        private const int MaxSeconds = 59;

        public int Hours { get; }
        public int Minutes { get; }
        public int Seconds { get; }
        
        public SonosTimeSpan(TimeSpan timeSpan) : this(timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds)
        {
        }

        public SonosTimeSpan(int hours, int minutes, int seconds)
        {
            if (hours < 0 || hours > MaxHours)
                throw new ArgumentOutOfRangeException(nameof(hours), $"Hours must be between 0 and {MaxHours}.");
            if (minutes < 0 || minutes > MaxMinutes)
                throw new ArgumentOutOfRangeException(nameof(minutes), $"Minutes must be between 0 and {MaxMinutes}.");
            if (seconds < 0 || seconds > MaxSeconds)
                throw new ArgumentOutOfRangeException(nameof(seconds), $"Seconds must be between 0 and {MaxSeconds}.");

            Hours = hours;
            Minutes = minutes;
            Seconds = seconds;
        }

        public SonosTimeSpan(int minutes, int seconds) : this(0, minutes, seconds)
        {
        }

        public SonosTimeSpan(int seconds) : this(0, 0, seconds)
        {
        }

        public SonosTimeSpan() : this(0, 0, 0)
        {
        }

        /// <summary>
        /// Returns the time span in hh:mm:ss format.
        /// </summary>
        public override string ToString()
        {
            return Hours.ToString().PadLeft(2, '0') + ":" +
                   Minutes.ToString().PadLeft(2, '0') + ":" +
                   Seconds.ToString().PadLeft(2, '0');
        }

        public TimeSpan ToTimeSpan()
        {
            return new TimeSpan(Hours, Minutes, Seconds);
        }
    }
}