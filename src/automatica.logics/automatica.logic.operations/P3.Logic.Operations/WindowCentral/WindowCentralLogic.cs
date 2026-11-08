using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.Control.Base;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using Microsoft.Extensions.Logging;
using P3.Logic.Operations.Common;
using P3.Logic.Operations.Garage;

namespace P3.Logic.Operations.WindowCentral
{
    /// <summary>
    /// Central control of motorised windows. The open / close / stop outputs send 1 per command (they fit the up / down / stop
    /// inputs of window actuators and of the shutter block), the position output the target opening for actuators with a
    /// position input. Weather protection closes all windows and locks the central.
    /// The blinds picked in the "windows" parameter (shutter blocks driving window actuators) are moved directly: open = up,
    /// close = down, opening % = 100 - blind position. Their positions count like the feedback inputs, polled every
    /// <see cref="PollInterval"/>.
    /// </summary>
    public class WindowCentralLogic : DispatchingLogic
    {
        private const double ClosedBelow = 0.5;
        internal static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);

        private readonly System.Timers.Timer _poll = new() { AutoReset = true, Interval = PollInterval.TotalMilliseconds };
        private IReadOnlyList<Guid> _windows = Array.Empty<Guid>();
        private bool _running;

        private readonly double?[] _positions = new double?[WindowCentralLogicFactory.InputWindowPositions.Length];
        private int _moving;
        private int _lastDirection = -1;
        private bool _weatherProtection;
        private bool _locked;
        internal int SlightlyOpenPosition = 10;

        public WindowCentralLogic(ILogicContext context) : base(context)
        {
            _poll.Elapsed += (_, _) => Poll();
        }

        internal int Moving => _moving;

        protected override Task<bool> Start(RuleInstance instance, CancellationToken token = default)
        {
            lock (Sync)
            {
                _running = true;
                Send(WindowCentralLogicFactory.OutputLocked, false);
                SendCounts();
            }
            return base.Start(instance, token);
        }

        protected override Task<bool> Stop(RuleInstance instance, CancellationToken token = default)
        {
            lock (Sync)
            {
                _running = false;
                _poll.Stop();
            }
            return base.Stop(instance, token);
        }

        /// <summary>
        /// The picked windows are not linked: their positions are polled
        /// </summary>
        internal void Poll()
        {
            lock (Sync)
            {
                if (_running)
                {
                    SendCounts();
                    FinishWhenArrived();
                }
            }
        }

        private List<IBlind> Windows() => CentralTargets.Resolve(Context, _windows).OfType<IBlind>().ToList();

        /// <summary>
        /// Opening in % (0 closed, 100 open) of the linked windows (feedback inputs) and of the picked ones
        /// </summary>
        private List<double> KnownPositions() =>
            _positions.Where(a => a.HasValue).Select(a => a.Value).Concat(Windows().Select(a => 100.0 - Math.Clamp(a.Position, 0, 100))).ToList();

        private void MoveWindows(Func<IBlind, Task> move)
        {
            foreach (var window in Windows())
            {
                Task task;
                try
                {
                    task = move(window);
                }
                catch (Exception e)
                {
                    task = Task.FromException(e);
                }
                task.ContinueWith(t => Context.Logger.LogWarning(t.Exception, $"Window central {Context.RuleInstance.Name}: moving {window.Name} failed"),
                    TaskContinuationOptions.OnlyOnFaulted);
            }
        }

        /// <summary>
        /// The target opening on the position output and on the picked windows
        /// </summary>
        private void SendPosition(double opening)
        {
            SendAlways(WindowCentralLogicFactory.OutputPosition, opening);
            var blindPosition = (int)Math.Round(100 - opening);
            MoveWindows(w => w.MoveAbsoluteAsync(blindPosition));
        }

