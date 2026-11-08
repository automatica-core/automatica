using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using P3.Logic.Operations.Garage;

namespace P3.Logic.Operations.WindowCentral
{
    /// <summary>
    /// Central control of motorised windows. The open / close / stop outputs send 1 per command (they fit the up / down / stop
    /// inputs of window actuators and of the shutter block), the position output the target opening for actuators with a
    /// position input. Weather protection closes all windows and locks the central.
    /// </summary>
    public class WindowCentralLogic : DispatchingLogic
    {
        private const double ClosedBelow = 0.5;

        private readonly double?[] _positions = new double?[WindowCentralLogicFactory.InputWindowPositions.Length];
        private int _moving;
        private int _lastDirection = -1;
        private bool _weatherProtection;
        private bool _locked;
        internal int SlightlyOpenPosition = 10;

        public WindowCentralLogic(ILogicContext context) : base(context)
        {
        }

        internal int Moving => _moving;

        protected override Task<bool> Start(RuleInstance instance, CancellationToken token = default)
        {
            lock (Sync)
            {
                Send(WindowCentralLogicFactory.OutputLocked, false);
                SendCounts();
            }
            return base.Start(instance, token);
        }

        protected override void ParameterValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            if (TemplateOf(instance) == WindowCentralLogicFactory.ParamSlightlyOpen)
            {
                SlightlyOpenPosition = Math.Clamp(LogicValue.ToInt(value, 10), 0, 100);
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
                        SendAlways(WindowCentralLogicFactory.OutputPosition, Math.Clamp(position.Value, 0, 100));
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
                        SendAlways(WindowCentralLogicFactory.OutputPosition, (double)SlightlyOpenPosition);
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

        private bool AnyOpen => _positions.Any(a => a is > ClosedBelow);

        private void Open(bool withPosition = true)
        {
            _moving = 1;
            _lastDirection = 1;
            SendAlways(WindowCentralLogicFactory.OutputOpen, true);
            if (withPosition)
            {
                SendAlways(WindowCentralLogicFactory.OutputPosition, 100.0);
            }
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
        }

        private void StopAll()
        {
            _moving = 0;
            SendAlways(WindowCentralLogicFactory.OutputStop, true);
        }

        /// <summary>
        /// The toggle cycle continues with the other direction once all windows with feedback reached the end
        /// </summary>
        private void FinishWhenArrived()
        {
            var known = _positions.Where(a => a.HasValue).Select(a => a.Value).ToList();
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
            var known = _positions.Where(a => a.HasValue).Select(a => a.Value).ToList();
            Send(WindowCentralLogicFactory.OutputOpenCount, known.Count(a => a > ClosedBelow));
            Send(WindowCentralLogicFactory.OutputClosedCount, known.Count(a => a <= ClosedBelow));
        }
    }
}
