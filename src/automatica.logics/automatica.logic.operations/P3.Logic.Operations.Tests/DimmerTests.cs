using System;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.Control.Base;
using Automatica.Core.UnitTests.Base.Logics;
using P3.Logic.Operations.Dimmer;
using Xunit;
using static P3.Logic.Operations.Tests.OperationsTestHelper;

namespace P3.Logic.Operations.Tests
{
    public class DimmerTests : LogicTest<DimmerLogicFactory>
    {
        private DimmerLogic Dimmer => (DimmerLogic)Logic;

        private System.Collections.Generic.IList<Automatica.Core.Logic.ILogicOutputChanged> Input(Guid template, object value, IDispatchable source = null)
        {
            return Logic.ValueChanged(GetLogicInterfaceByTemplate(template), source ?? Dispatchable, value);
        }

        // ---------- factory ----------

        [Fact]
        public void Factory_Templates_HaveThreeInputsAndTwoOutputs()
        {
            var template = Factory.GetById(Instance.LogicGuid);
            Assert.Equal(5, template.RuleInterfaceTemplate.Count);
            Assert.Equal(5, RuleInstance.RuleInterfaceInstance.Count);
            Assert.NotNull(GetLogicInterfaceByTemplate(DimmerLogicFactory.RuleInputState));
            Assert.NotNull(GetLogicInterfaceByTemplate(DimmerLogicFactory.RuleInputValue));
            Assert.NotNull(GetLogicInterfaceByTemplate(DimmerLogicFactory.RuleInputReset));
            Assert.NotNull(GetLogicInterfaceByTemplate(DimmerLogicFactory.DimmerValue));
            Assert.NotNull(GetLogicInterfaceByTemplate(DimmerLogicFactory.DimmerState));
            Assert.Equal("P3.Logic.Operations.Dimmer", Instance.LogicName);
        }

        [Fact]
        public void Initial_State_IsOffAndValueZero()
        {
            Assert.Equal(SwitchState.Off, Dimmer.State);
            Assert.False(Dimmer.DimmerState);
            Assert.Equal(0, Dimmer.DimmerValue);
            Assert.Equal(DimmerLogicFactory.RuleInputState, Dimmer.InputId);
            Assert.Equal(DimmerLogicFactory.DimmerState, Dimmer.OutputId);
            Assert.Equal(DimmerLogicFactory.DimmerValue, Dimmer.DimmerOutputValueId);
            Assert.Equal(DimmerLogicFactory.RuleInputValue, Dimmer.DimmerInputValueId);
            Assert.Equal(Instance.LogicGuid, Dimmer.Id);
        }

        // ---------- state input ----------

        [Fact]
        public void StateInput_TrueWithoutLastValue_EmitsOnlyState()
        {
            var outputs = Input(DimmerLogicFactory.RuleInputState, true);

            Assert.Single(outputs);
            Assert.Equal(true, Output(outputs, DimmerLogicFactory.DimmerState));
            Assert.Equal(SwitchState.On, Dimmer.State);
        }

        [Fact]
        public void StateInput_TrueWithLastValue_RestoresLastValue()
        {
            Input(DimmerLogicFactory.RuleInputValue, 40);
            Input(DimmerLogicFactory.RuleInputState, false);

            var outputs = Input(DimmerLogicFactory.RuleInputState, true);

            Assert.Equal(2, outputs.Count);
            Assert.Equal(true, Output(outputs, DimmerLogicFactory.DimmerState));
            Assert.Equal(40, Output(outputs, DimmerLogicFactory.DimmerValue));
        }

