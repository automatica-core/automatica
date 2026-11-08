using System;
using Innovative.SolarCalculator;

namespace P3.Logic.Time
{
    internal enum SunEvent
    {
        Sunrise,
        Sunset,
        Dawn,
        Dusk
    }

    /// <summary>
    /// Sun times of a local date, same calculation as the core automations (SolarCalculator, civil dawn/dusk)
    /// </summary>
    internal static class SunTimes
    {
        /// <summary>
        /// The instant of <paramref name="sunEvent"/> on <paramref name="date"/>, null for polar day/night
        /// </summary>
        public static DateTimeOffset? Get(TimeProvider provider, DateOnly date, SunEvent sunEvent, double latitude, double longitude)
        {
            var noon = LocalClock.At(provider, date, TimeSpan.FromHours(12));
            var solarTimes = new SolarTimes(noon, latitude, longitude);
            DateTime local;
            try
            {
                local = sunEvent switch
                {
                    SunEvent.Sunrise => solarTimes.Sunrise,
                    SunEvent.Sunset => solarTimes.Sunset,
                    SunEvent.Dawn => solarTimes.DawnCivil,
                    SunEvent.Dusk => solarTimes.DuskCivil,
                    _ => throw new ArgumentOutOfRangeException(nameof(sunEvent))
                };
            }
            catch (Exception e) when (e is ArgumentOutOfRangeException or OverflowException)
            {
                return null;
            }

            if (DateOnly.FromDateTime(local) != date)
            {
                return null;
            }

            return LocalClock.At(provider, date, local.TimeOfDay);
        }
    }
}
