using System;
using System.Linq;
using P3.Logic.Operations.Energy.EnergyManager;
using Xunit;
using F = P3.Logic.Operations.Energy.EnergyManager.EnergyManagerLogicFactory;

namespace P3.Logic.Operations.Tests.Energy
{
    /// <summary>
    /// Energy manager against a simulated house: the grid meter sees the PV surplus minus the power of the loads that are on.
    /// </summary>
    public class EnergyManagerTests : EnergyLogicTest<EnergyManagerLogicFactory>
    {
        private double _pvSurplus;

        private EnergyManagerRule Rule => (EnergyManagerRule)Sut;

        private void Configure(double reserve = 0.1, double hysteresis = 0.2, double onDelay = 60, double offDelay = 30, double modulation = 10)
        {
            Param(F.ParamReserve, reserve);
            Param(F.ParamHysteresis, hysteresis);
            Param(F.ParamOnDelay, onDelay);
            Param(F.ParamOffDelay, offDelay);
            Param(F.ParamModulationInterval, modulation);
        }

        private void Load(int load, int priority, double onPower, double maxPower = 0, EnergyLoadType type = EnergyLoadType.Digital, double minOn = 0, double minOff = 0)
        {
            Param(F.LoadInterface(load, F.FieldPriority), priority);
            Param(F.LoadInterface(load, F.FieldOnPower), onPower);
            Param(F.LoadInterface(load, F.FieldMaxPower), maxPower);
            Param(F.LoadInterface(load, F.FieldType), $"{(int)type}");
            Param(F.LoadInterface(load, F.FieldMinOn), minOn);
            Param(F.LoadInterface(load, F.FieldMinOff), minOff);
        }

        private bool Q(int load) => Last(F.LoadInterface(load, F.FieldSwitch)) is true;

        private double P(int load) => Last(F.LoadInterface(load, F.FieldSetpoint)) is double d ? d : 0;

        private double LoadsPower() => Enumerable.Range(1, EnergyManagerEngine.LoadCount).Where(Q).Sum(P);

        /// <summary>
        /// Grid meter: + import, - export
        /// </summary>
        private void Meter() => Input(F.InputGrid, Math.Round(LoadsPower() - _pvSurplus, 3));

        /// <summary>
        /// One second: the meter reads the house, then the timer ticks
        /// </summary>
        private void Simulate(double seconds, double? pvSurplus = null)
        {
            if (pvSurplus.HasValue)
            {
                _pvSurplus = pvSurplus.Value;
            }
            for (var i = 0; i < seconds; i++)
            {
                Now = Now.AddSeconds(1);
                Meter();
                Sut.Tick();
            }
        }

        [Fact]
        public void SwitchesOn_AfterTheOnDelay()
        {
            Configure();
            Load(1, 1, 2.0);
            Start();

            // 2.0 + hysteresis 0.2 + reserve 0.1 = 2.3 needed
            Simulate(59, 2.4);
            Assert.False(Q(1));
            Simulate(2);
            Assert.True(Q(1));
            Assert.Equal(2.0, P(1));
        }

        [Fact]
        public void DoesNotSwitchOn_BelowSwitchOnPowerPlusHysteresisAndReserve()
        {
            Configure();
            Load(1, 1, 2.0);
            Start();

            Simulate(300, 2.25);
            Assert.False(Q(1));
        }

        [Fact]
        public void ShortSurplus_ShorterThanTheDelay_DoesNothing()
        {
            Configure();
            Load(1, 1, 2.0);
            Start();

            Simulate(40, 3);
            Simulate(5, 0);
            Simulate(40, 3);
            Assert.False(Q(1));
            Simulate(25);
            Assert.True(Q(1));
        }

        [Fact]
        public void SwitchesLoadsOn_ByPriority_OneLoadPerDelay()
        {
            Configure();
            Load(1, 2, 1.0);
            Load(2, 1, 1.5);
            Start();

            Simulate(61, 3.5);
            Assert.True(Q(2));
            Assert.False(Q(1));

            Simulate(30);
            Assert.False(Q(1));
            Simulate(31);
            Assert.True(Q(1));
        }

