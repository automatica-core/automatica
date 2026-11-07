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

namespace P3.Logic.Time.PulseGenerator
{
    /// <summary>
    /// While EN is 1 the output alternates: 1 for the on time, 0 for the off time, starting with 1.
    /// EN = 0 stops it and sends 0.
    /// </summary>
    public class PulseGeneratorRule : Automatica.Core.Logic.Logic
    {
        internal const double MinimumSeconds = 0.1;

        private readonly RuleInterfaceInstance _output;
        private readonly object _sync = new();
        private System.Timers.Timer _timer;

        internal double OnSeconds = PulseGeneratorLogicFactory.DefaultOn;
        internal double OffSeconds = PulseGeneratorLogicFactory.DefaultOff;
        private bool _running;
        private bool _state;

        public PulseGeneratorRule(ILogicContext context) : base(context)
        {
            _output = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a => a.This2RuleInterfaceTemplate == PulseGeneratorLogicFactory.RuleOutput);
        }

        internal bool Running => _running;

        internal static double ToSeconds(object value, double fallback)
        {
            double seconds;
            try
            {
                seconds = value is string s
                    ? double.Parse(s, NumberStyles.Float, CultureInfo.InvariantCulture)
                    : Convert.ToDouble(value, CultureInfo.InvariantCulture);
            }
            catch (Exception)
            {
                return fallback;
            }

            return double.IsNaN(seconds) ? fallback : Math.Max(seconds, MinimumSeconds);
        }

        protected override Task<bool> Start(RuleInstance instance, CancellationToken token = default)
        {
            _timer = new System.Timers.Timer { AutoReset = false };
            _timer.Elapsed += _timer_Elapsed;
            return base.Start(instance, token);
        }

        protected override Task<bool> Stop(RuleInstance instance, CancellationToken token = default)
        {
            lock (_sync)
            {
                _running = false;
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
            if (instance.This2RuleInterfaceTemplate == PulseGeneratorLogicFactory.RuleParamOn)
            {
                OnSeconds = ToSeconds(value, PulseGeneratorLogicFactory.DefaultOn);
            }
            else if (instance.This2RuleInterfaceTemplate == PulseGeneratorLogicFactory.RuleParamOff)
            {
                OffSeconds = ToSeconds(value, PulseGeneratorLogicFactory.DefaultOff);
            }

            base.ParameterValueChanged(instance, source, value);
        }

        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            lock (_sync)
            {
                if (!_running || _timer == null)
                {
                    return;
                }

                _state = !_state;
                Dispatch(_state);
                Schedule();
            }
        }

        private void Schedule()
        {
            _timer.Stop();
            _timer.Interval = TimerHelper.ClampInterval((_state ? OnSeconds : OffSeconds) * 1000);
            _timer.Start();
        }

        private void Dispatch(bool value)
        {
            Context.Dispatcher.DispatchValue(new LogicOutputChanged(_output, value).Instance, value);
        }

        protected override IList<ILogicOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            if (instance.This2RuleInterfaceTemplate != PulseGeneratorLogicFactory.RuleEnable)
            {
                return new List<ILogicOutputChanged>();
            }

            var enable = TimerHelper.IsTruthy(value);
            lock (_sync)
            {
                if (enable && !_running && _timer != null)
                {
                    _running = true;
                    _state = true;
                    Dispatch(true);
                    Schedule();
                }
                else if (!enable && _running)
                {
                    _running = false;
                    _timer?.Stop();
                    _state = false;
                    Dispatch(false);
                }
            }

            return new List<ILogicOutputChanged>();
        }
    }
}
