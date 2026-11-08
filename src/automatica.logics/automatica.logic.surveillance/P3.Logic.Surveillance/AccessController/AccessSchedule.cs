using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace P3.Logic.Surveillance.AccessController
{
    /// <summary>
    /// A time window on some week days; the end may be after midnight (22:00-06:00 belongs to the day it starts)
    /// </summary>
    public sealed record AccessWindow(IReadOnlySet<DayOfWeek> Days, int StartMinute, int EndMinute)
    {
        public bool Contains(DateTime local)
        {
            var minute = local.Hour * 60 + local.Minute;
            if (EndMinute > StartMinute)
            {
                return Days.Contains(local.DayOfWeek) && minute >= StartMinute && minute < EndMinute;
            }

            // over midnight
            var previousDay = (DayOfWeek)(((int)local.DayOfWeek + 6) % 7);
            return Days.Contains(local.DayOfWeek) && minute >= StartMinute || Days.Contains(previousDay) && minute < EndMinute;
        }
    }

    /// <summary>
    /// User groups with access times, from the text parameter, e.g.
    /// "Family: Mo-Su 00:00-24:00; Cleaner: Mo,Th 08:00-12:00 | Sa 09:00-10:00". Day names in English or German.
    /// </summary>
    public sealed class AccessSchedule
    {
        private static readonly Dictionary<string, DayOfWeek> DayNames = new(StringComparer.OrdinalIgnoreCase)
        {
            ["mo"] = DayOfWeek.Monday, ["tu"] = DayOfWeek.Tuesday, ["di"] = DayOfWeek.Tuesday,
            ["we"] = DayOfWeek.Wednesday, ["mi"] = DayOfWeek.Wednesday, ["th"] = DayOfWeek.Thursday, ["do"] = DayOfWeek.Thursday,
            ["fr"] = DayOfWeek.Friday, ["sa"] = DayOfWeek.Saturday, ["su"] = DayOfWeek.Sunday, ["so"] = DayOfWeek.Sunday
        };

        // Monday first, for ranges like "Mo-Fr" or "Sa-Mo"
        private static readonly DayOfWeek[] Week =
        {
            DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday
        };

        private static readonly Regex WindowRegex = new("^(?<days>[A-Za-z,\\- ]+?)\\s+(?<from>\\d{1,2}:\\d{2})\\s*-\\s*(?<to>\\d{1,2}:\\d{2})$", RegexOptions.Compiled);

        public IReadOnlyDictionary<string, IReadOnlyList<AccessWindow>> Groups { get; }
        public IReadOnlyList<string> Errors { get; }

        private AccessSchedule(Dictionary<string, IReadOnlyList<AccessWindow>> groups, List<string> errors)
        {
            Groups = groups;
            Errors = errors;
        }

        public bool Allows(IEnumerable<string> groups, DateTime local) =>
            groups.Any(g => Groups.TryGetValue(g.Trim(), out var windows) && windows.Any(w => w.Contains(local)));

        public static AccessSchedule Parse(string text)
        {
            var groups = new Dictionary<string, IReadOnlyList<AccessWindow>>(StringComparer.OrdinalIgnoreCase);
            var errors = new List<string>();

            foreach (var entry in (text ?? "").Split(new[] { ';', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var colon = entry.IndexOf(':');
                if (colon <= 0)
                {
                    errors.Add($"'{entry}': missing group name");
                    continue;
                }

                var name = entry.Substring(0, colon).Trim();
                var windows = new List<AccessWindow>();
                foreach (var part in entry.Substring(colon + 1).Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    var window = ParseWindow(part);
                    if (window == null)
                    {
                        errors.Add($"{name}: cannot read '{part}'");
                    }
                    else
                    {
                        windows.Add(window);
                    }
                }
                groups[name] = windows;
            }

            return new AccessSchedule(groups, errors);
        }

        private static AccessWindow ParseWindow(string text)
        {
            var match = WindowRegex.Match(text);
            if (!match.Success)
            {
                return null;
            }

            var days = new HashSet<DayOfWeek>();
            foreach (var token in match.Groups["days"].Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var range = token.Split('-', StringSplitOptions.TrimEntries);
                if (range.Length == 1 && DayNames.TryGetValue(range[0], out var day))
                {
                    days.Add(day);
                }
                else if (range.Length == 2 && DayNames.TryGetValue(range[0], out var from) && DayNames.TryGetValue(range[1], out var to))
                {
                    var i = Array.IndexOf(Week, from);
                    while (true)
                    {
                        days.Add(Week[i]);
                        if (Week[i] == to)
                        {
                            break;
                        }
                        i = (i + 1) % 7;
                    }
                }
                else
                {
                    return null;
                }
            }

            var start = Minutes(match.Groups["from"].Value);
            var end = Minutes(match.Groups["to"].Value);
            if (start == null || end == null || start >= 24 * 60 || days.Count == 0)
            {
                return null;
            }

            return new AccessWindow(days, start.Value, end.Value == 0 ? 24 * 60 : end.Value);
        }

        private static int? Minutes(string hhmm)
        {
            var parts = hhmm.Split(':');
            var hours = int.Parse(parts[0], CultureInfo.InvariantCulture);
            var minutes = int.Parse(parts[1], CultureInfo.InvariantCulture);
            if (minutes > 59 || hours > 24 || hours == 24 && minutes > 0)
            {
                return null;
            }
            return hours * 60 + minutes;
        }
    }
}
