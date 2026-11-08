using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;

namespace P3.Logic.Time.Pwm
{
    /// <summary>
    /// Pulse width modulation (Loxone "Pulse Width Modulation").
    /// V (0..10) is the duty cycle: within every period the output is 1 for V/10 of the period and 0 for the rest.
    /// V &lt;= 0 keeps the output at 0, V &gt;= 10 keeps it at 1. A new V applies from the next phase change.
    /// Off = 1 sends 0 and locks the block until Off is 0 again.
    /// </summary>
    public class PwmRule : Automatica.Core.Logic.Logic
    {
        internal const double MinimumPeriod = 0.1;
        internal const double FullScale = 10;

        private readonly RuleInterfaceInstance _output;
        private readonly object _sync = new();
        private System.Timers.Timer _timer;

        internal double PeriodSeconds = PwmLogicFactory.DefaultPeriod;

        private double _duty;
        private bool? _state;
        private bool _cycling;
        private bool _locked;

        public PwmRule(ILogicContext context) : base(context)
        {
            _output = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a => a.This2RuleInterfaceTemplate == PwmLogicFactory.RuleOutput);
        }

        internal double Duty => _duty;

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
                _cycling = false;
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
            if (instance.This2RuleInterfaceTemplate == PwmLogicFactory.RuleParamPeriod)
            {
                PeriodSeconds = TimeValue.ToSeconds(value, PwmLogicFactory.DefaultPeriod, MinimumPeriod);
            }

            base.ParameterValueChanged(instance, source, value);
        }

        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            lock (_sync)
            {
                if (!_cycling || _timer == null)
                {
                    return;
                }

                Apply(_state != true);
            }
        }

        /// <summary>
        /// Starts the phase <paramref name="on"/>, or holds the output when the duty cycle is 0 or 100 %
        /// </summary>
        private void Apply(bool on)
        {
            if (_duty <= 0 || _duty >= 1)
            {
                _cycling = false;
                _timer.Stop();
                Set(_duty >= 1);
                return;
            }

            _cycling = true;
            Set(on);
            var seconds = (on ? _duty : 1 - _duty) * PeriodSeconds;
            _timer.Stop();
            _timer.Interval = TimerHelper.ClampInterval(seconds * 1000);
            _timer.Start();
        }

        private void Set(bool value)
        {
            if (_state == value)
            {
                return;
            }

            _state = value;
            Context.Dispatcher.DispatchValue(new LogicOutputChanged(_output, value).Instance, value);
        }

        protected override IList<ILogicOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            var template = instance.This2RuleInterfaceTemplate;
            lock (_sync)
            {
                if (_timer == null)
                {
                    return new List<ILogicOutputChanged>();
                }

                if (template == PwmLogicFactory.RuleOff)
                {
                    if (TimerHelper.IsTruthy(value))
                    {
                        _locked = true;
                        _cycling = false;
                        _timer.Stop();
                        _state = false;
                        Context.Dispatcher.DispatchValue(new LogicOutputChanged(_output, false).Instance, false);
                    }
                    else if (_locked)
                    {
                        _locked = false;
                        Apply(true);
                    }
                }
                else if (template == PwmLogicFactory.RuleValue)
                {
                    var v = TimeValue.ToDouble(value);
                    if (!v.HasValue)
                    {
                        return new List<ILogicOutputChanged>();
                    }

                    _duty = System.Math.Clamp(v.Value / FullScale, 0, 1);
                    // a running cycle picks the new duty cycle up at its next phase change
                    if (!_locked && !_cycling)
                    {
                        Apply(true);
                    }
                }
            }

            return new List<ILogicOutputChanged>();
        }
    }
}
