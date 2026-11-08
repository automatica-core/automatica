using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;

namespace P3.Logic.Math.BasicOperations.Analog
{
    /// <summary>
    /// Moving average (Loxone "Moving Average"): every C seconds the current V is put into a buffer of at most N entries
    /// (the oldest one drops out) and Avg is the average of the buffer. The first V is sampled right away, so Avg does not
    /// stay empty for a whole cycle. C = 0 samples every received V instead of polling.
    /// While R is 1 the averaging is off: the buffer is emptied and Avg follows V.
    /// </summary>
    public class MovingAverageRule : Automatica.Core.Logic.Logic
    {
        private readonly RuleInterfaceInstance _output;
        private readonly object _sync = new();
        private readonly Queue<double> _buffer = new();
        private System.Timers.Timer _timer;

        internal double CycleSeconds = MovingAverageLogicFactory.DefaultCycle;
        internal int Count = MovingAverageLogicFactory.DefaultCount;

        private double? _value;
        private double _sum;
        private bool _reset;
        private bool _polling;

        public MovingAverageRule(ILogicContext context) : base(context)
        {
            _output = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a => a.This2RuleInterfaceTemplate == MovingAverageLogicFactory.RuleOutput);
        }

        internal int Entries => _buffer.Count;

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
                _polling = false;
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
                if (template == MovingAverageLogicFactory.RuleParamCycle)
                {
                    CycleSeconds = System.Math.Max(0, MathValue.ToDouble(value) ?? MovingAverageLogicFactory.DefaultCycle);
                }
                else if (template == MovingAverageLogicFactory.RuleParamCount)
                {
                    var count = MathValue.ToDouble(value) ?? MovingAverageLogicFactory.DefaultCount;
                    Count = (int)System.Math.Clamp(count, 1, MovingAverageLogicFactory.MaximumCount);
                    Trim();
                }
            }

            base.ParameterValueChanged(instance, source, value);
        }

        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            lock (_sync)
            {
                if (!_polling || _timer == null)
                {
                    return;
                }

                _timer.Stop();
                if (_value.HasValue && !_reset)
                {
                    Sample(_value.Value);
                }
                Schedule();
            }
        }

        private void Schedule()
        {
            if (CycleSeconds <= 0 || _reset)
            {
                _polling = false;
                _timer.Stop();
                return;
            }

            _polling = true;
            _timer.Stop();
            _timer.Interval = System.Math.Clamp(CycleSeconds * 1000, 1, int.MaxValue);
            _timer.Start();
        }

        private void Trim()
        {
            while (_buffer.Count > Count)
            {
                _sum -= _buffer.Dequeue();
            }
        }

        private void Sample(double value)
        {
            _buffer.Enqueue(value);
            _sum += value;
            Trim();
            // the running sum drifts with many additions - add up again now and then
            if (_buffer.Count == Count)
            {
                _sum = _buffer.Sum();
            }
            Dispatch(_sum / _buffer.Count);
        }

        private void Dispatch(double value)
        {
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

                if (template == MovingAverageLogicFactory.RuleReset)
                {
                    var reset = MathValue.ToBool(value);
                    if (reset && !_reset)
                    {
                        _reset = true;
                        _buffer.Clear();
                        _sum = 0;
                        Schedule();
                        if (_value.HasValue)
                        {
                            Dispatch(_value.Value);
                        }
                    }
                    else if (!reset && _reset)
                    {
                        _reset = false;
                        if (_value.HasValue)
                        {
                            Sample(_value.Value);
                            Schedule();
                        }
                    }
                }
                else if (template == MovingAverageLogicFactory.RuleValue)
                {
                    var v = MathValue.ToDouble(value);
                    if (!v.HasValue)
                    {
                        return new List<ILogicOutputChanged>();
                    }

                    _value = v;
                    if (_reset)
                    {
                        Dispatch(v.Value);
                    }
                    else if (CycleSeconds <= 0 || _buffer.Count == 0 && !_polling)
                    {
                        Sample(v.Value);
                        Schedule();
                    }
                }
            }

            return new List<ILogicOutputChanged>();
        }
    }
}
