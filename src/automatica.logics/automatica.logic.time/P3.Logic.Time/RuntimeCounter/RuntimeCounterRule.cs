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
using Newtonsoft.Json;
using P3.Logic.Time.PulseAt;

namespace P3.Logic.Time.RuntimeCounter
{
    /// <summary>
    /// Runtime counter: while En = 1 the operating time is counted. To = total time, Rtm = time left until the maintenance interval,
    /// Me = 1 when the interval is reached (interval 0 = not used), Lst = last start. Rmc starts a new maintenance interval,
    /// Rto resets everything. Off = 1 pauses the counting. The counters survive restarts (saved every 5 minutes while running
    /// and on every change), the time unit only applies to the outputs.
    /// </summary>
    public class RuntimeCounterRule : Automatica.Core.Logic.Logic
    {
        internal const double UpdateSeconds = 60;
        internal const double SaveSeconds = 300;

        private readonly object _sync = new();
        private readonly RuleInterfaceInstance _maintenance;
        private readonly RuleInterfaceInstance _total;
        private readonly RuleInterfaceInstance _lastStart;
        private readonly RuleInterfaceInstance _remaining;
        private readonly RemanentValue _store;
        private System.Timers.Timer _timer;
        private bool _started;

        internal RuntimeUnit Unit = RuntimeUnit.Hours;
        internal double IntervalHours;

        private State _state = new();
        private bool _enabled;
        private bool _locked;
        private DateTimeOffset? _runningSince;
        private DateTimeOffset _lastSave = DateTimeOffset.MinValue;
        private bool? _lastMaintenance;

