using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using Microsoft.Extensions.Logging;
using P3.Logic.Time.AlarmClock;
using P3.Logic.Time.OperatingModes;
using P3.Logic.Time.PulseGenerator;

namespace P3.Logic.Time.PulseAt
{
    /// <summary>
    /// Pulse at: O is 1 for the on-duration at the selected time (daily time, sunrise/sunset/dawn/dusk + offset,
    /// or every minute/hour/day/week/month/year). With "one-time" a time or sun event pulses only on the set date.
    /// Off = 1 locks the block (O = 0, no pulses). The time can be changed from the visu, that change survives restarts.
    /// Sun events use the core location (published with the operating modes).
    /// </summary>
    public class PulseAtRule : Automatica.Core.Logic.Logic
    {
        private readonly object _sync = new();
        private readonly RuleInterfaceInstance _output;
        private readonly RuleInterfaceInstance _outputNext;
        private readonly PersistedParameter _persistedTime;
        private readonly OperatingModeWatcher _location;
        private System.Timers.Timer _timer;
        private bool _started;

        internal PulseAtType Type = PulseAtType.Time;
        private TimeSpan _time = TimeSpan.FromHours(7);
        private string _timeText = PulseAtLogicFactory.DefaultTime;
        internal double OffsetMinutes;
        internal bool OneTime;
        private DateOnly? _date;
        internal double DurationSeconds = PulseAtLogicFactory.DefaultDuration;

        private bool _locked;
        private bool _on;
        private DateTimeOffset? _offAt;
        private DateTimeOffset? _next;
        private DateTimeOffset? _lastPulse;
        private bool _nextSent;
        private DateTime? _lastNextSent;
        private bool _warnedLocation;

