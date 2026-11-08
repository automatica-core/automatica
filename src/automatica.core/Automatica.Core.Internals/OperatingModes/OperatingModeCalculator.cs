using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Automatica.Core.Internals.OperatingModes
{
    /// <summary>
    /// Why a mode is active on a day
    /// </summary>
    public sealed record OperatingModeReason(string Type, string Key, string Name);

    public sealed class OperatingModeDay
    {
        public DateOnly Date { get; init; }

        /// <summary>Active mode keys, highest priority first</summary>
        public IReadOnlyList<string> Modes { get; init; } = Array.Empty<string>();

        public IReadOnlyDictionary<string, IReadOnlyList<OperatingModeReason>> Reasons { get; init; } = new Dictionary<string, IReadOnlyList<OperatingModeReason>>();
    }

    /// <summary>
    /// Which modes are active on a date: public holidays of the country, operating times (periods), then the manual overrides
    /// (manual on/off applies to every day until it is handed back to the calendar).
    /// </summary>
    public static class OperatingModeCalculator
    {
        public static OperatingModeDay For(OperatingModeConfiguration configuration, DateOnly date)
        {
            var reasons = new Dictionary<string, List<OperatingModeReason>>(StringComparer.OrdinalIgnoreCase);

            void Add(string mode, OperatingModeReason reason)
            {
                if (!reasons.TryGetValue(mode, out var list))
                {
                    reasons[mode] = list = new List<OperatingModeReason>();
                }
                list.Add(reason);
            }

            foreach (var holiday in HolidayCalendar.For(configuration.Country, configuration.Region, date.Year))
            {
                if (holiday.Date == date && !configuration.DisabledHolidays.Contains(holiday.Key, StringComparer.OrdinalIgnoreCase))
                {
                    Add(OperatingModeContract.PublicHoliday, new OperatingModeReason("holiday", holiday.Key, ""));
                }
            }

            foreach (var period in configuration.Periods.Where(a => a.Covers(date)))
            {
                Add(period.Mode, new OperatingModeReason("period", period.Id, period.Name));
            }

            foreach (var mode in configuration.Modes.Where(a => a.Manual.HasValue))
            {
                if (mode.Manual == true)
                {
                    reasons.Remove(mode.Key);
                    Add(mode.Key, new OperatingModeReason("manual", mode.Key, ""));
                }
                else
                {
                    reasons.Remove(mode.Key);
                }
            }

            var order = configuration.Modes.ToDictionary(a => a.Key, a => a.Priority, StringComparer.OrdinalIgnoreCase);
            var modes = reasons.Keys
                .Where(order.ContainsKey)
                .OrderByDescending(a => order[a])
                .ThenBy(a => a, StringComparer.Ordinal)
                .ToList();

            return new OperatingModeDay
            {
                Date = date,
                Modes = modes,
                Reasons = modes.ToDictionary(a => a, a => (IReadOnlyList<OperatingModeReason>)reasons[a], StringComparer.OrdinalIgnoreCase)
            };
        }

        public static IReadOnlyList<OperatingModeDay> Range(OperatingModeConfiguration configuration, DateOnly from, int days)
        {
            return Enumerable.Range(0, Math.Max(0, days)).Select(i => For(configuration, from.AddDays(i))).ToList();
        }

        /// <summary>
        /// The JSON the logics read (<see cref="OperatingModeContract.StateId"/>)
        /// </summary>
        public static string StateJson(OperatingModeConfiguration configuration, DateOnly today, double? latitude, double? longitude)
        {
            var days = Range(configuration, today, OperatingModeContract.PublishedDays);
            var state = new
            {
                version = 1,
                latitude,
                longitude,
                today = today.ToString("yyyy-MM-dd"),
                modes = configuration.Modes
                    .OrderByDescending(a => a.Priority)
                    .Select(a => new { key = a.Key, name = string.IsNullOrEmpty(a.Name) ? a.Key : a.Name, priority = a.Priority, manual = a.Manual }),
                active = days[0].Modes,
                days = days.ToDictionary(a => a.Date.ToString("yyyy-MM-dd"), a => a.Modes)
            };
            return JsonSerializer.Serialize(state);
        }
    }
}
