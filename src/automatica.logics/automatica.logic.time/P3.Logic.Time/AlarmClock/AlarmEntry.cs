using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace P3.Logic.Time.AlarmClock
{
    /// <summary>
    /// One alarm time of the alarm clock. It rings on the given weekdays and/or while one of the given operating modes is active.
    /// </summary>
    public sealed class AlarmEntry
    {
        public const string DefaultId = "default";

        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Name { get; set; } = "";

        /// <summary>Local time of day, "HH:mm" or "HH:mm:ss"</summary>
        public string Time { get; set; } = "06:00";
        public bool Enabled { get; set; } = true;

        /// <summary><see cref="DayOfWeek"/> numbers, 0 = Sunday</summary>
        public List<int> Days { get; set; } = new();

        /// <summary>Operating mode keys, e.g. "vacation"</summary>
        public List<string> Modes { get; set; } = new();

        [JsonIgnore]
        public bool IsDefault => string.Equals(Id, DefaultId, StringComparison.OrdinalIgnoreCase);

        [JsonIgnore]
        public TimeSpan? TimeOfDay => AlarmEntries.ParseTime(Time);
    }

    public static class AlarmEntries
    {
        private static readonly JsonSerializerSettings Settings = new()
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            NullValueHandling = NullValueHandling.Ignore
        };

        public static string DefaultJson => Serialize(CreateDefault());

        public static List<AlarmEntry> CreateDefault() => new()
        {
            new AlarmEntry { Id = AlarmEntry.DefaultId, Time = "06:00", Enabled = true, Days = new List<int> { 1, 2, 3, 4, 5 } }
        };

        public static TimeSpan? ParseTime(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            if (TimeSpan.TryParseExact(value.Trim(), new[] { @"hh\:mm", @"h\:mm", @"hh\:mm\:ss", @"h\:mm\:ss" }, CultureInfo.InvariantCulture, out var time)
                && time >= TimeSpan.Zero && time < TimeSpan.FromDays(1))
            {
                return time;
            }
            return null;
        }

        public static string FormatTime(TimeSpan time) => time.Seconds == 0
            ? time.ToString(@"hh\:mm", CultureInfo.InvariantCulture)
            : time.ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture);

        /// <summary>
        /// Parses the entries, invalid JSON returns null. Entries without a valid time are dropped, ids are made unique.
        /// </summary>
        public static List<AlarmEntry> Parse(object value)
        {
            if (value == null)
            {
                return null;
            }

            var text = value.ToString();
            if (string.IsNullOrWhiteSpace(text))
            {
                return new List<AlarmEntry>();
            }

            try
            {
                var entries = JsonConvert.DeserializeObject<List<AlarmEntry>>(text, Settings) ?? new List<AlarmEntry>();
                var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var result = new List<AlarmEntry>();
                foreach (var entry in entries.Where(a => a != null && a.TimeOfDay.HasValue))
                {
                    if (string.IsNullOrWhiteSpace(entry.Id) || !ids.Add(entry.Id))
                    {
                        entry.Id = Guid.NewGuid().ToString("N");
                        ids.Add(entry.Id);
                    }
                    entry.Name ??= "";
                    entry.Days = (entry.Days ?? new List<int>()).Where(d => d is >= 0 and <= 6).Distinct().OrderBy(d => d).ToList();
                    entry.Modes = (entry.Modes ?? new List<string>()).Where(m => !string.IsNullOrWhiteSpace(m)).Select(m => m.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                    result.Add(entry);
                }
                return result;
            }
            catch (JsonException)
            {
                return null;
            }
        }

        public static string Serialize(IEnumerable<AlarmEntry> entries) => JsonConvert.SerializeObject(entries, Settings);

        public static IReadOnlyList<string> ParseModeList(object value)
        {
            var text = value?.ToString();
            if (string.IsNullOrWhiteSpace(text))
            {
                return Array.Empty<string>();
            }
            return text.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        /// <summary>
        /// The entries that ring on <paramref name="date"/>:
        /// enabled entries for one of the active operating modes win over the weekday entries;
        /// without such entries the weekday entries ring, unless one of <paramref name="suppressModes"/> is active.
        /// </summary>
        public static IReadOnlyList<AlarmEntry> ForDay(IEnumerable<AlarmEntry> entries, DateOnly date, IReadOnlyList<string> activeModes, IReadOnlyList<string> suppressModes)
        {
            var enabled = entries.Where(a => a.Enabled && a.TimeOfDay.HasValue).ToList();
            activeModes ??= Array.Empty<string>();

            var modeEntries = enabled.Where(a => a.Modes.Any(m => activeModes.Contains(m, StringComparer.OrdinalIgnoreCase))).ToList();
            if (modeEntries.Count > 0)
            {
                return modeEntries;
            }

            if (suppressModes != null && activeModes.Any(m => suppressModes.Contains(m, StringComparer.OrdinalIgnoreCase)))
            {
                return Array.Empty<AlarmEntry>();
            }

            var weekday = (int)date.DayOfWeek;
            return enabled.Where(a => a.Days.Contains(weekday)).ToList();
        }
    }
}
