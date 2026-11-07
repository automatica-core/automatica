using System;
using System.Reflection;
using Automatica.Core.UnitTests.Base.Logics;

namespace P3.Logic.Time.Tests.TestSupport
{
    internal static class TimeZones
    {
        private static readonly Lazy<TimeZoneInfo> ViennaZone = new(() =>
        {
            foreach (var id in new[] { "Europe/Vienna", "W. Europe Standard Time" })
            {
                try
                {
                    return TimeZoneInfo.FindSystemTimeZoneById(id);
                }
                catch (TimeZoneNotFoundException)
                {
                }
                catch (InvalidTimeZoneException)
                {
                }
            }

            throw new InvalidOperationException("Europe/Vienna is not available on this machine");
        });

        public static TimeZoneInfo Vienna => ViennaZone.Value;

        /// <summary>
        /// A UTC instant for a wall time in Vienna (throws for a wall time that does not exist)
        /// </summary>
        public static DateTime ViennaToUtc(int year, int month, int day, int hour, int minute = 0, int second = 0, int millisecond = 0)
        {
            var wall = new DateTime(year, month, day, hour, minute, second, millisecond, DateTimeKind.Unspecified);
            return TimeZoneInfo.ConvertTimeToUtc(wall, Vienna);
        }
    }

    /// <summary>
    /// Pins the time zone the logic sees. The logics take their time from <see cref="FakeTimeProvider"/>, but they compare
    /// <see cref="DateTimeOffset"/> values with <see cref="DateTime"/> values, and that implicit conversion uses the machine's
    /// <see cref="TimeZoneInfo.Local"/>. So the process-wide local zone is pinned too (via reflection, the test assemblies run
    /// serially), otherwise the result would depend on the zone of the machine that runs the tests.
    /// </summary>
    internal sealed class TimeZoneScope : IDisposable
    {
        private static readonly object Lock = new();

        private TimeZoneScope(TimeZoneInfo zone)
        {
            lock (Lock)
            {
                SetLocal(zone);
                FakeTimeProvider.Instance.SetTimeZone(TimeZoneInfo.Local);
            }
        }

        public static TimeZoneScope Vienna() => new(TimeZones.Vienna);

        public static TimeZoneScope Utc() => new(TimeZoneInfo.Utc);

        public static TimeZoneScope Of(TimeZoneInfo zone) => new(zone);

        public static bool CanPinLocalZone
        {
            get
            {
                var cachedDataField = typeof(TimeZoneInfo).GetField("s_cachedData", BindingFlags.NonPublic | BindingFlags.Static);
                var localField = cachedDataField?.FieldType.GetField("_localTimeZone", BindingFlags.NonPublic | BindingFlags.Instance);
                return localField != null;
            }
        }

        private static void SetLocal(TimeZoneInfo zone)
        {
            TimeZoneInfo.ClearCachedData();
            if (zone == null)
            {
                return;
            }

            var cachedDataField = typeof(TimeZoneInfo).GetField("s_cachedData", BindingFlags.NonPublic | BindingFlags.Static)
                                  ?? throw new InvalidOperationException("TimeZoneInfo.s_cachedData not found");
            var cachedData = cachedDataField.GetValue(null);
            var localField = cachedData.GetType().GetField("_localTimeZone", BindingFlags.NonPublic | BindingFlags.Instance)
                             ?? throw new InvalidOperationException("TimeZoneInfo.CachedData._localTimeZone not found");
            localField.SetValue(cachedData, zone);
        }

        public void Dispose()
        {
            lock (Lock)
            {
                // back to the real machine zone
                SetLocal(null);
                FakeTimeProvider.Instance.SetTimeZone(TimeZoneInfo.Local);
            }
        }
    }
}
