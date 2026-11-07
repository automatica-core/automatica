using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Automatica.Core.Logic;
using Automatica.Core.UnitTests.Base.Logics;
using P3.Logic.Operations.Shutter;
using Xunit;
using static P3.Logic.Operations.Tests.OperationsTestHelper;

namespace P3.Logic.Operations.Tests
{
    public class ShutterTests : LogicTest<ShutterLogicFactory>
    {
        private ShutterLogic Shutter => (ShutterLogic)Logic;

        private IList<ILogicOutputChanged> Input(Guid template, object value)
        {
            return Logic.ValueChanged(GetLogicInterfaceByTemplate(template), Dispatchable, value);
        }

        private IList<ILogicOutputChanged> Up() => Input(ShutterLogicFactory.RuleInputUp, true);
        private IList<ILogicOutputChanged> Down() => Input(ShutterLogicFactory.RuleInputDown, true);
        private IList<ILogicOutputChanged> StopInput() => Input(ShutterLogicFactory.RuleInputStop, true);
        private IList<ILogicOutputChanged> Feedback(object position) => Input(ShutterLogicFactory.RuleInputAbsolutePercentage, position);

        // ---------- factory / initial ----------

        [Fact]
        public void Factory_HasSevenInputsAndFiveOutputs()
        {
            Assert.Equal(12, RuleInstance.RuleInterfaceInstance.Count);
            Assert.Equal("P3.Logic.Operations.Shutter", Instance.LogicName);
        }

        [Fact]
        public void Initial_IsIdleDirectionDownPositionZero()
        {
            Assert.False(Shutter.IsMoving);
            Assert.Equal(0, Shutter.Direction);
            Assert.Equal(0, Shutter.Position);
            Assert.Equal(Instance.LogicGuid, Shutter.Id);
        }

        // ---------- up / down / stop truth table ----------

        [Fact]
        public void Up_WhenIdle_MovesUp()
        {
            var outputs = Up();

            Assert.Equal(2, outputs.Count);
            Assert.Equal(0, Output(outputs, ShutterLogicFactory.RuleOutputShutterMove));
            Assert.Equal(true, Output(outputs, ShutterLogicFactory.RuleOutputMoveState));
            Assert.True(Shutter.IsMoving);
            Assert.Equal(1, Shutter.Direction);
        }

        [Fact]
        public void Down_WhenIdle_MovesDown()
        {
            var outputs = Down();

            Assert.Equal(2, outputs.Count);
            Assert.Equal(1, Output(outputs, ShutterLogicFactory.RuleOutputShutterMove));
            Assert.Equal(true, Output(outputs, ShutterLogicFactory.RuleOutputMoveState));
            Assert.True(Shutter.IsMoving);
            Assert.Equal(0, Shutter.Direction);
        }

        [Fact]
        public void Up_WhenMovingUp_Stops()
        {
            Up();

            var outputs = Up();

            Assert.Equal(false, Output(outputs, ShutterLogicFactory.RuleOutputStop));
            Assert.Equal(false, Output(outputs, ShutterLogicFactory.RuleOutputMoveState));
            Assert.False(HasOutput(outputs, ShutterLogicFactory.RuleOutputShutterMove));
            Assert.False(Shutter.IsMoving);
        }

        [Fact]
        public void Down_WhenMovingDown_StopsWithStopTrue()
        {
            Down();

            var outputs = Down();

            Assert.Equal(true, Output(outputs, ShutterLogicFactory.RuleOutputStop));
            Assert.Equal(false, Output(outputs, ShutterLogicFactory.RuleOutputMoveState));
            Assert.False(Shutter.IsMoving);
        }

        [Fact]
        public void Down_WhenMovingUp_StopsAndSwitchesDirection()
        {
            Up();

            var outputs = Down();

            // stop value uses the direction of the running move (up => false)
            Assert.Equal(false, Output(outputs, ShutterLogicFactory.RuleOutputStop));
            Assert.False(Shutter.IsMoving);
            Assert.Equal(0, Shutter.Direction);

            // the next Down moves again
            Assert.Equal(1, Output(Down(), ShutterLogicFactory.RuleOutputShutterMove));
        }

