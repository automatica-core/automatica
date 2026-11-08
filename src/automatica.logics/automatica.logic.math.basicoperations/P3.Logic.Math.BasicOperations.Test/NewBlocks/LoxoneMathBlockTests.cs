using System;
using System.Collections.Generic;
using Automatica.Core.Logic;
using Automatica.Core.UnitTests.Base.Logics;
using P3.Logic.Math.BasicOperations.Analog;
using P3.Logic.Math.BasicOperations.SelectionSwitch;
using P3.Logic.Math.BasicOperations.Stepper;
using P3.Logic.Math.BasicOperations.Tests.TestSupport;
using Xunit;

namespace P3.Logic.Math.BasicOperations.Tests.NewBlocks
{
    public class StepperTests : LogicTest<StepperLogicFactory>
    {
        private IList<ILogicOutputChanged> Send(Guid template, object value) => Logic.ValueChanged(GetLogicInterfaceByTemplate(template), Dispatchable, value);

        private object Step() => Send(StepperLogicFactory.RuleStep, true).Of(StepperLogicFactory.RuleOutput);

        [Fact]
        public void Up_WrapsToZeroAfterTheMaximum()
        {
            Send(StepperLogicFactory.RuleParamStep, 2.5);
            Send(StepperLogicFactory.RuleParamMaximum, 5);
            Assert.Equal(2.5, Step());
            Assert.Equal(5d, Step());
            Assert.Equal(0d, Step());
            Assert.Empty(Send(StepperLogicFactory.RuleStep, false));
        }

        [Fact]
        public void Down_WrapsToTheMaximumBelowZero()
        {
            Send(StepperLogicFactory.RuleParamDown, true);
            Send(StepperLogicFactory.RuleParamMaximum, 3);
            Assert.Equal(3d, Step());
            Assert.Equal(2d, Step());
        }

        [Fact]
        public void DecimalSteps_DoNotDrift()
        {
            Send(StepperLogicFactory.RuleParamStep, 0.1);
            Send(StepperLogicFactory.RuleParamMaximum, 1);
            object last = null;
            for (var i = 0; i < 3; i++)
            {
                last = Step();
            }
            Assert.Equal(0.3, last);
        }

        [Fact]
        public void Off_ResetsAndLocks()
        {
            Step();
            Assert.Equal(0d, Send(StepperLogicFactory.RuleOff, true).Of(StepperLogicFactory.RuleOutput));
            Assert.Empty(Send(StepperLogicFactory.RuleStep, true));
            Assert.Empty(Send(StepperLogicFactory.RuleOff, false));
            Assert.Equal(1d, Step());
        }
    }

    public class AnalogMemoryTests : LogicTest<AnalogMemoryLogicFactory>
    {
        private IList<ILogicOutputChanged> Send(Guid template, object value) => Logic.ValueChanged(GetLogicInterfaceByTemplate(template), Dispatchable, value);

        [Fact]
        public void Set_CopiesAndHoldsTheValue()
        {
            Assert.Empty(Send(AnalogMemoryLogicFactory.RuleSet, true));
            Assert.Empty(Send(AnalogMemoryLogicFactory.RuleValue, 21.5));
            Assert.Equal(21.5, Send(AnalogMemoryLogicFactory.RuleSet, true).Of(AnalogMemoryLogicFactory.RuleOutput));

            Assert.Empty(Send(AnalogMemoryLogicFactory.RuleValue, 30));
            Assert.Empty(Send(AnalogMemoryLogicFactory.RuleSet, false));
            Assert.Equal(30d, Send(AnalogMemoryLogicFactory.RuleSet, 1).Of(AnalogMemoryLogicFactory.RuleOutput));
        }

        [Fact]
        public void Off_SendsZero()
        {
            Assert.Equal(0d, Send(AnalogMemoryLogicFactory.RuleOff, true).Of(AnalogMemoryLogicFactory.RuleOutput));
            Assert.Empty(Send(AnalogMemoryLogicFactory.RuleOff, false));
        }
    }

    public class MinMaxSinceResetTests : LogicTest<MinMaxSinceResetLogicFactory>
    {
        private IList<ILogicOutputChanged> Send(Guid template, object value) => Logic.ValueChanged(GetLogicInterfaceByTemplate(template), Dispatchable, value);

        [Fact]
        public void TracksExtremes_UntilReset()
        {
            var first = Send(MinMaxSinceResetLogicFactory.RuleValue, 5);
            Assert.Equal(5d, first.Of(MinMaxSinceResetLogicFactory.RuleMinimum));
            Assert.Equal(5d, first.Of(MinMaxSinceResetLogicFactory.RuleMaximum));

            var higher = Send(MinMaxSinceResetLogicFactory.RuleValue, 8);
            Assert.Single(higher);
            Assert.Equal(8d, higher.Of(MinMaxSinceResetLogicFactory.RuleMaximum));

            Assert.Empty(Send(MinMaxSinceResetLogicFactory.RuleValue, 6));
            Assert.Equal(2d, Send(MinMaxSinceResetLogicFactory.RuleValue, 2).Of(MinMaxSinceResetLogicFactory.RuleMinimum));

            var reset = Send(MinMaxSinceResetLogicFactory.RuleReset, true);
            Assert.Equal(2d, reset.Of(MinMaxSinceResetLogicFactory.RuleMinimum));
            Assert.Equal(2d, reset.Of(MinMaxSinceResetLogicFactory.RuleMaximum));
            Assert.Equal(3d, Send(MinMaxSinceResetLogicFactory.RuleValue, 3).Of(MinMaxSinceResetLogicFactory.RuleMaximum));
        }

