using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using Microsoft.Extensions.Logging;
using P3.Logic.Time.OperatingModes;
using P3.Logic.Time.PulseGenerator;

namespace P3.Logic.Time.AlarmClock
{
    /// <summary>
    /// Alarm clock modelled on the Loxone block:
    /// at an alarm time Aon pulses, A and Buzzer go to 1. The alarm ends after the maximum duration (or with Ca / Tg / Off);
    /// with "confirmation required" the snooze starts instead and the alarm rings again until it is confirmed.
    /// S snoozes: Buzzer 0 for the snooze duration, Rst counts down. Pa pulses the pre-alarm time before the alarm.
    /// Alarm times per weekday or per operating mode (mode entries win), see <see cref="AlarmEntries.ForDay"/>.
    /// The entries can be changed from the visu (parameter "alarms"), with Tg (default alarm on/off) and Set (default alarm time,
    /// minutes after midnight); such changes survive restarts.
    /// </summary>
    public class AlarmClockRule : Automatica.Core.Logic.Logic
    {
        private const int LookAheadDays = 8;

        private readonly object _sync = new();
        private readonly Dictionary<Guid, RuleInterfaceInstance> _outputs = new();
        private readonly PersistedParameter _persistedAlarms;
        private readonly OperatingModeWatcher _modes;
        private System.Timers.Timer _timer;
        private bool _started;

        private List<AlarmEntry> _entries = AlarmEntries.CreateDefault();
        private IReadOnlyList<string> _suppressModes = AlarmEntries.ParseModeList(AlarmClockLogicFactory.DefaultSuppressModes);
        internal double MaxDurationSeconds = AlarmClockLogicFactory.DefaultMaxDuration;
        internal double PreAlarmSeconds = AlarmClockLogicFactory.DefaultPreAlarm;
        internal double SnoozeSeconds = AlarmClockLogicFactory.DefaultSnooze;
        internal bool ConfirmRequired;
        private bool _disabled;

        // runtime state
        private DateTimeOffset? _nextAlarm;
        private DateTimeOffset? _lastAlarm;
        private DateTimeOffset? _preAlarmDoneFor;
        private bool _alarmActive;
        private bool _buzzer;
        private DateTimeOffset? _buzzerUntil;
        private DateTimeOffset? _snoozeUntil;
        private int _lastRemaining = -1;
        private bool? _lastDefaultEnabled;
        private DateTime? _lastNextAlarmSent;
        private bool _nextAlarmSent;

        public AlarmClockRule(ILogicContext context) : base(context)
        {
            foreach (var output in new[]
                     {
                         AlarmClockLogicFactory.OutputAlarmStart, AlarmClockLogicFactory.OutputAlarmEnd, AlarmClockLogicFactory.OutputBuzzer,
                         AlarmClockLogicFactory.OutputAlarm, AlarmClockLogicFactory.OutputPreAlarm, AlarmClockLogicFactory.OutputSnoozeRemaining,
                         AlarmClockLogicFactory.OutputDefaultEnabled, AlarmClockLogicFactory.OutputNextAlarm, AlarmClockLogicFactory.OutputEntries
                     })
            {
                _outputs[output] = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a => a.This2RuleInterfaceTemplate == output);
            }

