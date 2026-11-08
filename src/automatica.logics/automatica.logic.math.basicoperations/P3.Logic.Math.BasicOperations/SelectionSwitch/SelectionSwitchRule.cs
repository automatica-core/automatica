using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;

namespace P3.Logic.Math.BasicOperations.SelectionSwitch
{
    /// <summary>
    /// Selection switch (Loxone "Selection Switch (+)" and "Selection Switch +/-").
    /// Every 1 on + (or -) changes O by the step size. The (+) variant starts again at the minimum after the maximum,
    /// the (+/-) variant stops at the limits. V sets O directly (limited to minimum..maximum).
    /// Long click: while + or - stays 1 for longer than <see cref="LongClickSeconds"/>, O keeps changing every repetition
    /// interval until the button sends 0. Repetition 0 turns this off (needed for buttons that send no release).
    /// Off = 1 sets O to the default value and locks the block until Off is 0 again; DisPc = 1 ignores +, - and V.
    /// </summary>
    public class SelectionSwitchRule : Automatica.Core.Logic.Logic
    {
        private readonly SelectionSwitchIds _ids;
        private readonly bool _wrap;
        private readonly RuleInterfaceInstance _output;
        private readonly object _sync = new();
        private System.Timers.Timer _timer;

        /// <summary>
        /// Hold time before a held button starts repeating
        /// </summary>
        internal double LongClickSeconds = 0.5;

        internal double Minimum = SelectionSwitchTemplates.DefaultMinimum;
        internal double Maximum = SelectionSwitchTemplates.DefaultMaximum;
        internal double Step = SelectionSwitchTemplates.DefaultStep;
        internal double RepeatSeconds = SelectionSwitchTemplates.DefaultRepeat;
        internal double Default = SelectionSwitchTemplates.DefaultValue;

        private double _value = SelectionSwitchTemplates.DefaultValue;
        private bool _touched;
        private double? _sent;
        private int _repeatDirection;
        private bool _locked;
        private bool _disabled;

        internal SelectionSwitchRule(ILogicContext context, SelectionSwitchIds ids, bool wrap) : base(context)
        {
            _ids = ids;
            _wrap = wrap;
            _output = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a => a.This2RuleInterfaceTemplate == ids.Output);
        }

        internal double Value => _value;

        private double Low => System.Math.Min(Minimum, Maximum);
        private double High => System.Math.Max(Minimum, Maximum);

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
                _repeatDirection = 0;
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
                if (template == _ids.ParamMinimum)
                {
                    Minimum = MathValue.ToDouble(value) ?? SelectionSwitchTemplates.DefaultMinimum;
                }
                else if (template == _ids.ParamMaximum)
                {
                    Maximum = MathValue.ToDouble(value) ?? SelectionSwitchTemplates.DefaultMaximum;
                }
                else if (template == _ids.ParamStep)
                {
                    Step = System.Math.Abs(MathValue.ToDouble(value) ?? SelectionSwitchTemplates.DefaultStep);
                }
                else if (template == _ids.ParamRepeat)
                {
                    RepeatSeconds = System.Math.Max(0, MathValue.ToDouble(value) ?? SelectionSwitchTemplates.DefaultRepeat);
                }
                else if (template == _ids.ParamDefault)
                {
                    Default = MathValue.ToDouble(value) ?? SelectionSwitchTemplates.DefaultValue;
                    if (!_touched)
                    {
                        // the block starts at the default value
                        _value = Default;
                    }
                }
            }

            base.ParameterValueChanged(instance, source, value);
        }

        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            lock (_sync)
            {
                if (_repeatDirection == 0 || _timer == null)
                {
                    return;
                }

                _timer.Stop();
                StepBy(_repeatDirection);
                Schedule(RepeatSeconds);
            }
        }

        private void Schedule(double seconds)
        {
            _timer.Stop();
            _timer.Interval = System.Math.Clamp(seconds * 1000, 1, int.MaxValue);
            _timer.Start();
        }

        private void StopRepeat()
        {
            _repeatDirection = 0;
            _timer?.Stop();
        }

        private void StepBy(int direction)
        {
            var next = System.Math.Round(_value + direction * Step, 10);
            if (_wrap)
            {
                if (next > High)
                {
                    next = Low;
                }
                else if (next < Low)
                {
                    next = direction > 0 ? Low : High;
                }
            }
            else
            {
                next = System.Math.Clamp(next, Low, High);
            }

            Set(next);
        }

        private void Set(double value)
        {
            _touched = true;
            _value = value;
            if (_sent == value)
            {
                return;
            }

            _sent = value;
            Context.Dispatcher.DispatchValue(new LogicOutputChanged(_output, value).Instance, value);
        }

        private void Button(int direction, bool pressed)
        {
            if (!pressed)
            {
                if (_repeatDirection == direction)
                {
                    StopRepeat();
                }
                return;
            }

            StepBy(direction);
            if (RepeatSeconds > 0)
            {
                _repeatDirection = direction;
                Schedule(LongClickSeconds);
            }
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

                if (template == _ids.Off)
                {
                    if (MathValue.ToBool(value))
                    {
                        _locked = true;
                        StopRepeat();
                        _sent = null;
                        Set(Default);
                    }
                    else
                    {
                        _locked = false;
                    }
                }
                else if (template == _ids.Disable)
                {
                    _disabled = MathValue.ToBool(value);
                    if (_disabled)
                    {
                        StopRepeat();
                    }
                }
                else if (_locked || _disabled)
                {
                    return new List<ILogicOutputChanged>();
                }
                else if (template == _ids.Plus)
                {
                    Button(1, MathValue.ToBool(value));
                }
                else if (template == _ids.Minus)
                {
                    Button(-1, MathValue.ToBool(value));
                }
                else if (template == _ids.Set)
                {
                    var v = MathValue.ToDouble(value);
                    if (v.HasValue)
                    {
                        Set(System.Math.Clamp(v.Value, Low, High));
                    }
                }
            }

            return new List<ILogicOutputChanged>();
        }
    }
}
