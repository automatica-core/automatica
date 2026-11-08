using System;
using System.Collections.Generic;
using System.Linq;
using Automatica.Core.Logic;
using Automatica.Core.UnitTests.Base.Logics;
using P3.Logic.Compare.BaseOperations.Comparator;
using P3.Logic.Compare.BaseOperations.Monitor;
using P3.Logic.Compare.BaseOperations.Tests.TestSupport;
using P3.Logic.Compare.BaseOperations.Validator;
using Xunit;

namespace P3.Logic.Compare.BaseOperations.Tests.LoxoneBlocks
{
    public class AnalogValueMonitorTests : LogicTest<AnalogValueMonitorLogicFactory>
    {
        private IList<ILogicOutputChanged> Send(Guid template, object value) => Logic.ValueChanged(GetLogicInterfaceByTemplate(template), Dispatchable, value);

        private object Te(object value) => Send(AnalogValueMonitorLogicFactory.RuleValue, value).Single().Value;

        [Theory]
        [InlineData(2.9, true)]
        [InlineData(3, false)]
        [InlineData(5, false)]
        [InlineData(7, false)]
        [InlineData(7.1, true)]
        public void OutsideTheBand_IsReported(double value, bool expected)
        {
            Assert.Equal(expected, Te(value));
        }

        [Fact]
        public void SwappedThresholds_Work()
        {
            Send(AnalogValueMonitorLogicFactory.RuleParamUpper, 0);
            Send(AnalogValueMonitorLogicFactory.RuleParamLower, 10);
            Assert.Equal(false, Te(5));
            Assert.Equal(true, Te(-1));
        }

        [Fact]
        public void Off_SendsZero_AndLocks()
        {
            Assert.Equal(true, Te(100));
            Assert.Equal(false, Send(AnalogValueMonitorLogicFactory.RuleOff, true).Single().Value);
            Assert.Empty(Send(AnalogValueMonitorLogicFactory.RuleValue, 100));
            Assert.Empty(Send(AnalogValueMonitorLogicFactory.RuleOff, false));
            Assert.Equal(true, Te(100));
        }

        [Fact]
        public void NoNumber_SendsNothing()
        {
            Assert.Empty(Send(AnalogValueMonitorLogicFactory.RuleValue, "abc"));
        }
    }

    public class ComparatorTests : LogicTest<ComparatorLogicFactory>
    {
        private IList<ILogicOutputChanged> Send(Guid template, object value) => Logic.ValueChanged(GetLogicInterfaceByTemplate(template), Dispatchable, value);

        [Fact]
        public void Difference_SwitchesWithHysteresis()
        {
            // collector 40, storage 38: difference 2 lies in the band, stays off
            Assert.Empty(Send(ComparatorLogicFactory.RuleValue1, 40));
            Assert.Equal(false, Send(ComparatorLogicFactory.RuleValue2, 38).Single().Value);

            Assert.Equal(true, Send(ComparatorLogicFactory.RuleValue1, 45.5).Single().Value);
            // 3 is still above the off value
            Assert.Equal(true, Send(ComparatorLogicFactory.RuleValue2, 42.5).Single().Value);
            Assert.Equal(false, Send(ComparatorLogicFactory.RuleValue2, 45).Single().Value);
        }

        [Fact]
        public void DifferenceIsSigned()
        {
            Send(ComparatorLogicFactory.RuleValue1, 0);
            Assert.Equal(false, Send(ComparatorLogicFactory.RuleValue2, 10).Single().Value);
        }

        [Fact]
        public void Thresholds_AreParameters()
        {
            Send(ComparatorLogicFactory.RuleParamOn, 0.5);
            Send(ComparatorLogicFactory.RuleParamOff, 0);
            Send(ComparatorLogicFactory.RuleValue1, 1);
            Assert.Equal(true, Send(ComparatorLogicFactory.RuleValue2, 0).Single().Value);
            Assert.Empty(Send(ComparatorLogicFactory.RuleValue2, "x"));
        }
    }

