using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Automatica.Core.Base.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace P3.Logic.Time.OperatingModes
{
    /// <summary>
    /// Well known dispatcher ids of the core operating modes. They must match
    /// <c>Automatica.Core.Internals.OperatingModes.OperatingModeContract</c> in the core.
    /// </summary>
    public static class OperatingModeContract
    {
        /// <summary>JSON state of the operating modes, published by the core (<see cref="DispatchableType.Control"/>)</summary>
        public static readonly Guid StateId = new Guid("f78460f9-88bb-4ab5-85e0-0701d6cebfd3");

        /// <summary>Manual on/off requests from logics: <c>{"mode":"vacation","state":true|false|null}</c></summary>
        public static readonly Guid RequestId = new Guid("f84abac5-dfd4-4efc-a4fa-cde8d11788c0");

        public const string PublicHoliday = "public-holiday";
        public const string Vacation = "vacation";
        public const string SchoolHolidays = "school-holidays";
    }

    /// <summary>
    /// Parsed operating mode state of the core
    /// </summary>
    public sealed class OperatingModeSnapshot
    {
        public static readonly OperatingModeSnapshot Empty = new OperatingModeSnapshot();

        private readonly Dictionary<DateOnly, IReadOnlyList<string>> _days = new();

        public IReadOnlyList<string> Active { get; private set; } = Array.Empty<string>();
        public IReadOnlyDictionary<string, string> Names { get; private set; } = new Dictionary<string, string>();
        public double? Latitude { get; private set; }
        public double? Longitude { get; private set; }
        public bool IsEmpty { get; private set; } = true;

        public bool HasLocation => Latitude.HasValue && Longitude.HasValue && (Latitude != 0 || Longitude != 0);

        public bool IsActive(string key) => Active.Contains(key, StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Active modes of <paramref name="date"/>, highest priority first. Days beyond the published window use <see cref="Active"/>
        /// only for today, otherwise nothing is known and an empty list is returned.
        /// </summary>
        public IReadOnlyList<string> ActiveOn(DateOnly date, DateOnly today)
        {
            if (_days.TryGetValue(date, out var keys))
            {
                return keys;
            }
            return date == today ? Active : Array.Empty<string>();
        }

        public static OperatingModeSnapshot Parse(object value)
        {
            if (value == null)
            {
                return Empty;
            }

            try
            {
                var json = value as JObject ?? JObject.Parse(value.ToString()!);
                var snapshot = new OperatingModeSnapshot { IsEmpty = false };
                snapshot.Active = ReadKeys(json["active"]);
                snapshot.Latitude = json["latitude"]?.Type is JTokenType.Float or JTokenType.Integer ? json["latitude"]!.Value<double>() : null;
                snapshot.Longitude = json["longitude"]?.Type is JTokenType.Float or JTokenType.Integer ? json["longitude"]!.Value<double>() : null;

                if (json["days"] is JObject days)
                {
                    foreach (var day in days.Properties())
                    {
                        if (DateOnly.TryParseExact(day.Name, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                        {
                            snapshot._days[date] = ReadKeys(day.Value);
                        }
                    }
                }

                var names = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                if (json["modes"] is JArray modes)
                {
                    foreach (var mode in modes.OfType<JObject>())
                    {
                        var key = mode["key"]?.ToString();
                        if (!string.IsNullOrEmpty(key))
                        {
                            names[key] = mode["name"]?.ToString() ?? key;
                        }
                    }
                }
                snapshot.Names = names;
                return snapshot;
            }
            catch (Exception e) when (e is JsonException or FormatException or InvalidCastException or ArgumentException)
            {
                return Empty;
            }
        }

        private static IReadOnlyList<string> ReadKeys(JToken token)
        {
            if (token is not JArray array)
            {
                return Array.Empty<string>();
            }
            return array.Select(a => a.ToString()).Where(a => !string.IsNullOrWhiteSpace(a)).ToArray();
        }
    }

    /// <summary>
    /// Follows the operating mode state the core publishes on the dispatcher. No core API is needed for it.
    /// The state is polled (<see cref="Refresh"/>): core NuGet 10.0.0.2049 cannot remove a single dispatcher callback,
    /// so a registration could not be undone without dropping the ones of other logics. Callers poll at least once a minute.
    /// </summary>
    internal sealed class OperatingModeWatcher : IDisposable
    {
        public const double PollMilliseconds = 60_000;

        private readonly IDispatcher _dispatcher;
        private object _lastRaw;

        public OperatingModeWatcher(IDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        public OperatingModeSnapshot Current { get; private set; } = OperatingModeSnapshot.Empty;

        /// <summary>
        /// Reads the published state, true when it changed since the last call
        /// </summary>
        public bool Refresh()
        {
            var raw = _dispatcher.GetValue(DispatchableType.Control, OperatingModeContract.StateId)?.Value;
            return Update(raw);
        }

        /// <summary>
        /// Applies a state (also used by tests), true when it changed
        /// </summary>
        internal bool Update(object raw)
        {
            if (Equals(raw, _lastRaw) || (raw != null && _lastRaw != null && raw.ToString() == _lastRaw.ToString()))
            {
                return false;
            }
            _lastRaw = raw;
            Current = OperatingModeSnapshot.Parse(raw);
            return true;
        }

        /// <summary>
        /// Asks the core to switch a mode on/off manually, null hands it back to the calendar
        /// </summary>
        public void Request(string mode, bool? state)
        {
            var json = JsonConvert.SerializeObject(new { mode, state });
            _dispatcher.DispatchValue(new RequestDispatchable(), json);
        }

        public void Dispose()
        {
        }

        private sealed class RequestDispatchable : IDispatchable
        {
            public DispatchableSource Source => DispatchableSource.RuleInstance;
            public DispatchableType Type => DispatchableType.Control;
            public string Name => "OperatingModeRequest";
            public Guid Id => OperatingModeContract.RequestId;
            public bool IsRemanent => false;
        }
    }
}