        [Fact]
        public void Stop_WhenIdle_EmitsNothing()
        {
            Assert.Empty(StopInput());
            Assert.False(Shutter.IsMoving);
        }

        [Fact]
        public void Stop_WhenMovingDown_EmitsStopTrue()
        {
            Down();

            var outputs = StopInput();

            Assert.Equal(true, Output(outputs, ShutterLogicFactory.RuleOutputStop));
            Assert.Equal(false, Output(outputs, ShutterLogicFactory.RuleOutputMoveState));
            Assert.False(Shutter.IsMoving);
        }

        [Fact]
        public void Stop_WhenMovingUp_EmitsStopFalse()
        {
            Up();

            var outputs = StopInput();

            Assert.Equal(false, Output(outputs, ShutterLogicFactory.RuleOutputStop));
            Assert.False(Shutter.IsMoving);
        }

        [Fact]
        public void UpDown_InputValueIsIgnored_EveryTelegramToggles()
        {
            // the value of the up input does not matter, false also moves
            var outputs = Input(ShutterLogicFactory.RuleInputUp, false);
            Assert.Equal(true, Output(outputs, ShutterLogicFactory.RuleOutputMoveState));

            outputs = Input(ShutterLogicFactory.RuleInputUp, false);
            Assert.Equal(false, Output(outputs, ShutterLogicFactory.RuleOutputMoveState));
        }

        // ---------- locked ----------

        [Theory]
        [InlineData(true)]
        [InlineData("true")]
        [InlineData(1)]
        public void Locked_IgnoresUpAndDown(object lockValue)
        {
            Assert.Empty(Input(ShutterLogicFactory.RuleInputLocked, lockValue));

            Assert.Empty(Up());
            Assert.Empty(Down());
            Assert.False(Shutter.IsMoving);
        }

        [Fact]
        public void Locked_StopStillWorks()
        {
            Down();
            Input(ShutterLogicFactory.RuleInputLocked, true);

            var outputs = StopInput();

            Assert.Equal(true, Output(outputs, ShutterLogicFactory.RuleOutputStop));
            Assert.False(Shutter.IsMoving);
        }

        [Fact]
        public void Unlocked_UpWorksAgain()
        {
            Input(ShutterLogicFactory.RuleInputLocked, true);
            Input(ShutterLogicFactory.RuleInputLocked, false);

            Assert.Equal(0, Output(Up(), ShutterLogicFactory.RuleOutputShutterMove));
        }

        [Fact]
        public void Locked_InvalidValue_KeepsPreviousLockState()
        {
            Input(ShutterLogicFactory.RuleInputLocked, true);
            Input(ShutterLogicFactory.RuleInputLocked, "yes");

            Assert.Empty(Up());
        }

        // ---------- direction / move ----------

        [Theory]
        [InlineData(0, 0)]
        [InlineData(1, 1)]
        [InlineData(true, 1)]
        [InlineData("1", 1)]
        [InlineData(1.4, 1)]
        public void DirectionInput_EmitsDirection(object input, int expected)
        {
            var outputs = Input(ShutterLogicFactory.RuleInputDirection, input);

            Assert.Equal(expected, Output(outputs, ShutterLogicFactory.RuleOutputDirection));
            Assert.Equal(expected, Shutter.Direction);
        }

        [Fact]
        public void DirectionInput_Invalid_EmitsNothing()
        {
            Assert.Empty(Input(ShutterLogicFactory.RuleInputDirection, "up"));
            Assert.Equal(0, Shutter.Direction);
        }

        [Theory]
        [InlineData(0, true)]
        [InlineData(1, false)]
        public void MoveInput_UsesDirection(int direction, bool expectedMove)
        {
            Input(ShutterLogicFactory.RuleInputDirection, direction);

            var outputs = Input(ShutterLogicFactory.RuleInputMove, true);

            Assert.Equal(expectedMove, Output(outputs, ShutterLogicFactory.RuleOutputShutterMove));
            Assert.Equal(true, Output(outputs, ShutterLogicFactory.RuleOutputMoveState));
        }

