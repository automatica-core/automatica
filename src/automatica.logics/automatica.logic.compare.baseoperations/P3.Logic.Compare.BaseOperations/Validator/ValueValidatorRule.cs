using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using P3.Logic.Compare.BaseOperations.Threshold;

namespace P3.Logic.Compare.BaseOperations.Validator
{
    /// <summary>
    /// Value validator (Loxone "Value Validator"). V passes to the output only while it is a number within minimum..maximum;
    /// an invalid value is dropped and sets E, the output keeps the last valid value.
    /// Minimum change interval (&gt; 0): if V does not change within this time, E is set until it changes again.
    /// En: as long as nothing was received on En the block is always enabled. Once En is used, En = 0 ignores V and stops
    /// the interval check, and every En = 1 waits the delay once before validated values reach the output
    /// (the last one received during the delay is sent when it ends).
    /// </summary>
    public class ValueValidatorRule : Automatica.Core.Logic.Logic
    {
        private readonly RuleInterfaceInstance _output;
        private readonly RuleInterfaceInstance _error;
        private readonly object _sync = new();

        /// <summary>
        /// Delay after En = 1
        /// </summary>
        private System.Timers.Timer _timer;

        /// <summary>
        /// Minimum change interval
        /// </summary>
        private System.Timers.Timer _watchdog;

        internal double ChangeIntervalSeconds = ValueValidatorLogicFactory.DefaultChangeInterval;
        internal double Minimum = ValueValidatorLogicFactory.DefaultMinimum;
        internal double Maximum = ValueValidatorLogicFactory.DefaultMaximum;
        internal double DelaySeconds = ValueValidatorLogicFactory.DefaultDelay;

        private bool _enableUsed;
        private bool _enabled = true;
        private bool _delaying;
        private double? _pending;
        private double? _lastInput;
        private double? _sentValue;
        private bool _invalid;
        private bool _timedOut;
        private bool? _sentError;

        public ValueValidatorRule(ILogicContext context) : base(context)
        {
            _output = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a => a.This2RuleInterfaceTemplate == ValueValidatorLogicFactory.RuleOutput);
            _error = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a => a.This2RuleInterfaceTemplate == ValueValidatorLogicFactory.RuleError);
        }

        private bool Active => !_enableUsed || _enabled;

        protected override Task<bool> Start(RuleInstance instance, CancellationToken token = default)
        {
            lock (_sync)
            {
                _timer = new System.Timers.Timer { AutoReset = false };
                _timer.Elapsed += _timer_Elapsed;
                _watchdog = new System.Timers.Timer { AutoReset = false };
                _watchdog.Elapsed += _watchdog_Elapsed;
            }
            return base.Start(instance, token);
        }

        protected override Task<bool> Stop(RuleInstance instance, CancellationToken token = default)
        {
            lock (_sync)
            {
                _delaying = false;
                if (_timer != null)
                {
                    _timer.Elapsed -= _timer_Elapsed;
                    _timer.Dispose();
                    _timer = null;
                }
                if (_watchdog != null)
                {
                    _watchdog.Elapsed -= _watchdog_Elapsed;
                    _watchdog.Dispose();
                    _watchdog = null;
                }
            }
            return base.Stop(instance, token);
        }

        protected override void ParameterValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            var template = instance.This2RuleInterfaceTemplate;
            lock (_sync)
            {
                if (template == ValueValidatorLogicFactory.RuleParamChangeInterval)
                {
                    ChangeIntervalSeconds = ThresholdSwitchRule.ToDouble(value) ?? ValueValidatorLogicFactory.DefaultChangeInterval;
                    if (Active)
                    {
                        RestartWatchdog();
                    }
                }
                else if (template == ValueValidatorLogicFactory.RuleParamMinimum)
                {
                    Minimum = ThresholdSwitchRule.ToDouble(value) ?? ValueValidatorLogicFactory.DefaultMinimum;
                }
                else if (template == ValueValidatorLogicFactory.RuleParamMaximum)
                {
                    Maximum = ThresholdSwitchRule.ToDouble(value) ?? ValueValidatorLogicFactory.DefaultMaximum;
                }
                else if (template == ValueValidatorLogicFactory.RuleParamDelay)
                {
                    DelaySeconds = System.Math.Max(0, ThresholdSwitchRule.ToDouble(value) ?? ValueValidatorLogicFactory.DefaultDelay);
                }
            }

            base.ParameterValueChanged(instance, source, value);
        }

        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            lock (_sync)
            {
                if (!_delaying || _timer == null)
                {
                    return;
                }

                _timer.Stop();
                _delaying = false;
                if (_pending.HasValue)
                {
                    SendValue(_pending.Value);
                    _pending = null;
                }
            }
        }

        private void _watchdog_Elapsed(object sender, ElapsedEventArgs e)
        {
            lock (_sync)
            {
                if (_watchdog == null || !Active || ChangeIntervalSeconds <= 0)
                {
                    return;
                }

                _watchdog.Stop();
                _timedOut = true;
                SendError();
            }
        }

        private void RestartWatchdog()
        {
            if (_watchdog == null)
            {
                return;
            }

            _watchdog.Stop();
            if (ChangeIntervalSeconds > 0)
            {
                _watchdog.Interval = System.Math.Clamp(ChangeIntervalSeconds * 1000, 1, int.MaxValue);
                _watchdog.Start();
            }
        }

        private void SendValue(double value)
        {
            if (_sentValue == value)
            {
                return;
            }

            _sentValue = value;
            Context.Dispatcher.DispatchValue(new LogicOutputChanged(_output, value).Instance, value);
        }

        private void SendError()
        {
            var error = _invalid || _timedOut;
            if (_sentError == error)
            {
                return;
            }

            _sentError = error;
            Context.Dispatcher.DispatchValue(new LogicOutputChanged(_error, error).Instance, error);
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

                if (template == ValueValidatorLogicFactory.RuleEnable)
                {
                    var enable = ThresholdSwitchRule.ToDouble(value) is { } d && d != 0;
                    var wasActive = Active;
                    // the first 1 on En counts as an activation too, a repeated 1 does not
                    var rising = enable && !(_enableUsed && _enabled);
                    _enableUsed = true;
                    _enabled = enable;

                    if (rising)
                    {
                        if (!wasActive)
                        {
                            RestartWatchdog();
                        }
                        if (DelaySeconds > 0)
                        {
                            _delaying = true;
                            _pending = null;
                            _timer.Stop();
                            _timer.Interval = System.Math.Clamp(DelaySeconds * 1000, 1, int.MaxValue);
                            _timer.Start();
                        }
                    }
                    else if (!enable)
                    {
                        _delaying = false;
                        _pending = null;
                        _timer.Stop();
                        _watchdog.Stop();
                    }
                }
                else if (template == ValueValidatorLogicFactory.RuleValue && Active)
                {
                    var v = ThresholdSwitchRule.ToDouble(value);
                    var low = System.Math.Min(Minimum, Maximum);
                    var high = System.Math.Max(Minimum, Maximum);

                    if (!v.HasValue || double.IsInfinity(v.Value) || v.Value < low || v.Value > high)
                    {
                        _invalid = true;
                        SendError();
                        return new List<ILogicOutputChanged>();
                    }

                    _invalid = false;
                    if (_lastInput != v)
                    {
                        _lastInput = v;
                        _timedOut = false;
                        RestartWatchdog();
                    }
                    SendError();

                    if (_delaying)
                    {
                        _pending = v;
                    }
                    else
                    {
                        SendValue(v.Value);
                    }
                }
            }

            return new List<ILogicOutputChanged>();
        }
    }
}
