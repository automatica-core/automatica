using System;
using Automatica.Core.Base.IO;
using Automatica.Core.Logic;
using P3.Logic.Lightning.Common;
using F = P3.Logic.Lightning.ConstantBrightness.ConstantBrightnessLogicFactory;

namespace P3.Logic.Lightning.ConstantBrightness
{
    /// <summary>
    /// While Act is 1: every interval the level goes one step up when the brightness is below target − hysteresis, one step down when
    /// it is above target + hysteresis (hysteresis in % of the target), otherwise it stays. Act 1 starts from off (a bright room stays dark),
    /// Act 0 and Off switch off. Set forces a level, the control continues from there while Act is 1.
    /// </summary>
    public class ConstantBrightnessLogic : ClockedLogic
    {
        private double _target = F.DefaultTarget;
        private double _hysteresis = F.DefaultHysteresis;
        private double _step = F.DefaultStep;
        private double _interval = F.DefaultInterval;

        private double? _brightness;
        private bool _active;
        private bool _offEdge;
        private double _level;
        private DateTime? _nextStep;

        public ConstantBrightnessLogic(ILogicContext context) : base(context)
        {
        }

        internal double CurrentLevel => _level;

        protected override void ApplyParameter(Guid template, object value)
        {
            if (template == F.Target)
            {
                _target = Math.Max(LightingValues.ToDouble(value, F.DefaultTarget), 0);
            }
            else if (template == F.Hysteresis)
            {
                _hysteresis = Math.Clamp(LightingValues.ToDouble(value, F.DefaultHysteresis), 0, 100);
            }
            else if (template == F.Step)
            {
                _step = Math.Clamp(LightingValues.ToDouble(value, F.DefaultStep), 0.1, 100);
            }
            else if (template == F.Interval)
            {
                _interval = Math.Max(LightingValues.ToDouble(value, F.DefaultInterval), 0.5);
            }
        }

        protected override void OnInput(Guid template, object value, IDispatchable source, DateTime now)
        {
            if (template == F.Brightness)
            {
                var lux = LightingValues.ToDouble(value, double.NaN);
                _brightness = double.IsNaN(lux) ? null : Math.Max(lux, 0);
            }
            else if (template == F.Active)
            {
                var active = LightingValues.IsTruthy(value);
                if (active && !_active)
                {
                    _active = true;
                    Regulate(now);
                }
                else if (!active && _active)
                {
                    _active = false;
                    _nextStep = null;
                    SetLevel(0);
                }
            }
            else if (template == F.Set)
            {
                var level = LightingValues.ToDouble(value, double.NaN);
                if (!double.IsNaN(level))
                {
                    SetLevel(level);
                    if (_active)
                    {
                        _nextStep = now.AddSeconds(_interval);
                    }
                }
            }
            else if (template == F.Off)
            {
                var off = LightingValues.IsTruthy(value);
                if (off && !_offEdge)
                {
                    _active = false;
                    _nextStep = null;
                    SetLevel(0);
                }
                _offEdge = off;
            }
        }

        /// <summary>
        /// One control step and the time of the next one
        /// </summary>
        private void Regulate(DateTime now)
        {
            if (_brightness.HasValue)
            {
                var band = _target * _hysteresis / 100;
                if (_brightness.Value < _target - band && _level < 100)
                {
                    SetLevel(_level + _step);
                }
                else if (_brightness.Value > _target + band && _level > 0)
                {
                    SetLevel(_level - _step);
                }
                else
                {
                    SetLevel(_level);
                }
            }
            else
            {
                SetLevel(_level);
            }
            _nextStep = now.AddSeconds(_interval);
        }

        private void SetLevel(double level)
        {
            _level = Math.Round(Math.Clamp(level, 0, 100), 1);
            Output(Interface(F.Level), _level);
        }

        protected override DateTime? OnTick(DateTime now)
        {
            if (_active && _nextStep.HasValue && now >= _nextStep.Value)
            {
                Regulate(now);
            }
            return _active ? _nextStep : null;
        }
    }
}