        [Fact]
        public void MoveInput_SetsMoving()
        {
            // fix ab70dd2d: the move input sets the internal moving state
            Input(ShutterLogicFactory.RuleInputMove, true);

            Assert.True(Shutter.IsMoving);

            var outputs = StopInput();
            Assert.Equal(true, Output(outputs, ShutterLogicFactory.RuleOutputStop));
        }

        [Fact]
        public void MoveInput_ThenDown_Stops()
        {
            Input(ShutterLogicFactory.RuleInputMove, true);

            var outputs = Down();

            Assert.True(HasOutput(outputs, ShutterLogicFactory.RuleOutputStop));
            Assert.False(Shutter.IsMoving);
        }

        // ---------- position feedback ----------

        [Theory]
        [InlineData(50, 50)]
        [InlineData(50.4, 50)]
        [InlineData(12.6, 13)]
        [InlineData("30", 30)]
        public void Feedback_TracksPosition_WithoutOutput(object input, int expected)
        {
            var outputs = Feedback(input);

            Assert.Empty(outputs);
            Assert.Equal(expected, Shutter.Position);
        }

        [Theory]
        [InlineData(100)]
        [InlineData(99.5)]
        [InlineData(0)]
        [InlineData(0.5)]
        [InlineData(-3)]
        public void Feedback_AtEndPosition_ClearsMoving(double position)
        {
            Down();
            Assert.True(Shutter.IsMoving);

            Feedback(position);

            Assert.False(Shutter.IsMoving);
        }

        [Fact]
        public void Feedback_InBetweenWithoutTarget_KeepsMoving()
        {
            Down();

            Assert.Empty(Feedback(40));

            Assert.True(Shutter.IsMoving);
        }

        [Fact]
        public void Feedback_Invalid_EmitsNothing()
        {
            Assert.Empty(Feedback("abc"));
            Assert.Equal(0, Shutter.Position);
        }

        // ---------- absolute move (fix ab70dd2d) ----------

        [Fact]
        public async Task MoveAbsoluteAsync_InBetween_DispatchesMovingTrueAndTarget()
        {
            await Context.Dispatcher.ClearValues();

            await Shutter.MoveAbsoluteAsync(50);

            Assert.True(Shutter.IsMoving);
            Assert.Equal(50, Shutter.Position);
            Assert.Equal(true, DispatchedValue(Context.Dispatcher, ShutterLogicFactory.RuleOutputMoveState));
            Assert.Equal(50d, Convert.ToDouble(DispatchedValue(Context.Dispatcher, ShutterLogicFactory.RuleOutputAbsolutePercentage)));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(100)]
        public async Task MoveAbsoluteAsync_EndPosition_DispatchesMovingFalse(int position)
        {
            await Context.Dispatcher.ClearValues();
            await Shutter.MoveDownAsync();

            await Shutter.MoveAbsoluteAsync(position);

            Assert.False(Shutter.IsMoving);
            Assert.Equal(false, DispatchedValue(Context.Dispatcher, ShutterLogicFactory.RuleOutputMoveState));
            Assert.Equal((double)position, Convert.ToDouble(DispatchedValue(Context.Dispatcher, ShutterLogicFactory.RuleOutputAbsolutePercentage)));
        }

        [Fact]
        public async Task MoveAbsolute_FeedbackReachesTarget_ClearsMovingAndEmitsMoveStateFalse()
        {
            await Shutter.MoveAbsoluteAsync(60);

            var outputs = Feedback(60);

            Assert.False(Shutter.IsMoving);
            Assert.Equal(false, Output(outputs, ShutterLogicFactory.RuleOutputMoveState));
        }

