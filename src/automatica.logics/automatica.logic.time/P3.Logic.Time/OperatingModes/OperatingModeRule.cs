using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;

namespace P3.Logic.Time.OperatingModes
{
    /// <summary>
    /// A = 1 while the operating mode (parameter, e.g. "vacation") is active, M = all active modes (comma separated, highest priority first).
    /// On = 1 switches the mode on manually, On = 0 hands it back to the operating times (calendar).
    /// The state comes from the core operating modes on the dispatcher and is polled once a minute.
    /// </summary>
    public class OperatingModeRule : Automatica.Core.Logic.Logic
    {
        private readonly object _sync = new();
        private readonly RuleInterfaceInstance _active;
        private readonly RuleInterfaceInstance _modes;
        private readonly OperatingModeWatcher _watcher;
        private System.Timers.Timer _timer;
        private bool _started;

        internal string Mode = OperatingModeContract.PublicHoliday;
        private bool? _lastActive;
        private string _lastModes;

        public OperatingModeRule(ILogicContext context) : base(context)
        {
            _active = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a => a.This2RuleInterfaceTemplate == OperatingModeLogicFactory.OutputActive);
            _modes = context.RuleInstance.RuleInterfaceInstance.SingleOrDefault(a => a.This2RuleInterfaceTemplate == OperatingModeLogicFactory.OutputModes);
            _watcher = new OperatingModeWatcher(context.Dispatcher);
        }

        protected override Task<bool> Start(RuleInstance instance, CancellationToken token = default)
        {
            lock (_sync)
            {
                _timer = new System.Timers.Timer { AutoReset = false };
                _timer.Elapsed += _timer_Elapsed;
                _started = true;
                _lastActive = null;
                _lastModes = null;
            }
            return base.Start(instance, token);
        }

        protected override Task<bool> Stop(RuleInstance instance, CancellationToken token = default)
        {
            lock (_sync)
            {
                _started = false;
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
            if (instance.This2RuleInterfaceTemplate != OperatingModeLogicFactory.ParamMode)
            {
                return;
            }

            lock (_sync)
            {
                Mode = string.IsNullOrWhiteSpace(value?.ToString()) ? OperatingModeContract.PublicHoliday : value.ToString()!.Trim();
                _lastActive = null;
                Evaluate(OperatingModeWatcher.PollMilliseconds);
            }
        }

        protected override IList<ILogicOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            if (instance.This2RuleInterfaceTemplate == OperatingModeLogicFactory.InputOn)
            {
                lock (_sync)
                {
                    _watcher.Request(Mode, TimerHelper.IsTruthy(value) ? true : null);
                    // the core answers on the dispatcher, look again soon
                    Evaluate(1000);
                }
            }
            return new List<ILogicOutputChanged>();
        }

        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            lock (_sync)
            {
                Evaluate(OperatingModeWatcher.PollMilliseconds);
            }
        }

        private void Evaluate(double nextCheckMs)
        {
            if (!_started || _timer == null)
            {
                return;
            }

            _watcher.Refresh();
            var snapshot = _watcher.Current;
            var active = snapshot.IsActive(Mode);
            if (_lastActive != active)
            {
                _lastActive = active;
                Dispatch(_active, active);
            }

            var modes = string.Join(",", snapshot.Active);
            if (_lastModes != modes)
            {
                _lastModes = modes;
                Dispatch(_modes, modes);
            }

            _timer.Stop();
            _timer.Interval = TimerHelper.ClampInterval(nextCheckMs);
            _timer.Start();
        }

        private void Dispatch(RuleInterfaceInstance output, object value)
        {
            if (output != null)
            {
                Context.Dispatcher.DispatchValue(new LogicOutputChanged(output, value).Instance, value);
            }
        }
    }
}