        [Fact]
        public void SmallerLowerPriorityLoad_UsesSurplus_TheHigherOneCannotUse()
        {
            Configure();
            Load(1, 1, 3.0);
            Load(2, 2, 1.0);
            Start();

            Simulate(200, 1.5);
            Assert.False(Q(1));
            Assert.True(Q(2));
        }

        [Fact]
        public void HigherPriorityLoad_TakesThePowerOfALowerOne_BothSwitchTogether()
        {
            Configure();
            Load(1, 1, 2.0);
            Load(2, 2, 1.0);
            Start();

            Simulate(61, 1.5);
            Assert.True(Q(2));

            // 2.4 kW: load 1 needs 2.3 - only fits without load 2
            Simulate(1, 2.4);
            var switched = Values(F.LoadInterface(2, F.FieldSwitch)).Count;
            for (var i = 0; i < 120 && !Q(1); i++)
            {
                Simulate(1);
                // load 2 stays on until load 1 takes over
                Assert.True(Q(1) || Q(2));
            }
            Assert.True(Q(1));
            Assert.False(Q(2));
            Assert.Equal(switched + 1, Values(F.LoadInterface(2, F.FieldSwitch)).Count);
        }

        [Fact]
        public void Import_ShedsLowestPriorityFirst_AfterTheOffDelay()
        {
            Configure();
            Load(1, 1, 1.0);
            Load(2, 2, 1.0);
            Start();

            Simulate(200, 3);
            Assert.True(Q(1));
            Assert.True(Q(2));

            // 0.5 kW import with both on: one load is enough
            Simulate(29, 1.5);
            Assert.True(Q(2));
            Simulate(2);
            Assert.False(Q(2));
            Assert.True(Q(1));

            Simulate(300);
            Assert.True(Q(1));
            Assert.False(Q(2));
        }

        [Fact]
        public void LargeDeficit_ShedsSeveralLoadsAtOnce()
        {
            Configure();
            Load(1, 1, 1.0);
            Load(2, 2, 1.0);
            Load(3, 3, 1.0);
            Start();

            Simulate(300, 4);
            Assert.True(Q(1) && Q(2) && Q(3));

            // 1.8 kW import: loads 3 and 2
            Simulate(31, 1.2);
            Assert.False(Q(2));
            Assert.False(Q(3));
            Assert.True(Q(1));
        }

        [Fact]
        public void SmallImport_WithinTheHysteresis_KeepsTheLoadOn()
        {
            Configure();
            Load(1, 1, 2.0);
            Start();

            Simulate(61, 2.5);
            Assert.True(Q(1));

            // the PV drops to 1.95 kW: 0.05 kW import, spare -0.15 > -0.2
            Simulate(600, 1.95);
            Assert.True(Q(1));
            Assert.Single(Values(F.LoadInterface(1, F.FieldSwitch)), v => v is true);
        }

        [Fact]
        public void MinimumOnTime_KeepsTheLoadOn()
        {
            Configure(offDelay: 5);
            Load(1, 1, 2.0, minOn: 300);
            Start();

            Simulate(61, 2.5);
            Assert.True(Q(1));

            Simulate(200, 0);
            Assert.True(Q(1));
            Simulate(110);
            Assert.False(Q(1));
        }

        [Fact]
        public void MinimumOffTime_KeepsTheLoadOff()
        {
            Configure(onDelay: 5, offDelay: 5);
            Load(1, 1, 2.0, minOff: 300);
            Start();

            Simulate(6, 2.5);
            Assert.True(Q(1));
            Simulate(10, 0);
            Assert.False(Q(1));

            Simulate(200, 3);
            Assert.False(Q(1));
            Simulate(110);
            Assert.True(Q(1));
        }

