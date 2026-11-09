using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Control.Base;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using P3.Driver.HomeKit.Hap.Model;
using P3.Driver.HomeKitFactory.Accessories;
using Xunit;

namespace P3.Driver.HomeKit.UnitTests.Factory
{
    public class BlindAccessoryTests
    {
        private sealed class FakeBlind : IBlind
        {
            private readonly List<Action> _callbacks = new();

            public Guid Id { get; } = Guid.NewGuid();
            public string Name => "blind";
            public bool IsMoving { get; set; }
            public int Direction { get; set; }
            public int Position { get; set; }
            public List<string> Calls { get; } = new();
            public int CallbackCount => _callbacks.Count;

            public void Changed() => _callbacks.ForEach(a => a());

            public Guid RegisterValueCallback(Action callback)
            {
                _callbacks.Add(callback);
                return Guid.NewGuid();
            }

            public void UnregisterValueCallback(Guid id) => _callbacks.Clear();

            public Task StopAsync(CancellationToken token = default)
            {
                Calls.Add("stop");
                IsMoving = false;
                return Task.CompletedTask;
            }

            public Task MoveUpAsync(CancellationToken token = default)
            {
                Calls.Add("up");
                return Task.CompletedTask;
            }

            public Task MoveDownAsync(CancellationToken token = default)
            {
                Calls.Add("down");
                return Task.CompletedTask;
            }

            public Task MoveAbsoluteAsync(int pos, CancellationToken token = default)
            {
                Calls.Add($"abs {pos}");
                return Task.CompletedTask;
            }
        }

        private readonly FakeTimeProvider _time = new();
        private readonly List<(Characteristic Characteristic, object Value)> _written = new();
        private readonly WindowCovering _accessory = AccessoryFactory.CreateWindowCovering(3, "blind", "AutomaticaCore", "1", 0);

        private BlindAccessory Bind(FakeBlind blind)
        {
            return BlindAccessory.Bind(_accessory, blind, c => _written.Add((c, c.Value)), NullLogger.Instance, _time);
        }

        private int Target => Convert.ToInt32(_accessory.TargetPosition.Value);
        private int Current => Convert.ToInt32(_accessory.CurrentPosition.Value);
        private int State => Convert.ToInt32(_accessory.PositionType.Value);

        [Fact]
        public void Characteristics_FollowTheHapSpec()
        {
            Assert.Equal(new[] { 8, 9, 10 }, new[] { _accessory.TargetPosition.Id, _accessory.CurrentPosition.Id, _accessory.PositionType.Id });

            foreach (var position in new[] { _accessory.TargetPosition, _accessory.CurrentPosition })
            {
                Assert.Equal(CharacteristicFormat.UInt8, position.Format);
                Assert.Equal(CharacteristicUnit.Percentage, position.Unit);
                Assert.Equal(0, position.MinValue);
                Assert.Equal(100, position.MaxValue);
                Assert.Equal(1, position.MinStep);
                Assert.Contains("ev", position.Permissions);
            }

            Assert.Equal(CharacteristicFormat.UInt8, _accessory.PositionType.Format);
            Assert.Equal(new[] { 0, 1, 2 }, _accessory.PositionType.ValidValues);
            Assert.Equal(2, _accessory.PositionType.Value);
        }

        [Fact]
        public void Bind_InitializesFromThePosition()
        {
            Bind(new FakeBlind { Position = 30 }); // 30 % closed => HomeKit 70 % open

            Assert.Equal(70, Target);
            Assert.Equal(70, Current);
            Assert.Equal(BlindAccessory.PositionStateStopped, State);
        }

        [Theory]
        [InlineData(100, "up")]
        [InlineData(0, "down")]
        [InlineData(25, "abs 75")]
        [InlineData(60L, "abs 40")] // json numbers arrive as long
        public async Task HomeKitTarget_MovesTheBlind(object target, string expected)
        {
            var blind = new FakeBlind { Position = 50 };
            var binding = Bind(blind);

            await binding.HandleTargetAsync(target);

            Assert.Equal(new[] { expected }, blind.Calls);
            Assert.Equal(Convert.ToInt32(target), binding.PendingTarget);
        }

        [Fact]
        public async Task HomeKitOpen_WhileMoving_StopsFirst()
        {
            // ShutterLogic stops instead of moving up when it thinks it is moving
            var blind = new FakeBlind { Position = 50, IsMoving = true };
            var binding = Bind(blind);

            await binding.HandleTargetAsync(100);

            Assert.Equal(new[] { "stop", "up" }, blind.Calls);
        }

