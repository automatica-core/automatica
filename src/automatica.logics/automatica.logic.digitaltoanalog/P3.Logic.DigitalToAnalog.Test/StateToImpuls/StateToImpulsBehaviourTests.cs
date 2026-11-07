using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.UnitTests.Base.Logics;
using P3.Logic.DigitalToAnalog.StateToImpuls;
using P3.Logic.DigitalToAnalog.Tests.TestSupport;
using Xunit;

namespace P3.Logic.DigitalToAnalog.Tests.StateToImpuls
{
    public class StateToImpulsBehaviourTests : LogicTest<StateToImpulsLogicFactory>
    {
        private readonly ValueRecorder _output;

        // the logic under test dispatches into its own recording dispatcher, not into the shared one of the base class
        private new Automatica.Core.Logic.ILogic Logic { get; }

        public StateToImpulsBehaviourTests()
        {
            var dispatcher = new RecordingDispatcher();
            Logic = Instance.CreateLogicInstance(new LogicContextMock(RuleInstance, Factory, dispatcher));
            Logic.Start().GetAwaiter().GetResult();
            _output = new ValueRecorder(dispatcher, StateToImpulsLogicFactory.RuleOutput);
        }

        private StateToImpulsRule Rule => (StateToImpulsRule)Logic;

        private IList<Automatica.Core.Logic.ILogicOutputChanged> Input(object value)
        {
            return Logic.ValueChanged(GetLogicInterfaceByTemplate(StateToImpulsLogicFactory.RuleInput), Dispatchable, value);
        }

        private void SetDelay(object value)
        {
            Logic.ValueChanged(GetLogicInterfaceByTemplate(StateToImpulsLogicFactory.DelayParameter), Dispatchable, value);
        }

        [Fact]
        public void Delay_Default_Is1000()
        {
            Assert.Equal(1000, Rule.Delay);
        }

        public static IEnumerable<object[]> DelayParameterData => new List<object[]>
        {
            new object[] { 250, 250 },
            new object[] { 250L, 250 },
            new object[] { "250", 250 },
            new object[] { 12.7d, 13 },
            new object[] { 0, 0 },
            new object[] { -5, -5 },
            new object[] { "abc", 1000 },
            new object[] { "12.7", 1000 },
            new object[] { null, 1000 },
            new object[] { 5_000_000_000L, 1000 },
        };

        [Theory]
        [MemberData(nameof(DelayParameterData))]
        public void DelayParameter_Changed_IsConvertedOrFallsBackToDefault(object value, int expected)
        {
            SetDelay(value);

            Assert.Equal(expected, Rule.Delay);
        }

        [Fact]
        public async Task Start_ReadsTheCurrentDelayParameter()
        {
            // parameters are pushed again on (re)start - the value cached in the constructor must not win
            GetLogicInterfaceByTemplate(StateToImpulsLogicFactory.DelayParameter).Value = 300;
            await Logic.Start();

            Assert.Equal(300, Rule.Delay);
        }

        public static IEnumerable<object[]> ImpulseTruthTable => new List<object[]>
        {
            new object[] { 1, true },
            new object[] { 2, true },
            new object[] { 255L, true },
            new object[] { true, true },
            new object[] { "1", true },
            new object[] { 0.6d, true },
            new object[] { 0, false },
            new object[] { false, false },
            new object[] { -1, false },
            new object[] { 0.4d, false },
            new object[] { "0", false },
        };

        [Theory]
        [MemberData(nameof(ImpulseTruthTable))]
        public async Task Input_WithoutDelay_DispatchesOneThenZeroOnlyForPositiveValues(object value, bool impulse)
        {
            SetDelay(0);

            var result = Input(value);

            // with no impulse time both values are dispatched directly, nothing is returned
            Assert.Empty(result);

            if (impulse)
            {
                var values = await _output.WaitForCountAsync(2);
                Assert.Equal(new object[] { 1, 0 }, values);
            }
            else
            {
                await Task.Delay(50);
                Assert.Empty(_output.Values);
            }
        }

        [Fact]
        public async Task Input_NegativeDelay_BehavesLikeNoDelay()
        {
            SetDelay(-10);

            Assert.Empty(Input(1));

            var values = await _output.WaitForCountAsync(2);
            Assert.Equal(new object[] { 1, 0 }, values);
        }

        [Fact]
        public async Task Input_WithDelay_ReturnsOneAndDispatchesZeroAfterTheDelay()
        {
            SetDelay(150);

            var result = Input(1);

            Assert.Single(result);
            Assert.Equal(1, result.First().Value);
            Assert.Equal(StateToImpulsLogicFactory.RuleOutput, result.First().Instance.RuleInterfaceInstance.This2RuleInterfaceTemplate);

            // the 0 can not be there before the impulse time
            Assert.Empty(_output.Values);

            var values = await _output.WaitForCountAsync(1);
            Assert.Equal(0, values.Single());
        }

        [Theory]
        [InlineData("abc")]
        [InlineData(double.NaN)]
        [InlineData(double.MaxValue)]
        public async Task Input_NotConvertible_IsIgnored(object value)
        {
            SetDelay(0);

            Assert.Empty(Input(value));

            await Task.Delay(50);
            Assert.Empty(_output.Values);
        }

        [Fact]
        public async Task Input_NullAsFirstValue_DoesNothing()
        {
            SetDelay(0);

            Assert.Empty(Input(null));

            await Task.Delay(50);
            Assert.Empty(_output.Values);
        }

        [Fact]
        public async Task Input_NullAfterPositiveValue_RepeatsTheImpulse()
        {
            // current behaviour: null does not reset the last input, so the last positive value pulses again
            SetDelay(0);

            Input(1);
            await _output.WaitForCountAsync(2);

            Input(null);

            var values = await _output.WaitForCountAsync(4);
            Assert.Equal(new object[] { 1, 0, 1, 0 }, values);
        }

        [Fact]
        public async Task Input_PositiveThenZero_OnlyTheFirstPulses()
        {
            SetDelay(0);

            Input(1);
            await _output.WaitForCountAsync(2);

            Assert.Empty(Input(0));

            await Task.Delay(50);
            Assert.Equal(2, _output.Count);
        }

        [Fact]
        public async Task Input_SameValueTwice_PulsesTwice()
        {
            SetDelay(0);

            Input(1);
            Input(1);

            // with impulse time 0 the logic dispatches 1 and 0 itself, in this order (the 0 may never overtake the 1)
            var values = await _output.WaitForCountAsync(4);
            Assert.Equal(new object[] { 1, 0, 1, 0 }, values);
        }

        [Fact]
        public async Task Input_WithDelay_SecondImpulseDispatchesASecondZero()
        {
            SetDelay(30);

            Assert.Equal(1, Assert.Single(Input(1)).Value);
            Assert.Equal(1, Assert.Single(Input(1)).Value);

            var values = await _output.WaitForCountAsync(2);
            Assert.Equal(new object[] { 0, 0 }, values);
        }
    }
}
