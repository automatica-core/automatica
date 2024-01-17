using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.Calendar;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace P3.Logic.Time.AdvancedTimer
{
    public class AdvancedTimerRule : Automatica.Core.Logic.Logic
    {
        private RuleInterfaceInstance _timerProperty;
        private CalendarPropertyData _timerPropertyData;
        private RuleInterfaceInstance _output;
        private readonly System.Timers.Timer _timer;
        private readonly object _lock = new object();
        
        private readonly TimeProvider _timeProvider = TimeProvider.System;
        

        private bool _value;

        public AdvancedTimerRule(ILogicContext context) : base(context)
        {
           
            _timer = new System.Timers.Timer();

        }

        protected override Task<bool> Start(RuleInstance ruleInstance, CancellationToken token = new CancellationToken())
        {
            _timerProperty = Context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
                a.This2RuleInterfaceTemplate == AdvancedTimerRuleFactory.RuleTimerParameter);

            _output = Context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
                a.This2RuleInterfaceTemplate == AdvancedTimerRuleFactory.RuleOutput);

            
            if (_timerProperty?.Value != null)
            {
                _timerPropertyData = (CalendarPropertyData)_timerProperty.Value;
                Context.Logger.LogInformation($"Timer {Context.RuleInstance.Name}: {_timerProperty?.ValueString}");
            }
            else
            {
                Context.Logger.LogError(_timerProperty == null
                    ? "No timer property found"
                    : $"No timer property value found ({_timerProperty?.ValueString})");
                Context.Logger.LogError("No or invalid timer property found");
                return Task.FromResult(false);
            }

            
            CalculateTickTime();
            


            _timer.Elapsed += _timer_Elapsed;
            _timer.Start();
            return base.Start(ruleInstance, token);
        }

        private void CalculateTickTime()
        { 
            if (_timerPropertyData == null || _timerPropertyData.Value == null)
            {
                return;
            }
            var now = Context.TimeProvider.GetLocalNow();

            Context.Logger.LogInformation($"Now is {now}");

            var list = new List<(DateTime startTime, DateTime endTime)>();
            
            foreach (var entry in _timerPropertyData.Value)
            {
                var values = GetStartEndTime(entry);
                Context.Logger.LogDebug($"Date values are start: {values.startTime} and end: {values.endTime}");
                list.Add(values);
            }
            
            list = list.OrderBy(a => a.startTime).ToList();

            if (list.Any())
            {
                var next = list.MinBy(t => Math.Abs((t.startTime - now).Ticks));

                Context.Logger.LogDebug($"Next is start: {next.startTime} and end: {next.endTime}");
                if (next.startTime.IsToday() || next.endTime.IsToday())
                {
                    CalculateTickTime(next.startTime, next.endTime);
                }
                else
                {
                    _timer.Interval = TimeSpan.FromHours(1).TotalMilliseconds;

                    Context.Logger.LogInformation($"Set value to {false}");
                    Context.Dispatcher.DispatchValue(new LogicOutputChanged(_output, false).Instance, false);
                }
            } 
            else
            {
                _timer.Interval = TimeSpan.FromHours(1).TotalMilliseconds;

                Context.Logger.LogInformation($"Set value to {false}");
                Context.Dispatcher.DispatchValue(new LogicOutputChanged(_output, false).Instance, false);
            }

        }

        private void CalculateTickTime(DateTime start, DateTime end)
        {
            var now = Context.TimeProvider.GetLocalNow();
            var nowTime = now.TimeOfDay;

            Context.Logger.LogInformation($"Now is {nowTime}");

            var isDayDif = start.Date != end.Date;

            var startTime = start.TimeOfDay;
            var stopTime = end.TimeOfDay;


            var tickTime = startTime - nowTime;
            Context.Logger.LogDebug($"Start time is {startTime} endTime is {stopTime} difference is {tickTime}");
            double timerTickTime;
            if (isDayDif)
            {
                if (now > start && now < end)
                {
                    tickTime = (start - now);
                }
                else
                {
                    tickTime = (end - now);
                }
            }

            if (tickTime.TotalMilliseconds < 0)
            {
                if (!isDayDif)
                {
                    if (startTime > stopTime)
                    {
                        timerTickTime = (nowTime - startTime).TotalMilliseconds;
                    }
                    else
                    {
                        timerTickTime = (stopTime - nowTime).TotalMilliseconds;
                    }
                }
                else
                {
                    if (now > start && now < end)
                    {
                        timerTickTime = (now - start).TotalMilliseconds;
                    }
                    else
                    {
                        timerTickTime = (end - now).TotalMilliseconds;
                    }
                }
                if (timerTickTime <= 0)
                {
                    _timer.Interval = TimeSpan.FromMinutes(1).TotalMilliseconds;
                    Context.Logger.LogDebug(
                        $"Timer {Context.RuleInstance.Name}: Next tick time is {_timer.Interval}ms at {stopTime}");

                    Context.Logger.LogInformation($"Set value to {false}");
                    Context.Dispatcher.DispatchValue(new LogicOutputChanged(_output, false).Instance, false);

                    _value = false;
                    return;
                }

                Context.Logger.LogInformation($"Set value to {true}");
                Context.Dispatcher.DispatchValue(new LogicOutputChanged(_output, true).Instance, true);


                _value = true;
            }
            else
            {
                
                
                timerTickTime = tickTime.TotalMilliseconds;

                Context.Logger.LogInformation($"Set value to {false}");
                Context.Dispatcher.DispatchValue(new LogicOutputChanged(_output, false).Instance, false);


                _value = false;
            }

            _timer.Interval = timerTickTime == 0 ? 1 : timerTickTime;
            Context.Logger.LogDebug(
                $"Timer {Context.RuleInstance.Name}: Next tick time is {_timer.Interval}ms at {startTime}");
            _timer.Start();
        }

        private (DateTime startTime, DateTime endTime) GetStartEndTime(CalendarPropertyDataEntry entry)
        {
            var timezone = Context.TimeProvider.LocalTimeZone;
            var localNow = TimeZoneInfo.ConvertTime(Context.TimeProvider.GetUtcNow(), timezone).DateTime;
            
            if (entry.AllDay)
            {
                return (localNow.StartOfDay(), localNow.EndOfDay());
            }

            if (String.IsNullOrEmpty(entry.RecurrenceRule))
            {
                return (TimeZoneInfo.ConvertTime(entry.StartDate, timezone), TimeZoneInfo.ConvertTime(entry.EndDate, timezone));
            }

            var rfcStart = new RFC2445Recur(TimeZoneInfo.ConvertTime(entry.StartDate, timezone), entry.RecurrenceRule);

            if (!rfcStart.Freq.IsValid)
            {
                Context.Logger.LogWarning($"Rule is not valid {entry.RecurrenceRule}");
                return (DateTime.MaxValue, DateTime.MaxValue);
            }

            var startDates = rfcStart.Iterate(Direction.Forward);

            if (startDates != null)
            {
                startDates = startDates.Where(a => a <= localNow || a.StartOfDay().AddDays(-1) == TimeZoneInfo.ConvertTime(entry.StartDate, timezone).StartOfDay()).OrderBy(time => time);
            }

            var endDates = new RFC2445Recur(TimeZoneInfo.ConvertTime(entry.EndDate, timezone), entry.RecurrenceRule).Iterate(Direction.Forward);
            if (endDates != null)
            {
                endDates = endDates.Where(a => a > localNow ||  a.EndOfDay() == TimeZoneInfo.ConvertTime(entry.EndDate, timezone).EndOfDay()).OrderBy(time => time);
            }

            var startDateList = startDates?.ToList();
            var endDateList = endDates?.ToList();

            if (startDateList != null && endDateList != null)
            {
                if (startDateList.Any() && endDateList.Any())
                {
                    var startDate = startDateList.FirstOrDefault();
                    var endDate = endDateList.FirstOrDefault();

                    if (startDate.Date == entry.StartDate.Date)
                    {
                        startDate = new DateTime(DateOnly.FromDateTime(localNow),
                            new TimeOnly(entry.StartDate.Hour, entry.StartDate.Minute, entry.StartDate.Second), DateTimeKind.Utc);
                    }
                    else if (startDate.Date == entry.StartDate.Date.AddDays(-1))
                    {
                        startDate = new DateTime(DateOnly.FromDateTime(localNow),
                            new TimeOnly(entry.StartDate.Hour, entry.StartDate.Minute, entry.StartDate.Second), DateTimeKind.Utc);
                    }
                    else if (startDate.Date < localNow)
                    {
                        startDate = new DateTime(DateOnly.FromDateTime(localNow),
                            new TimeOnly(entry.StartDate.Hour, entry.StartDate.Minute, entry.StartDate.Second), DateTimeKind.Utc);
                    }
                    if (endDate.Date == entry.EndDate.Date)
                    {
                        endDate = new DateTime(DateOnly.FromDateTime(localNow),
                            new TimeOnly(entry.EndDate.Hour, entry.EndDate.Minute, entry.EndDate.Second), DateTimeKind.Utc);
                    }
                    else if (endDate.Date == entry.EndDate.Date.AddDays(1))
                    {
                        endDate = new DateTime(DateOnly.FromDateTime(localNow),
                            new TimeOnly(entry.EndDate.Hour, entry.EndDate.Minute, entry.EndDate.Second), DateTimeKind.Utc);
                    }
                    else if (endDate.Date > localNow)
                    {
                        endDate = new DateTime(DateOnly.FromDateTime(localNow),
                            new TimeOnly(entry.EndDate.Hour, entry.EndDate.Minute, entry.EndDate.Second), DateTimeKind.Utc);
                    }

                    if (startDate > endDate)
                    {
                        startDate = startDate.AddDays(-1);
                    }
                    
                    return (TimeZoneInfo.ConvertTime(startDate, timezone), TimeZoneInfo.ConvertTime(endDate, timezone));
                }
                if (!startDateList.Any() && endDateList.Any())
                {
                    var endDate = endDateList.FirstOrDefault();
                    var startDate = TimeZoneInfo.ConvertTime(entry.StartDate, timezone);
                    return (startDate, endDate);
                }
            }
            return (DateTime.MaxValue, DateTime.MaxValue);
        }

        protected override Task<bool> Stop(RuleInstance ruleInstance, CancellationToken token = default)
        {
            _timer.Elapsed -= _timer_Elapsed;
            _timer.Stop();
            return base.Stop(ruleInstance, token);
        }

        private void _timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            _timer.Stop();
            lock (_lock)
            {
                CalculateTickTime();
            }
        }

        protected override IList<ILogicOutputChanged> InputValueChanged(RuleInterfaceInstance instance,
            IDispatchable source, object value)
        {

            return new List<ILogicOutputChanged>();
        }
    }
}