        [Fact]
        public async Task HomeKitMove_ReportsTheDirectionUntilTheTargetIsReached()
        {
            var blind = new FakeBlind { Position = 0 }; // open
            var binding = Bind(blind);

            await binding.HandleTargetAsync(0); // close
            Assert.Equal(BlindAccessory.PositionStateDecreasing, State);

            // a position update before the blind reports a movement keeps the target
            blind.Changed();
            Assert.Equal(0, Target);

            blind.Position = 60;
            blind.Changed();
            Assert.Equal(40, Current);
            Assert.Equal(0, Target);
            Assert.Equal(BlindAccessory.PositionStateDecreasing, State);

            blind.Position = 100;
            blind.Changed();
            Assert.Equal(0, Current);
            Assert.Equal(0, Target);
            Assert.Equal(BlindAccessory.PositionStateStopped, State);
            Assert.Null(binding.PendingTarget);
        }

        [Fact]
        public async Task HomeKitMove_ReachedWithinOnePercent()
        {
            var blind = new FakeBlind { Position = 0 };
            var binding = Bind(blind);

            await binding.HandleTargetAsync(50);
            blind.Position = 49; // knx 8 bit rounding
            blind.Changed();

            Assert.Null(binding.PendingTarget);
            Assert.Equal(51, Target);
            Assert.Equal(BlindAccessory.PositionStateStopped, State);
        }

        [Fact]
        public async Task HomeKitMove_StoppedElsewhere_TargetFollows()
        {
            var blind = new FakeBlind { Position = 0 };
            var binding = Bind(blind);

            await binding.HandleTargetAsync(0);
            blind.IsMoving = true;
            blind.Position = 30;
            blind.Changed();
            Assert.Equal(0, Target);

            // stopped by a wall switch
            blind.IsMoving = false;
            blind.Position = 40;
            blind.Changed();

            Assert.Null(binding.PendingTarget);
            Assert.Equal(60, Target);
            Assert.Equal((_accessory.PositionType, (object)BlindAccessory.PositionStateStopped), _written[^1]);
        }

        [Fact]
        public async Task HomeKitMove_WithoutFeedback_TimesOut()
        {
            // e.g. the absolute position output of the blind logic is not linked
            var blind = new FakeBlind { Position = 0 };
            var binding = Bind(blind);

            await binding.HandleTargetAsync(30);

            _time.Advance(BlindAccessory.PendingTimeout - TimeSpan.FromSeconds(1));
            Assert.Equal(30, Target);

            _time.Advance(TimeSpan.FromSeconds(1));
            Assert.Null(binding.PendingTarget);
            Assert.Equal(100, Target);
            Assert.Equal(BlindAccessory.PositionStateStopped, State);
            Assert.Contains((_accessory.TargetPosition, (object)100), _written);
        }

        [Fact]
        public async Task HomeKitMove_PositionUpdatesRestartTheTimeout()
        {
            var blind = new FakeBlind { Position = 0 };
            var binding = Bind(blind);

            await binding.HandleTargetAsync(0);
            _time.Advance(TimeSpan.FromSeconds(90));
            blind.Position = 50;
            blind.Changed();
            _time.Advance(TimeSpan.FromSeconds(90));

            Assert.Equal(0, binding.PendingTarget);
            Assert.Equal(0, Target);
        }

        [Fact]
        public void WallSwitch_TargetFollowsThePosition()
        {
            // the blind logic does not know about movements started by a wall switch: no IsMoving, only positions
            var blind = new FakeBlind { Position = 0 };
            Bind(blind);

            blind.Position = 40;
            blind.Changed();

            Assert.Equal(60, Current);
            Assert.Equal(60, Target);
            Assert.Equal(BlindAccessory.PositionStateStopped, State);
            Assert.Contains((_accessory.TargetPosition, (object)60), _written);
        }

        [Fact]
        public void ReportedMovement_SetsThePositionState()
        {
            var blind = new FakeBlind { Position = 0 };
            Bind(blind);

            blind.IsMoving = true;
            blind.Direction = 1; // down => closing
            blind.Position = 40;
            blind.Changed();
            Assert.Equal(BlindAccessory.PositionStateDecreasing, State);
            Assert.Equal(60, Target);

            blind.Direction = 0; // up => opening
            blind.Changed();
            Assert.Equal(BlindAccessory.PositionStateIncreasing, State);

            blind.IsMoving = false;
            blind.Changed();
            Assert.Equal(BlindAccessory.PositionStateStopped, State);
        }

        [Fact]
        public async Task Dispose_UnregistersAndStopsTheTimer()
        {
            var blind = new FakeBlind { Position = 0 };
            var binding = Bind(blind);
            await binding.HandleTargetAsync(30);

            binding.Dispose();
            _written.Clear();
            _time.Advance(BlindAccessory.PendingTimeout);

            Assert.Equal(0, blind.CallbackCount);
            Assert.Empty(_written);
        }

        [Theory]
        [InlineData(0, 100)]
        [InlineData(100, 0)]
        [InlineData(30, 70)]
        [InlineData(-5, 100)]
        [InlineData(120, 0)]
        public void Positions_AreReversedAndClamped(int blind, int hap)
        {
            Assert.Equal(hap, BlindAccessory.ToHapPosition(blind));
        }
    }
}
