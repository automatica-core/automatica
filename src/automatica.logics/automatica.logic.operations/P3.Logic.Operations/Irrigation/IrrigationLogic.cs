using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using Newtonsoft.Json;
using P3.Logic.Operations.Garage;
using F = P3.Logic.Operations.Irrigation.IrrigationLogicFactory;

namespace P3.Logic.Operations.Irrigation
{
    /// <summary>
    /// One entry of the irrigation history shown in the visualization
    /// </summary>
    public class IrrigationHistoryEntry
    {
        /// <summary>UTC time</summary>
        public DateTime At { get; set; }
        /// <summary>start, manual, skip, zone, done, stop, select</summary>
        public string Kind { get; set; }
        public int Zone { get; set; }
        public double Factor { get; set; }
        public double RainForecast { get; set; }
        public double RainSeconds { get; set; }
    }

    /// <summary>
    /// Irrigation of up to 8 zones. A run opens the zones one after the other for their time, the pump runs while a valve is
    /// open. Act (and the schedule) only starts when the rain of the last 24 hours and the rain forecast stay below their
    /// limits, optionally the zone times are shortened by the share of the rain in the limits. Start (and the visualization)
    /// always starts.
    /// </summary>
    public class IrrigationLogic : DispatchingLogic
    {
        internal const int ZoneCount = 8;
        internal const int AllValves = 9;
        private const int MaxHistory = 20;
        internal static readonly TimeSpan RainWindow = TimeSpan.FromHours(24);
        /// <summary>
        /// a schedule start is caught up this long after its time (e.g. after a restart)
        /// </summary>
        internal static readonly TimeSpan ScheduleGrace = TimeSpan.FromHours(1);

        private readonly System.Timers.Timer _timer = new() { AutoReset = false };
        private readonly System.Timers.Timer _clockTimer = new(30000) { AutoReset = true };

        internal readonly int[] Durations = { 600, 600, 600, 600, 0, 0, 0, 0 };
        private readonly string[] _zoneNames = new string[ZoneCount];
        internal double MaxRainForecast = 2;
        internal int MaxRainSeconds = 1800;
        internal bool AdjustByRain;
        internal TimerPropertyData Schedule;
        internal bool ScheduleEnabled;

        private bool _running;
        /// <summary>true for a run through the zones, false for a selected valve</summary>
        private bool _sequence;
        private int _active;
        private int _zone = -1;
        private double _factor = 1;
        private DateTimeOffset? _zoneEnd;
        private bool _skipped;
        private bool _locked;
        private double? _forecast;
        private DateTimeOffset? _rainSince;
        private readonly List<(DateTimeOffset Start, DateTimeOffset End)> _rain = new();
        private DateTime? _lastScheduledDay;
        private readonly LinkedList<IrrigationHistoryEntry> _history = new();

        public IrrigationLogic(ILogicContext context) : base(context)
        {
            _timer.Elapsed += _timer_Elapsed;
            _clockTimer.Elapsed += _clockTimer_Elapsed;
        }

        internal int Active => _active;
        internal bool Running => _running;
        internal double Factor => _factor;
        internal IReadOnlyCollection<IrrigationHistoryEntry> History => _history;

        protected override Task<bool> Start(RuleInstance instance, CancellationToken token = default)
        {
            lock (Sync)
            {
                SendOutputs();
                Send(F.OutputSkipped, false);
                Send(F.OutputRainTime, 0.0);
                _clockTimer.Start();
            }
            return base.Start(instance, token);
        }

        protected override Task<bool> Stop(RuleInstance instance, CancellationToken token = default)
        {
            lock (Sync)
            {
                _timer.Stop();
                _clockTimer.Stop();
            }
            return base.Stop(instance, token);
        }

        protected override void ParameterValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            lock (Sync)
            {
                var template = TemplateOf(instance);
                var durationIndex = Array.IndexOf(F.ParamDurations, template);
                if (durationIndex >= 0)
                {
                    Durations[durationIndex] = Math.Max(0, LogicValue.ToInt(value, 0));
                }
                else if (template == F.ParamMaxRainForecast)
                {
                    MaxRainForecast = Math.Max(0, LogicValue.ToDouble(value, 2));
                }
                else if (template == F.ParamMaxRainTime)
                {
                    MaxRainSeconds = Math.Max(0, LogicValue.ToInt(value, 1800));
                }
                else if (template == F.ParamAdjustByRain)
                {
                    AdjustByRain = LogicValue.IsTruthy(value);
                }
                else if (template == F.ParamScheduleEnabled)
                {
                    ScheduleEnabled = LogicValue.IsTruthy(value);
                }
                else if (template == F.ParamSchedule)
                {
                    Schedule = ParseSchedule(value);
                }
                else if (template == F.ParamZoneNames)
                {
                    var names = LogicValue.ToText(value).Split(';');
                    for (var i = 0; i < ZoneCount; i++)
                    {
                        _zoneNames[i] = i < names.Length ? names[i].Trim() : "";
                    }
                }
            }
        }