        public PulseAtRule(ILogicContext context) : base(context)
        {
            _output = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a => a.This2RuleInterfaceTemplate == PulseAtLogicFactory.Output);
            _outputNext = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a => a.This2RuleInterfaceTemplate == PulseAtLogicFactory.OutputNext);
            var timeParam = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a => a.This2RuleInterfaceTemplate == PulseAtLogicFactory.ParamTime);
            _persistedTime = new PersistedParameter(context.Dispatcher, timeParam?.ObjId ?? context.RuleInstance.ObjId, "pulse-at.time");
            _location = new OperatingModeWatcher(context.Dispatcher);
        }

        internal DateTimeOffset? Next => _next;
        internal string TimeText => _timeText;
        internal OperatingModeWatcher Location => _location;

        protected override Task<bool> Start(RuleInstance instance, CancellationToken token = default)
        {
            lock (_sync)
            {
                _timer = new System.Timers.Timer { AutoReset = false };
                _timer.Elapsed += _timer_Elapsed;
                _started = true;
            }
            _location.Refresh();
            return base.Start(instance, token);
        }

        protected override Task<bool> Stop(RuleInstance instance, CancellationToken token = default)
        {
            lock (_sync)
            {
                _started = false;
                if (_timer != null)
                {
                    _timer.Elapsed -= _timer_Elapsed;
                    _timer.Dispose();
                    _timer = null;
                }
            }
            return base.Stop(instance, token);
        }

        public override object GetDataForVisu()
        {
            var data = base.GetDataForVisu();
            if (data is Dictionary<string, object> dict)
            {
                lock (_sync)
                {
                    dict[PulseAtLogicFactory.TimeKey] = _timeText;
                    dict["next"] = _next?.DateTime;
                    dict["type"] = (int)Type;
                }
            }
            return data;
        }

        internal static PulseAtType ParseType(object value)
        {
            try
            {
                var number = value is string s ? int.Parse(s, CultureInfo.InvariantCulture) : Convert.ToInt32(value, CultureInfo.InvariantCulture);
                return Enum.IsDefined(typeof(PulseAtType), number) ? (PulseAtType)number : PulseAtType.Time;
            }
            catch (Exception)
            {
                return PulseAtType.Time;
            }
        }

        internal static DateOnly? ParseDate(object value)
        {
            var text = value?.ToString()?.Trim();
            if (string.IsNullOrEmpty(text))
            {
                return null;
            }
            if (DateOnly.TryParseExact(text, new[] { "yyyy-MM-dd", "dd.MM.yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            {
                return date;
            }
            if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTime))
            {
                return DateOnly.FromDateTime(dateTime);
            }
            return null;
        }

        protected override void ParameterValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            var template = instance.This2RuleInterfaceTemplate;
            lock (_sync)
            {
                if (template == PulseAtLogicFactory.ParamType)
                {
                    Type = ParseType(value);
                }
                else if (template == PulseAtLogicFactory.ParamTime)
                {
                    if (PersistedParameter.IsRuntimeSource(source))
                    {
                        var parsed = AlarmEntries.ParseTime(value?.ToString());
                        if (parsed == null)
                        {
                            Context.Logger.LogWarning($"Pulse at {Context.RuleInstance?.Name}: invalid time {value} from the visu ignored");
                            return;
                        }
                        SetTime(parsed.Value);
                        _persistedTime.Changed(_timeText);
                    }
                    else
                    {
                        var effective = _persistedTime.Configured(value?.ToString());
                        var parsed = AlarmEntries.ParseTime(effective) ?? AlarmEntries.ParseTime(value?.ToString());
                        if (parsed == null)
                        {
                            Context.Logger.LogWarning($"Pulse at {Context.RuleInstance?.Name}: invalid time {value}, using {PulseAtLogicFactory.DefaultTime}");
                        }
                        SetTime(parsed ?? AlarmEntries.ParseTime(PulseAtLogicFactory.DefaultTime)!.Value);
                    }
                }
                else if (template == PulseAtLogicFactory.ParamOffset)
                {
                    // may be negative (before the sun event)
                    OffsetMinutes = ParseSigned(value);
                }
                else if (template == PulseAtLogicFactory.ParamOneTime)
                {
                    OneTime = TimerHelper.IsTruthy(value);
                }
                else if (template == PulseAtLogicFactory.ParamDate)
                {
                    _date = ParseDate(value);
                }
                else if (template == PulseAtLogicFactory.ParamDuration)
                {
                    DurationSeconds = Math.Max(0, ParseSigned(value, PulseAtLogicFactory.DefaultDuration));
                }
                else
                {
                    return;
                }

                Reschedule();
            }
        }

        private static double ParseSigned(object value, double fallback = 0)
        {
            try
            {
                var result = value is string s
                    ? double.Parse(s, NumberStyles.Float, CultureInfo.InvariantCulture)
                    : Convert.ToDouble(value, CultureInfo.InvariantCulture);
                return double.IsNaN(result) || double.IsInfinity(result) ? fallback : result;
            }
            catch (Exception)
            {
                return fallback;
            }
        }

        private void SetTime(TimeSpan time)
        {
            _time = time;
            _timeText = time.ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture);
            ParameterValues[PulseAtLogicFactory.TimeKey] = _timeText;
        }

        protected override IList<ILogicOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            if (instance.This2RuleInterfaceTemplate == PulseAtLogicFactory.InputOff)
            {
                lock (_sync)
                {
                    _locked = TimerHelper.IsTruthy(value);
                    if (_locked)
                    {
                        SwitchOff();
                    }
                    Reschedule();
                }
            }
            return new List<ILogicOutputChanged>();
        }

        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            lock (_sync)
            {
                if (!_started || _timer == null)
                {
                    return;
                }
                Evaluate();
            }
        }

        private void Evaluate()
        {
            var now = Context.TimeProvider.GetUtcNow();
            _location.Refresh();

            if (_on && _offAt.HasValue && now >= _offAt.Value)
            {
                SwitchOff();
            }

            if (!_locked && _next.HasValue && now >= _next.Value)
            {
                _lastPulse = _next;
                if (DurationSeconds <= 0)
                {
                    Dispatch(_output, true);
                    Dispatch(_output, false);
                }
                else
                {
                    _offAt = now.AddSeconds(DurationSeconds);
                    if (!_on)
                    {
                        _on = true;
                        Dispatch(_output, true);
                    }
                }
            }

            ComputeNext(now);
            Arm(now);
        }

        private void Reschedule()
        {
            if (!_started || _timer == null)
            {
                return;
            }
            var now = Context.TimeProvider.GetUtcNow();
            ComputeNext(now);
            Arm(now);
        }

        private void SwitchOff()
        {
            _offAt = null;
            if (_on)
            {
                _on = false;
                Dispatch(_output, false);
            }
        }

        private void ComputeNext(DateTimeOffset now)
        {
            var after = _lastPulse.HasValue && _lastPulse.Value > now ? _lastPulse.Value : now;
            _next = _locked ? null : NextAfter(after);

            var local = _next?.DateTime;
            if (!_nextSent || local != _lastNextSent)
            {
                _nextSent = true;
                _lastNextSent = local;
                Dispatch(_outputNext, local);
            }
        }

        /// <summary>
        /// The first pulse strictly after <paramref name="after"/>
        /// </summary>
        internal DateTimeOffset? NextAfter(DateTimeOffset after)
        {
            var provider = Context.TimeProvider;
            var zone = provider.LocalTimeZone;
            var localAfter = TimeZoneInfo.ConvertTime(after, zone);
            var today = DateOnly.FromDateTime(localAfter.DateTime);

            switch (Type)
            {
                case PulseAtType.Time:
                {
                    if (OneTime)
                    {
                        if (_date == null)
                        {
                            return null;
                        }
                        var at = LocalClock.At(provider, _date.Value, _time);
                        return at > after ? at : null;
                    }
                    for (var i = 0; i <= 2; i++)
                    {
                        var at = LocalClock.At(provider, today.AddDays(i), _time);
                        if (at > after)
                        {
                            return at;
                        }
                    }
                    return null;
                }
                case PulseAtType.Sunrise:
                case PulseAtType.Sunset:
                case PulseAtType.Dawn:
                case PulseAtType.Dusk:
                {
                    var snapshot = _location.Current;
                    if (!snapshot.HasLocation)
                    {
                        if (!_warnedLocation)
                        {
                            _warnedLocation = true;
                            Context.Logger.LogWarning($"Pulse at {Context.RuleInstance?.Name}: no server location (latitude/longitude) known yet, sun events wait for it");
                        }
                        return null;
                    }
                    _warnedLocation = false;
                    var sunEvent = Type switch
                    {
                        PulseAtType.Sunrise => SunEvent.Sunrise,
                        PulseAtType.Sunset => SunEvent.Sunset,
                        PulseAtType.Dawn => SunEvent.Dawn,
                        _ => SunEvent.Dusk
                    };

                    IEnumerable<DateOnly> days = OneTime
                        ? (_date.HasValue ? new[] { _date.Value } : Array.Empty<DateOnly>())
                        : Enumerable.Range(-1, 4).Select(i => today.AddDays(i));
                    foreach (var day in days)
                    {
                        var sun = SunTimes.Get(provider, day, sunEvent, snapshot.Latitude!.Value, snapshot.Longitude!.Value);
                        if (sun == null)
                        {
                            continue;
                        }
                        var at = sun.Value.AddMinutes(OffsetMinutes);
                        if (at > after)
                        {
                            return at;
                        }
                    }
                    return null;
                }
                case PulseAtType.Minute:
                {
                    var local = localAfter.DateTime;
                    var minute = new DateTime(local.Year, local.Month, local.Day, local.Hour, local.Minute, 0).AddMinutes(1);
                    return LocalClock.At(provider, DateOnly.FromDateTime(minute), minute.TimeOfDay);
                }
                case PulseAtType.Hour:
                {
                    var local = localAfter.DateTime;
                    var hour = new DateTime(local.Year, local.Month, local.Day, local.Hour, 0, 0).AddHours(1);
                    var at = LocalClock.At(provider, DateOnly.FromDateTime(hour), hour.TimeOfDay);
                    return at > after ? at : at.AddHours(1);
                }
                case PulseAtType.Day:
                    return LocalClock.At(provider, today.AddDays(1), TimeSpan.Zero);
                case PulseAtType.Week:
                {
                    var days = ((int)DayOfWeek.Monday - (int)today.DayOfWeek + 7) % 7;
                    return LocalClock.At(provider, today.AddDays(days == 0 ? 7 : days), TimeSpan.Zero);
                }
                case PulseAtType.Month:
                    return LocalClock.At(provider, new DateOnly(today.Year, today.Month, 1).AddMonths(1), TimeSpan.Zero);
                case PulseAtType.Year:
                    return LocalClock.At(provider, new DateOnly(today.Year + 1, 1, 1), TimeSpan.Zero);
                default:
                    return null;
            }
        }

        private void Arm(DateTimeOffset now)
        {
            var target = now.AddHours(1);
            if (_offAt.HasValue && _offAt.Value < target)
            {
                target = _offAt.Value;
            }
            if (_next.HasValue && _next.Value < target)
            {
                target = _next.Value;
            }
            if (Type is PulseAtType.Sunrise or PulseAtType.Sunset or PulseAtType.Dawn or PulseAtType.Dusk && !_location.Current.HasLocation)
            {
                var poll = now.AddMilliseconds(OperatingModeWatcher.PollMilliseconds);
                if (poll < target)
                {
                    target = poll;
                }
            }

            _timer.Stop();
            _timer.Interval = LocalClock.IntervalUntil(Context.TimeProvider, target);
            _timer.Start();
        }

        private void Dispatch(RuleInterfaceInstance output, object value)
        {
            if (output != null)
            {
                Context.Dispatcher.DispatchValue(new LogicOutputChanged(output, value).Instance, value);
            }
        }
    }
}
