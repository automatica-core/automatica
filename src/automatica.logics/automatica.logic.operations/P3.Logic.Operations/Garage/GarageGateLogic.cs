using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Automatica.Core.Base.IO;
using Automatica.Core.Control.Base;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;

namespace P3.Logic.Operations.Garage
{
    /// <summary>
    /// State output of the Garage / Gate block
    /// </summary>
    public enum GarageState
    {
        Closed = 0,
        Open = 1,
        PartiallyOpen = 2,
        Opening = 3,
        Closing = 4
    }

    /// <summary>
    /// Command output of the Garage / Gate Central, input "central" of the Garage / Gate block
    /// </summary>
    public enum GarageCommand
    {
        None = 0,
        Open = 1,
        Close = 2,
        Stop = 3,
        PartialOpen = 4
    }

    /// <summary>
    /// Garage door or gate. The position (0 closed, 100 open) is estimated from the travel times and corrected by the end switches.
    /// Two outputs (open / close) drive the motor directly; in single button mode the block sends pulses and follows the
    /// open, stop, close, stop cycle of the drive.
    /// A control (<see cref="IControl"/>): the Garage / Gate Central picks its doors from the controls.
    /// </summary>
    public class GarageGateLogic : DispatchingLogic, IControl
    {
        internal const double Tolerance = 0.5;
        private const int MaxCyclePulses = 4;

        private readonly System.Timers.Timer _timer = new() { AutoReset = false };
        private readonly System.Timers.Timer _pulseTimer = new() { AutoReset = false };
        private readonly System.Timers.Timer _autoCloseTimer = new() { AutoReset = false };

        internal int Type;
        internal bool SingleButton;
        internal double OpenSeconds = 20;
        internal double CloseSeconds = 20;
        internal int PartialPosition = 20;
        internal int PulseMs = 500;
        internal int AutoCloseSeconds;

        private double _position;
        private int _motion;
        private double _target;
        private DateTimeOffset _moveStart;
        private double _moveStartPosition;
        private int _lastDirection = -1;
        private bool _locked;
        private bool _disablePeriphery;
        private bool _obstacle;

        // single button drive: what the drive does after our pulses
        private bool _driveMoving;
        private int _driveDirection = -1;
        private int _pulsesPending;
        private bool _pulseHigh;

        public GarageGateLogic(ILogicContext context) : base(context)
        {
            _timer.Elapsed += _timer_Elapsed;
            _pulseTimer.Elapsed += _pulseTimer_Elapsed;
            _autoCloseTimer.Elapsed += _autoCloseTimer_Elapsed;
        }

        internal int Motion => _motion;
        internal double Target => _target;
        internal bool Locked => _locked;
        internal int PulsesPending => _pulsesPending;

        public Guid Id => Context.RuleInstance.ObjId;
        public string Name => Context.RuleInstance.Name;

        protected override Task<bool> Start(RuleInstance instance, CancellationToken token = default)
        {
            lock (Sync)
            {
                SendState();
                Send(GarageGateLogicFactory.OutputLocked, false);
            }
            return base.Start(instance, token);
        }

        protected override Task<bool> Stop(RuleInstance instance, CancellationToken token = default)
        {
            lock (Sync)
            {
                _timer.Stop();
                _pulseTimer.Stop();
                _autoCloseTimer.Stop();
            }
            return base.Stop(instance, token);
        }

        protected override void ParameterValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            lock (Sync)
            {
                var template = TemplateOf(instance);
                if (template == GarageGateLogicFactory.ParamType)
                {
                    Type = Math.Clamp(LogicValue.ToInt(value, 0), 0, 2);
                }
                else if (template == GarageGateLogicFactory.ParamMotorMode)
                {
                    SingleButton = LogicValue.ToInt(value, 0) == 1;
                }
                else if (template == GarageGateLogicFactory.ParamOpenTime)
                {
                    OpenSeconds = Math.Max(1, LogicValue.ToDouble(value, 20));
                }
                else if (template == GarageGateLogicFactory.ParamCloseTime)
                {
                    CloseSeconds = Math.Max(1, LogicValue.ToDouble(value, 20));
                }
                else if (template == GarageGateLogicFactory.ParamPartialPosition)
                {
                    PartialPosition = Math.Clamp(LogicValue.ToInt(value, 20), 1, 99);
                }
                else if (template == GarageGateLogicFactory.ParamPulseLength)
                {
                    PulseMs = Math.Max(50, LogicValue.ToInt(value, 500));
                }
                else if (template == GarageGateLogicFactory.ParamAutoClose)
                {
                    AutoCloseSeconds = Math.Max(0, LogicValue.ToInt(value, 0));
                    if (AutoCloseSeconds == 0)
                    {
                        _autoCloseTimer.Stop();
                    }
                }
            }
        }

