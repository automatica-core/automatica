using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Automatica.Core.Internals.OperatingModes
{
    /// <summary>
    /// Operating modes (Loxone "Operating Modes") and operating times (the periods in which a mode is active).
    /// Stored as one JSON document in the hidden setting <see cref="SettingKey"/>.
    /// </summary>
    public sealed class OperatingModeConfiguration
    {
        public const string SettingKey = "OperatingModes";

        public int Version { get; set; } = 1;

        /// <summary>ISO country code of the predefined public holidays, empty = none</summary>
        public string Country { get; set; } = "";

        /// <summary>Region (e.g. German state "BY"), empty = country wide holidays only</summary>
        public string Region { get; set; } = "";

        /// <summary>Holiday keys of the country that are not used</summary>
        public List<string> DisabledHolidays { get; set; } = new();

        public List<OperatingModeDefinition> Modes { get; set; } = new();

        public List<OperatingPeriod> Periods { get; set; } = new();

        public static OperatingModeConfiguration CreateDefault() => new()
        {
            Country = "",
            Modes = BuiltInModes().ToList()
        };

        public static IEnumerable<OperatingModeDefinition> BuiltInModes()
        {
            yield return new OperatingModeDefinition { Key = OperatingModeContract.PublicHoliday, Name = "", Priority = 300, BuiltIn = true };
            yield return new OperatingModeDefinition { Key = OperatingModeContract.Vacation, Name = "", Priority = 200, BuiltIn = true };
            yield return new OperatingModeDefinition { Key = OperatingModeContract.SchoolHolidays, Name = "", Priority = 100, BuiltIn = true };
        }

        public static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = true
        };

        public static OperatingModeConfiguration Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return CreateDefault();
            }

            try
            {
                return Normalize(JsonSerializer.Deserialize<OperatingModeConfiguration>(json, JsonOptions));
            }
            catch (JsonException)
            {
                return CreateDefault();
            }
        }

        public string ToJson() => JsonSerializer.Serialize(this, JsonOptions);

        /// <summary>
        /// Adds missing built-in modes, drops broken entries, makes keys and ids unique. Never throws.
        /// </summary>
        public static OperatingModeConfiguration Normalize(OperatingModeConfiguration configuration)
        {
            configuration ??= CreateDefault();
            configuration.Country = (configuration.Country ?? "").Trim().ToUpperInvariant();
            configuration.Region = (configuration.Region ?? "").Trim().ToUpperInvariant();
            configuration.DisabledHolidays = (configuration.DisabledHolidays ?? new List<string>()).Where(a => !string.IsNullOrWhiteSpace(a)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            var modes = new List<OperatingModeDefinition>();
            var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var mode in configuration.Modes ?? new List<OperatingModeDefinition>())
            {
                if (mode == null)
                {
                    continue;
                }
                mode.Key = Slug(mode.Key);
                if (string.IsNullOrEmpty(mode.Key) || !keys.Add(mode.Key))
                {
                    continue;
                }
                mode.Name ??= "";
                mode.BuiltIn = BuiltInModes().Any(a => a.Key == mode.Key);
                modes.Add(mode);
            }
            foreach (var builtIn in BuiltInModes())
            {
                if (keys.Add(builtIn.Key))
                {
                    modes.Add(builtIn);
                }
            }
            configuration.Modes = modes;

            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var periods = new List<OperatingPeriod>();
            foreach (var period in configuration.Periods ?? new List<OperatingPeriod>())
            {
                if (period == null || !keys.Contains(period.Mode ?? ""))
                {
                    continue;
                }
                if (string.IsNullOrWhiteSpace(period.Id) || !ids.Add(period.Id))
                {
                    period.Id = Guid.NewGuid().ToString("N");
                    ids.Add(period.Id);
                }
                if (period.End < period.Start)
                {
                    (period.Start, period.End) = (period.End, period.Start);
                }
                period.Name ??= "";
                periods.Add(period);
            }
            configuration.Periods = periods;
            return configuration;
        }

        private static readonly Regex NotSlug = new("[^a-z0-9-]+", RegexOptions.Compiled);

        /// <summary>
        /// Lower case key of letters, digits and dashes ("Home office" -> "home-office")
        /// </summary>
        public static string Slug(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "";
            }
            var slug = value.Trim().ToLowerInvariant()
                .Replace("ä", "ae").Replace("ö", "oe").Replace("ü", "ue").Replace("ß", "ss");
            slug = NotSlug.Replace(slug, "-").Trim('-');
            return slug.Length > 50 ? slug[..50].Trim('-') : slug;
        }
    }

    public sealed class OperatingModeDefinition
    {
        public string Key { get; set; }

        /// <summary>Display name, empty for the built-in modes (translated in the UI)</summary>
        public string Name { get; set; }

        /// <summary>Higher wins (order in the published list, e.g. for alarm clocks)</summary>
        public int Priority { get; set; }

        public bool BuiltIn { get; set; }

        /// <summary>Manual override: true = on, false = off, null = operating times (calendar)</summary>
        public bool? Manual { get; set; }
    }

    /// <summary>
    /// An operating time: the mode is active from <see cref="Start"/> to <see cref="End"/> (whole days, both included)
    /// </summary>
    public sealed class OperatingPeriod
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Mode { get; set; }
        public DateOnly Start { get; set; }
        public DateOnly End { get; set; }

        /// <summary>Repeats every year (day and month of start/end)</summary>
        public bool Yearly { get; set; }

        public bool Enabled { get; set; } = true;

        public bool Covers(DateOnly date)
        {
            if (!Enabled)
            {
                return false;
            }
            if (!Yearly)
            {
                return date >= Start && date <= End;
            }

            // same period moved into the year of the date (and the year before, for periods across new year)
            foreach (var shift in new[] { 0, -1 })
            {
                var years = date.Year + shift - Start.Year;
                var start = Start.AddYears(years);
                var end = End.AddYears(years);
                if (date >= start && date <= end)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
