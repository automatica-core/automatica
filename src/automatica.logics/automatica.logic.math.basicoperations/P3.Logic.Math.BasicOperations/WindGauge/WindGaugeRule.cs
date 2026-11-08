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

namespace P3.Logic.Math.BasicOperations.WindGauge
{
    /// <summary>
    /// Wind speed from the frequency of a wind sensor (speed = frequency * factor). The frequency is a held value: the averages
    /// weigh each value with the time it was present. Every second the 3 second gust, the average over the averaging time, the
    /// highest gust in that time and the wind alarm are updated.
    /// </summary>
    public class WindGaugeRule : Automatica.Core.Logic.Logic
    {
        internal static readonly TimeSpan GustWindow = TimeSpan.FromSeconds(3);
        internal const double AlarmRelease = 0.9;

        private readonly object _sync = new();
        private readonly System.Timers.Timer _timer = new(1000) { AutoReset = true };
        private readonly Dictionary<Guid, RuleInterfaceInstance> _byTemplate;
        private readonly Dictionary<Guid, object> _sent = new();
        private readonly List<(DateTimeOffset At, double Speed)> _samples = new();
        private readonly List<(DateTimeOffset At, double Gust)> _gusts = new();

        internal double AveragingMinutes = 10;
        internal double Factor = 1;
        internal double AlarmSpeed = 50;
        private bool _alarm;

        public WindGaugeRule(ILogicContext context) : base(context)
        {
            _byTemplate = context.RuleInstance.RuleInterfaceInstance
                .GroupBy(a => a.This2RuleInterfaceTemplate)
                .ToDictionary(a => a.Key, a => a.First());
            _timer.Elapsed += _timer_Elapsed;
        }

        internal bool Alarm => _alarm;

        private DateTimeOffset Now => Context.TimeProvider.GetUtcNow();

        private TimeSpan AveragingWindow => TimeSpan.FromMinutes(AveragingMinutes);

        protected override Task<bool> Start(RuleInstance instance, CancellationToken token = default)
        {
            lock (_sync)
            {
                Send(WindGaugeLogicFactory.OutputAlarm, false);
                _timer.Start();
            }
            return base.Start(instance, token);
        }

        protected override Task<bool> Stop(RuleInstance instance, CancellationToken token = default)
        {
            lock (_sync)
            {
                _timer.Stop();
            }
            return base.Stop(instance, token);
        }

        protected override void ParameterValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            lock (_sync)
            {
                var template = instance.This2RuleInterfaceTemplate;
                if (template == WindGaugeLogicFactory.ParamAveragingTime)
                {
                    AveragingMinutes = System.Math.Max(0.1, ToDouble(value) ?? 10);
                }
                else if (template == WindGaugeLogicFactory.ParamFactor)
                {
                    Factor = ToDouble(value) ?? 1;
                }
                else if (template == WindGaugeLogicFactory.ParamAlarmSpeed)
                {
                    AlarmSpeed = System.Math.Max(0.1, ToDouble(value) ?? 50);
                }
            }
        }

        protected override IList<ILogicOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            lock (_sync)
            {
                if (instance.This2RuleInterfaceTemplate == WindGaugeLogicFactory.InputFrequency)
                {
                    var frequency = ToDouble(value);
                    if (frequency.HasValue)
                    {
                        _samples.Add((Now, System.Math.Max(0, frequency.Value * Factor)));
                        Update();
                    }
                }
            }
            return new List<ILogicOutputChanged>();
        }

        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            lock (_sync)
            {
                Update();
            }
        }

        /// <summary>
        /// Time weighted average of the held speed from <paramref name="from"/> to <paramref name="to"/>, null without samples
        /// </summary>
        internal double? Average(DateTimeOffset from, DateTimeOffset to)
        {
            if (_samples.Count == 0 || to <= _samples[0].At)
            {
                return _samples.Count == 0 ? null : _samples[0].Speed;
            }
            // before the first sample there is no value: the window starts with it
            if (from < _samples[0].At)
            {
                from = _samples[0].At;
            }
            if (to <= from)
            {
                return _samples.Last(a => a.At <= to).Speed;
            }

            double sum = 0;
            for (var i = 0; i < _samples.Count; i++)
            {
                var start = _samples[i].At < from ? from : _samples[i].At;
                var end = i + 1 < _samples.Count ? _samples[i + 1].At : to;
                if (end > to)
                {
                    end = to;
                }
                if (end > start)
                {
                    sum += _samples[i].Speed * (end - start).TotalSeconds;
                }
            }
            return sum / (to - from).TotalSeconds;
        }

        private void Update()
        {
            if (_samples.Count == 0)
            {
                return;
            }
            var now = Now;
            var window = AveragingWindow;

            // keep the last sample before the window, it is the value at the start of the window
            var keepFrom = now - window - GustWindow;
            var firstInside = _samples.FindIndex(a => a.At >= keepFrom);
            if (firstInside > 1)
            {
                _samples.RemoveRange(0, firstInside - 1);
            }
            else if (firstInside < 0 && _samples.Count > 1)
            {
                _samples.RemoveRange(0, _samples.Count - 1);
            }

            var gust = Average(now - GustWindow, now) ?? 0;
            var average = Average(now - window, now) ?? 0;
            _gusts.Add((now, gust));
            _gusts.RemoveAll(a => a.At < now - window);
            var maximum = _gusts.Max(a => a.Gust);

            if (!_alarm && gust >= AlarmSpeed)
            {
                _alarm = true;
            }
            else if (_alarm && gust < AlarmSpeed * AlarmRelease && average < AlarmSpeed * AlarmRelease)
            {
                _alarm = false;
            }

            Send(WindGaugeLogicFactory.OutputGust, System.Math.Round(gust, 1));
            Send(WindGaugeLogicFactory.OutputAverage, System.Math.Round(average, 1));
            Send(WindGaugeLogicFactory.OutputMaximum, System.Math.Round(maximum, 1));
            Send(WindGaugeLogicFactory.OutputAlarm, _alarm);
        }

        private void Send(Guid output, object value)
        {
            if (!_byTemplate.TryGetValue(output, out var instance) || (_sent.TryGetValue(output, out var last) && Equals(last, value)))
            {
                return;
            }
            _sent[output] = value;
            Context.Dispatcher.DispatchValue(new LogicOutputChanged(instance, value).Instance, value);
        }

        internal static double? ToDouble(object value)
        {
            try
            {
                double d = value switch
                {
                    null => double.NaN,
                    bool b => b ? 1 : 0,
                    string s => double.Parse(s, NumberStyles.Float, CultureInfo.InvariantCulture),
                    IConvertible c => c.ToDouble(CultureInfo.InvariantCulture),
                    _ => double.NaN
                };
                return double.IsNaN(d) || double.IsInfinity(d) ? null : d;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
