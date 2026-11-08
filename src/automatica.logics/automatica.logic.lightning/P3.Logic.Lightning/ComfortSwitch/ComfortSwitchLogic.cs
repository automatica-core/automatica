using System;
using Automatica.Core.Logic;
using P3.Logic.Lightning.Common;

namespace P3.Logic.Lightning.ComfortSwitch
{
    /// <summary>
    /// Tg is a push button: 1 then 0 within the long-click time is a short press (off: on for the on duration, on: off),
    /// holding it for the long-click time switches the light on permanently (decided while the button is still held).
    /// </summary>
    public class ComfortSwitchLogic : TimedSwitchLogic
    {
        private double _longClick = ComfortSwitchLogicFactory.DefaultLongClickMs / 1000.0;
        private bool _pressed;
        private DateTime? _longClickAt;

        public ComfortSwitchLogic(ILogicContext context) : base(context, ComfortSwitchLogicFactory.Ids)
        {
        }

        internal DateTime? LongClickAt => _longClickAt;

        protected override void ApplyOtherParameter(Guid template, object value)
        {
            if (template == ComfortSwitchLogicFactory.LongClick)
            {
                _longClick = Math.Max(LightingValues.ToDouble(value, ComfortSwitchLogicFactory.DefaultLongClickMs), 50) / 1000.0;
            }
        }

        protected override void OnTrigger(Guid template, object value, DateTime now)
        {
            if (template != ComfortSwitchLogicFactory.Trigger)
            {
                return;
            }

            var pressed = LightingValues.IsTruthy(value);
            if (pressed && !_pressed)
            {
                _longClickAt = now.AddSeconds(_longClick);
            }
            else if (!pressed && _pressed && _longClickAt.HasValue)
            {
                // released before the long-click time: short press
                _longClickAt = null;
                if (IsOn)
                {
                    SwitchOff(now);
                }
                else
                {
                    SwitchOnTimed(now);
                }
            }
            _pressed = pressed;
        }

        protected override DateTime? OnOtherTick(DateTime now)
        {
            if (_longClickAt.HasValue && now >= _longClickAt.Value)
            {
                _longClickAt = null;
                if (_pressed)
                {
                    SwitchOnPermanent(now);
                }
            }
            return _longClickAt;
        }
    }
}