        [Fact]
        public void AnalogLoad_GetsASetpointBetweenSwitchOnAndMaxPower()
        {
            Configure(onDelay: 10, modulation: 10);
            Load(1, 1, 1.4, 11, EnergyLoadType.Analog);
            Start();

            Simulate(11, 4.1);
            Assert.True(Q(1));
            Assert.Equal(1.4, P(1));

            // raised once the meter shows the spare power, at most once per interval
            Simulate(15);
            Assert.Equal(4.0, P(1), 3);

            // more PV: up to max power
            Simulate(30, 20);
            Assert.Equal(11, P(1));

            // less PV: lowered at once
            Simulate(1, 3.1);
            Assert.Equal(3.0, P(1), 3);
        }

        [Fact]
        public void AnalogLoad_AStaleMeterValue_IsNotAppliedTwice()
        {
            Configure(onDelay: 10, modulation: 5);
            Load(1, 1, 1.4, 11, EnergyLoadType.Analog);
            Start();

            Simulate(60, 20);
            Assert.Equal(11, P(1));

            // PV drops to 5 kW: the meter shows 6 kW import once
            _pvSurplus = 5;
            Meter();
            Assert.Equal(4.9, P(1), 3);

            // ticks without a new meter value keep the setpoint
            for (var i = 0; i < 10; i++)
            {
                Advance(1);
            }
            Assert.Equal(4.9, P(1), 3);
            Assert.True(Q(1));
        }

        [Fact]
        public void AnalogLoad_OfHigherPriority_IsFilledBeforeALowerLoadStarts()
        {
            Configure(onDelay: 10, modulation: 5);
            Load(1, 1, 1.4, 11, EnergyLoadType.Analog);
            Load(2, 2, 1.0);
            Start();

            Simulate(300, 6);
            Assert.True(Q(1));
            Assert.False(Q(2));
            Assert.Equal(5.9, P(1), 3);

            // above max of the wallbox the rest goes to load 2
            Simulate(300, 13);
            Assert.Equal(11, P(1));
            Assert.True(Q(2));
        }

        [Fact]
        public void Lock_SwitchesOffAtOnce_AndKeepsTheLoadOff()
        {
            Configure();
            Load(1, 1, 1.0);
            Start();

            Simulate(61, 3);
            Assert.True(Q(1));

            Input(F.LoadInterface(1, F.FieldLock), true);
            Assert.False(Q(1));
            Simulate(300);
            Assert.False(Q(1));

            Input(F.LoadInterface(1, F.FieldLock), false);
            Simulate(61);
            Assert.True(Q(1));
        }

        [Fact]
        public void Manual_SwitchesOnAtOnce_WithFullPower_EvenWithoutSurplus()
        {
            Configure();
            Load(1, 1, 1.4, 11, EnergyLoadType.Analog);
            Start();

            Input(F.LoadInterface(1, F.FieldManual), true);
            Assert.True(Q(1));
            Assert.Equal(11, P(1));

            Simulate(300, 0);
            Assert.True(Q(1));
            Assert.Equal(0, Rule.Engine.Allocated);
        }

        [Fact]
        public void Manual_LoadIsNotCountedAsAvailablePower()
        {
            Configure();
            Load(1, 1, 2.0);
            Load(2, 2, 2.0);
            Start();

            Input(F.LoadInterface(1, F.FieldManual), true);
            // 3 kW PV, 2 kW taken by the manual load: 1 kW is not enough for load 2
            Simulate(300, 3);
            Assert.True(Q(1));
            Assert.False(Q(2));
        }

        [Fact]
        public void LockWinsOverManual()
        {
            Configure();
            Load(1, 1, 1.0);
            Start();

            Input(F.LoadInterface(1, F.FieldManual), true);
            Input(F.LoadInterface(1, F.FieldLock), true);
            Assert.False(Q(1));
        }

        [Fact]
        public void Disable_SwitchesTheAutomaticLoadsOffAtOnce()
        {
            Configure();
            Load(1, 1, 1.0);
            Load(2, 2, 1.0);
            Start();

            Input(F.LoadInterface(2, F.FieldManual), true);
            Simulate(61, 3);
            Assert.True(Q(1));

            Input(F.InputEnable, false);
            Assert.False(Q(1));
            Assert.True(Q(2));
            Simulate(300);
            Assert.False(Q(1));
        }

