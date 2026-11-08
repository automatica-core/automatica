using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Automatica.Core.EF.Models;
using Automatica.Core.Base.IO;
using Automatica.Core.Logic;
using P3.Logic.StringOperations.Common;

namespace P3.Logic.StringOperations.TextGenerator
{
    /// <summary>
    /// Generates the texts whose template is set: on the rising edge of Tr (directly, or after Td ms through the timer), again every
    /// Tu s while Tr is on; the falling edge sends empty texts. Timer results go out through the dispatcher.
    /// </summary>
    public class TextGeneratorRule : TextLogicBase
    {
        private readonly object _sync = new();
        private readonly object[] _values = new object[TextGeneratorLogicFactory.ValueCount];
        private System.Timers.Timer _timer;
        private bool _trigger;

        public TextGeneratorRule(ILogicContext context) : base(context)
        {
        }

        internal bool Active => _trigger;

        private double DelayMs => Math.Max(0, ParameterDouble(TextGeneratorLogicFactory.ParamDelay, 0));

        private double IntervalSeconds => Math.Max(0, ParameterDouble(TextGeneratorLogicFactory.ParamInterval, 0));

        protected override Task<bool> Start(RuleInstance instance, CancellationToken token = default)
        {
            lock (_sync)
            {
                _timer = new System.Timers.Timer { AutoReset = false };
                _timer.Elapsed += _timer_Elapsed;
            }
            return base.Start(instance, token);
        }

        protected override Task<bool> Stop(RuleInstance instance, CancellationToken token = default)
        {
            lock (_sync)
            {
                _trigger = false;
                if (_timer != null)
                {
                    _timer.Elapsed -= _timer_Elapsed;
                    _timer.Dispose();
                    _timer = null;
                }
            }
            return base.Stop(instance, token);
        }

        protected override IList<ILogicOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            var valueIndex = Array.IndexOf(TextGeneratorLogicFactory.InputValues, instance.This2RuleInterfaceTemplate);
            if (valueIndex >= 0)
            {
                lock (_sync)
                {
                    _values[valueIndex] = value;
                }
                return NoChanges();
            }

            if (instance.This2RuleInterfaceTemplate != TextGeneratorLogicFactory.InputTrigger)
            {
                return NoChanges();
            }

            var trigger = TextValue.IsTruthy(value);
            lock (_sync)
            {
                if (_timer == null || trigger == _trigger)
                {
                    // not started, or no edge
                    return NoChanges();
                }
                _trigger = trigger;

                if (!trigger)
                {
                    _timer?.Stop();
                    return Generate(clear: true);
                }

                var delay = DelayMs;
                if (delay > 0)
                {
                    Schedule(delay);
                    return NoChanges();
                }

                ScheduleUpdate();
                return Generate(clear: false);
            }
        }

        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            lock (_sync)
            {
                if (!_trigger || _timer == null)
                {
                    return;
                }
                Dispatch(Generate(clear: false));
                ScheduleUpdate();
            }
        }

        private void ScheduleUpdate()
        {
            var interval = IntervalSeconds;
            if (interval > 0)
            {
                Schedule(interval * 1000);
            }
        }

        private void Schedule(double milliseconds)
        {
            _timer.Stop();
            _timer.Interval = Math.Clamp(milliseconds, 1, int.MaxValue);
            _timer.Start();
        }

        /// <summary>
        /// The texts whose template is set, or empty texts for them on clear
        /// </summary>
        internal IList<ILogicOutputChanged> Generate(bool clear)
        {
            var changes = new List<ILogicOutputChanged>();
            for (var i = 0; i < TextGeneratorLogicFactory.TextCount; i++)
            {
                var template = ParameterText(TextGeneratorLogicFactory.ParamTexts[i]);
                if (string.IsNullOrEmpty(template))
                {
                    continue;
                }
                var change = Changed(TextGeneratorLogicFactory.Outputs[i], clear ? string.Empty : TextTemplate.Render(template, _values));
                if (change != null)
                {
                    changes.Add(change);
                }
            }
            return changes;
        }
    }
}
