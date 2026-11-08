using System;
using System.Collections.Generic;
using Automatica.Core.Base.Templates;
using Automatica.Core.Logic;
using P3.Logic.Operations.Climate.Common;

namespace P3.Logic.Operations.Climate.MixingValve
{
    public class MixingValveLogicFactory : LogicFactory
    {
        public static readonly Guid Id = new Guid("42fa8838-bba6-4436-b92d-cef2a1b5a13a");

        public static readonly Guid InTarget = ClimateTemplateBuilder.InputId(Id, "tt");
        public static readonly Guid InCurrent = ClimateTemplateBuilder.InputId(Id, "tc");
        public static readonly Guid InOff = ClimateTemplateBuilder.InputId(Id, "off");
        public static readonly Guid InDisablePeriphery = ClimateTemplateBuilder.InputId(Id, "dispc");

        public static readonly Guid ParamTravel = ClimateTemplateBuilder.ParamId(Id, "td");
        public static readonly Guid ParamSampling = ClimateTemplateBuilder.ParamId(Id, "st");
        public static readonly Guid ParamKp = ClimateTemplateBuilder.ParamId(Id, "kp");
        public static readonly Guid ParamKi = ClimateTemplateBuilder.ParamId(Id, "ki");
        public static readonly Guid ParamOffMode = ClimateTemplateBuilder.ParamId(Id, "offm");
        public static readonly Guid ParamMode = ClimateTemplateBuilder.ParamId(Id, "mode");
        public static readonly Guid ParamMinPosition = ClimateTemplateBuilder.ParamId(Id, "minp");
        public static readonly Guid ParamMaxPosition = ClimateTemplateBuilder.ParamId(Id, "maxp");
        public static readonly Guid ParamInvert = ClimateTemplateBuilder.ParamId(Id, "inv");

        public static readonly Guid OutValve = ClimateTemplateBuilder.OutputId(Id, "v");
        public static readonly Guid OutOpen = ClimateTemplateBuilder.OutputId(Id, "o");
        public static readonly Guid OutClose = ClimateTemplateBuilder.OutputId(Id, "c");
        public static readonly Guid OutError = ClimateTemplateBuilder.OutputId(Id, "error");

        internal const double DefaultTravel = 150;
        internal const double DefaultSampling = 40;
        internal const double DefaultKp = 2;
        internal const double DefaultKi = 0.03;

        public override string LogicName => "Climate.MixingValve";
        public override Guid LogicGuid => Id;
        public override Version LogicVersion => new Version(1, 0, 0, 0);

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            var b = new ClimateTemplateBuilder(factory, Id, "MIXING_VALVE", "climate.mixing-valve");
            b.Input("tt", "Tt", 1);
            b.Input("tc", "Tc", 2);
            b.Input("off", "Off", 3);
            b.Input("dispc", "DisPc", 4);

            b.Output("v", "V", 1);
            b.Output("o", "O", 2);
            b.Output("c", "C", 3);
            b.Output("error", "Error", 4);

            b.Number("td", 1, DefaultTravel);
            b.Number("st", 2, DefaultSampling);
            b.Number("kp", 3, DefaultKp);
            b.Number("ki", 4, DefaultKi);
            b.Enum("offm", 5, 2, (0, "UNCHANGED"), (1, "OPEN"), (2, "CLOSE"));
            b.Enum("mode", 6, 0, (0, "NORMAL"), (1, "INVERTED"));
            b.Number("minp", 7, 0);
            b.Number("maxp", 8, 100);
            b.Bool("inv", 9, false);
            b.VisuTile(ClimateTile.Central);
        }