        [Fact]
        public void ResetWithoutValue_SendsNothing()
        {
            Assert.Empty(Send(MinMaxSinceResetLogicFactory.RuleReset, true));
            Assert.Empty(Send(MinMaxSinceResetLogicFactory.RuleValue, "abc"));
        }
    }

    public class AnalogLimiterTests : LogicTest<AnalogLimiterLogicFactory>
    {
        private IList<ILogicOutputChanged> Send(Guid template, object value) => Logic.ValueChanged(GetLogicInterfaceByTemplate(template), Dispatchable, value);

        [Theory]
        [InlineData(-3, 0)]
        [InlineData(4.2, 4.2)]
        [InlineData(15, 10)]
        public void LimitsToTheDefaultRange(double input, double expected)
        {
            Assert.Equal(expected, Send(AnalogLimiterLogicFactory.RuleValue, input).Of(AnalogLimiterLogicFactory.RuleOutput));
        }

        [Fact]
        public void SwappedLimits_Work()
        {
            Send(AnalogLimiterLogicFactory.RuleParamMinimum, 20);
            Send(AnalogLimiterLogicFactory.RuleParamMaximum, 5);
            Assert.Equal(20d, Send(AnalogLimiterLogicFactory.RuleValue, 30).Of(AnalogLimiterLogicFactory.RuleOutput));
            Assert.Equal(5d, Send(AnalogLimiterLogicFactory.RuleValue, "1").Of(AnalogLimiterLogicFactory.RuleOutput));
            Assert.Empty(Send(AnalogLimiterLogicFactory.RuleValue, "x"));
        }
    }

    public class MovingAverageTests : TimerLogicTest<MovingAverageLogicFactory>
    {
        private static readonly Guid Output = MovingAverageLogicFactory.RuleOutput;

        private void Configure(double cycle, int count)
        {
            Interface(MovingAverageLogicFactory.RuleParamCycle).Value = cycle;
            Interface(MovingAverageLogicFactory.RuleParamCount).Value = count;
            Start();
        }

        private void V(object value) => Assert.Empty(Input(MovingAverageLogicFactory.RuleValue, value));
        private void R(object value) => Assert.Empty(Input(MovingAverageLogicFactory.RuleReset, value));

        [Fact]
        public void SamplesEveryCycle_AndKeepsTheLastN()
        {
            Configure(60, 3);
            V(10);
            Assert.Equal(new object[] { 10d }, Values(Output));
            Assert.Equal(60000, SutTimer.Interval);

            // only the value at the tick counts
            V(15);
            V(20);
            FireTick();
            V(30);
            FireTick();
            Assert.Equal(new object[] { 10d, 15d, 20d }, Values(Output));

            // 10 drops out: (20 + 30 + 30) / 3
            FireTick();
            Assert.Equal(80d / 3, (double)Values(Output)[^1], 10);
        }

        [Fact]
        public void ZeroCycle_SamplesEveryValue()
        {
            Configure(0, 2);
            V(1);
            V(3);
            V(5);
            Assert.Equal(new object[] { 1d, 2d, 4d }, Values(Output));
            Assert.False(SutTimer.Enabled);
        }

        [Fact]
        public void Reset_PassesTheValueThrough_ThenStartsAgain()
        {
            Configure(60, 10);
            V(10);
            FireTick();
            R(true);
            V(50);
            FireTick();
            Assert.Equal(new object[] { 10d, 10d, 10d, 50d }, Values(Output));

            R(false);
            Assert.Equal(50d, Values(Output)[^1]);
            Assert.True(SutTimer.Enabled);
            V(70);
            FireTick();
            Assert.Equal(60d, Values(Output)[^1]);
        }

        [Fact]
        public void BeforeStart_ValuesAreIgnored()
        {
            V(5);
            Assert.Empty(Values(Output));
        }
    }

    public class SelectionSwitchPlusTests : TimerLogicTest<SelectionSwitchPlusLogicFactory>
    {
        private static readonly Guid Output = SelectionSwitchPlusLogicFactory.RuleOutput;

        private void Configure(double min = 1, double max = 3, double step = 1, double repeat = 1000, double def = 2)
        {
            ((SelectionSwitchRule)Sut).LongClickSeconds = 3600;
            Interface(SelectionSwitchPlusLogicFactory.RuleParamMinimum).Value = min;
            Interface(SelectionSwitchPlusLogicFactory.RuleParamMaximum).Value = max;
            Interface(SelectionSwitchPlusLogicFactory.RuleParamStep).Value = step;
            Interface(SelectionSwitchPlusLogicFactory.RuleParamRepeat).Value = repeat;
            Interface(SelectionSwitchPlusLogicFactory.RuleParamDefault).Value = def;
            Start();
        }

