using System;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.UnitTests.Base.Logics;
using P3.Logic.Lightning.FlashingLights;
using P3.Logic.Lightning.Tests.TestSupport;
using Xunit;

// LogicTest uses a process-wide dispatcher singleton, so the test classes must not run in parallel
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace P3.Logic.Lightning.Tests.FlashingLights
{
    public class FlashingLightsTests : LogicTest<FlashingLightsLogicFactory>, IDisposable
    {
        private readonly ValueRecorder _output;
        private bool _stopped;

        // the logic under test dispatches into its own recording dispatcher (in call order), not into the shared one of the base class
        private new Automatica.Core.Logic.ILogic Logic { get; }

        public FlashingLightsTests()
        {
            var dispatcher = new RecordingDispatcher();
            Logic = Instance.CreateLogicInstance(new LogicContextMock(RuleInstance, Factory, dispatcher));
            Logic.Start().GetAwaiter().GetResult();
            _output = new ValueRecorder(dispatcher, FlashingLightsLogicFactory.Output);
        }

        public void Dispose()
        {
            // cancels a sequence that is still running
            if (!_stopped)
            {
                Logic.Stop().GetAwaiter().GetResult();
            }
        }

        private async Task Configure(long delay, long repetitions)
        {
            GetLogicInterfaceByTemplate(FlashingLightsLogicFactory.Delay).Value = delay;
            GetLogicInterfaceByTemplate(FlashingLightsLogicFactory.RepeatCount).Value = repetitions;
            await Logic.Start();
        }

        private void Trigger(object value)
        {
            Assert.Empty(Logic.ValueChanged(GetLogicInterfaceByTemplate(FlashingLightsLogicFactory.Trigger), Dispatchable, value));
        }

        private void State(object value)
        {
            Assert.Empty(Logic.ValueChanged(GetLogicInterfaceByTemplate(FlashingLightsLogicFactory.State), Dispatchable, value));
        }

        private async Task StopAsync()
        {
            _stopped = true;
            await Logic.Stop();
        }

        [Fact]
        public async Task Trigger_StateOff_FlashesOnOffAndEndsOff()
        {
            await Configure(20, 2);

            Trigger(true);

            var values = await _output.WaitForCountAsync(5);
            Assert.Equal(new object[] { true, false, true, false, false }, values);
        }

        [Fact]
        public async Task Trigger_StateOn_FlashesOffOnAndEndsOn()
        {
            await Configure(20, 2);
            State(true);

            Trigger(true);

            var values = await _output.WaitForCountAsync(5);
            Assert.Equal(new object[] { false, true, false, true, true }, values);
        }

        [Fact]
        public async Task Trigger_StateOnThenOff_UsesTheLatestState()
        {
            await Configure(20, 1);
            State(true);
            State(false);

            Trigger(true);

            var values = await _output.WaitForCountAsync(3);
            Assert.Equal(new object[] { true, false, false }, values);
        }

        [Fact]
        public async Task Trigger_OneRepetition_FlashesOnce()
        {
            await Configure(20, 1);

            Trigger(true);

            var values = await _output.WaitForCountAsync(3);
            Assert.Equal(new object[] { true, false, false }, values);
        }

        [Fact]
        public async Task Trigger_ThreeRepetitions_FlashesThreeTimes()
        {
            await Configure(10, 3);

            Trigger(true);

            var values = await _output.WaitForCountAsync(7);
            Assert.Equal(new object[] { true, false, true, false, true, false, false }, values);
        }

        [Fact]
        public async Task Trigger_ZeroRepetitions_OnlyRestoresTheState()
        {
            await Configure(10, 0);
            State(true);

            Trigger(true);

            var values = await _output.WaitForCountAsync(1);
            Assert.Equal(new object[] { true }, values);
        }

        [Fact]
        public async Task Trigger_ZeroDelay_StillRunsTheWholeSequence()
        {
            await Configure(0, 2);

            Trigger(true);

            var values = await _output.WaitForCountAsync(5);
            Assert.Equal(new object[] { true, false, true, false, false }, values);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(1)]
        [InlineData("true")]
        [InlineData(null)]
        public async Task Trigger_NotBooleanTrue_DoesNothing(object value)
        {
            await Configure(10, 1);

            Trigger(value);

            await Task.Delay(100);
            Assert.Empty(_output.Values);
        }

        [Fact]
        public async Task Trigger_WhileRunning_DoesNotStartASecondSequence()
        {
            await Configure(30, 1);

            Trigger(true);
            Trigger(true);
            Trigger(true);

            await _output.WaitForCountAsync(3);
            await Task.Delay(150);
            Assert.Equal(new object[] { true, false, false }, _output.Values);
        }

        [Fact]
        public async Task Trigger_AfterSequenceFinished_RunsAgain()
        {
            await Configure(10, 1);

            Trigger(true);
            await _output.WaitForCountAsync(3);

            // the running flag is cleared in a finally block after the last dispatch
            await Task.Delay(50);
            Trigger(true);

            var values = await _output.WaitForCountAsync(6);
            Assert.Equal(new object[] { true, false, false, true, false, false }, values);
        }

        [Fact(Skip = "BUG: a trigger while a sequence runs overwrites the state to restore (FlashingLightsLogic.cs:109) - with the state input linked to the light's feedback the light ends in a flashed state instead of the state before flashing")]
        public async Task Trigger_WhileRunningWithFlashedFeedback_RestoresTheStateBeforeFlashing()
        {
            await Configure(50, 1);

            Trigger(true);
            // feedback of the light follows the flashing output
            State(true);
            Trigger(true);

            var values = await _output.WaitForCountAsync(3);
            Assert.False((bool)values.Last());
        }

        [Theory]
        [InlineData("true")]
        [InlineData(1)]
        [InlineData(null)]
        public async Task State_NotBoolean_IsIgnoredAndKeepsTheLastState(object value)
        {
            await Configure(10, 1);
            State(true);

            // the cast to bool throws, the base logic swallows it
            State(value);
            Trigger(true);

            var values = await _output.WaitForCountAsync(3);
            Assert.Equal(new object[] { false, true, true }, values);
        }

        [Fact]
        public async Task State_Alone_DispatchesNothing()
        {
            await Configure(10, 1);

            State(true);
            State(false);

            await Task.Delay(50);
            Assert.Empty(_output.Values);
        }

        [Fact]
        public async Task Stop_WhileRunning_CancelsTheSequenceWithoutRestoringTheState()
        {
            await Configure(100, 2);

            Trigger(true);
            await _output.WaitForCountAsync(1);
            await StopAsync();

            await Task.Delay(150);
            Assert.Equal(new object[] { true }, _output.Values);
        }

        [Fact]
        public async Task Trigger_AfterStop_DoesNothing()
        {
            await Configure(10, 1);
            await StopAsync();

            // no cancellation token source any more - the exception is swallowed by the base logic
            Trigger(true);

            await Task.Delay(50);
            Assert.Empty(_output.Values);
        }

        [Fact]
        public async Task StartAfterStop_TriggerWorksAgain()
        {
            await Configure(100, 2);
            Trigger(true);
            await _output.WaitForCountAsync(1);
            await Logic.Stop();

            // the cancelled sequence clears its running flag in a finally block
            await Task.Delay(50);
            await Configure(10, 1);
            var before = _output.Count;

            Trigger(true);

            await _output.WaitForCountAsync(before + 3);
            Assert.Equal(new object[] { true, false, false }, _output.Values.Skip(before).ToArray());
        }

        [Fact(Skip = "BUG: Stop() a second time throws a NullReferenceException, the token source is set to null on the first stop (FlashingLightsLogic.cs:63)")]
        public async Task Stop_Twice_DoesNotThrow()
        {
            await StopAsync();

            var exception = await Record.ExceptionAsync(() => Logic.Stop());

            Assert.Null(exception);
        }

        [Fact]
        public void Factory_Templates_HaveTheDocumentedDefaults()
        {
            Assert.Equal(1000L, GetLogicInterfaceByTemplate(FlashingLightsLogicFactory.Delay).Value);
            Assert.Equal(2L, GetLogicInterfaceByTemplate(FlashingLightsLogicFactory.RepeatCount).Value);
            Assert.NotNull(GetLogicInterfaceByTemplate(FlashingLightsLogicFactory.Trigger));
            Assert.NotNull(GetLogicInterfaceByTemplate(FlashingLightsLogicFactory.State));
            Assert.NotNull(GetLogicInterfaceByTemplate(FlashingLightsLogicFactory.Output));
        }
    }
}