    public class ValueValidatorTests : TimerLogicTest<ValueValidatorLogicFactory>
    {
        private static readonly Guid Output = ValueValidatorLogicFactory.RuleOutput;
        private static readonly Guid Error = ValueValidatorLogicFactory.RuleError;

        private System.Timers.Timer Watchdog => (System.Timers.Timer)GetField("_watchdog");

        private void Configure(double interval = 3600, double min = 0, double max = 100, double delay = 0)
        {
            Interface(ValueValidatorLogicFactory.RuleParamChangeInterval).Value = interval;
            Interface(ValueValidatorLogicFactory.RuleParamMinimum).Value = min;
            Interface(ValueValidatorLogicFactory.RuleParamMaximum).Value = max;
            Interface(ValueValidatorLogicFactory.RuleParamDelay).Value = delay;
            Start();
        }

        private void V(object value) => Assert.Empty(Input(ValueValidatorLogicFactory.RuleValue, value));
        private void En(object value) => Assert.Empty(Input(ValueValidatorLogicFactory.RuleEnable, value));

        [Fact]
        public void InvalidValues_AreDropped_AndSetTheError()
        {
            Configure();
            V(20);
            V(150);
            V("abc");
            V(-1);
            Assert.Equal(new object[] { 20d }, Values(Output));
            Assert.Equal(new object[] { false, true }, Values(Error));

            V(30);
            Assert.Equal(new object[] { 20d, 30d }, Values(Output));
            Assert.Equal(new object[] { false, true, false }, Values(Error));
        }

        [Fact]
        public void NoChange_WithinTheInterval_SetsTheError()
        {
            Configure(interval: 600);
            Assert.True(Watchdog.Enabled);
            Assert.Equal(600_000, Watchdog.Interval);

            V(20);
            V(20);
            FireTick("_watchdog_Elapsed");
            Assert.Equal(new object[] { false, true }, Values(Error));

            // the same value again is no change
            V(20);
            Assert.Equal(new object[] { false, true }, Values(Error));

            V(21);
            Assert.Equal(new object[] { false, true, false }, Values(Error));
            Assert.Equal(new object[] { 20d, 21d }, Values(Output));
        }

        [Fact]
        public void ZeroInterval_DisablesTheCheck()
        {
            Configure(interval: 0);
            Assert.False(Watchdog.Enabled);
            FireTick("_watchdog_Elapsed");
            Assert.Empty(Values(Error));
        }

        [Fact]
        public void EnableOff_IgnoresValues_AndStopsTheCheck()
        {
            Configure(interval: 600);
            V(20);
            En(false);
            Assert.False(Watchdog.Enabled);
            V(30);
            V(500);
            FireTick("_watchdog_Elapsed");

            Assert.Equal(new object[] { 20d }, Values(Output));
            Assert.Equal(new object[] { false }, Values(Error));
        }

        [Fact]
        public void Enable_WaitsTheDelay_ThenSendsTheLastValidValue()
        {
            Configure(delay: 30);
            En(false);
            En(true);
            Assert.True(SutTimer.Enabled);
            Assert.Equal(30_000, SutTimer.Interval);

            V(10);
            V(11);
            V(999);
            Assert.Empty(Values(Output));

            FireTick();
            Assert.Equal(new object[] { 11d }, Values(Output));

            V(12);
            Assert.Equal(new object[] { 11d, 12d }, Values(Output));

            // a repeated 1 is no new activation
            En(true);
            Assert.False(SutTimer.Enabled);
        }

        [Fact]
        public void FirstEnable_IsAnActivation()
        {
            Configure(delay: 5);
            En(true);
            V(50);
            Assert.Empty(Values(Output));
            FireTick();
            Assert.Equal(new object[] { 50d }, Values(Output));
        }
    }
}