        public RuntimeCounterRule(ILogicContext context) : base(context)
        {
            _maintenance = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a => a.This2RuleInterfaceTemplate == RuntimeCounterLogicFactory.OutputMaintenance);
            _total = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a => a.This2RuleInterfaceTemplate == RuntimeCounterLogicFactory.OutputTotal);
            _lastStart = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a => a.This2RuleInterfaceTemplate == RuntimeCounterLogicFactory.OutputLastStart);
            _remaining = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a => a.This2RuleInterfaceTemplate == RuntimeCounterLogicFactory.OutputRemaining);
            _store = new RemanentValue(context.Dispatcher, _total?.ObjId ?? context.RuleInstance.ObjId, "runtime-counter.state");
        }

        internal double TotalSeconds
        {
            get
            {
                lock (_sync)
                {
                    return CurrentTotal(Context.TimeProvider.GetUtcNow());
                }
            }
        }

        internal bool Running => _runningSince.HasValue;

        protected override Task<bool> Start(RuleInstance instance, CancellationToken token = default)
        {
            lock (_sync)
            {
                _state = Load();
                _timer = new System.Timers.Timer { AutoReset = false };
                _timer.Elapsed += _timer_Elapsed;
                _started = true;
                _runningSince = null;
                _lastMaintenance = null;
            }
            return base.Start(instance, token);
        }

        protected override Task<bool> Stop(RuleInstance instance, CancellationToken token = default)
        {
            lock (_sync)
            {
                if (_started)
                {
                    Accumulate(Context.TimeProvider.GetUtcNow());
                    Save(Context.TimeProvider.GetUtcNow());
                }
                _started = false;
                _runningSince = null;
                if (_timer != null)
                {
                    _timer.Elapsed -= _timer_Elapsed;
                    _timer.Dispose();
                    _timer = null;
                }
            }
            return base.Stop(instance, token);
        }

        protected override void ParameterValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            lock (_sync)
            {
                if (instance.This2RuleInterfaceTemplate == RuntimeCounterLogicFactory.ParamUnit)
                {
                    Unit = ParseUnit(value);
                }
                else if (instance.This2RuleInterfaceTemplate == RuntimeCounterLogicFactory.ParamInterval)
                {
                    IntervalHours = Math.Max(0, ParseDouble(value));
                }
                else
                {
                    return;
                }

                if (_started)
                {
                    SendOutputs(Context.TimeProvider.GetUtcNow(), true);
                }
            }
        }

        internal static RuntimeUnit ParseUnit(object value)
        {
            var type = (int)PulseAtRule.ParseType(value); // same integer parsing, unknown values fall back to 0
            return Enum.IsDefined(typeof(RuntimeUnit), type) ? (RuntimeUnit)type : RuntimeUnit.Seconds;
        }

        private static double ParseDouble(object value)
        {
            try
            {
                var result = value is string s
                    ? double.Parse(s, NumberStyles.Float, CultureInfo.InvariantCulture)
                    : Convert.ToDouble(value, CultureInfo.InvariantCulture);
                return double.IsNaN(result) || double.IsInfinity(result) ? 0 : result;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        protected override IList<ILogicOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            var template = instance.This2RuleInterfaceTemplate;
            var truthy = TimerHelper.IsTruthy(value);

            lock (_sync)
            {
                if (!_started)
                {
                    return new List<ILogicOutputChanged>();
                }

                var now = Context.TimeProvider.GetUtcNow();
                if (template == RuntimeCounterLogicFactory.InputEnable)
                {
                    _enabled = truthy;
                    UpdateRunning(now);
                }
                else if (template == RuntimeCounterLogicFactory.InputOff)
                {
                    _locked = truthy;
                    UpdateRunning(now);
                }
                else if (template == RuntimeCounterLogicFactory.InputResetMaintenance && truthy)
                {
                    Accumulate(now);
                    _state.MaintenanceBase = _state.TotalSeconds;
                    Save(now);
                    SendOutputs(now, true);
                }
                else if (template == RuntimeCounterLogicFactory.InputResetTotal && truthy)
                {
                    Accumulate(now);
                    _state.TotalSeconds = 0;
                    _state.MaintenanceBase = 0;
                    Save(now);
                    SendOutputs(now, true);
                }
            }

            return new List<ILogicOutputChanged>();
        }

        private void UpdateRunning(DateTimeOffset now)
        {
            var shouldRun = _enabled && !_locked;
            if (shouldRun && !_runningSince.HasValue)
            {
                _runningSince = now;
                _state.LastStart = now;
                Save(now);
                SendOutputs(now, true);
                Arm();
            }
            else if (!shouldRun && _runningSince.HasValue)
            {
                Accumulate(now);
                _runningSince = null;
                Save(now);
                SendOutputs(now, true);
                _timer?.Stop();
            }
        }

        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            lock (_sync)
            {
                if (!_started || _timer == null || !_runningSince.HasValue)
                {
                    return;
                }

                var now = Context.TimeProvider.GetUtcNow();
                Accumulate(now);
                if (now - _lastSave >= TimeSpan.FromSeconds(SaveSeconds))
                {
                    Save(now);
                }
                SendOutputs(now, false);
                Arm();
            }
        }

        private void Arm()
        {
            if (_timer == null)
            {
                return;
            }
            _timer.Stop();
            _timer.Interval = UpdateSeconds * 1000;
            _timer.Start();
        }

        private void Accumulate(DateTimeOffset now)
        {
            if (_runningSince.HasValue)
            {
                var elapsed = (now - _runningSince.Value).TotalSeconds;
                if (elapsed > 0)
                {
                    _state.TotalSeconds += elapsed;
                }
                _runningSince = now;
            }
        }

        private double CurrentTotal(DateTimeOffset now)
        {
            var total = _state.TotalSeconds;
            if (_runningSince.HasValue)
            {
                total += Math.Max(0, (now - _runningSince.Value).TotalSeconds);
            }
            return total;
        }

        internal double ToUnit(double seconds)
        {
            var value = Unit switch
            {
                RuntimeUnit.Minutes => seconds / 60,
                RuntimeUnit.Hours => seconds / 3600,
                RuntimeUnit.Days => seconds / 86400,
                _ => seconds
            };
            return Math.Round(value, 3, MidpointRounding.AwayFromZero);
        }

        private void SendOutputs(DateTimeOffset now, bool includeStatic)
        {
            var total = CurrentTotal(now);
            Dispatch(_total, ToUnit(total));

            var intervalSeconds = IntervalHours * 3600;
            var used = total - _state.MaintenanceBase;
            var maintenance = intervalSeconds > 0 && used >= intervalSeconds;
            Dispatch(_remaining, intervalSeconds > 0 ? ToUnit(Math.Max(0, intervalSeconds - used)) : 0d);

            if (_lastMaintenance != maintenance)
            {
                _lastMaintenance = maintenance;
                Dispatch(_maintenance, maintenance);
            }

            if (includeStatic)
            {
                Dispatch(_lastStart, _state.LastStart.HasValue ? TimeZoneInfo.ConvertTime(_state.LastStart.Value, Context.TimeProvider.LocalTimeZone).DateTime : null);
            }
        }

        private State Load()
        {
            var text = _store.Load();
            if (string.IsNullOrEmpty(text))
            {
                return new State();
            }
            try
            {
                return JsonConvert.DeserializeObject<State>(text) ?? new State();
            }
            catch (JsonException)
            {
                return new State();
            }
        }

        private void Save(DateTimeOffset now)
        {
            _lastSave = now;
            _store.Save(JsonConvert.SerializeObject(_state));
        }

        private void Dispatch(RuleInterfaceInstance output, object value)
        {
            if (output != null)
            {
                Context.Dispatcher.DispatchValue(new LogicOutputChanged(output, value).Instance, value);
            }
        }

        internal sealed class State
        {
            public double TotalSeconds { get; set; }
            public double MaintenanceBase { get; set; }
            public DateTimeOffset? LastStart { get; set; }
        }
    }
}