        [Fact]
        public void StateInput_False_EmitsStateFalseAndValueZero()
        {
            var outputs = Input(DimmerLogicFactory.RuleInputState, false);

            Assert.Equal(2, outputs.Count);
            Assert.Equal(false, Output(outputs, DimmerLogicFactory.DimmerState));
            Assert.Equal(0, Output(outputs, DimmerLogicFactory.DimmerValue));
            Assert.Equal(SwitchState.Off, Dimmer.State);
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(false, false)]
        [InlineData("true", true)]
        [InlineData("False", false)]
        [InlineData(1, true)]
        [InlineData(0, false)]
        [InlineData(2.5, true)]
        public void StateInput_CoercesToBoolean(object input, bool expected)
        {
            var outputs = Input(DimmerLogicFactory.RuleInputState, input);

            Assert.Equal(expected, Output(outputs, DimmerLogicFactory.DimmerState));
            Assert.Equal(expected, Dimmer.DimmerState);
        }

        [Theory]
        [InlineData("1")]
        [InlineData("on")]
        [InlineData("")]
        public void StateInput_UnparsableString_EmitsNothing(object input)
        {
            var outputs = Input(DimmerLogicFactory.RuleInputState, input);

            Assert.Empty(outputs);
            Assert.False(Dimmer.DimmerState);
        }

        [Fact]
        public void StateInput_Null_IsTreatedAsFalse()
        {
            // Convert.ToBoolean(null) == false
            var outputs = Input(DimmerLogicFactory.RuleInputState, null);

            Assert.Equal(false, Output(outputs, DimmerLogicFactory.DimmerState));
            Assert.Equal(0, Output(outputs, DimmerLogicFactory.DimmerValue));
        }

        [Fact]
        public void StateInput_FromNonVisualizationSource_IsIgnored()
        {
            var outputs = Input(DimmerLogicFactory.RuleInputState, true, new NonVisuDispatchable());

            Assert.Empty(outputs);
            Assert.False(Dimmer.DimmerState);
        }

        // ---------- value input ----------

        [Theory]
        [InlineData(40, 40, true)]
        [InlineData(100, 100, true)]
        [InlineData(1, 1, true)]
        [InlineData(0, 0, false)]
        [InlineData(-10, -10, false)]
        [InlineData(40.6, 41, true)]
        [InlineData("60", 60, true)]
        public void ValueInput_EmitsStateAndValue(object input, int expectedValue, bool expectedState)
        {
            var outputs = Input(DimmerLogicFactory.RuleInputValue, input);

            Assert.Equal(2, outputs.Count);
            Assert.Equal(expectedState, Output(outputs, DimmerLogicFactory.DimmerState));
            Assert.Equal(expectedValue, Output(outputs, DimmerLogicFactory.DimmerValue));
            Assert.Equal(expectedValue, Dimmer.DimmerValue);
        }

        [Fact]
        public void ValueInput_SameValueTwice_SecondIsSuppressed()
        {
            Assert.NotEmpty(Input(DimmerLogicFactory.RuleInputValue, 40));
            Assert.Empty(Input(DimmerLogicFactory.RuleInputValue, 40));
            Assert.Empty(Input(DimmerLogicFactory.RuleInputValue, 40.0));
            Assert.NotEmpty(Input(DimmerLogicFactory.RuleInputValue, 41));
        }

        [Fact]
        public void ValueInput_DoesNotChangeLastState()
        {
            // the value input only emits the state, the internal state is only set by the state input / control API
            Input(DimmerLogicFactory.RuleInputValue, 40);

            Assert.False(Dimmer.DimmerState);
            Assert.Equal(40, Dimmer.DimmerValue);
        }

        [Theory]
        [InlineData(double.NaN)]
        [InlineData(double.PositiveInfinity)]
        [InlineData("abc")]
        public void ValueInput_InvalidNumber_EmitsNothing(object input)
        {
            var outputs = Input(DimmerLogicFactory.RuleInputValue, input);

            Assert.Empty(outputs);
            Assert.Equal(0, Dimmer.DimmerValue);
        }

        [Fact]
        public void ValueInput_FromNonVisualizationSource_IsIgnored()
        {
            var outputs = Input(DimmerLogicFactory.RuleInputValue, 40, new NonVisuDispatchable());

            Assert.Empty(outputs);
            Assert.Equal(0, Dimmer.DimmerValue);
        }

