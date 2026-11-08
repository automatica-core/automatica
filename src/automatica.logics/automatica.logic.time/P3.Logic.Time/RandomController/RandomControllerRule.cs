using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using P3.Logic.Time.PulseGenerator;

namespace P3.Logic.Time.RandomController
{
    /// <summary>
    /// While En = 1: wait a random time up to the maximum switch-on delay, switch on, wait a random time up to the maximum
    /// switch-off delay, switch off, and so on. En = 0 stops it and switches off.
    /// </summary>
    public class RandomControllerRule : Automatica.Core.Logic.Logic
    {
        internal const double MinimumSeconds = 1;

        private readonly object _sync = new();
        private readonly RuleInterfaceInstance _output;
        private System.Timers.Timer _timer;

        internal double MaxOnDelaySeconds = RandomControllerLogicFactory.DefaultMaxOnDelay;
        internal double MaxOffDelaySeconds = RandomControllerLogicFactory.DefaultMaxOffDelay;
        internal Func<double> NextRandom = Random.Shared.NextDouble;

        private bool _running;
        private bool _state;

        public RandomControllerRule(ILogicContext context) : base(context)
        {
            _output = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a => a.This2RuleInterfaceTemplate == RandomControllerLogicFactory.Output);
        }

        internal bool Running => _running;

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
            if (instance.This2RuleInterfaceTemplate == RandomControllerLogicFactory.ParamMaxOnDelay)
            {
                MaxOnDelaySeconds = PulseGeneratorRule.ToSeconds(value, RandomControllerLogicFactory.DefaultMaxOnDelay);
            }
            else if (instance.This2RuleInterfaceTemplate == RandomControllerLogicFactory.ParamMaxOffDelay)
            {
                MaxOffDelaySeconds = PulseGeneratorRule.ToSeconds(value, RandomControllerLogicFactory.DefaultMaxOffDelay);
            }
        }

        /// <summary>
        /// A random duration between <see cref="MinimumSeconds"/> and <paramref name="maxSeconds"/>
        /// </summary>
        internal double RandomSeconds(double maxSeconds)
        {
            var max = Math.Max(maxSeconds, MinimumSeconds);
            var random = Math.Clamp(NextRandom(), 0, 1);
            return MinimumSeconds + random * (max - MinimumSeconds);
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
            // off now: the next step is switching on after the switch-on delay, and vice versa
            var seconds = RandomSeconds(_state ? MaxOffDelaySeconds : MaxOnDelaySeconds);
            _timer.Stop();
            _timer.Interval = TimerHelper.ClampInterval(seconds * 1000);
            _timer.Start();
        }

        private void Dispatch(bool value)
        {
            Context.Dispatcher.DispatchValue(new LogicOutputChanged(_output, value).Instance, value);
        }

        protected override IList<ILogicOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            if (instance.This2RuleInterfaceTemplate != RandomControllerLogicFactory.InputEnable)
            {
                return new List<ILogicOutputChanged>();
            }

            var enable = TimerHelper.IsTruthy(value);
            lock (_sync)
            {
                if (enable && !_running && _timer != null)
                {
                    _running = true;
                    _state = false;
                    Schedule();
                }
                else if (!enable && _running)
                {
                    _running = false;
                    _timer?.Stop();
                    if (_state)
                    {
                        _state = false;
                        Dispatch(false);
                    }
                }
            }

            return new List<ILogicOutputChanged>();
        }
    }
}
