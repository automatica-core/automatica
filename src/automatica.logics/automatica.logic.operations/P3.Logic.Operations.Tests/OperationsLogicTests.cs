using System;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.UnitTests.Base.Logics;
using P3.Logic.Operations.Dimmer;
using P3.Logic.Operations.Shutter;
using P3.Logic.Operations.Slider;
using Xunit;

// LogicTest uses a process-wide dispatcher singleton, so the test classes must not run in parallel
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace P3.Logic.Operations.Tests
{
    public class DimmerLogicTests : LogicTest<DimmerLogicFactory>
    {
        [Fact]
        public async Task SwitchOnAndOff_DispatchesBooleanState()
        {
            await Context.Dispatcher.ClearValues();
            var dimmer = (DimmerLogic)Logic;

            await dimmer.SwitchOnAsync();
            await Task.Delay(100);

            var values = Context.Dispatcher.GetValues(DispatchableType.RuleInstance);
            Assert.Equal(true, values[DimmerLogicFactory.DimmerState].Value);
            Assert.Equal(100, values[DimmerLogicFactory.DimmerValue].Value);

            await dimmer.SwitchOffAsync();
            await Task.Delay(100);

            values = Context.Dispatcher.GetValues(DispatchableType.RuleInstance);
            Assert.Equal(false, values[DimmerLogicFactory.DimmerState].Value);
            Assert.Equal(0, values[DimmerLogicFactory.DimmerValue].Value);
        }
    }

    public class SliderLogicTests : LogicTest<SliderLogicFactory>
    {
        [Fact]
        public async Task Value_IsClampedToMax_AndResetClearsLastValue()
        {
            await Context.Dispatcher.ClearValues();

            // max param defaults to 100
            LogicInputChanged(GetLogicInterfaceByTemplate(SliderLogicFactory.RuleInputValue), 150d);
            var values = Context.Dispatcher.GetValues(DispatchableType.RuleInstance);
            Assert.Equal(100d, Convert.ToDouble(values[SliderLogicFactory.RuleOutput].Value));

            LogicInputChanged(GetLogicInterfaceByTemplate(SliderLogicFactory.RuleInputReset), true);
            values = Context.Dispatcher.GetValues(DispatchableType.RuleInstance);
            Assert.Equal(0d, Convert.ToDouble(values[SliderLogicFactory.RuleOutput].Value));

            // after a reset the same value as before must be emitted again
            LogicInputChanged(GetLogicInterfaceByTemplate(SliderLogicFactory.RuleInputValue), 100d);
            values = Context.Dispatcher.GetValues(DispatchableType.RuleInstance);
            Assert.Equal(100d, Convert.ToDouble(values[SliderLogicFactory.RuleOutput].Value));
        }
    }

    public class ShutterLogicTests : LogicTest<ShutterLogicFactory>
    {
        [Fact]
        public async Task MoveAbsolute_ThenPositionReached_ThenUp_StartsMoving()
        {
            await Context.Dispatcher.ClearValues();
            var shutter = (ShutterLogic)Logic;

            await shutter.MoveAbsoluteAsync(50);

            var values = Context.Dispatcher.GetValues(DispatchableType.RuleInstance);
            Assert.True(shutter.IsMoving);
            Assert.Equal(true, values[ShutterLogicFactory.RuleOutputMoveState].Value);
            Assert.Equal(50d, Convert.ToDouble(values[ShutterLogicFactory.RuleOutputAbsolutePercentage].Value));

            // position feedback reports the target
            LogicInputChanged(GetLogicInterfaceByTemplate(ShutterLogicFactory.RuleInputAbsolutePercentage), 50);
            values = Context.Dispatcher.GetValues(DispatchableType.RuleInstance);
            Assert.False(shutter.IsMoving);
            Assert.Equal(false, values[ShutterLogicFactory.RuleOutputMoveState].Value);

            // Up now moves instead of only sending Stop
            LogicInputChanged(GetLogicInterfaceByTemplate(ShutterLogicFactory.RuleInputUp), true);
            values = Context.Dispatcher.GetValues(DispatchableType.RuleInstance);
            Assert.True(shutter.IsMoving);
            Assert.Equal(0, values[ShutterLogicFactory.RuleOutputShutterMove].Value);
            Assert.Equal(true, values[ShutterLogicFactory.RuleOutputMoveState].Value);
            Assert.False(values.ContainsKey(ShutterLogicFactory.RuleOutputStop));
        }

        [Fact]
        public void MoveInput_SetsMoving_SoTheNextUpStops()
        {
            var shutter = (ShutterLogic)Logic;

            LogicInputChanged(GetLogicInterfaceByTemplate(ShutterLogicFactory.RuleInputMove), true);
            Assert.True(shutter.IsMoving);

            LogicInputChanged(GetLogicInterfaceByTemplate(ShutterLogicFactory.RuleInputUp), true);
            Assert.False(shutter.IsMoving);
        }
    }
}