        protected override IList<ILogicOutputChanged> InputValueChanged(RuleInterfaceInstance instance, IDispatchable source, object value)
        {
            lock (Sync)
            {
                var template = TemplateOf(instance);
                var on = LogicValue.IsTruthy(value);

                if (template == GarageGateLogicFactory.InputLock)
                {
                    SetLocked(on);
                }
                else if (template == GarageGateLogicFactory.InputDisablePeriphery)
                {
                    _disablePeriphery = on;
                }
                else if (template == GarageGateLogicFactory.InputEndOpen)
                {
                    if (on)
                    {
                        ReachedEnd(100);
                    }
                }
                else if (template == GarageGateLogicFactory.InputEndClosed)
                {
                    if (on)
                    {
                        ReachedEnd(0);
                    }
                }
                else if (template == GarageGateLogicFactory.InputObstacle)
                {
                    _obstacle = on;
                    if (on && _motion < 0)
                    {
                        // safety: never keep closing onto an obstacle, open again
                        StopMoving(true);
                        MoveTo(100);
                    }
                }
                else if (template == GarageGateLogicFactory.InputCentral)
                {
                    if (!_locked)
                    {
                        Execute((GarageCommand)LogicValue.ToInt(value, 0));
                    }
                }
                else if (on && !_locked && (!_disablePeriphery || IsFromVisu(source)))
                {
                    Button(template);
                }
            }

            return new List<ILogicOutputChanged>();
        }

        private void Button(Guid template)
        {
            var position = CurrentPosition();
            if (template == GarageGateLogicFactory.InputToggle)
            {
                if (_motion != 0)
                {
                    StopMoving(true);
                }
                else if (position >= 100 - Tolerance || (_lastDirection > 0 && position > Tolerance))
                {
                    MoveTo(0);
                }
                else
                {
                    MoveTo(100);
                }
            }
            else if (template == GarageGateLogicFactory.InputOpen)
            {
                if (_motion != 0)
                {
                    StopMoving(true);
                }
                else
                {
                    MoveTo(100);
                }
            }
            else if (template == GarageGateLogicFactory.InputClose)
            {
                if (_motion != 0)
                {
                    StopMoving(true);
                }
                else
                {
                    MoveTo(0);
                }
            }
            else if (template == GarageGateLogicFactory.InputCompleteOpen)
            {
                Execute(GarageCommand.Open);
            }
            else if (template == GarageGateLogicFactory.InputCompleteClose)
            {
                Execute(GarageCommand.Close);
            }
            else if (template == GarageGateLogicFactory.InputPartialOpen)
            {
                Execute(GarageCommand.PartialOpen);
            }
            else if (template == GarageGateLogicFactory.InputStop)
            {
                Execute(GarageCommand.Stop);
            }
        }

        private void Execute(GarageCommand command)
        {
            switch (command)
            {
                case GarageCommand.Open:
                    MoveTo(100);
                    break;
                case GarageCommand.Close:
                    MoveTo(0);
                    break;
                case GarageCommand.Stop:
                    StopMoving(true);
                    break;
                case GarageCommand.PartialOpen:
                    MoveTo(PartialPosition);
                    break;
            }
        }

        private void SetLocked(bool locked)
        {
            if (locked)
            {
                StopMoving(true);
                _autoCloseTimer.Stop();
            }
            _locked = locked;
            Send(GarageGateLogicFactory.OutputLocked, locked);
        }

        private DateTimeOffset Now => Context.TimeProvider.GetUtcNow();

        private double TravelSeconds(int direction) => direction > 0 ? OpenSeconds : CloseSeconds;

        internal double CurrentPosition()
        {
            if (_motion == 0)
            {
                return _position;
            }
            var elapsed = Math.Max(0, (Now - _moveStart).TotalSeconds);
            var position = _moveStartPosition + _motion * elapsed * 100 / TravelSeconds(_motion);
            return Math.Clamp(position, Math.Min(_moveStartPosition, _target), Math.Max(_moveStartPosition, _target));
        }

        private void MoveTo(double target)
        {
            target = Math.Clamp(target, 0, 100);
            var position = CurrentPosition();
            var direction = Math.Abs(target - position) < Tolerance ? 0 : Math.Sign(target - position);
            _autoCloseTimer.Stop();

            if (direction == 0)
            {
                if (_motion != 0)
                {
                    StopMoving(true);
                }
                return;
            }
            if (direction < 0 && _obstacle)
            {
                // closing is blocked while the light barrier is interrupted
                return;
            }

            if (_motion != direction)
            {
                Drive(direction);
            }

            _position = position;
            _moveStartPosition = position;
            _moveStart = Now;
            _target = target;
            _motion = direction;
            _lastDirection = direction;

            _timer.Stop();
            _timer.Interval = LogicValue.TimerInterval(Math.Abs(target - position) / 100 * TravelSeconds(direction) * 1000);
            _timer.Start();
            SendState();
        }

        /// <summary>
        /// Stops a movement where the door is now. <paramref name="driveStops"/> = false when the drive stopped by itself (end of travel)
        /// </summary>
        private void StopMoving(bool driveStops, double? finalPosition = null)
        {
            if (_motion == 0)
            {
                return;
            }
            _position = finalPosition ?? CurrentPosition();
            _motion = 0;
            _timer.Stop();
            if (driveStops)
            {
                Drive(0);
            }
            else
            {
                _driveMoving = false;
                if (!SingleButton)
                {
                    Drive(0);
                }
            }
            SendState();
        }