        private void Plus(object value) => Assert.Empty(Input(SelectionSwitchPlusLogicFactory.RulePlus, value));

        [Fact]
        public void Plus_StartsAtDefault_AndWrapsToMinimum()
        {
            Configure();
            Plus(true);
            Plus(false);
            Plus(true);
            Plus(false);
            Plus(true);
            Plus(false);
            Assert.Equal(new object[] { 3d, 1d, 2d }, Values(Output));
        }

        [Fact]
        public void LongClick_Repeats_UntilRelease()
        {
            Configure(min: 0, max: 10, def: 0);
            Plus(true);
            Assert.Equal(3_600_000, SutTimer.Interval);

            FireTick();
            Assert.Equal(1_000_000, SutTimer.Interval);
            FireTick();
            Assert.Equal(new object[] { 1d, 2d, 3d }, Values(Output));

            Plus(false);
            Assert.False(SutTimer.Enabled);
            FireTick();
            Assert.Equal(3, Values(Output).Count);
        }

        [Fact]
        public void ZeroRepetition_NeverRepeats()
        {
            Configure(repeat: 0);
            Plus(true);
            Assert.False(SutTimer.Enabled);
            FireTick();
            Assert.Equal(new object[] { 3d }, Values(Output));
        }

        [Fact]
        public void Set_IsLimited()
        {
            Configure();
            Assert.Empty(Input(SelectionSwitchPlusLogicFactory.RuleSet, 7));
            Assert.Empty(Input(SelectionSwitchPlusLogicFactory.RuleSet, "1.5"));
            Assert.Equal(new object[] { 3d, 1.5 }, Values(Output));
        }

        [Fact]
        public void OffAndDisable_BlockTheInputs()
        {
            Configure();
            Plus(true);
            Assert.Empty(Input(SelectionSwitchPlusLogicFactory.RuleOff, true));
            Plus(true);
            Assert.Empty(Input(SelectionSwitchPlusLogicFactory.RuleOff, false));
            Assert.Equal(new object[] { 3d, 2d }, Values(Output));

            Assert.Empty(Input(SelectionSwitchPlusLogicFactory.RuleDisable, true));
            Plus(true);
            Assert.Empty(Input(SelectionSwitchPlusLogicFactory.RuleSet, 1));
            Assert.Empty(Input(SelectionSwitchPlusLogicFactory.RuleDisable, false));
            Plus(true);
            Assert.Equal(new object[] { 3d, 2d, 3d }, Values(Output));
        }
    }

    public class SelectionSwitchPlusMinusTests : TimerLogicTest<SelectionSwitchPlusMinusLogicFactory>
    {
        private static readonly Guid Output = SelectionSwitchPlusMinusLogicFactory.RuleOutput;

        private void Configure()
        {
            ((SelectionSwitchRule)Sut).LongClickSeconds = 3600;
            Interface(SelectionSwitchPlusMinusLogicFactory.RuleParamMinimum).Value = 0;
            Interface(SelectionSwitchPlusMinusLogicFactory.RuleParamMaximum).Value = 1;
            Interface(SelectionSwitchPlusMinusLogicFactory.RuleParamStep).Value = 0.5;
            Interface(SelectionSwitchPlusMinusLogicFactory.RuleParamRepeat).Value = 1000;
            Interface(SelectionSwitchPlusMinusLogicFactory.RuleParamDefault).Value = 0;
            Start();
        }

        private void Press(Guid button)
        {
            Assert.Empty(Input(button, true));
            Assert.Empty(Input(button, false));
        }

        [Fact]
        public void PlusAndMinus_StopAtTheLimits()
        {
            Configure();
            Press(SelectionSwitchPlusMinusLogicFactory.RulePlus);
            Press(SelectionSwitchPlusMinusLogicFactory.RulePlus);
            Press(SelectionSwitchPlusMinusLogicFactory.RulePlus);
            Press(SelectionSwitchPlusMinusLogicFactory.RuleMinus);
            Press(SelectionSwitchPlusMinusLogicFactory.RuleMinus);
            Press(SelectionSwitchPlusMinusLogicFactory.RuleMinus);

            // at the limit nothing new is sent
            Assert.Equal(new object[] { 0.5, 1d, 0.5, 0d }, Values(Output));
        }

        [Fact]
        public void LongClickOnMinus_Repeats()
        {
            Configure();
            Assert.Empty(Input(SelectionSwitchPlusMinusLogicFactory.RuleSet, 1));
            Assert.Empty(Input(SelectionSwitchPlusMinusLogicFactory.RuleMinus, true));
            FireTick();
            Assert.Equal(new object[] { 1d, 0.5, 0d }, Values(Output));

            // releasing the other button does not stop the repeat
            Assert.Empty(Input(SelectionSwitchPlusMinusLogicFactory.RulePlus, false));
            Assert.True(SutTimer.Enabled);
        }
    }
}