        // ---------- reset ----------

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ResetInput_AlwaysEmitsValueZero(bool value)
        {
            Input(DimmerLogicFactory.RuleInputValue, 40);

            var outputs = Input(DimmerLogicFactory.RuleInputReset, value);

            Assert.Single(outputs);
            Assert.Equal(0, Output(outputs, DimmerLogicFactory.DimmerValue));
        }

        [Fact]
        public void ResetInput_FromNonVisualizationSource_IsAccepted()
        {
            var outputs = Input(DimmerLogicFactory.RuleInputReset, true, new NonVisuDispatchable());

            Assert.Equal(0, Output(outputs, DimmerLogicFactory.DimmerValue));
        }

        [Fact(Skip = "BUG: Dimmer reset emits 0 but keeps _lastValue, so the same value from the visu is suppressed afterwards (DimmerLogic.cs:80-83, the Slider got this fix in ab70dd2d)")]
        public void ResetInput_ThenSameValue_IsEmittedAgain()
        {
            Input(DimmerLogicFactory.RuleInputValue, 40);
            Input(DimmerLogicFactory.RuleInputReset, true);

            var outputs = Input(DimmerLogicFactory.RuleInputValue, 40);

            Assert.Equal(40, Output(outputs, DimmerLogicFactory.DimmerValue));
        }

        [Fact]
        public void UnknownInterface_EmitsNothing()
        {
            // an output instance fed back as input is not handled
            var outputs = Logic.ValueChanged(GetLogicInterfaceByTemplate(DimmerLogicFactory.DimmerValue), Dispatchable, 10);
            Assert.Empty(outputs);
        }

        // ---------- control API (fix ab70dd2d: the state output gets the bool state, not the value interface) ----------

        private async Task AssertDispatched(int expectedValue, bool expectedState)
        {
            Assert.True(await WaitForDispatchedValue(Context.Dispatcher, DimmerLogicFactory.DimmerValue, expectedValue),
                $"value was {DispatchedValue(Context.Dispatcher, DimmerLogicFactory.DimmerValue)}");
            Assert.True(await WaitForDispatchedValue(Context.Dispatcher, DimmerLogicFactory.DimmerState, expectedState),
                $"state was {DispatchedValue(Context.Dispatcher, DimmerLogicFactory.DimmerState)}");
            Assert.IsType<bool>(DispatchedValue(Context.Dispatcher, DimmerLogicFactory.DimmerState));
        }

        [Fact]
        public async Task SwitchOnAsync_DispatchesBoolTrueAnd100()
        {
            await Context.Dispatcher.ClearValues();

            Assert.True(await Dimmer.SwitchOnAsync());

            await AssertDispatched(100, true);
            Assert.Equal(SwitchState.On, Dimmer.State);
            Assert.True(Dimmer.DimmerState);
            Assert.Equal(100, Dimmer.DimmerValue);
        }

        [Fact]
        public async Task SwitchOffAsync_DispatchesBoolFalseAndZero()
        {
            await Context.Dispatcher.ClearValues();
            await Dimmer.SwitchOnAsync();

            Assert.True(await Dimmer.SwitchOffAsync());

            await AssertDispatched(0, false);
            Assert.Equal(SwitchState.Off, Dimmer.State);
        }

        [Theory]
        [InlineData(true, 100)]
        [InlineData(false, 0)]
        public async Task SwitchAsyncBool_DispatchesBoolState(bool state, int expectedValue)
        {
            await Context.Dispatcher.ClearValues();
            if (!state)
            {
                await Dimmer.SwitchOnAsync();
            }

            Assert.True(await Dimmer.SwitchAsync(state));

            await AssertDispatched(expectedValue, state);
        }

        [Theory]
        [InlineData(SwitchState.On, true, 100)]
        [InlineData(SwitchState.Off, false, 0)]
        public async Task SwitchAsyncSwitchState_DispatchesBoolState(SwitchState state, bool expectedState, int expectedValue)
        {
            await Context.Dispatcher.ClearValues();
            if (!expectedState)
            {
                await Dimmer.SwitchOnAsync();
            }

            Assert.True(await Dimmer.SwitchAsync(state));

            await AssertDispatched(expectedValue, expectedState);
            Assert.Equal(state, Dimmer.State);
        }

