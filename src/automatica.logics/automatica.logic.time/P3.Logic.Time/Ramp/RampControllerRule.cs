using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;

namespace P3.Logic.Time.Ramp
{
    /// <summary>
    /// Ramp controller with two levels (Loxone "Ramp Controller").
    /// V starts at the start value and moves towards level 1 (S = 0) or level 2 (S = 1) by the step size every 100 ms.
    /// St = 1 sets V to the start value and holds it there, St = 0 lets it ramp again.
    /// Off = 1 sets V to 0 and locks the block; after Off = 0 V ramps from 0 to the selected level.
    /// </summary>
    public class RampControllerRule : Automatica.Core.Logic.Logic
    {
        private readonly RuleInterfaceInstance _output;
        private readonly object _sync = new();
        private System.Timers.Timer _timer;

        /// <summary>
        /// Time between two steps, the tests set it high so the real timer cannot tick into them
        /// </summary>
        internal double TickMilliseconds = 100;

        internal double StartValue = RampControllerLogicFactory.DefaultStart;
        internal double Step = RampControllerLogicFactory.DefaultStep;
        internal double Level1 = RampControllerLogicFactory.DefaultLevel1;
        internal double Level2 = RampControllerLogicFactory.DefaultLevel2;

        private double? _value;
        private bool _level2;
        private bool _stopped;
        private bool _locked;
        private bool _scheduled;

        public RampControllerRule(ILogicContext context) : base(context)
        {
            _output = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a => a.This2RuleInterfaceTemplate == RampControllerLogicFactory.RuleOutput);
        }

        internal double Target => _level2 ? Level2 : Level1;

        protected override Task<bool> Start(RuleInstance instance, CancellationToken token = default)
        {
            lock (_sync)
            {
                _timer = new System.Timers.Timer { AutoReset = false };
                _timer.Elapsed += _timer_Elapsed;
                // the parameters are pushed right after this, the first tick sends the start value
                Kick();
            }
            return base.Start(instance, token);
        }

        protected override Task<bool> Stop(RuleInstance instance, CancellationToken token = default)
        {
            lock (_sync)
            {
                _scheduled = false;
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
            var template = instance.This2RuleInterfaceTemplate;
            lock (_sync)
            {
                if (template == RampControllerLogicFactory.RuleParamStart)
                {
                    StartValue = TimeValue.ToDouble(value) ?? RampControllerLogicFactory.DefaultStart;
                }
                else if (template == RampControllerLogicFactory.RuleParamStep)
                {
                    Step = System.Math.Max(0, TimeValue.ToDouble(value) ?? RampControllerLogicFactory.DefaultStep);
                }
                else if (template == RampControllerLogicFactory.RuleParamLevel1)
                {
                    Level1 = TimeValue.ToDouble(value) ?? RampControllerLogicFactory.DefaultLevel1;
                }
                else if (template == RampControllerLogicFactory.RuleParamLevel2)
                {
                    Level2 = TimeValue.ToDouble(value) ?? RampControllerLogicFactory.DefaultLevel2;
                }

                Kick();
            }

            base.ParameterValueChanged(instance, source, value);
        }

        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            lock (_sync)
            {
                _scheduled = false;
                if (_timer == null || _locked || _stopped)
                {
                    return;
                }

                _timer.Stop();

                if (!_value.HasValue)
                {
                    Set(StartValue);
                }
                else
                {
                    var target = Target;
                    var diff = target - _value.Value;
                    Set(Step <= 0 || System.Math.Abs(diff) <= Step ? target : _value.Value + System.Math.Sign(diff) * Step);
                }

                Kick();
            }
        }

        /// <summary>
        /// Schedules the next step unless V is at the target or the block is stopped / locked
        /// </summary>
        private void Kick()
        {
            if (_timer == null || _scheduled || _locked || _stopped)
            {
                return;
            }

            if (_value.HasValue && _value.Value == Target)
            {
                return;
            }

            _scheduled = true;
            _timer.Stop();
            _timer.Interval = TimerHelper.ClampInterval(TickMilliseconds);
            _timer.Start();
        }

        private void Set(double value)
        {
            if (_value == value)
            {
                return;
            }

            _value = value;
            Context.Dispatcher.DispatchValue(new LogicOutputChanged(_output, value).Instance, value);
        }

        private void Halt()
        {
            _scheduled = false;
            _timer.Stop();
        }

        protected override IList<ILogicOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            var template = instance.This2RuleInterfaceTemplate;
            var on = TimerHelper.IsTruthy(value);
            lock (_sync)
            {
                if (_timer == null)
                {
                    return new List<ILogicOutputChanged>();
                }

                if (template == RampControllerLogicFactory.RuleOff)
                {
                    if (on)
                    {
                        _locked = true;
                        Halt();
                        Set(0);
                    }
                    else if (_locked)
                    {
                        _locked = false;
                        Kick();
                    }
                }
                else if (template == RampControllerLogicFactory.RuleStop)
                {
                    if (on)
                    {
                        _stopped = true;
                        Halt();
                        if (!_locked)
                        {
                            Set(StartValue);
                        }
                    }
                    else if (_stopped)
                    {
                        _stopped = false;
                        Kick();
                    }
                }
                else if (template == RampControllerLogicFactory.RuleSelect)
                {
                    _level2 = on;
                    Kick();
                }
            }

            return new List<ILogicOutputChanged>();
        }
    }
}
