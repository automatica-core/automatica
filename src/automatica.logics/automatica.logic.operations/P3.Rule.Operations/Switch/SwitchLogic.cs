using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Rule;
using Microsoft.Extensions.Logging;

namespace P3.Rule.Time.Timer
{
    public class SwitchLogic : Automatica.Core.Rule.Rule
    {
        private readonly RuleInterfaceInstance _timerProperty;
        private TimerPropertyData _timerPropertyData;
        private readonly RuleInterfaceInstance _output;
        private readonly System.Timers.Timer _timer;
        private readonly object _lock = new object();

        private bool _value = false;

        public TimerRule(IRuleContext context) : base(context)
        {
            _timerProperty = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
                a.This2RuleInterfaceTemplate == SwitchRuleFactory.RuleTimerParameter);

            _output = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a =>
                a.This2RuleInterfaceTemplate == SwitchRuleFactory.RuleOutput);

            _timer = new System.Timers.Timer();

        }

        public override Task<bool> Start()
        {
            if (_timerProperty?.Value != null)
            {
                _timerPropertyData = (TimerPropertyData)_timerProperty.Value;

            }
            else
            {
                Context.Logger.LogError("No or invalid timer property found");
                return Task.FromResult(false);
            }

           CalculateTickTime(true);

            
            _timer.Elapsed += _timer_Elapsed;
            _timer.Start();
            return base.Start();
        }

        private void CalculateTickTime(bool isStartup)
        {
            var now = DateTime.Now;
            var nowTime = now.TimeOfDay;

            if (!_timerPropertyData.EnabledDays.Contains(now.DayOfWeek))
            {
                Context.Logger.LogDebug($"Timer {Context.RuleInstance.Name}: Weekday {now.DayOfWeek} is not in enabled days list");
                _timer.Interval = (new DateTime(now.Year, now.Month, now.Day, 23, 59, 59, 99).TimeOfDay - nowTime).TotalMilliseconds;
                return;
            }

            var startTime = _timerPropertyData.StartTime.ToLocalTime().TimeOfDay;
            var stopTime = _timerPropertyData.StopTime.ToLocalTime().TimeOfDay;
            

            var tickTime = startTime - nowTime;
            double timerTickTime = 0;

            if (tickTime.TotalMilliseconds < 0)
            {
              
                timerTickTime = (stopTime - nowTime).TotalMilliseconds;
                if (timerTickTime < 0)
                {
                    _timer.Interval = (new DateTime(now.Year, now.Month, now.Day, 23, 59, 59, 99).TimeOfDay - nowTime).TotalMilliseconds;
                    Context.Logger.LogDebug($"Timer {Context.RuleInstance.Name}: Next tick time is {_timer.Interval}ms");
                    if (isStartup)
                    {
                        Context.Dispatcher.DispatchValue(new RuleOutputChanged(_output, false).Instance, false);
                    }
                    _value = false;
                    return;
                }
                if (isStartup)
                {
                    Context.Dispatcher.DispatchValue(new RuleOutputChanged(_output, true).Instance, true);
                }

                _value = true;
            }
            else
            {
                timerTickTime = tickTime.TotalMilliseconds;
                if (isStartup)
                {
                    Context.Dispatcher.DispatchValue(new RuleOutputChanged(_output, false).Instance, false);
                }

                _value = false;
            }

            _timer.Interval = timerTickTime;
            Context.Logger.LogDebug($"Timer {Context.RuleInstance.Name}: Next tick time is {_timer.Interval}ms");
            _timer.Start();
        }

        public override Task<bool> Stop()
        {
            _timer.Elapsed -= _timer_Elapsed;
            _timer.Stop();
            return base.Stop();
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
                Context.Dispatcher.DispatchValue(new RuleOutputChanged(_output, _value).Instance, _value);

                CalculateTickTime(false);
            }
        }

        protected override IList<IRuleOutputChanged> InputValueChanged(RuleInterfaceInstance instance,
            IDispatchable source, object value)
        {

            return new List<IRuleOutputChanged>();
        }
    }
}
