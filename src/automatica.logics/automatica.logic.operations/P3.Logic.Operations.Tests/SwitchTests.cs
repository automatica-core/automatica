using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Automatica.Core.Control.Base;
using Automatica.Core.Logic;
using Automatica.Core.UnitTests.Base.Logics;
using P3.Logic.Operations.Switch;
using Xunit;
using static P3.Logic.Operations.Tests.OperationsTestHelper;

namespace P3.Logic.Operations.Tests
{
    public class SwitchTests : LogicTest<SwitchLogicFactory>
    {
        private SwitchLogic Switch => (SwitchLogic)Logic;

        private IList<ILogicOutputChanged> Input(object value)
        {
            return Logic.ValueChanged(GetLogicInterfaceByTemplate(SwitchLogicFactory.RuleInput), Dispatchable, value);
        }

        [Fact]
        public void Factory_HasInputStateAndOutput()
        {
            Assert.Equal(3, RuleInstance.RuleInterfaceInstance.Count);
            Assert.Equal(SwitchLogicFactory.RuleInput, Switch.InputId);
            Assert.Equal(SwitchLogicFactory.RuleOutput, Switch.OutputId);
            Assert.Equal(Instance.LogicGuid, Switch.Id);
        }

        [Fact]
        public void Initial_StateIsOff()
        {
            Assert.Equal(SwitchState.Off, Switch.State);
        }

        [Theory]
        [InlineData(true, SwitchState.On)]
        [InlineData(false, SwitchState.Off)]
        [InlineData("true", SwitchState.On)]
        [InlineData("False", SwitchState.Off)]
        [InlineData(1, SwitchState.On)]
        [InlineData(0, SwitchState.Off)]
        [InlineData(-1, SwitchState.On)]
        [InlineData(0.0, SwitchState.Off)]
        public void Input_SetsState_AndForwardsTheInputValue(object input, SwitchState expected)
        {
            var outputs = Input(input);

            Assert.Equal(expected, Switch.State);
            // the raw input value is forwarded unchanged
            Assert.Equal(input, Output(outputs, SwitchLogicFactory.RuleOutput));
        }

        [Fact]
        public void Input_SameValueTwice_IsForwardedTwice()
        {
            Assert.Single(Input(true));
            Assert.Single(Input(true));
        }

        [Theory]
        [InlineData("1")]
        [InlineData("on")]
        public void Input_UnparsableString_EmitsNothingAndKeepsState(object input)
        {
            Input(true);

            Assert.Empty(Input(input));
            Assert.Equal(SwitchState.On, Switch.State);
        }

        [Fact]
        public void Input_Null_SwitchesOff()
        {
            Input(true);

            var outputs = Input(null);

            Assert.Equal(SwitchState.Off, Switch.State);
            Assert.Null(Output(outputs, SwitchLogicFactory.RuleOutput));
        }

        [Fact]
        public void StateInput_IsIgnored()
        {
            var outputs = Logic.ValueChanged(GetLogicInterfaceByTemplate(SwitchLogicFactory.RuleState), Dispatchable, true);

            Assert.Empty(outputs);
            Assert.Equal(SwitchState.Off, Switch.State);
        }

        [Fact]
        public async Task SwitchOnAsync_DispatchesTrue()
        {
            await Context.Dispatcher.ClearValues();

            Assert.True(await Switch.SwitchOnAsync());

            Assert.True(await WaitForDispatchedValue(Context.Dispatcher, SwitchLogicFactory.RuleOutput, true));
            Assert.Equal(SwitchState.On, Switch.State);
        }

        [Fact]
        public async Task SwitchOffAsync_DispatchesFalse()
        {
            await Context.Dispatcher.ClearValues();
            await Switch.SwitchOnAsync();

            Assert.True(await Switch.SwitchOffAsync());

            Assert.True(await WaitForDispatchedValue(Context.Dispatcher, SwitchLogicFactory.RuleOutput, false));
            Assert.Equal(SwitchState.Off, Switch.State);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task SwitchAsyncBool_DispatchesState(bool state)
        {
            await Context.Dispatcher.ClearValues();
            await Switch.SwitchAsync(!state);

            await Switch.SwitchAsync(state);

            Assert.True(await WaitForDispatchedValue(Context.Dispatcher, SwitchLogicFactory.RuleOutput, state));
            Assert.Equal(state ? SwitchState.On : SwitchState.Off, Switch.State);
        }

        [Theory]
        [InlineData(SwitchState.On, true)]
        [InlineData(SwitchState.Off, false)]
        public async Task SwitchAsyncSwitchState_DispatchesBool(SwitchState state, bool expected)
        {
            await Context.Dispatcher.ClearValues();
            await Switch.SwitchAsync(!expected);

            await Switch.SwitchAsync(state);

            Assert.True(await WaitForDispatchedValue(Context.Dispatcher, SwitchLogicFactory.RuleOutput, expected));
            Assert.Equal(state, Switch.State);
        }

        [Fact]
        public async Task ControlApi_ThenInput_InputWins()
        {
            await Switch.SwitchOnAsync();

            Input(false);

            Assert.Equal(SwitchState.Off, Switch.State);
        }
    }
}
