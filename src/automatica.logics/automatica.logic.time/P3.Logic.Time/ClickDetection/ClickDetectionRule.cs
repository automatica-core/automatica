using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;

namespace P3.Logic.Time.ClickDetection
{
    /// <summary>
    /// Tells single, double and triple clicks and a long press of one push button apart.
    /// The button sends 1 on press and 0 on release. Clicks are counted until no further click follows
    /// within the click window, then a pulse goes to the matching output (single clicks are therefore
    /// reported one click window late). Holding the button for the long press time sets L to 1 until release.
    /// Long press time 0 turns long press detection off; then every 1 is a click, for buttons that send no release.
    /// </summary>
    public class ClickDetectionRule : Automatica.Core.Logic.Logic
    {
        private enum Phase
        {
            Idle,
            WaitLongPress,
            WaitNextClick
        }

        private readonly RuleInterfaceInstance _single;
        private readonly RuleInterfaceInstance _double;
        private readonly RuleInterfaceInstance _triple;
        private readonly RuleInterfaceInstance _long;
        private readonly object _sync = new();
        private System.Timers.Timer _timer;

        internal int ClickWindowMs = ClickDetectionLogicFactory.DefaultClickWindow;
        internal int LongPressMs = ClickDetectionLogicFactory.DefaultLongPress;

        private Phase _phase = Phase.Idle;
        private bool _pressed;
        private bool _longActive;
        private int _clicks;

        public ClickDetectionRule(ILogicContext context) : base(context)
        {
            RuleInterfaceInstance Find(Guid template) => context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a => a.This2RuleInterfaceTemplate == template);

            _single = Find(ClickDetectionLogicFactory.RuleSingle);
            _double = Find(ClickDetectionLogicFactory.RuleDouble);
            _triple = Find(ClickDetectionLogicFactory.RuleTriple);
            _long = Find(ClickDetectionLogicFactory.RuleLong);
        }

        internal static int ToMilliseconds(object value, int fallback)
        {
            try
            {
                var ms = value is string s
                    ? double.Parse(s, NumberStyles.Float, CultureInfo.InvariantCulture)
                    : Convert.ToDouble(value, CultureInfo.InvariantCulture);
                return double.IsNaN(ms) || ms < 0 ? fallback : (int)Math.Min(ms, int.MaxValue);
            }
            catch (Exception)
            {
                return fallback;
            }
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
                if (_timer != null)
                {
                    _timer.Elapsed -= _timer_Elapsed;
                    _timer.Dispose();
                    _timer = null;
                }
                _phase = Phase.Idle;
                _pressed = false;
                _longActive = false;
                _clicks = 0;
            }
            return base.Stop(instance, token);
        }

        protected override void ParameterValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            if (instance.This2RuleInterfaceTemplate == ClickDetectionLogicFactory.RuleParamClickWindow)
            {
                ClickWindowMs = Math.Max(ToMilliseconds(value, ClickDetectionLogicFactory.DefaultClickWindow), 1);
            }
            else if (instance.This2RuleInterfaceTemplate == ClickDetectionLogicFactory.RuleParamLongPress)
            {
                LongPressMs = ToMilliseconds(value, ClickDetectionLogicFactory.DefaultLongPress);
            }

            base.ParameterValueChanged(instance, source, value);
        }

        private void Arm(Phase phase, int milliseconds)
        {
            _phase = phase;
            if (_timer == null)
            {
                return;
            }
            _timer.Stop();
            _timer.Interval = TimerHelper.ClampInterval(milliseconds);
            _timer.Start();
        }

        private void Disarm()
        {
            _phase = Phase.Idle;
            _timer?.Stop();
        }

        private void Dispatch(RuleInterfaceInstance output, bool value)
        {
            Context.Dispatcher.DispatchValue(new LogicOutputChanged(output, value).Instance, value);
        }

        private void Pulse(RuleInterfaceInstance output)
        {
            Dispatch(output, true);
            Dispatch(output, false);
        }

        private void Click()
        {
            _clicks++;
            if (_clicks >= 3)
            {
                // nothing beyond a triple click - no need to wait for the window
                ReportClicks();
                return;
            }
            Arm(Phase.WaitNextClick, ClickWindowMs);
        }

        private void ReportClicks()
        {
            var clicks = _clicks;
            _clicks = 0;
            Disarm();

            switch (clicks)
            {
                case 0:
                    return;
                case 1:
                    Pulse(_single);
                    return;
                case 2:
                    Pulse(_double);
                    return;
                default:
                    Pulse(_triple);
                    return;
            }
        }

        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            lock (_sync)
            {
                switch (_phase)
                {
                    case Phase.WaitLongPress:
                        _phase = Phase.Idle;
                        if (_pressed)
                        {
                            // a long press ends the click sequence, earlier short clicks are dropped
                            _clicks = 0;
                            _longActive = true;
                            Dispatch(_long, true);
                        }
                        break;
                    case Phase.WaitNextClick:
                        ReportClicks();
                        break;
                }
            }
        }

        protected override IList<ILogicOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            if (instance.This2RuleInterfaceTemplate != ClickDetectionLogicFactory.RuleInput)
            {
                return new List<ILogicOutputChanged>();
            }

            var pressed = TimerHelper.IsTruthy(value);
            lock (_sync)
            {
                if (LongPressMs <= 0)
                {
                    // no long press - every press is a click, releases do not matter
                    if (pressed)
                    {
                        Click();
                    }
                    return new List<ILogicOutputChanged>();
                }

                if (pressed == _pressed)
                {
                    // repeated telegram
                    return new List<ILogicOutputChanged>();
                }

                _pressed = pressed;
                if (pressed)
                {
                    Arm(Phase.WaitLongPress, LongPressMs);
                }
                else if (_longActive)
                {
                    _longActive = false;
                    Disarm();
                    Dispatch(_long, false);
                }
                else
                {
                    Click();
                }
            }

            return new List<ILogicOutputChanged>();
        }
    }
}