        [Fact]
        public void Battery_BelowMinSoc_ChargesFirst()
        {
            Configure();
            Param(F.ParamMinSoc, 50);
            Load(1, 1, 2.0);
            Start();

            // the battery takes all the surplus, nothing is exported
            Input(F.InputBattery, 3.0);
            Input(F.InputSoc, 30);
            Simulate(300, 0);
            Assert.False(Q(1));

            // above the minimum SoC the charging power is available
            Input(F.InputSoc, 60);
            Simulate(61);
            Assert.True(Q(1));
        }

        [Fact]
        public void Soc_BelowMinimum_BlocksNewLoads_EvenWithExport()
        {
            Configure();
            Param(F.ParamMinSoc, 50);
            Load(1, 1, 2.0);
            Start();

            Input(F.InputSoc, 20);
            Simulate(300, 5);
            Assert.False(Q(1));
        }

        [Fact]
        public void BatteryDischarge_CountsAsDeficit()
        {
            Configure();
            Load(1, 1, 2.0);
            Start();

            Simulate(61, 3);
            Assert.True(Q(1));

            // the battery covers the load, the grid shows 0
            Input(F.InputBattery, -2.0);
            Simulate(31, 0);
            Assert.False(Q(1));
        }

        [Fact]
        public void SurplusInput_IsUsedInsteadOfTheGrid()
        {
            Configure();
            Load(1, 1, 1.0);
            Start();

            Input(F.InputGrid, 5); // importing
            Input(F.InputSurplus, 2.0);
            for (var i = 0; i < 61; i++)
            {
                Advance(1);
            }
            Assert.True(Q(1));
        }

        [Fact]
        public void InvertedGrid_TreatsPositiveAsExport()
        {
            Configure();
            Param(F.ParamInvertGrid, true);
            Load(1, 1, 1.0);
            Start();

            Input(F.InputGrid, 2.0);
            for (var i = 0; i < 61; i++)
            {
                Advance(1);
            }
            Assert.True(Q(1));
            // the fixed meter value does not show the load: 2.0 - reserve + the 1.0 kW of the load
            Assert.Equal(2.9, Last(F.OutputAvailable));
        }

        [Fact]
        public void NoMeasurement_NothingSwitchesOn()
        {
            Configure();
            Load(1, 1, 1.0);
            Start();

            for (var i = 0; i < 300; i++)
            {
                Advance(1);
            }
            Assert.False(Q(1));
        }

        [Fact]
        public void UnconfiguredLoads_StayOff_AndOutputsAreOnlySentOnChange()
        {
            Configure();
            Load(1, 1, 1.0);
            Start();

            Simulate(600, 10);
            for (var load = 2; load <= EnergyManagerEngine.LoadCount; load++)
            {
                Assert.Equal(new object[] { false }, Values(F.LoadInterface(load, F.FieldSwitch)));
            }
            Assert.Equal(new object[] { false, true }, Values(F.LoadInterface(1, F.FieldSwitch)));
        }

        [Fact]
        public void BeforeStart_InputsDoNotDispatch()
        {
            Load(1, 1, 1.0);
            Input(F.InputGrid, -5);
            Assert.Empty(Recorder.GetValues());
        }

        [Fact]
        public void Template_HasTwelveLoads_WithUniqueInterfaces()
        {
            var ids = RuleInstance.RuleInterfaceInstance.Select(i => i.This2RuleInterfaceTemplate).ToList();
            Assert.Equal(ids.Count, ids.Distinct().Count());
            for (var load = 1; load <= 12; load++)
            {
                for (var field = F.FieldManual; field <= F.FieldMinOff; field++)
                {
                    Assert.Contains(F.LoadInterface(load, field), ids);
                }
            }
            // 5 inputs + 24 load inputs, 2 + 24 outputs, 7 + 72 parameters
            Assert.Equal(5 + 24 + 2 + 24 + 7 + 72, ids.Count);
        }
    }
}