        internal static TimerPropertyData ParseSchedule(object value)
        {
            switch (value)
            {
                case TimerPropertyData data:
                    return data;
                case string json when !string.IsNullOrWhiteSpace(json):
                    try
                    {
                        return JsonConvert.DeserializeObject<TimerPropertyData>(json);
                    }
                    catch (Exception)
                    {
                        return null;
                    }
                default:
                    return null;
            }
        }

        private DateTimeOffset Now => Context.TimeProvider.GetUtcNow();

        protected override IList<ILogicOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            lock (Sync)
            {
                var template = TemplateOf(instance);
                var on = LogicValue.IsTruthy(value);
                if (template == F.InputLock)
                {
                    _locked = on;
                    if (on)
                    {
                        StopAll("stop");
                    }
                }
                else if (template == F.InputRain)
                {
                    Rain(on);
                }
                else if (template == F.InputRainForecast)
                {
                    _forecast = LogicValue.ToDouble(value);
                }
                else if (_locked)
                {
                    // locked: no commands
                }
                else if (template == F.InputSelect)
                {
                    var valve = LogicValue.ToInt(value, -1);
                    if (valve == 0)
                    {
                        StopAll("stop");
                    }
                    else if (valve is > 0 and <= AllValves)
                    {
                        Select(valve);
                    }
                }
                else if (on && template == F.InputActivate)
                {
                    Activate();
                }
                else if (on && template == F.InputStart)
                {
                    StartRun(1, "manual");
                }
                else if (on && template == F.InputStop)
                {
                    StopAll("stop");
                }
            }
            return new List<ILogicOutputChanged>();
        }

        private void Rain(bool raining)
        {
            var now = Now;
            if (raining && !_rainSince.HasValue)
            {
                _rainSince = now;
            }
            else if (!raining && _rainSince.HasValue)
            {
                _rain.Add((_rainSince.Value, now));
                _rainSince = null;
            }
            SendRainTime();
        }

        /// <summary>
        /// How long it rained in the last 24 hours
        /// </summary>
        internal double RainSeconds()
        {
            var now = Now;
            var from = now - RainWindow;
            _rain.RemoveAll(a => a.End <= from);
            var seconds = _rain.Sum(a => (a.End - (a.Start < from ? from : a.Start)).TotalSeconds);
            if (_rainSince.HasValue)
            {
                seconds += (now - (_rainSince.Value < from ? from : _rainSince.Value)).TotalSeconds;
            }
            return Math.Max(0, seconds);
        }

        /// <summary>
        /// Share of the zone times for a run started by Act: 0 = skip, 1 = full time
        /// </summary>
        internal double RainFactor(out bool tooMuchRain)
        {
            var forecast = Math.Max(0, _forecast ?? 0);
            var rain = RainSeconds();
            tooMuchRain = forecast > MaxRainForecast || rain > MaxRainSeconds;
            if (tooMuchRain)
            {
                return 0;
            }
            if (!AdjustByRain)
            {
                return 1;
            }
            var forecastShare = MaxRainForecast > 0 ? forecast / MaxRainForecast : 0;
            var rainShare = MaxRainSeconds > 0 ? rain / MaxRainSeconds : 0;
            return Math.Clamp(1 - Math.Max(forecastShare, rainShare), 0, 1);
        }

        private void Activate()
        {
            var factor = RainFactor(out var tooMuchRain);
            if (tooMuchRain || Durations.All(a => a * factor < 1))
            {
                _skipped = true;
                Send(F.OutputSkipped, true);
                AddHistory("skip", 0);
                return;
            }
            StartRun(factor, "start");
        }

        private void StartRun(double factor, string kind)
        {
            StopValves();
            _skipped = false;
            Send(F.OutputSkipped, false);
            _factor = factor;
            _running = true;
            _sequence = true;
            _zone = -1;
            AddHistory(kind, 0);
            NextZone();
        }

        private void NextZone()
        {
            var next = -1;
            for (var i = _zone + 1; i < ZoneCount; i++)
            {
                if (Durations[i] * _factor >= 1)
                {
                    next = i;
                    break;
                }
            }
            if (next < 0)
            {
                StopAll("done");
                return;
            }
            _zone = next;
            OpenValve(next + 1, Durations[next] * _factor);
            AddHistory("zone", next + 1);
        }

        private void Select(int valve)
        {
            StopValves();
            _running = true;
            _sequence = false;
            _zone = valve == AllValves ? ZoneCount : valve - 1;
            _factor = 1;
            // a zone without time and "all valves" run for the longest zone time (or 10 minutes)
            var longest = Durations.Max();
            var seconds = valve == AllValves || Durations[valve - 1] == 0 ? (longest > 0 ? longest : 600) : Durations[valve - 1];
            OpenValve(valve, seconds);
            AddHistory("select", valve);
        }

        private void OpenValve(int valve, double seconds)
        {
            // the next valve opens before the previous one closes, the pump never runs against closed valves
            for (var i = 0; i < ZoneCount; i++)
            {
                if (valve == AllValves || i == valve - 1)
                {
                    Send(F.OutputValves[i], true);
                }
            }
            for (var i = 0; i < ZoneCount; i++)
            {
                if (valve != AllValves && i != valve - 1)
                {
                    Send(F.OutputValves[i], false);
                }
            }
            _active = valve;
            Send(F.OutputPump, true);
            _zoneEnd = Now.AddSeconds(seconds);
            _timer.Stop();
            _timer.Interval = LogicValue.TimerInterval(seconds * 1000);
            _timer.Start();
            SendOutputs();
        }

        private void StopValves()
        {
            _timer.Stop();
            Send(F.OutputPump, false);
            foreach (var valve in F.OutputValves)
            {
                Send(valve, false);
            }
            _active = 0;
            _zoneEnd = null;
        }

        private void StopAll(string kind)
        {
            var wasRunning = _running;
            StopValves();
            _running = false;
            _zone = -1;
            SendOutputs();
            if (wasRunning)
            {
                AddHistory(kind, 0);
            }
        }

        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            lock (Sync)
            {
                _timer.Stop();
                if (!_running)
                {
                    return;
                }
                if (_sequence)
                {
                    NextZone();
                }
                else
                {
                    StopAll("done");
                }
            }
        }

        private void _clockTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            lock (Sync)
            {
                SendRainTime();
                SendRemaining();
                CheckSchedule();
            }
        }

        private void CheckSchedule()
        {
            if (!ScheduleEnabled || Schedule?.EnabledDays == null || _locked || _running)
            {
                return;
            }
            var zone = Context.TimeProvider.LocalTimeZone;
            var local = TimeZoneInfo.ConvertTime(Now, zone).DateTime;
            var start = TimeZoneInfo.ConvertTime(Schedule.StartTime, zone).TimeOfDay;
            var due = local.Date + start;
            if (_lastScheduledDay == local.Date || !Schedule.EnabledDays.Contains(local.DayOfWeek) || local < due || local >= due + ScheduleGrace)
            {
                return;
            }
            _lastScheduledDay = local.Date;
            Activate();
        }

        private void SendRainTime()
        {
            Send(F.OutputRainTime, Math.Round(RainSeconds()));
        }

        internal double RemainingSeconds => _zoneEnd.HasValue ? Math.Max(0, Math.Round((_zoneEnd.Value - Now).TotalSeconds)) : 0;

        private void SendRemaining()
        {
            Send(F.OutputRemaining, RemainingSeconds);
        }

        private void SendOutputs()
        {
            Send(F.OutputActiveValve, _active);
            Send(F.OutputRunning, _running);
            SendRemaining();
        }

        private void AddHistory(string kind, int zone)
        {
            _history.AddFirst(new IrrigationHistoryEntry
            {
                At = Now.UtcDateTime,
                Kind = kind,
                Zone = zone,
                Factor = Math.Round(_factor, 2),
                RainForecast = Math.Round(Math.Max(0, _forecast ?? 0), 1),
                RainSeconds = Math.Round(RainSeconds())
            });
            while (_history.Count > MaxHistory)
            {
                _history.RemoveLast();
            }
        }

        public override object GetDataForVisu()
        {
            lock (Sync)
            {
                var data = SentValuesForVisu();
                data["running"] = _running;
                data["activeValve"] = _active;
                data["remaining"] = RemainingSeconds;
                data["factor"] = Math.Round(_factor, 2);
                data["skipped"] = _skipped;
                data["locked"] = _locked;
                data["rainSeconds"] = Math.Round(RainSeconds());
                data["raining"] = _rainSince.HasValue;
                data["rainForecast"] = _forecast;
                data["maxRainForecast"] = MaxRainForecast;
                data["maxRainSeconds"] = MaxRainSeconds;
                data["zoneNames"] = _zoneNames.Select((a, i) => string.IsNullOrEmpty(a) ? $"{i + 1}" : a).ToArray();
                data["durations"] = Durations.ToArray();
                data["scheduleEnabled"] = ScheduleEnabled && Schedule?.EnabledDays != null;
                data["scheduleTime"] = Schedule == null ? null : TimeZoneInfo.ConvertTime(Schedule.StartTime, Context.TimeProvider.LocalTimeZone).ToString("HH:mm");
                data["scheduleDays"] = Schedule?.EnabledDays?.Select(a => (int)a).ToArray();
                data["history"] = _history.ToArray();
                return data;
            }
        }
    }
}
