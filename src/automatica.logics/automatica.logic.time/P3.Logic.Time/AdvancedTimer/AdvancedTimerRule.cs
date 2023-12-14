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
using P3.Logic.Time.Timer;

namespace P3.Logic.Time.AdvancedTimer
{
    public class AdvancedTimerRule : Automatica.Core.Logic.Logic
    {
        private RuleInterfaceInstance _timerProperty;
        private CalendarPropertyData _timerPropertyData;
        private RuleInterfaceInstance _output;
        private readonly System.Timers.Timer _timer;
        private readonly object _lock = new object();
        

        private bool _value = false;

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
            
            var list = new List<(DateTime startTime, DateTime endTime)>();
            
            foreach (var entry in _timerPropertyData.Value)
            {
                list.Add(GetStartEndTime(entry));
            }
            
            list = list.OrderBy(a => a.startTime).ToList();

            if (list.Any())
            {
                var next = list.First();
                if (next.startTime.IsToday())
                {
                    CalculateTickTime(next.startTime, next.endTime);
                }
                else
                {
                    _timer.Interval = TimeSpan.FromHours(1).TotalMilliseconds;
                }
            }
            else
            {
                _timer.Interval = TimeSpan.FromHours(1).TotalMilliseconds;
            }

        }

        private void CalculateTickTime(DateTime start, DateTime end)
        {
            var now = DateTime.Now;
            var nowTime = now.TimeOfDay;

            Context.Logger.LogInformation($"Now is {nowTime}");


            var startTime = start.TimeOfDay;
            var stopTime = end.TimeOfDay;


            var tickTime = startTime - nowTime;
            Context.Logger.LogDebug($"Start time is {startTime} endTime is {stopTime} difference is {tickTime}");
            double timerTickTime;

            if (tickTime.TotalMilliseconds < 0)
            {
                timerTickTime = (stopTime - nowTime).TotalMilliseconds;
                if (timerTickTime < 0)
                {
                    _timer.Interval = (new DateTime(now.Year, now.Month, now.Day, 23, 59, 59, 99).TimeOfDay - nowTime)
                        .TotalMilliseconds;
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

            _timer.Interval = timerTickTime;
            Context.Logger.LogDebug(
                $"Timer {Context.RuleInstance.Name}: Next tick time is {_timer.Interval}ms at {startTime}");
            _timer.Start();
        }

        private (DateTime startTime, DateTime endTime) GetStartEndTime(CalendarPropertyDataEntry entry)
        {
            if (entry.AllDay)
            {
                return (DateTime.Now.StartOfDay(), DateTime.Now.EndOfDay());
            }

            if (String.IsNullOrEmpty(entry.RecurrenceRule))
            {
                return (entry.StartDate, entry.EndDate);
            }

            var rfcStart = new RFC2445Recur(entry.StartDate, entry.RecurrenceRule);

            if (!rfcStart.Freq.IsValid)
            {
                Context.Logger.LogWarning($"Rule is not valid {entry.RecurrenceRule}");
                return (DateTime.MaxValue, DateTime.MaxValue);
            }

            var startDates = rfcStart.Iterate(Direction.Forward);

            if (startDates != null)
            {
                startDates = startDates.Where(a => a > DateTime.Now).OrderBy(time => time);
            }

            var endDates = new RFC2445Recur(entry.EndDate, entry.RecurrenceRule).Iterate(Direction.Forward);
            if (endDates != null)
            {
                endDates = endDates.Where(a => a > DateTime.Now).OrderBy(time => time);
            }

            var startDateList = startDates?.ToList();
            var endDateList = endDates?.ToList();

            if (startDateList != null && endDateList != null)
            {
                if (startDateList.Any() && endDateList.Any())
                {
                    var startDate = startDateList.FirstOrDefault();
                    var endDate = endDateList.FirstOrDefault();
                    return (startDate, endDate);
                }
                if (!startDateList.Any() && endDateList.Any())
                {
                    var endDate = endDateList.FirstOrDefault();
                    return (DateTime.Now.StartOfDay(), endDate);
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
