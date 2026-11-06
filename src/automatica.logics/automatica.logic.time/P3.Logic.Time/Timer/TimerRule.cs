using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using Microsoft.Extensions.Logging;

namespace P3.Logic.Time.Timer
{
    public class TimerRule : Automatica.Core.Logic.Logic
    {
        private RuleInterfaceInstance _output;
        private TimerPropertyData _timerPropertyData;
        private readonly System.Timers.Timer _timer;
        private readonly object _lock = new object();

        private bool _running;
        private DateTime? _nextChange;
        private bool _value;

        public TimerRule(ILogicContext context) : base(context)
        {
            _timer = new System.Timers.Timer { AutoReset = false };
            _timer.Elapsed += _timer_Elapsed;
        }

        protected override Task<bool> Start(RuleInstance ruleInstance, CancellationToken token = new CancellationToken())
        {
            // always resolve the interfaces from the instance we are started with - on a reload it is a fresh copy
            var timerProperty = ruleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
                a.This2RuleInterfaceTemplate == TimerLogicFactory.RuleTimerParameter);

            _output = ruleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
                a.This2RuleInterfaceTemplate == TimerLogicFactory.RuleOutput);

            if (timerProperty?.Value is TimerPropertyData timerPropertyData)
            {
                if (timerPropertyData.EnabledDays == null)
                {
                    Context.Logger.LogError("No enabled days found..");
                    return Task.FromResult(false);
                }

                _timerPropertyData = timerPropertyData;
            }
            else
            {
                Context.Logger.LogError("No or invalid timer property found");
                return Task.FromResult(false);
            }

            lock (_lock)
            {
                _running = true;
                _nextChange = null;
                Evaluate(Context.TimeProvider.GetLocalNow().DateTime);
            }

            return base.Start(ruleInstance, token);
        }

        protected override Task<bool> Stop(RuleInstance ruleInstance, CancellationToken token = default)
        {
            lock (_lock)
            {
                _running = false;
                _nextChange = null;
                _timer.Stop();
            }
            return base.Stop(ruleInstance, token);
        }

        private void _timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            lock (_lock)
            {
                if (!_running || _timerPropertyData == null)
                {
                    return;
                }

                var now = Context.TimeProvider.GetLocalNow().DateTime;

                // the tick belongs to the boundary it was scheduled for, even if the timer fires a bit early
                var evaluationTime = _nextChange.HasValue && now < _nextChange.Value ? _nextChange.Value : now;
                Evaluate(evaluationTime);
            }
        }

        /// <summary>
        /// Calculates the output state for <paramref name="evaluationTime"/>, emits it and schedules the next tick at the next start/stop boundary.
        /// </summary>
        private void Evaluate(DateTime evaluationTime)
        {
            _timer.Stop();

            var (active, nextChange) = CalculateState(_timerPropertyData, evaluationTime, Context.TimeProvider.LocalTimeZone);

            _value = active;
            _nextChange = nextChange;

            Context.Logger.LogInformation($"Timer {Context.RuleInstance?.Name}: state at {evaluationTime} is {_value}, next change at {nextChange?.ToString() ?? "never"}");
            Context.Dispatcher.DispatchValue(new LogicOutputChanged(_output, _value).Instance, _value);

            if (nextChange.HasValue)
            {
                var now = Context.TimeProvider.GetLocalNow().DateTime;
                _timer.Interval = TimerHelper.ClampInterval((nextChange.Value - now).TotalMilliseconds);
                _timer.Start();
            }
        }

        /// <summary>
        /// The output is on while <paramref name="now"/> is inside [start, stop) of a window whose start day is enabled.
        /// A stop time before the start time means the window ends on the next day.
        /// </summary>
        /// <param name="data">The timer configuration</param>
        /// <param name="now">The local wall time in <paramref name="timeZone"/></param>
        /// <param name="timeZone">The local time zone</param>
        /// <returns>The state and the local wall time of the next state change (null if the state never changes)</returns>
        internal static (bool active, DateTime? nextChange) CalculateState(TimerPropertyData data, DateTime now, TimeZoneInfo timeZone)
        {
            if (data?.EnabledDays == null)
            {
                return (false, null);
            }

            var startTime = TimeZoneInfo.ConvertTime(data.StartTime, timeZone).TimeOfDay;
            var stopTime = TimeZoneInfo.ConvertTime(data.StopTime, timeZone).TimeOfDay;

            if (startTime == stopTime)
            {
                // empty window
                return (false, null);
            }

            var crossesMidnight = stopTime < startTime;
            var today = now.Date;

            (DateTime start, DateTime end) Window(DateTime day) =>
                (day + startTime, (crossesMidnight ? day.AddDays(1) : day) + stopTime);

            bool IsEnabled(DateTime day) => data.EnabledDays.Contains(day.DayOfWeek);

            // a window that started yesterday can still be running
            foreach (var day in new[] { today.AddDays(-1), today })
            {
                var (start, end) = Window(day);
                if (IsEnabled(day) && start <= now && now < end)
                {
                    return (true, end);
                }
            }

            for (var i = 0; i <= 7; i++)
            {
                var day = today.AddDays(i);
                var (start, _) = Window(day);
                if (IsEnabled(day) && start > now)
                {
                    return (false, start);
                }
            }

            return (false, null);
        }

        protected override IList<ILogicOutputChanged> InputValueChanged(RuleInterfaceInstance instance,
            IDispatchable source, object value)
        {
            if (instance.This2RuleInterfaceTemplate == TimerLogicFactory.RuleReset && TimerHelper.IsTruthy(value))
            {
                // reset: recalculate the state for now, emit it and reschedule the next tick
                lock (_lock)
                {
                    if (_running && _timerPropertyData != null)
                    {
                        Evaluate(Context.TimeProvider.GetLocalNow().DateTime);
                    }
                }
            }

            return new List<ILogicOutputChanged>();
        }
    }
}
