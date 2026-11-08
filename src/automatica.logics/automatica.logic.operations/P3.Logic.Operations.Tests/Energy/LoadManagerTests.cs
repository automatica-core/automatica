using System;
using System.Linq;
using P3.Logic.Operations.Energy.LoadManager;
using Xunit;
using F = P3.Logic.Operations.Energy.LoadManager.LoadManagerLogicFactory;

namespace P3.Logic.Operations.Tests.Energy
{
    /// <summary>
    /// Load manager against a simulated house: the meter sees the base load plus the loads that are running and not locked.
    /// </summary>
    public class LoadManagerTests : EnergyLogicTest<LoadManagerLogicFactory>
    {
        private double _baseLoad;
        private readonly double[] _running = new double[LoadManagerEngine.LoadCount + 1];

        private void Configure(double maxPower = 11, double hysteresis = 0.5, double shedDelay = 2, double releaseDelay = 60)
        {
            Param(F.ParamMaxPower, maxPower);
            Param(F.ParamHysteresis, hysteresis);
            Param(F.ParamShedDelay, shedDelay);
            Param(F.ParamReleaseDelay, releaseDelay);
        }

        /// <summary>
        /// Configures a load and lets it draw its power
        /// </summary>
        private void Load(int load, int priority, double power, bool running = true)
        {
            Param(F.LoadInterface(load, F.FieldPriority), priority);
            Param(F.LoadInterface(load, F.FieldPower), power);
            _running[load] = running ? power : 0;
        }

        private bool Locked(int load) => Last(F.LoadInterface(load, F.FieldLock)) is true;

        private double Total() => _baseLoad + Enumerable.Range(1, LoadManagerEngine.LoadCount).Where(l => !Locked(l)).Sum(l => _running[l]);

        private void Simulate(double seconds, double? baseLoad = null)
        {
            if (baseLoad.HasValue)
            {
                _baseLoad = baseLoad.Value;
            }
            for (var i = 0; i < seconds; i++)
            {
                Now = Now.AddSeconds(1);
                Input(F.InputPower, Math.Round(Total(), 3));
                Sut.Tick();
            }
        }

        [Fact]
        public void BelowMaxPower_NothingIsLocked()
        {
            Configure();
            Load(1, 1, 3);
            Load(2, 2, 3);
            Start();

            Simulate(100, 4);
            Assert.False(Locked(1));
            Assert.False(Locked(2));
            Assert.Equal(false, Last(F.OutputOverload));
            Assert.Equal(1.0, Last(F.OutputReserve));
        }

        [Fact]
        public void Overload_ShedsTheLowestPriority_AfterTheShedDelay()
        {
            Configure();
            Load(1, 1, 3);
            Load(2, 2, 3);
            Load(3, 3, 3);
            Start();

            // 3 + 9 = 12 > 11
            Simulate(1, 3);
            Assert.False(Locked(3));
            Assert.Equal(true, Last(F.OutputOverload));
            Simulate(2);
            Assert.True(Locked(3));
            Assert.False(Locked(2));
            Assert.False(Locked(1));

            Simulate(100);
            Assert.True(Locked(3));
            Assert.False(Locked(2));
        }

        [Fact]
        public void LargeExcess_ShedsAsManyLoadsAsNeededAtOnce()
        {
            Configure();
            Load(1, 1, 3);
            Load(2, 2, 3);
            Load(3, 3, 3);
            Start();

            // 15 kW: 4 kW too much, two loads
            Simulate(3, 6);
            Assert.True(Locked(3));
            Assert.True(Locked(2));
            Assert.False(Locked(1));
        }

        [Fact]
        public void PriorityNumbers_DecideTheOrder_NotThePosition()
        {
            Configure();
            Load(1, 3, 3);
            Load(2, 1, 3);
            Load(3, 2, 3);
            Start();

            Simulate(3, 3);
            Assert.True(Locked(1));
            Assert.False(Locked(2));
            Assert.False(Locked(3));
        }

        [Fact]
        public void IdleLoads_AreSkipped()
        {
            Configure();
            Load(1, 1, 3);
            Load(2, 2, 3);
            Load(3, 3, 3, running: false);
            Start();

            Input(F.LoadInterface(3, F.FieldStatus), false);
            Simulate(3, 6);
            Assert.False(Locked(3));
            Assert.True(Locked(2));
        }

        [Fact]
        public void ShortPeak_ShorterThanTheShedDelay_DoesNothing()
        {
            Configure(shedDelay: 5);
            Load(1, 1, 3);
            Start();

            Simulate(4, 9);
            Simulate(1, 2);
            Simulate(4, 9);
            Assert.False(Locked(1));
        }

        [Fact]
        public void Release_AfterTheReleaseDelay_HighestPriorityFirst_OneAtATime()
        {
            Configure();
            Load(1, 1, 3);
            Load(2, 2, 3);
            Load(3, 3, 3);
            Start();

            Simulate(3, 6);
            Assert.True(Locked(2) && Locked(3));

            // base load drops to 1 kW: 1 + 3 = 4 running, 4 + 3 <= 10.5
            Simulate(59, 1);
            Assert.True(Locked(2));
            Simulate(2);
            Assert.False(Locked(2));
            Assert.True(Locked(3));

            Simulate(61);
            Assert.False(Locked(3));
        }

        [Fact]
        public void Release_WaitsUntilTheLoadFitsBelowMaxPowerMinusHysteresis()
        {
            Configure();
            Load(1, 1, 3);
            Load(2, 2, 3);
            Start();

            Simulate(3, 6);
            Assert.True(Locked(2));

            // 5 + 3 = 8 running, 8 + 3 = 11 > 10.5
            Simulate(300, 5);
            Assert.True(Locked(2));
            Simulate(61, 4.4);
            Assert.False(Locked(2));
        }

        [Fact]
        public void LinkedMaxPower_ActsAtOnce()
        {
            Configure(shedDelay: 0);
            Load(1, 1, 3);
            Start();

            Simulate(5, 4);
            Assert.False(Locked(1));

            Param(F.ParamMaxPower, 5);
            Assert.True(Locked(1));
        }

        [Fact]
        public void UnconfiguredLoads_AreNeverLocked()
        {
            Configure();
            Load(1, 1, 3);
            Start();

            Simulate(10, 20);
            Assert.True(Locked(1));
            for (var load = 2; load <= LoadManagerEngine.LoadCount; load++)
            {
                Assert.Equal(new object[] { false }, Values(F.LoadInterface(load, F.FieldLock)));
            }
        }

        [Fact]
        public void Template_HasEightLoads_WithUniqueInterfaces()
        {
            var ids = RuleInstance.RuleInterfaceInstance.Select(i => i.This2RuleInterfaceTemplate).ToList();
            Assert.Equal(ids.Count, ids.Distinct().Count());
            // 1 + 8 inputs, 2 + 8 outputs, 4 + 16 parameters
            Assert.Equal(1 + 8 + 2 + 8 + 4 + 16, ids.Count);
        }
    }
}