        private void ReachedEnd(double end)
        {
            if (_motion != 0 && Math.Sign(end - 50) == _motion)
            {
                _target = end;
                StopMoving(false, end);
            }
            _position = end;
            if (_motion == 0)
            {
                SendState();
                ArmAutoClose();
            }
        }

        private void ArmAutoClose()
        {
            _autoCloseTimer.Stop();
            if (AutoCloseSeconds > 0 && _position >= 100 - Tolerance && !_locked)
            {
                _autoCloseTimer.Interval = LogicValue.TimerInterval(AutoCloseSeconds * 1000.0);
                _autoCloseTimer.Start();
            }
        }

        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            lock (Sync)
            {
                // also when the handler runs by hand (tests): the timer must not tick again on its own
                _timer.Stop();
                if (_motion == 0)
                {
                    return;
                }
                var atEnd = _target <= 0 || _target >= 100;
                // the drive stops by itself at the ends, a partial position needs a stop
                StopMoving(!atEnd, _target);
                ArmAutoClose();
            }
        }

        private void _autoCloseTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            lock (Sync)
            {
                // also when the handler runs by hand (tests): the timer must not tick again on its own
                _autoCloseTimer.Stop();
                if (_motion == 0 && !_locked && _position >= 100 - Tolerance)
                {
                    MoveTo(0);
                }
            }
        }

        /// <summary>
        /// Makes the motor move in <paramref name="direction"/> (+1 open, -1 close) or stop (0)
        /// </summary>
        private void Drive(int direction)
        {
            if (!SingleButton)
            {
                // first switch off the other direction, never both relays at once
                if (direction >= 0)
                {
                    Send(GarageGateLogicFactory.OutputClose, false);
                }
                if (direction <= 0)
                {
                    Send(GarageGateLogicFactory.OutputOpen, false);
                }
                if (direction > 0)
                {
                    Send(GarageGateLogicFactory.OutputOpen, true);
                }
                else if (direction < 0)
                {
                    Send(GarageGateLogicFactory.OutputClose, true);
                }
                return;
            }

            // single button: each pulse moves the drive one step in open, stop, close, stop
            var pulses = 0;
            while (pulses < MaxCyclePulses && !(direction == 0 ? !_driveMoving : _driveMoving && _driveDirection == direction))
            {
                if (_driveMoving)
                {
                    _driveMoving = false;
                }
                else
                {
                    _driveDirection = -_driveDirection;
                    _driveMoving = true;
                }
                pulses++;
            }
            QueuePulses(pulses);
        }

        private void QueuePulses(int count)
        {
            if (count <= 0)
            {
                return;
            }
            var idle = _pulsesPending == 0;
            _pulsesPending += count;
            if (idle)
            {
                _pulseHigh = true;
                SendAlways(GarageGateLogicFactory.OutputPulse, true);
                _pulseTimer.Interval = LogicValue.TimerInterval(PulseMs);
                _pulseTimer.Start();
            }
        }

        private void _pulseTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            lock (Sync)
            {
                // also when the handler runs by hand (tests): the timer must not tick again on its own
                _pulseTimer.Stop();
                if (_pulsesPending == 0)
                {
                    return;
                }
                if (_pulseHigh)
                {
                    _pulseHigh = false;
                    _pulsesPending--;
                    SendAlways(GarageGateLogicFactory.OutputPulse, false);
                    if (_pulsesPending == 0)
                    {
                        return;
                    }
                }
                else
                {
                    _pulseHigh = true;
                    SendAlways(GarageGateLogicFactory.OutputPulse, true);
                }
                _pulseTimer.Interval = LogicValue.TimerInterval(PulseMs);
                _pulseTimer.Start();
            }
        }

        internal GarageState State
        {
            get
            {
                if (_motion > 0)
                {
                    return GarageState.Opening;
                }
                if (_motion < 0)
                {
                    return GarageState.Closing;
                }
                if (_position <= Tolerance)
                {
                    return GarageState.Closed;
                }
                return _position >= 100 - Tolerance ? GarageState.Open : GarageState.PartiallyOpen;
            }
        }

        private void SendState()
        {
            var position = Math.Round(CurrentPosition(), 1);
            Send(GarageGateLogicFactory.OutputPosition, position);
            Send(GarageGateLogicFactory.OutputState, (int)State);
            Send(GarageGateLogicFactory.OutputIsOpen, State != GarageState.Closed);
        }

        public override object GetDataForVisu()
        {
            lock (Sync)
            {
                var data = SentValuesForVisu();
                data["position"] = Math.Round(CurrentPosition(), 1);
                data["state"] = (int)State;
                data["motion"] = _motion;
                data["target"] = _target;
                data["locked"] = _locked;
                data["gateType"] = Type;
                data["openSeconds"] = OpenSeconds;
                data["closeSeconds"] = CloseSeconds;
                data["partialPosition"] = PartialPosition;
                return data;
            }
        }
    }
}