        public override ILogic CreateLogicInstance(ILogicContext context) => new MixingValveLogic(context);
    }

    /// <summary>
    /// Mixing valve controller. Every sampling time (St) a PI controller calculates the valve position (MinP..MaxP %) from target (Tt)
    /// and current (Tc) flow temperature; inverted mode (cooling) opens while the temperature is too high.
    /// V is the position for 0-100 % actuators (Inv: 100 - position). For 3-point actuators the block tracks the position from the
    /// travel time (Td) and drives O (open) / C (close) until it is reached; moves to 0 or 100 % run on for 20 % of Td (calibration).
    /// Off sets the position per Offm (unchanged / open / close). DisPc = 1 freezes the target temperature.
    /// Error = 1 when Tt and Tc are more than 5 K apart for longer than 10 minutes.
    /// </summary>
    public class MixingValveLogic : ClimateLogic
    {
        internal const double MinStep = 1;
        internal const double ErrorDeviation = 5;
        internal const double ErrorSeconds = 600;
        internal const double CalibrationFactor = 0.2;

        private readonly PiController _pi = new();
        private double? _acceptedTarget;
        private double _sampleAccumulator;
        private bool _firstSample = true;

        private double _target;
        private bool _hasTarget;
        private double _position;
        private bool _moving;
        private double _calibrationLeft;
        private int _direction;
        private double _errorSeconds;
        private bool _wasOff;

        public MixingValveLogic(ILogicContext context) : base(context)
        {
        }

        protected override double TickSeconds => 1;

        internal double Position => _position;
        internal double TargetPosition => _target;

        protected override void OnInput(Guid template, object value, object previous)
        {
            if (template == MixingValveLogicFactory.InTarget && !InBool(MixingValveLogicFactory.InDisablePeriphery))
            {
                _acceptedTarget = ClimateValue.ToDouble(value);
            }
            else if (template == MixingValveLogicFactory.InDisablePeriphery && !ClimateValue.ToBool(value))
            {
                _acceptedTarget = InNumber(MixingValveLogicFactory.InTarget);
            }
        }

        protected override void OnTick(double elapsedSeconds)
        {
            var off = InBool(MixingValveLogicFactory.InOff);
            if (off != _wasOff)
            {
                _errorSeconds = 0;
                _firstSample = true;
                _sampleAccumulator = 0;
            }
            _wasOff = off;

            var minP = Clamp(Param(MixingValveLogicFactory.ParamMinPosition, 0), 0, 100);
            var maxP = Clamp(Param(MixingValveLogicFactory.ParamMaxPosition, 100), 0, 100);

            if (off)
            {
                switch (ParamInt(MixingValveLogicFactory.ParamOffMode, 2))
                {
                    case 1:
                        SetTarget(100);
                        break;
                    case 2:
                        SetTarget(0);
                        break;
                }
                _pi.Reset(_target);
            }
            else
            {
                var sampling = Math.Max(1, Param(MixingValveLogicFactory.ParamSampling, MixingValveLogicFactory.DefaultSampling));
                _sampleAccumulator += elapsedSeconds;
                if (_firstSample || _sampleAccumulator >= sampling)
                {
                    _sampleAccumulator = _firstSample ? 0 : _sampleAccumulator - sampling;
                    Sample(_firstSample ? 0 : sampling, minP, maxP);
                }
            }

            Drive(elapsedSeconds);
            UpdateError(elapsedSeconds, off);
        }

        private void Sample(double dt, double minP, double maxP)
        {
            var current = InNumber(MixingValveLogicFactory.InCurrent);
            if (!_acceptedTarget.HasValue || !current.HasValue)
            {
                return;
            }
            if (_firstSample)
            {
                // continue from the tracked position, not from closed
                _pi.Reset(_position);
                _firstSample = false;
            }
            var error = _acceptedTarget.Value - current.Value;
            if (ParamInt(MixingValveLogicFactory.ParamMode, 0) == 1)
            {
                error = -error;
            }
            _pi.Kp = Param(MixingValveLogicFactory.ParamKp, MixingValveLogicFactory.DefaultKp);
            _pi.Ki = Param(MixingValveLogicFactory.ParamKi, MixingValveLogicFactory.DefaultKi);
            _pi.Min = minP;
            _pi.Max = maxP;
            SetTarget(_pi.Step(error, dt));
        }

        private void SetTarget(double target)
        {
            target = Clamp(target, 0, 100);
            var endPosition = target <= 0 || target >= 100;
            // the real position is unknown until the first end position run - the first one is always calibrated
            var changed = !_hasTarget || Math.Abs(_target - target) > 1e-9;
            _hasTarget = true;
            if (!_moving && Math.Abs(target - _position) < MinStep && !(endPosition && changed))
            {
                _target = target;
                return;
            }
            if (endPosition && changed)
            {
                _calibrationLeft = CalibrationFactor * Travel;
            }
            _target = target;
            _moving = true;
        }

        private double Travel => Math.Max(1, Param(MixingValveLogicFactory.ParamTravel, MixingValveLogicFactory.DefaultTravel));

        private void Drive(double elapsedSeconds)
        {
            if (!_moving)
            {
                _direction = 0;
                return;
            }
            var step = 100 * elapsedSeconds / Travel;
            if (_position < _target - 1e-9)
            {
                _direction = 1;
                _position = Math.Min(_target, _position + step);
                return;
            }
            if (_position > _target + 1e-9)
            {
                _direction = -1;
                _position = Math.Max(_target, _position - step);
                return;
            }

            // reached: an end position runs on for the calibration time
            if ((_target <= 0 || _target >= 100) && _calibrationLeft > 0)
            {
                _direction = _target >= 100 ? 1 : -1;
                _calibrationLeft -= elapsedSeconds;
                if (_calibrationLeft > 0)
                {
                    return;
                }
            }
            _calibrationLeft = 0;
            _moving = false;
            _direction = 0;
        }

        private void UpdateError(double elapsedSeconds, bool off)
        {
            var current = InNumber(MixingValveLogicFactory.InCurrent);
            if (off || !_acceptedTarget.HasValue || !current.HasValue || Math.Abs(_acceptedTarget.Value - current.Value) <= ErrorDeviation)
            {
                _errorSeconds = 0;
                return;
            }
            _errorSeconds += elapsedSeconds;
        }

        protected override void Compute(IDictionary<Guid, object> outputs)
        {
            var valve = ParamBool(MixingValveLogicFactory.ParamInvert, false) ? 100 - _target : _target;
            Set(outputs, MixingValveLogicFactory.OutValve, valve, 1);
            outputs[MixingValveLogicFactory.OutOpen] = _direction > 0;
            outputs[MixingValveLogicFactory.OutClose] = _direction < 0;
            outputs[MixingValveLogicFactory.OutError] = _errorSeconds > ErrorSeconds;
        }
    }
}
