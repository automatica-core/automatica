using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;

namespace P3.Logic.Math.BasicOperations.Pid
{
    /// <summary>
    /// PID controller, calculated once per cycle: O = Kp*e + Ki*∫e dt + Kd*d(-actual)/dt with e = setpoint - actual.
    /// The output is limited to [min, max]; the integral stops growing while the output is at a limit (anti-windup).
    /// Ki = 0 and Kd = 0 make it a P controller, Kd = 0 a PI controller. A negative Kp reverses the direction (cooling).
    /// EN = 0 sends min and clears the integral. An unlinked EN counts as enabled.
    /// </summary>
    public class PidControllerRule : Automatica.Core.Logic.Logic
    {
        private readonly RuleInterfaceInstance _output;
        private readonly object _sync = new();
        private System.Timers.Timer _timer;

        internal double Kp = PidControllerLogicFactory.DefaultKp;
        internal double Ki = PidControllerLogicFactory.DefaultKi;
        internal double Kd;
        internal double CycleSeconds = PidControllerLogicFactory.DefaultCycle;
        internal double OutMin;
        internal double OutMax = PidControllerLogicFactory.DefaultMax;

        private double? _actual;
        private double? _setpoint;
        private double? _previousActual;
        private bool _enabled = true;
        private double _integral;

        public PidControllerRule(ILogicContext context) : base(context)
        {
            _output = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a => a.This2RuleInterfaceTemplate == PidControllerLogicFactory.RuleOutput);
        }

        internal double Integral => _integral;

        protected override Task<bool> Start(RuleInstance instance, CancellationToken token = default)
        {
            _timer?.Dispose();
            _timer = new System.Timers.Timer(CycleSeconds * 1000) { AutoReset = true };
            _timer.Elapsed += _timer_Elapsed;
            _timer.Start();
            return base.Start(instance, token);
        }

        protected override Task<bool> Stop(RuleInstance instance, CancellationToken token = default)
        {
            if (_timer != null)
            {
                _timer.Elapsed -= _timer_Elapsed;
                _timer.Dispose();
                _timer = null;
            }
            return base.Stop(instance, token);
        }

        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            var value = Compute();
            if (value.HasValue)
            {
                Context.Dispatcher.DispatchValue(new LogicOutputChanged(_output, value.Value).Instance, value.Value);
            }
        }

        protected override void ParameterValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            var template = instance.This2RuleInterfaceTemplate;
            lock (_sync)
            {
                if (template == PidControllerLogicFactory.RuleParamKp)
                {
                    Kp = MathValue.ToDouble(value) ?? PidControllerLogicFactory.DefaultKp;
                }
                else if (template == PidControllerLogicFactory.RuleParamKi)
                {
                    Ki = MathValue.ToDouble(value) ?? PidControllerLogicFactory.DefaultKi;
                }
                else if (template == PidControllerLogicFactory.RuleParamKd)
                {
                    Kd = MathValue.ToDouble(value) ?? 0;
                }
                else if (template == PidControllerLogicFactory.RuleParamMin)
                {
                    OutMin = MathValue.ToDouble(value) ?? 0;
                }
                else if (template == PidControllerLogicFactory.RuleParamMax)
                {
                    OutMax = MathValue.ToDouble(value) ?? PidControllerLogicFactory.DefaultMax;
                }
                else if (template == PidControllerLogicFactory.RuleParamCycle)
                {
                    var cycle = MathValue.ToDouble(value) ?? PidControllerLogicFactory.DefaultCycle;
                    CycleSeconds = cycle > 0 ? cycle : PidControllerLogicFactory.DefaultCycle;
                    if (_timer != null)
                    {
                        _timer.Interval = CycleSeconds * 1000;
                    }
                }
            }

            base.ParameterValueChanged(instance, source, value);
        }

        protected override IList<ILogicOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            var template = instance.This2RuleInterfaceTemplate;
            lock (_sync)
            {
                if (template == PidControllerLogicFactory.RuleActual)
                {
                    _actual = MathValue.ToDouble(value);
                }
                else if (template == PidControllerLogicFactory.RuleSetpoint)
                {
                    _setpoint = MathValue.ToDouble(value);
                }
                else if (template == PidControllerLogicFactory.RuleEnable)
                {
                    _enabled = value == null || MathValue.ToBool(value);
                }
            }

            // the output is calculated in the cycle only - the integral needs a fixed time step
            return new List<ILogicOutputChanged>();
        }

        /// <summary>
        /// One controller cycle. Returns the new output, or null while actual value or setpoint are unknown.
        /// </summary>
        internal double? Compute()
        {
            lock (_sync)
            {
                var low = System.Math.Min(OutMin, OutMax);
                var high = System.Math.Max(OutMin, OutMax);

                if (!_enabled)
                {
                    _integral = 0;
                    _previousActual = null;
                    return low;
                }

                if (!_actual.HasValue || !_setpoint.HasValue)
                {
                    return null;
                }

                var dt = CycleSeconds;
                var actual = _actual.Value;
                var error = _setpoint.Value - actual;

                // derivative on the measurement - a setpoint step does not kick the output
                var derivative = _previousActual.HasValue ? -(actual - _previousActual.Value) / dt : 0;
                _previousActual = actual;

                var integral = _integral + error * dt;
                var output = Kp * error + Ki * integral + Kd * derivative;

                // anti-windup: while the integral pushes into a limit, it grows only up to the value that puts the output on the limit
                var limit = output > high && Ki * error > 0 ? high
                    : output < low && Ki * error < 0 ? low
                    : (double?)null;
                if (limit.HasValue)
                {
                    var atLimit = (limit.Value - Kp * error - Kd * derivative) / Ki;
                    integral = System.Math.Clamp(atLimit, System.Math.Min(_integral, integral), System.Math.Max(_integral, integral));
                    output = Kp * error + Ki * integral + Kd * derivative;
                }

                _integral = integral;
                return System.Math.Clamp(output, low, high);
            }
        }
    }
}
