using Automatica.Core.Automation.Definition;

namespace Automatica.Core.Automation.Engine
{
    /// <summary>
    /// Local time helpers on top of a <see cref="TimeProvider"/> (daylight saving aware)
    /// </summary>
    internal static class LocalTime
    {
        public static DateTimeOffset Now(TimeProvider time)
        {
            return time.GetLocalNow();
        }

        /// <summary>
        /// <paramref name="date"/> at <paramref name="timeOfDay"/> in the local time zone. A time in the spring-forward gap
        /// moves to the first valid time after it.
        /// </summary>
        public static DateTimeOffset At(TimeProvider time, DateOnly date, TimeSpan timeOfDay)
        {
            var zone = time.LocalTimeZone;
            var local = date.ToDateTime(TimeOnly.MinValue).Add(timeOfDay);
            while (zone.IsInvalidTime(local))
            {
                local = local.AddMinutes(30);
            }
            return new DateTimeOffset(local, zone.GetUtcOffset(local));
        }

        public static bool DayAllowed(IReadOnlyCollection<DayOfWeek> days, DayOfWeek day)
        {
            return days == null || days.Count == 0 || days.Contains(day);
        }

        /// <summary>
        /// Next time of day after <paramref name="after"/> on an allowed day, within 8 days
        /// </summary>
        public static DateTimeOffset? NextTime(TimeProvider time, TimeSpan timeOfDay, IReadOnlyCollection<DayOfWeek> days, DateTimeOffset after)
        {
            var start = DateOnly.FromDateTime(after.ToOffset(time.LocalTimeZone.GetUtcOffset(after)).DateTime);
            for (var i = -1; i <= 8; i++)
            {
                var date = start.AddDays(i);
                if (!DayAllowed(days, date.DayOfWeek))
                {
                    continue;
                }

                var candidate = At(time, date, timeOfDay);
                if (candidate > after)
                {
                    return candidate;
                }
            }

            return null;
        }

        /// <summary>
        /// Next sun event (with offset) after <paramref name="after"/> on an allowed day, within 8 days
        /// </summary>
        public static DateTimeOffset? NextSun(IAutomationEnvironment environment, SunEvent sunEvent, int offsetMinutes, IReadOnlyCollection<DayOfWeek> days, DateTimeOffset after)
        {
            var time = environment.Time;
            var start = DateOnly.FromDateTime(after.ToOffset(time.LocalTimeZone.GetUtcOffset(after)).DateTime);
            for (var i = -1; i <= 8; i++)
            {
                var date = start.AddDays(i);
                if (!DayAllowed(days, date.DayOfWeek))
                {
                    continue;
                }

                var sun = environment.GetSunTime(date, sunEvent);
                if (sun == null)
                {
                    continue;
                }

                var candidate = sun.Value.AddMinutes(offsetMinutes);
                if (candidate > after)
                {
                    return candidate;
                }
            }

            return null;
        }
    }
}