        [Theory]
        [InlineData(50, 50, true)]
        [InlineData(100, 100, true)]
        [InlineData(150, 100, true)]
        [InlineData(1, 1, true)]
        [InlineData(0, 0, false)]
        public async Task DimAsync_ClampsAndDispatchesStateAndValue(int input, int expectedValue, bool expectedState)
        {
            await Context.Dispatcher.ClearValues();
            if (!expectedState)
            {
                await Dimmer.SwitchOnAsync();
            }

            Assert.True(await Dimmer.DimAsync(input));

            await AssertDispatched(expectedValue, expectedState);
            Assert.Equal(expectedValue, Dimmer.DimmerValue);
            Assert.Equal(expectedState, Dimmer.DimmerState);
        }

        [Fact(Skip = "BUG: DimAsync clamps values > 100 but not < 0, a negative value is dispatched as is (DimmerLogic.cs:133-145)")]
        public async Task DimAsync_NegativeValue_IsClampedToZero()
        {
            await Context.Dispatcher.ClearValues();

            await Dimmer.DimAsync(-5);

            await AssertDispatched(0, false);
        }

        [Fact]
        public async Task SwitchOffAsync_ThenStateInputTrue_RestoresStoredZero()
        {
            await Context.Dispatcher.ClearValues();
            await Dimmer.SwitchOnAsync();
            await Dimmer.SwitchOffAsync();

            // SwitchOff stored 0 as last value - switching on from the visu restores 0
            var outputs = Input(DimmerLogicFactory.RuleInputState, true);
            Assert.Equal(true, Output(outputs, DimmerLogicFactory.DimmerState));
            Assert.Equal(0, Output(outputs, DimmerLogicFactory.DimmerValue));
        }

        [Fact]
        public async Task DimAsync_ThenSameValueInput_IsSuppressed()
        {
            await Context.Dispatcher.ClearValues();
            await Dimmer.DimAsync(30);

            Assert.Empty(Input(DimmerLogicFactory.RuleInputValue, 30));
            Assert.NotEmpty(Input(DimmerLogicFactory.RuleInputValue, 31));
        }

        [Fact]
        public void LogicInputChanged_DispatchesOutputs()
        {
            Context.Dispatcher.ClearValues().GetAwaiter().GetResult();

            LogicInputChanged(GetLogicInterfaceByTemplate(DimmerLogicFactory.RuleInputValue), 70);

            Assert.Equal(70, DispatchedValue(Context.Dispatcher, DimmerLogicFactory.DimmerValue));
            Assert.Equal(true, DispatchedValue(Context.Dispatcher, DimmerLogicFactory.DimmerState));
        }

        [Fact]
        public void StateAndValueInputs_TruthTableSequence()
        {
            // value 30 -> off -> on (restore 30) -> value 0 -> on (restore 0)
            Assert.Equal(30, Output(Input(DimmerLogicFactory.RuleInputValue, 30), DimmerLogicFactory.DimmerValue));
            Assert.Equal(0, Output(Input(DimmerLogicFactory.RuleInputState, false), DimmerLogicFactory.DimmerValue));
            Assert.Equal(30, Output(Input(DimmerLogicFactory.RuleInputState, true), DimmerLogicFactory.DimmerValue));

            var off = Input(DimmerLogicFactory.RuleInputValue, 0);
            Assert.Equal(false, Output(off, DimmerLogicFactory.DimmerState));
            Assert.Equal(0, Output(off, DimmerLogicFactory.DimmerValue));

            var on = Input(DimmerLogicFactory.RuleInputState, true);
            Assert.Equal(0, Output(on, DimmerLogicFactory.DimmerValue));
            Assert.Equal(2, on.Count);
        }
    }
}
