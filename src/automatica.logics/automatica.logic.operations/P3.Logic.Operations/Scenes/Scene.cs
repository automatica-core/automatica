using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Automatica.Core.Base.IO;
using Automatica.Core.Control.Base;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;

namespace P3.Logic.Operations.Scenes
{
    /// <summary>
    /// Every value on Act that is not 0/false (the visualization button sends true) pulses Q: Q goes to 1 and back to 0
    /// after <see cref="PulseMilliseconds"/>. An activation during the pulse sends 0 and 1 again, so every activation is an
    /// edge. While Off is 1 the scene is locked: Act is ignored and L is 1.
    /// The class is a control (type "Scene"), so the automation editor can offer it for the trigger "scene is activated".
    /// </summary>
    public class Scene : Automatica.Core.Logic.Logic, IControl
    {
        internal const double PulseMilliseconds = 500;

        private readonly RuleInterfaceInstance _activated;
        private readonly RuleInterfaceInstance _locked;
        private readonly object _sync = new();
        private System.Timers.Timer _timer;
        private bool _high;
        private bool _isLocked;

        public Scene(ILogicContext context) : base(context)
        {
            _activated = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a => a.This2RuleInterfaceTemplate == SceneLogicFactory.RuleActivated);
            _locked = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a => a.This2RuleInterfaceTemplate == SceneLogicFactory.RuleLocked);
        }

        public Guid Id => Context.RuleInstance.ObjId;
        public string Name => Context.RuleInstance.Name;

        internal bool IsLocked => _isLocked;

        /// <summary>
        /// Number of accepted activations since the start
        /// </summary>
        internal long Activations { get; private set; }

        protected override Task<bool> Start(RuleInstance instance, CancellationToken token = default)
        {
            lock (_sync)
            {
                _timer ??= CreateTimer();
                _high = false;
                // a known 0 before the first activation
                Dispatch(false);
            }
            return base.Start(instance, token);
        }

        private System.Timers.Timer CreateTimer()
        {
            var timer = new System.Timers.Timer { AutoReset = false, Interval = PulseMilliseconds };
            timer.Elapsed += _timer_Elapsed;
            return timer;
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
                _high = false;
            }
            return base.Stop(instance, token);
        }

        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            lock (_sync)
            {
                if (!_high)
                {
                    return;
                }
                _high = false;
                Dispatch(false);
            }
        }

        private void Dispatch(bool value)
        {
            if (_activated != null)
            {
                Context.Dispatcher.DispatchValue(new LogicOutputChanged(_activated, value).Instance, value);
            }
        }

        internal static bool IsTruthy(object value)
        {
            switch (value)
            {
                case null:
                    return false;
                case bool b:
                    return b;
                case string s:
                    if (bool.TryParse(s, out var sb))
                    {
                        return sb;
                    }
                    return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var sd) && sd != 0;
                case IConvertible c:
                    try
                    {
                        return c.ToDouble(CultureInfo.InvariantCulture) != 0;
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                default:
                    return false;
            }
        }

        protected override IList<ILogicOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            if (instance.This2RuleInterfaceTemplate == SceneLogicFactory.RuleOff)
            {
                var locked = IsTruthy(value);
                lock (_sync)
                {
                    if (locked == _isLocked)
                    {
                        return new List<ILogicOutputChanged>();
                    }
                    _isLocked = locked;
                }
                return _locked == null ? new List<ILogicOutputChanged>() : SingleOutputChanged(new LogicOutputChanged(_locked, locked));
            }

            if (instance.This2RuleInterfaceTemplate == SceneLogicFactory.RuleActivate && IsTruthy(value))
            {
                Activate();
            }

            return new List<ILogicOutputChanged>();
        }

        /// <summary>
        /// Pulses Q unless the scene is locked or not started
        /// </summary>
        internal bool Activate()
        {
            lock (_sync)
            {
                if (_isLocked || _timer == null)
                {
                    return false;
                }

                if (_high)
                {
                    // still high from the previous activation: a new edge
                    Dispatch(false);
                }
                _high = true;
                Activations++;
                Dispatch(true);

                _timer.Stop();
                _timer.Start();
                return true;
            }
        }
    }
}