        protected override void ParameterValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            if (TemplateOf(instance) == WindowCentralLogicFactory.ParamSlightlyOpen)
            {
                SlightlyOpenPosition = Math.Clamp(LogicValue.ToInt(value, 10), 0, 100);
            }
            else if (TemplateOf(instance) == WindowCentralLogicFactory.ParamWindows)
            {
                lock (Sync)
                {
                    _windows = CentralTargets.Ids(value);
                    if (_running && _windows.Count > 0)
                    {
                        _poll.Start();
                    }
                    else
                    {
                        _poll.Stop();
                    }
                    SendCounts();
                }
            }
        }

        protected override IList<ILogicOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            lock (Sync)
            {
                var template = TemplateOf(instance);
                var on = LogicValue.IsTruthy(value);
                var windowIndex = Array.IndexOf(WindowCentralLogicFactory.InputWindowPositions, template);

                if (windowIndex >= 0)
                {
                    var position = LogicValue.ToDouble(value);
                    _positions[windowIndex] = position.HasValue ? Math.Clamp(position.Value, 0, 100) : null;
                    SendCounts();
                    FinishWhenArrived();
                }
                else if (template == WindowCentralLogicFactory.InputWeatherProtection)
                {
                    _weatherProtection = on;
                    if (on)
                    {
                        Close();
                    }
                    SendLocked();
                }
                else if (template == WindowCentralLogicFactory.InputLock)
                {
                    _locked = on;
                    if (on)
                    {
                        StopAll();
                    }
                    SendLocked();
                }
                else if (_weatherProtection || _locked)
                {
                    // locked: no commands
                }
                else if (template == WindowCentralLogicFactory.InputPartialOpen)
                {
                    if (on)
                    {
                        Open(false);
                    }
                    else
                    {
                        StopAll();
                    }
                }
                else if (template == WindowCentralLogicFactory.InputPartialClose)
                {
                    if (on)
                    {
                        Close(false);
                    }
                    else
                    {
                        StopAll();
                    }
                }
                else if (template == WindowCentralLogicFactory.InputPosition)
                {
                    var position = LogicValue.ToDouble(value);
                    if (position.HasValue)
                    {
                        SendPosition(Math.Clamp(position.Value, 0, 100));
                    }
                }
                else if (on)
                {
                    if (template == WindowCentralLogicFactory.InputCompleteOpen)
                    {
                        Open();
                    }
                    else if (template == WindowCentralLogicFactory.InputCompleteClose)
                    {
                        Close();
                    }
                    else if (template == WindowCentralLogicFactory.InputSlightlyOpen)
                    {
                        SendPosition(SlightlyOpenPosition);
                    }
                    else if (template == WindowCentralLogicFactory.InputToggle)
                    {
                        Toggle();
                    }
                }
            }
            return new List<ILogicOutputChanged>();
        }

        /// <summary>
        /// open, stop, close, stop
        /// </summary>
        private void Toggle()
        {
            if (_moving != 0)
            {
                StopAll();
            }
            else if (_lastDirection > 0 || AnyOpen)
            {
                Close();
            }
            else
            {
                Open();
            }
        }

        private bool AnyOpen => KnownPositions().Any(a => a > ClosedBelow);

        private void Open(bool withPosition = true)
        {
            _moving = 1;
            _lastDirection = 1;
            SendAlways(WindowCentralLogicFactory.OutputOpen, true);
            if (withPosition)
            {
                SendAlways(WindowCentralLogicFactory.OutputPosition, 100.0);
            }
            MoveWindows(w => w.MoveUpAsync());
        }

        private void Close(bool withPosition = true)
        {
            _moving = -1;
            _lastDirection = -1;
            SendAlways(WindowCentralLogicFactory.OutputClose, true);
            if (withPosition)
            {
                SendAlways(WindowCentralLogicFactory.OutputPosition, 0.0);
            }
            MoveWindows(w => w.MoveDownAsync());
        }

        private void StopAll()
        {
            _moving = 0;
            SendAlways(WindowCentralLogicFactory.OutputStop, true);
            MoveWindows(w => w.StopAsync());
        }

        /// <summary>
        /// The toggle cycle continues with the other direction once all windows with feedback reached the end
        /// </summary>
        private void FinishWhenArrived()
        {
            var known = KnownPositions();
            if (_moving == 0 || known.Count == 0)
            {
                return;
            }
            if (_moving > 0 ? known.All(a => a >= 100 - ClosedBelow) : known.All(a => a <= ClosedBelow))
            {
                _moving = 0;
            }
        }

        private void SendLocked()
        {
            Send(WindowCentralLogicFactory.OutputLocked, _weatherProtection || _locked);
        }

        private void SendCounts()
        {
            var known = KnownPositions();
            Send(WindowCentralLogicFactory.OutputOpenCount, known.Count(a => a > ClosedBelow));
            Send(WindowCentralLogicFactory.OutputClosedCount, known.Count(a => a <= ClosedBelow));
        }
    }
}
