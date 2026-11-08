using System;

namespace P3.Logic.Time
{
    /// <summary>
    /// Wall clock helpers on top of a <see cref="TimeProvider"/> (the fake one in tests)
    /// </summary>
    internal static class LocalClock
    {
        /// <summary>
        /// The instant of the local wall time <paramref name="time"/> on <paramref name="date"/>.
        /// A time that does not exist (DST gap) is moved forward by the gap, an ambiguous one uses the first occurrence.
        /// </summary>
        public static DateTimeOffset At(TimeProvider provider, DateOnly date, TimeSpan time)
        {
            var zone = provider.LocalTimeZone;
            var local = date.ToDateTime(TimeOnly.MinValue).Add(time);

            if (zone.IsInvalidTime(local))
            {
                local = local.AddHours(1);
            }

            var offset = zone.IsAmbiguousTime(local)
                ? Max(zone.GetAmbiguousTimeOffsets(local))
                : zone.GetUtcOffset(local);
            return new DateTimeOffset(local, offset);
        }

        public static DateTimeOffset Now(TimeProvider provider) => provider.GetLocalNow();

        public static DateOnly Today(TimeProvider provider) => DateOnly.FromDateTime(provider.GetLocalNow().DateTime);

        /// <summary>
        /// Timer interval in ms until <paramref name="target"/>, at least 1 ms and at most <paramref name="maxMs"/>
        /// (the timer then re-checks, so clock changes are corrected)
        /// </summary>
        public static double IntervalUntil(TimeProvider provider, DateTimeOffset target, double maxMs = 3_600_000)
        {
            var ms = (target - provider.GetUtcNow()).TotalMilliseconds;
            return TimerHelper.ClampInterval(Math.Min(ms, maxMs));
        }

        private static TimeSpan Max(TimeSpan[] offsets)
        {
            var max = offsets[0];
            foreach (var o in offsets)
            {
                if (o > max)
                {
                    max = o;
                }
            }
            return max;
        }
    }
}