            var alarmsParam = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a => a.This2RuleInterfaceTemplate == AlarmClockLogicFactory.ParamAlarms);
            _persistedAlarms = new PersistedParameter(context.Dispatcher, alarmsParam?.ObjId ?? context.RuleInstance.ObjId, "alarm-clock.alarms");
            _modes = new OperatingModeWatcher(context.Dispatcher);
        }

        internal IReadOnlyList<AlarmEntry> Entries
        {
            get
            {
                lock (_sync)
                {
                    return _entries.ToList();
                }
            }
        }

        internal DateTimeOffset? NextAlarm => _nextAlarm;
        internal bool AlarmActive => _alarmActive;
        internal bool Buzzer => _buzzer;
        internal OperatingModeWatcher Modes => _modes;

        protected override Task<bool> Start(RuleInstance instance, CancellationToken token = default)
        {
            lock (_sync)
            {
                _timer = new System.Timers.Timer { AutoReset = false };
                _timer.Elapsed += _timer_Elapsed;
                _started = true;
            }
            _modes.Refresh();
            return base.Start(instance, token);
        }

        protected override Task<bool> Stop(RuleInstance instance, CancellationToken token = default)
        {
            _modes.Dispose();
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
                    dict[AlarmClockLogicFactory.AlarmsKey] = AlarmEntries.Serialize(_entries);
                    dict["nextAlarm"] = _nextAlarm?.DateTime;
                    dict["alarmActive"] = _alarmActive;
                    dict["buzzer"] = _buzzer;
                    dict["snoozeUntil"] = _snoozeUntil?.DateTime;
                    dict["modes"] = _modes.Current.Names;
                }
            }
            return data;
        }

        protected override void ParameterValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            var template = instance.This2RuleInterfaceTemplate;
            lock (_sync)
            {
                if (template == AlarmClockLogicFactory.ParamMaxDuration)
                {
                    MaxDurationSeconds = Math.Max(0, PulseGeneratorRule.ToSeconds(value, AlarmClockLogicFactory.DefaultMaxDuration));
                }
                else if (template == AlarmClockLogicFactory.ParamPreAlarm)
                {
                    PreAlarmSeconds = Math.Max(0, PulseGeneratorRule.ToSeconds(value, AlarmClockLogicFactory.DefaultPreAlarm));
                }
                else if (template == AlarmClockLogicFactory.ParamSnooze)
                {
                    SnoozeSeconds = Math.Max(1, PulseGeneratorRule.ToSeconds(value, AlarmClockLogicFactory.DefaultSnooze));
                }
                else if (template == AlarmClockLogicFactory.ParamConfirmRequired)
                {
                    ConfirmRequired = TimerHelper.IsTruthy(value);
                }
                else if (template == AlarmClockLogicFactory.ParamSuppressModes)
                {
                    _suppressModes = AlarmEntries.ParseModeList(value);
                }
                else if (template == AlarmClockLogicFactory.ParamAlarms)
                {
                    if (PersistedParameter.IsRuntimeSource(source))
                    {
                        var parsed = AlarmEntries.Parse(value);
                        if (parsed == null)
                        {
                            Context.Logger.LogWarning($"Alarm clock {Context.RuleInstance?.Name}: invalid alarm entries from the visu ignored");
                        }
                        else
                        {
                            SetEntries(parsed, true);
                        }
                    }
                    else
                    {
                        var effective = _persistedAlarms.Configured(value?.ToString());
                        var parsed = AlarmEntries.Parse(effective) ?? AlarmEntries.Parse(value) ?? AlarmEntries.CreateDefault();
                        SetEntries(parsed, false);
                    }
                    return;
                }
                else
                {
                    return;
                }

                Reschedule();
            }
        }

        private void SetEntries(List<AlarmEntry> entries, bool persist)
        {
            _entries = entries;
            var json = AlarmEntries.Serialize(entries);
            if (persist)
            {
                _persistedAlarms.Changed(json);
            }

            ParameterValues[AlarmClockLogicFactory.AlarmsKey] = json;
            Dispatch(AlarmClockLogicFactory.OutputEntries, json);
            SendDefaultEnabled();
            Reschedule();
        }

        private AlarmEntry DefaultEntry => _entries.FirstOrDefault(a => a.IsDefault);

        private void SendDefaultEnabled()
        {
            var enabled = DefaultEntry?.Enabled ?? false;
            if (_lastDefaultEnabled != enabled)
            {
                _lastDefaultEnabled = enabled;
                Dispatch(AlarmClockLogicFactory.OutputDefaultEnabled, enabled);
            }
        }

        protected override IList<ILogicOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            var template = instance.This2RuleInterfaceTemplate;
            var truthy = TimerHelper.IsTruthy(value);

            lock (_sync)
            {
                if (template == AlarmClockLogicFactory.InputDisable)
                {
                    _disabled = truthy;
                    Reschedule();
                }
                else if (template == AlarmClockLogicFactory.InputConfirm && truthy)
                {
                    EndAlarm();
                    Reschedule();
                }
                else if (template == AlarmClockLogicFactory.InputSnooze && truthy)
                {
                    if (_alarmActive)
                    {
                        StartSnooze();
                        Reschedule();
                    }
                }
                else if (template == AlarmClockLogicFactory.InputToggle && truthy)
                {
                    if (_alarmActive)
                    {
                        EndAlarm();
                    }
                    else
                    {
                        var entries = _entries.ToList();
                        var def = entries.FirstOrDefault(a => a.IsDefault);
                        if (def == null)
                        {
                            def = AlarmEntries.CreateDefault()[0];
                            def.Enabled = false;
                            entries.Insert(0, def);
                        }
                        def.Enabled = !def.Enabled;
                        SetEntries(entries, true);
                    }
                    Reschedule();
                }
                else if (template == AlarmClockLogicFactory.InputSet && value != null)
                {
                    var minutes = PulseGeneratorRule.ToSeconds(value, double.NaN);
                    if (!double.IsNaN(minutes))
                    {
                        var clamped = Math.Clamp((int)Math.Round(minutes), 0, 1439);
                        var entries = _entries.ToList();
                        var def = entries.FirstOrDefault(a => a.IsDefault);
                        if (def == null)
                        {
                            def = AlarmEntries.CreateDefault()[0];
                            entries.Insert(0, def);
                        }
                        def.Time = AlarmEntries.FormatTime(TimeSpan.FromMinutes(clamped));
                        SetEntries(entries, true);
                    }
                }
                else if (template == AlarmClockLogicFactory.InputOff && truthy)
                {
                    EndAlarm();
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

        /// <summary>
        /// Brings the outputs up to date with the clock and arms the timer for the next event
        /// </summary>
        private void Evaluate()
        {
            var now = Context.TimeProvider.GetUtcNow();
            _modes.Refresh();

            if (_alarmActive)
            {
                if (_snoozeUntil.HasValue)
                {
                    if (now >= _snoozeUntil.Value)
                    {
                        _snoozeUntil = null;
                        SendRemaining(0);
                        StartBuzzer(now);
                    }
                    else
                    {
                        SendRemaining((int)Math.Ceiling((_snoozeUntil.Value - now).TotalSeconds));
                    }
                }
                else if (_buzzer && _buzzerUntil.HasValue && now >= _buzzerUntil.Value)
                {
                    if (ConfirmRequired)
                    {
                        StartSnooze();
                    }
                    else
                    {
                        EndAlarm();
                    }
                }
            }

            if (_nextAlarm.HasValue && !_disabled)
            {
                var preAlarm = _nextAlarm.Value.AddSeconds(-PreAlarmSeconds);
                if (PreAlarmSeconds > 0 && now >= preAlarm && now < _nextAlarm.Value && _preAlarmDoneFor != _nextAlarm)
                {
                    _preAlarmDoneFor = _nextAlarm;
                    Pulse(AlarmClockLogicFactory.OutputPreAlarm);
                }

                if (now >= _nextAlarm.Value)
                {
                    _lastAlarm = _nextAlarm;
                    _preAlarmDoneFor = _nextAlarm;
                    StartAlarm(now);
                }
            }

            ComputeNextAlarm(now);
            Arm(now);
        }

        private void Reschedule()
        {
            if (!_started || _timer == null)
            {
                return;
            }
            var now = Context.TimeProvider.GetUtcNow();
            ComputeNextAlarm(now);
            Arm(now);
        }

        private void ComputeNextAlarm(DateTimeOffset now)
        {
            DateTimeOffset? next = null;
            if (!_disabled)
            {
                var today = LocalClock.Today(Context.TimeProvider);
                for (var i = 0; i <= LookAheadDays && next == null; i++)
                {
                    var date = today.AddDays(i);
                    var active = _modes.Current.ActiveOn(date, today);
                    foreach (var entry in AlarmEntries.ForDay(_entries, date, active, _suppressModes))
                    {
                        var at = LocalClock.At(Context.TimeProvider, date, entry.TimeOfDay!.Value);
                        if (at <= now || (_lastAlarm.HasValue && at <= _lastAlarm.Value))
                        {
                            continue;
                        }
                        if (next == null || at < next)
                        {
                            next = at;
                        }
                    }
                }
            }

            _nextAlarm = next;
            var local = next?.DateTime;
            if (!_nextAlarmSent || local != _lastNextAlarmSent)
            {
                _nextAlarmSent = true;
                _lastNextAlarmSent = local;
                Dispatch(AlarmClockLogicFactory.OutputNextAlarm, local);
            }
        }

        private void Arm(DateTimeOffset now)
        {
            var candidates = new List<DateTimeOffset>();
            if (_alarmActive)
            {
                if (_snoozeUntil.HasValue)
                {
                    // Rst counts down once per second
                    candidates.Add(now.AddSeconds(1) < _snoozeUntil.Value ? now.AddSeconds(1) : _snoozeUntil.Value);
                }
                else if (_buzzer && _buzzerUntil.HasValue)
                {
                    candidates.Add(_buzzerUntil.Value);
                }
            }

            if (_nextAlarm.HasValue)
            {
                var preAlarm = _nextAlarm.Value.AddSeconds(-PreAlarmSeconds);
                if (PreAlarmSeconds > 0 && preAlarm > now && _preAlarmDoneFor != _nextAlarm)
                {
                    candidates.Add(preAlarm);
                }
                else if (PreAlarmSeconds > 0 && _preAlarmDoneFor != _nextAlarm)
                {
                    candidates.Add(now); // inside the pre-alarm window already
                }
                candidates.Add(_nextAlarm.Value);
            }

            // the next local midnight: operating modes and weekdays change
            var tomorrow = LocalClock.Today(Context.TimeProvider).AddDays(1);
            candidates.Add(LocalClock.At(Context.TimeProvider, tomorrow, TimeSpan.FromSeconds(1)));
            // operating modes are polled
            candidates.Add(now.AddMilliseconds(OperatingModeWatcher.PollMilliseconds));

            var target = candidates.Min();
            _timer.Stop();
            _timer.Interval = LocalClock.IntervalUntil(Context.TimeProvider, target);
            _timer.Start();
        }

        private void StartAlarm(DateTimeOffset now)
        {
            if (!_alarmActive)
            {
                _alarmActive = true;
                Dispatch(AlarmClockLogicFactory.OutputAlarm, true);
            }
            _snoozeUntil = null;
            SendRemaining(0);
            Pulse(AlarmClockLogicFactory.OutputAlarmStart);
            StartBuzzer(now);
        }

        private void StartBuzzer(DateTimeOffset now)
        {
            _buzzerUntil = MaxDurationSeconds > 0 ? now.AddSeconds(MaxDurationSeconds) : null;
            if (!_buzzer)
            {
                _buzzer = true;
                Dispatch(AlarmClockLogicFactory.OutputBuzzer, true);
            }
        }

        private void StartSnooze()
        {
            var now = Context.TimeProvider.GetUtcNow();
            _snoozeUntil = now.AddSeconds(SnoozeSeconds);
            _buzzerUntil = null;
            if (_buzzer)
            {
                _buzzer = false;
                Dispatch(AlarmClockLogicFactory.OutputBuzzer, false);
            }
            SendRemaining((int)Math.Ceiling(SnoozeSeconds));
        }

        private void EndAlarm()
        {
            var wasActive = _alarmActive;
            _snoozeUntil = null;
            _buzzerUntil = null;
            if (_buzzer)
            {
                _buzzer = false;
                Dispatch(AlarmClockLogicFactory.OutputBuzzer, false);
            }
            SendRemaining(0);
            if (wasActive)
            {
                _alarmActive = false;
                Dispatch(AlarmClockLogicFactory.OutputAlarm, false);
                Pulse(AlarmClockLogicFactory.OutputAlarmEnd);
            }
        }

        private void SendRemaining(int seconds)
        {
            if (seconds != _lastRemaining)
            {
                _lastRemaining = seconds;
                Dispatch(AlarmClockLogicFactory.OutputSnoozeRemaining, seconds);
            }
        }

        private void Pulse(Guid output)
        {
            Dispatch(output, true);
            Dispatch(output, false);
        }

        private void Dispatch(Guid output, object value)
        {
            if (_outputs.TryGetValue(output, out var instance) && instance != null)
            {
                Context.Dispatcher.DispatchValue(new LogicOutputChanged(instance, value).Instance, value);
            }
        }
    }
}