        [Fact]
        public async Task MoveAbsolute_FeedbackNotYetAtTarget_KeepsMoving()
        {
            await Shutter.MoveAbsoluteAsync(60);

            var outputs = Feedback(30);

            Assert.Empty(outputs);
            Assert.True(Shutter.IsMoving);
            Assert.Equal(30, Shutter.Position);
        }

        [Fact]
        public async Task MoveAbsolute_FeedbackRoundsToTarget_ClearsMoving()
        {
            await Shutter.MoveAbsoluteAsync(60);

            Feedback(59.7);

            Assert.False(Shutter.IsMoving);
        }

        [Fact]
        public async Task MoveAbsolute_TargetReached_TargetIsCleared()
        {
            await Shutter.MoveAbsoluteAsync(60);
            Feedback(60);

            // a new manual move passes the old target again - that must not stop the move state
            Up();
            var outputs = Feedback(60);

            Assert.Empty(outputs);
            Assert.True(Shutter.IsMoving);
        }

        [Fact]
        public async Task MoveAbsolute_TargetReached_ThenUpMoves()
        {
            await Shutter.MoveAbsoluteAsync(60);
            Feedback(60);

            var outputs = Up();

            Assert.Equal(0, Output(outputs, ShutterLogicFactory.RuleOutputShutterMove));
            Assert.False(HasOutput(outputs, ShutterLogicFactory.RuleOutputStop));
            Assert.True(Shutter.IsMoving);
        }

        [Fact]
        public async Task MoveAbsolute_WhileMoving_UpStops()
        {
            await Shutter.MoveAbsoluteAsync(60);

            var outputs = Up();

            Assert.True(HasOutput(outputs, ShutterLogicFactory.RuleOutputStop));
            Assert.False(Shutter.IsMoving);
        }

        // ---------- control API ----------

        [Fact]
        public async Task MoveUpAsync_DispatchesMoveZeroAndMovingTrue()
        {
            await Context.Dispatcher.ClearValues();

            await Shutter.MoveUpAsync();

            Assert.Equal(0, DispatchedValue(Context.Dispatcher, ShutterLogicFactory.RuleOutputShutterMove));
            Assert.Equal(true, DispatchedValue(Context.Dispatcher, ShutterLogicFactory.RuleOutputMoveState));
            Assert.Equal(1, Shutter.Direction);
        }

        [Fact]
        public async Task MoveDownAsync_DispatchesMoveOneAndMovingTrue()
        {
            await Context.Dispatcher.ClearValues();

            await Shutter.MoveDownAsync();

            Assert.Equal(1, DispatchedValue(Context.Dispatcher, ShutterLogicFactory.RuleOutputShutterMove));
            Assert.Equal(true, DispatchedValue(Context.Dispatcher, ShutterLogicFactory.RuleOutputMoveState));
        }

        [Fact]
        public async Task StopAsync_WhileMoving_DispatchesStopAndMovingFalse()
        {
            await Context.Dispatcher.ClearValues();
            await Shutter.MoveDownAsync();

            await Shutter.StopAsync();

            Assert.Equal(true, DispatchedValue(Context.Dispatcher, ShutterLogicFactory.RuleOutputStop));
            Assert.Equal(false, DispatchedValue(Context.Dispatcher, ShutterLogicFactory.RuleOutputMoveState));
            Assert.False(Shutter.IsMoving);
        }

        [Fact]
        public async Task StopAsync_WhenIdle_DispatchesNothing()
        {
            await Context.Dispatcher.ClearValues();

            await Shutter.StopAsync();

            Assert.Null(DispatchedValue(Context.Dispatcher, ShutterLogicFactory.RuleOutputStop));
            Assert.Null(DispatchedValue(Context.Dispatcher, ShutterLogicFactory.RuleOutputMoveState));
        }

        [Fact]
        public async Task MoveUpAsync_IgnoresLock()
        {
            // the lock only applies to the up/down inputs, the control API is not locked
            Input(ShutterLogicFactory.RuleInputLocked, true);

            await Shutter.MoveUpAsync();

            Assert.True(Shutter.IsMoving);
        }
    }
}
