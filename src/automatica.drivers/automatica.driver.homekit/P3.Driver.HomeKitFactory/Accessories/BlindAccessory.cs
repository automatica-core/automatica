using System;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Control.Base;
using Microsoft.Extensions.Logging;
using P3.Driver.HomeKit.Hap.Model;

namespace P3.Driver.HomeKitFactory.Accessories
{
    /// <summary>
    /// Connects a HomeKit window covering with an <see cref="IBlind"/>.
    /// HomeKit counts 100 = fully open and 0 = fully closed, Automatica blinds 0 = open and 100 = closed.
    /// The home app shows "opening"/"closing" while the target differs from the current position, so the target must follow
    /// the blind whenever HomeKit did not ask for a movement. Blinds often report neither the movement nor intermediate
    /// positions, a HomeKit movement therefore ends when the position is reached, the blind stops or nothing happened for
    /// <see cref="PendingTimeout"/>.
    /// </summary>
    internal sealed class BlindAccessory : IDisposable
    {
        /// <summary>
        /// How long a HomeKit target is kept without a position update before the target is set to the current position
        /// </summary>
        internal static readonly TimeSpan PendingTimeout = TimeSpan.FromMinutes(2);

        internal const int PositionStateDecreasing = 0;
        internal const int PositionStateIncreasing = 1;
        internal const int PositionStateStopped = 2;

        private readonly WindowCovering _accessory;
        private readonly IBlind _blind;
        private readonly Action<Characteristic> _write;
        private readonly ILogger _logger;
        private readonly TimeProvider _timeProvider;
        private readonly object _lock = new();
        private readonly Guid _callbackId;

        private bool _wasMoving;
        private int? _pendingTarget;
        private ITimer _pendingTimer;
        private int _timerGeneration;

        private BlindAccessory(WindowCovering accessory, IBlind blind, Action<Characteristic> write, ILogger logger, TimeProvider timeProvider)
        {
            _accessory = accessory;
            _blind = blind;
            _write = write;
            _logger = logger;
            _timeProvider = timeProvider;

            var current = ToHapPosition(blind.Position);
            accessory.CurrentPosition.Value = current;
            accessory.TargetPosition.Value = current;
            accessory.PositionType.Value = MovingState(blind);
            _wasMoving = blind.IsMoving;

            _callbackId = blind.RegisterValueCallback(BlindChanged);
        }

        public static BlindAccessory Bind(WindowCovering accessory, IBlind blind, Action<Characteristic> write, ILogger logger, TimeProvider timeProvider)
        {
            return new BlindAccessory(accessory, blind, write, logger, timeProvider);
        }

        internal int? PendingTarget
        {
            get
            {
                lock (_lock)
                {
                    return _pendingTarget;
                }
            }
        }

        /// <summary>
        /// HomeKit wrote the target position
        /// </summary>
        public async Task HandleTargetAsync(object value)
        {
            var target = Clamp(Convert.ToInt32(value));

            lock (_lock)
            {
                _pendingTarget = target;
                _accessory.TargetPosition.Value = target;
                RestartTimer();
                _accessory.PositionType.Value = PositionState(Current, target);
            }
            _write(_accessory.PositionType);

            _logger.LogInformation($"{_blind.Name}: HomeKit target {target} % open (current {Current} %)");

            if (target == 100 || target == 0)
            {
                // fully open/close with up/down: works without the absolute position output of the blind being linked.
                // ShutterLogic stops instead of moving when it thinks it is moving, stop first so the command is never lost
                if (_blind.IsMoving)
                {
                    await _blind.StopAsync();
                }

                if (target == 100)
                {
                    await _blind.MoveUpAsync();
                }
                else
                {
                    await _blind.MoveDownAsync();
                }
            }
            else
            {
                await _blind.MoveAbsoluteAsync(ToBlindPosition(target));
            }
        }

        private int Current => Convert.ToInt32(_accessory.CurrentPosition.Value);

        private void BlindChanged()
        {
            lock (_lock)
            {
                var current = ToHapPosition(_blind.Position);
                var stopped = _wasMoving && !_blind.IsMoving;
                _wasMoving = _blind.IsMoving;

                _accessory.CurrentPosition.Value = current;

                if (_pendingTarget.HasValue)
                {
                    if (Math.Abs(current - _pendingTarget.Value) <= 1 || stopped)
                    {
                        // reached, or stopped somewhere else (wall switch, obstacle)
                        EndPending(current);
                    }
                    else
                    {
                        // still on the way: wait for the next update
                        RestartTimer();
                        _accessory.PositionType.Value = PositionState(current, _pendingTarget.Value);
                    }
                }
                else
                {
                    // not moved by HomeKit (wall switch, visu, logic): the target follows the blind
                    _accessory.TargetPosition.Value = current;
                    _accessory.PositionType.Value = MovingState(_blind);
                }

                _logger.LogDebug($"{_blind.Name}: moving {_blind.IsMoving} direction {_blind.Direction} position {_blind.Position} => current {current} target {_accessory.TargetPosition.Value} state {_accessory.PositionType.Value}");
            }

            _write(_accessory.CurrentPosition);
            _write(_accessory.TargetPosition);
            _write(_accessory.PositionType);
        }

        private void PendingTimedOut(int generation)
        {
            lock (_lock)
            {
                // a timer that was replaced or stopped while its callback was already queued
                if (!_pendingTarget.HasValue || generation != _timerGeneration)
                {
                    return;
                }

                _logger.LogInformation($"{_blind.Name}: target {_pendingTarget} % not reached within {PendingTimeout}, stays at {Current} %");
                EndPending(Current);
            }

            _write(_accessory.TargetPosition);
            _write(_accessory.PositionType);
        }

        private void EndPending(int current)
        {
            _pendingTarget = null;
            _timerGeneration++;
            _pendingTimer?.Dispose();
            _pendingTimer = null;

            _accessory.TargetPosition.Value = current;
            _accessory.PositionType.Value = PositionStateStopped;
        }

        private void RestartTimer()
        {
            _pendingTimer?.Dispose();
            var generation = ++_timerGeneration;
            _pendingTimer = _timeProvider.CreateTimer(_ => PendingTimedOut(generation), null, PendingTimeout, Timeout.InfiniteTimeSpan);
        }

        private static int PositionState(int current, int target)
        {
            if (current == target)
            {
                return PositionStateStopped;
            }

            return target > current ? PositionStateIncreasing : PositionStateDecreasing;
        }

        /// <summary>
        /// Movement reported by the blind, direction 0 = up (opening), 1 = down (closing)
        /// </summary>
        private static int MovingState(IBlind blind)
        {
            if (!blind.IsMoving)
            {
                return PositionStateStopped;
            }

            return blind.Direction == 0 ? PositionStateIncreasing : PositionStateDecreasing;
        }

        internal static int ToHapPosition(int blindPosition)
        {
            return 100 - Clamp(blindPosition);
        }

        internal static int ToBlindPosition(int hapPosition)
        {
            return 100 - Clamp(hapPosition);
        }

        private static int Clamp(int value)
        {
            return Math.Clamp(value, 0, 100);
        }

        public void Dispose()
        {
            _blind.UnregisterValueCallback(_callbackId);
            lock (_lock)
            {
                _pendingTimer?.Dispose();
                _pendingTimer = null;
            }
        }
    }
}
