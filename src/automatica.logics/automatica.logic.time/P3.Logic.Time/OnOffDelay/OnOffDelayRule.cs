using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;

namespace P3.Logic.Time.OnOffDelay
{
    /// <summary>
    /// Switch-on and switch-off delay (Loxone "Switch-On and -Off Delay").
    /// O follows Tr: it switches on once Tr has been 1 for the on delay and off once Tr has been 0 for the off delay.
    /// A change of Tr back before the delay has run out cancels the pending switch.
    /// Off = 1 switches O off at once and locks the block until Off is 0 again, then O follows Tr again.
    /// </summary>
    public class OnOffDelayRule : Automatica.Core.Logic.Logic
    {
        private readonly RuleInterfaceInstance _output;
        private readonly object _sync = new();
        private System.Timers.Timer _timer;

        internal double OnSeconds = OnOffDelayLogicFactory.DefaultOn;
        internal double OffSeconds = OnOffDelayLogicFactory.DefaultOff;

        private bool _input;
        private bool _state;
        private bool _locked;
        private bool _pending;
        private bool _pendingState;

        public OnOffDelayRule(ILogicContext context) : base(context)
        {
            _output = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a => a.This2RuleInterfaceTemplate == OnOffDelayLogicFactory.RuleOutput);
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
                _pending = false;
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
            if (instance.This2RuleInterfaceTemplate == OnOffDelayLogicFactory.RuleParamOn)
            {
                OnSeconds = TimeValue.ToSeconds(value, OnOffDelayLogicFactory.DefaultOn);
            }
            else if (instance.This2RuleInterfaceTemplate == OnOffDelayLogicFactory.RuleParamOff)
            {
                OffSeconds = TimeValue.ToSeconds(value, OnOffDelayLogicFactory.DefaultOff);
            }

            base.ParameterValueChanged(instance, source, value);
        }

        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            lock (_sync)
            {
                if (!_pending || _timer == null)
                {
                    return;
                }

                _pending = false;
                _timer.Stop();
                SetState(_pendingState);
            }
        }

        private void SetState(bool state)
        {
            if (_state == state)
            {
                return;
            }

            _state = state;
            Context.Dispatcher.DispatchValue(new LogicOutputChanged(_output, state).Instance, state);
        }

        private void Cancel()
        {
            _pending = false;
            _timer?.Stop();
        }

        /// <summary>
        /// Moves the output towards the input: nothing to do, a delayed switch or (delay 0) an immediate one
        /// </summary>
        private void Follow()
        {
            if (_input == _state)
            {
                Cancel();
                return;
            }

            if (_pending && _pendingState == _input)
            {
                return;
            }

            var seconds = _input ? OnSeconds : OffSeconds;
            if (seconds <= 0)
            {
                Cancel();
                SetState(_input);
                return;
            }

            _pending = true;
            _pendingState = _input;
            _timer.Stop();
            _timer.Interval = TimerHelper.ClampInterval(seconds * 1000);
            _timer.Start();
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

                if (template == OnOffDelayLogicFactory.RuleOff)
                {
                    if (TimerHelper.IsTruthy(value))
                    {
                        _locked = true;
                        Cancel();
                        _state = false;
                        Context.Dispatcher.DispatchValue(new LogicOutputChanged(_output, false).Instance, false);
                    }
                    else if (_locked)
                    {
                        _locked = false;
                        Follow();
                    }
                }
                else if (template == OnOffDelayLogicFactory.RuleTrigger)
                {
                    _input = TimerHelper.IsTruthy(value);
                    if (!_locked)
                    {
                        Follow();
                    }
                }
            }

            return new List<ILogicOutputChanged>();
        }
    }
}
