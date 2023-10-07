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
        private readonly RuleInterfaceInstance _timerProperty;
        private TimerPropertyData _timerPropertyData;
        private readonly RuleInterfaceInstance _output;
        private readonly System.Timers.Timer _timer;
        private readonly object _lock = new object();
        

        private bool _value = false;

        public TimerRule(ILogicContext context) : base(context)
        {
            _timerProperty = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
                a.This2RuleInterfaceTemplate == TimerLogicFactory.RuleTimerParameter);

            _output = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
                a.This2RuleInterfaceTemplate == TimerLogicFactory.RuleOutput);

            _timer = new System.Timers.Timer();

        }

        protected override Task<bool> Start(RuleInstance ruleInstance, CancellationToken token = new CancellationToken())
        {
            if (_timerProperty?.Value != null)
            {
                _timerPropertyData = (TimerPropertyData)_timerProperty.Value;

                if (_timerPropertyData.EnabledDays == null)
                {
                    Context.Logger.LogError("No enabled days found..");
                    return Task.FromResult(false);
                }
            }
            else
            {
                Context.Logger.LogError("No or invalid timer property found");
                return Task.FromResult(false);
            }

            CalculateTickTime(true);

            
            _timer.Elapsed += _timer_Elapsed;
            _timer.Start();
            return base.Start(ruleInstance, token);
        }

        private void CalculateTickTime(bool isStartup=false)
        {
            var now = DateTime.UtcNow;
            var nowTime = now.TimeOfDay;

            Context.Logger.LogInformation($"Now is {nowTime}");
            

            if (!_timerPropertyData.EnabledDays.Contains(now.DayOfWeek))
            {
                Context.Logger.LogDebug($"Timer {Context.RuleInstance.Name}: Weekday {now.DayOfWeek} is not in enabled days list");
                _timer.Interval = (new DateTime(now.Year, now.Month, now.Day, 23, 59, 59, 99).TimeOfDay - nowTime).TotalMilliseconds;
                return;
            }

            var startTime = _timerPropertyData.StartTime.ToLocalTime().TimeOfDay;
            var stopTime = _timerPropertyData.StopTime.ToLocalTime().TimeOfDay;
            

            var tickTime = startTime - nowTime;
            double timerTickTime;

            if (tickTime.TotalMilliseconds < 0)
            {
              
                timerTickTime = (stopTime - nowTime).TotalMilliseconds;
                if (timerTickTime < 0)
                {
                    _timer.Interval = (new DateTime(now.Year, now.Month, now.Day, 23, 59, 59, 99).TimeOfDay - nowTime).TotalMilliseconds;
                    Context.Logger.LogDebug($"Timer {Context.RuleInstance.Name}: Next tick time is {_timer.Interval}ms at {stopTime}");
                    if (isStartup)
                    {
                        Context.Logger.LogInformation($"Start event, set value to {false}");
                        Context.Dispatcher.DispatchValue(new LogicOutputChanged(_output, false).Instance, false);
                    }
                    _value = false;
                    return;
                }
                if (isStartup)
                {
                    Context.Logger.LogInformation($"Start event, set value to {true}");
                    Context.Dispatcher.DispatchValue(new LogicOutputChanged(_output, true).Instance, true);
                }

                _value = true;
            }
            else
            {
                timerTickTime = tickTime.TotalMilliseconds;
                if (isStartup)
                {
                    Context.Logger.LogInformation($"Start event, set value to {false}");
                    Context.Dispatcher.DispatchValue(new LogicOutputChanged(_output, false).Instance, false);
                }

                _value = false;
            }

            _timer.Interval = timerTickTime;
            Context.Logger.LogDebug($"Timer {Context.RuleInstance.Name}: Next tick time is {_timer.Interval}ms at {startTime}");
            _timer.Start();
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
                if (_timerPropertyData == null)
                {
                    return;
                }

                _value = !_value;
                Context.Dispatcher.DispatchValue(new LogicOutputChanged(_output, _value).Instance, _value);

                Context.Logger.LogInformation($"Tick received, set value to {_value}");

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
