using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using Automatica.Core.UnitTests.Base.Logics;
using Automatica.Logic.Metering.Meters;
using Automatica.Logic.Metering.Tests.EnergyMonitor;
using Xunit;

namespace Automatica.Logic.Metering.Tests.Meters
{
    internal static class MeterTestSetup
    {
        /// <summary>the tests never write state files: every meter gets its own in-memory store</summary>
        [ModuleInitializer]
        internal static void UseMemoryStore() => MeterRuleBase.StoreFactory = () => new MemoryMeterStateStore();
    }

    /// <summary>LogicTest with Vienna time, a clean state and helpers to read the changed outputs</summary>
    public abstract class MeterTest<T> : LogicTest<T> where T : LogicFactory
    {
        protected MeterRuleBase Rule => (MeterRuleBase)Logic;

        protected static DateTimeOffset At(int day, int hour, int minute = 0, int month = 3) => EnergyDayCounterTests.At(day, hour, minute, month);

        /// <summary>starts the meter again at the given time with an empty store (the constructor started it at the real time)</summary>
        protected async Task StartAt(DateTimeOffset time)
        {
            Rule.Store = null;
            await Logic.Stop();
            FakeTimeProvider.SetTimeZone(EnergyDayCounterTests.Vienna);
            FakeTimeProvider.SetDateTime(time.UtcDateTime);
            Rule.Store = new MemoryMeterStateStore();
            await Logic.Start();
        }

        protected void SetTime(DateTimeOffset time) => FakeTimeProvider.SetDateTime(time.UtcDateTime);

        protected IList<ILogicOutputChanged> Input(Guid template, object value) =>
            Logic.ValueChanged(GetLogicInterfaceByTemplate(template), Dispatchable, value);

        /// <summary>sets a parameter like the core does: stored on the interface (read again on start) and sent to the logic</summary>
        protected void Param(Guid template, object value)
        {
            var param = GetLogicInterfaceByTemplate(template);
            switch (value)
            {
                case bool b: param.ValueBool = b; break;
                case string text: param.ValueString = text; break;
                default: param.ValueDouble = Convert.ToDouble(value); break;
            }
            Logic.ValueChanged(param, Dispatchable, value);
        }

        /// <summary>the value the visu gets for an output (all current outputs)</summary>
        protected double? Visu(Guid template)
        {
            var data = (Dictionary<string, object>)Logic.GetDataForVisu();
            return data.TryGetValue(GetLogicInterfaceByTemplate(template).ObjId.ToString(), out var v) ? Convert.ToDouble(v) : null;
        }

        protected static double? ValueOf(IEnumerable<ILogicOutputChanged> changes, Guid template)
        {
            var change = changes.SingleOrDefault(a => a.Instance.RuleInterfaceInstance.This2RuleInterfaceTemplate == template);
            return change == null ? null : Convert.ToDouble(change.Value);
        }

        protected void AssertKeys(params string[] keys)
        {
            var all = RuleInstance.RuleInterfaceInstance.Select(a => a.This2RuleInterfaceTemplateNavigation.Key).ToList();
            Assert.Equal(all.Count, all.Distinct().Count());
            foreach (var key in keys)
            {
                Assert.Contains(key, all);
            }
        }
    }

    public class MeterRuleTests : MeterTest<MeterLogicFactory>
    {
        private static readonly Guid[] C = MeterLogicFactory.OutputCounters;

        [Fact]
        public void Template_Interfaces()
        {
            AssertKeys("powerIn", "readingIn", "power", "today", "month", "total", "powerFactor", "readingFactor");
            Assert.Equal(RuleInterfaceType.Status, GetLogicInterfaceByTemplate(MeterLogicFactory.OutputPower).This2RuleInterfaceTemplateNavigation.InterfaceType);
        }

        [Fact]
        public async Task Power_IsCounted()
        {
            await StartAt(At(10, 8));

            var changes = Input(MeterLogicFactory.InputPower, 2d);
            Assert.Equal(2, ValueOf(changes, MeterLogicFactory.OutputPower));

            SetTime(At(10, 9, 30));
            Rule.Tick();

            Assert.Equal(3, Visu(C[0]));
            Assert.Equal(3, Visu(C[1]));
            Assert.Equal(3, Visu(C[2]));
        }

        [Fact]
        public async Task PowerFactor_TurnsWattIntoKilowatt()
        {
            await StartAt(At(10, 8));
            Param(MeterLogicFactory.ParamPowerFactor, 0.001);

            Assert.Equal(1.5, ValueOf(Input(MeterLogicFactory.InputPower, 1500), MeterLogicFactory.OutputPower));
        }

        [Fact]
        public async Task Readings_AreCounted_AndGiveThePower()
        {
            await StartAt(At(10, 8));
            Param(MeterLogicFactory.ParamReadingFactor, 0.001); // Wh

            Input(MeterLogicFactory.InputReading, 1_000_000d);
            SetTime(At(10, 8, 15));
            var changes = Input(MeterLogicFactory.InputReading, 1_000_500d);

            Assert.Equal(0.5, ValueOf(changes, C[0]));
            Assert.Equal(1000.5, ValueOf(changes, C[2]));
            Assert.Equal(2, ValueOf(changes, MeterLogicFactory.OutputPower)); // 0.5 kWh in 15 min
        }

        [Fact]
        public async Task Midnight_StartsTheDay_ByTheTimer()
        {
            await StartAt(At(10, 23));
            Input(MeterLogicFactory.InputPower, 1d);

            SetTime(At(11, 0, 30));
            Rule.Tick();

            Assert.Equal(0.5, Visu(C[0]));
            Assert.Equal(1.5, Visu(C[1]));
        }

        [Fact]
        public async Task Restart_KeepsTheCounters()
        {
            await StartAt(At(10, 8));
            Input(MeterLogicFactory.InputReading, 100d);
            SetTime(At(10, 9));
            Input(MeterLogicFactory.InputReading, 104d);

            await Logic.Restart(RuleInstance);
            SetTime(At(10, 10));
            var changes = Input(MeterLogicFactory.InputReading, 105d);

            Assert.Equal(5, ValueOf(changes, C[0]));
            Assert.Equal(105, ValueOf(changes, C[2]));
        }

        [Fact]
        public async Task NewInstanceWithTheSameStore_ContinuesTheTotal()
        {
            await StartAt(At(10, 8));
            Input(MeterLogicFactory.InputPower, 2d);
            SetTime(At(10, 10));
            await Logic.Stop();
            var store = Rule.Store;

            var other = (MeterRuleBase)Instance.CreateLogicInstance(Context);
            other.Store = store;
            await other.Start();

            Assert.Equal(4, other.Register(MeterRule.Energy).Total, 6);
        }

        [Fact]
        public async Task BrokenStore_StartsAtZero()
        {
            await StartAt(At(10, 8));
            Rule.Store = new ThrowingStore();

            await Logic.Restart(RuleInstance);

            Assert.Equal(0, Rule.Register(MeterRule.Energy).Total);
        }

        [Fact]
        public async Task OtherInputsAndNonNumbers_ChangeNothing()
        {
            await StartAt(At(10, 8));

            Assert.Empty(Input(MeterLogicFactory.InputPower, "garbage"));
            Assert.Empty(Input(MeterLogicFactory.InputReading, null));
        }

        [Fact]
        public async Task OnlyChangesAreSent()
        {
            await StartAt(At(10, 8));
            Input(MeterLogicFactory.InputPower, 2d);

            Assert.Empty(Input(MeterLogicFactory.InputPower, 2d));
        }

        private class ThrowingStore : IMeterStateStore
        {
            public MeterState Load(Guid logicInstance) => throw new InvalidOperationException("broken");
            public void Save(Guid logicInstance, MeterState state) => throw new InvalidOperationException("broken");
        }
    }

    public class BidirectionalMeterRuleTests : MeterTest<BidirectionalMeterLogicFactory>
    {
        private static readonly Guid[] I = BidirectionalMeterLogicFactory.OutputImportCounters;
        private static readonly Guid[] E = BidirectionalMeterLogicFactory.OutputExportCounters;

        [Fact]
        public void Template_Interfaces()
        {
            AssertKeys("powerIn", "importPowerIn", "exportPowerIn", "importReadingIn", "exportReadingIn",
                "power", "importPower", "exportPower", "importToday", "importMonth", "importTotal", "exportToday", "exportMonth", "exportTotal", "invert");
        }

        [Fact]
        public async Task SignedPower_SplitsIntoImportAndExport()
        {
            await StartAt(At(10, 8));

            var changes = Input(BidirectionalMeterLogicFactory.InputPower, -3d);

            Assert.Equal(-3, ValueOf(changes, BidirectionalMeterLogicFactory.OutputPower));
            Assert.Equal(0, ValueOf(changes, BidirectionalMeterLogicFactory.OutputImportPower));
            Assert.Equal(3, ValueOf(changes, BidirectionalMeterLogicFactory.OutputExportPower));

            SetTime(At(10, 10));
            Input(BidirectionalMeterLogicFactory.InputPower, 1d);
            SetTime(At(10, 11));
            Rule.Tick();

            Assert.Equal(1, Visu(I[0]));
            Assert.Equal(6, Visu(E[0]));
        }

        [Fact]
        public async Task Invert_FlipsTheSign()
        {
            await StartAt(At(10, 8));
            Param(BidirectionalMeterLogicFactory.ParamInvert, true);

            var changes = Input(BidirectionalMeterLogicFactory.InputPower, -3d);

            Assert.Equal(3, ValueOf(changes, BidirectionalMeterLogicFactory.OutputImportPower));
        }

        [Fact]
        public async Task SeparateImportAndExportPower()
        {
            await StartAt(At(10, 8));

            Input(BidirectionalMeterLogicFactory.InputImportPower, 2d);
            var changes = Input(BidirectionalMeterLogicFactory.InputExportPower, 0.5);

            Assert.Equal(1.5, ValueOf(changes, BidirectionalMeterLogicFactory.OutputPower));
        }

        [Fact]
        public async Task Readings_CountImportAndExport_AndGiveThePower()
        {
            await StartAt(At(10, 8));
            Input(BidirectionalMeterLogicFactory.InputImportReading, 500d);
            Input(BidirectionalMeterLogicFactory.InputExportReading, 200d);

            SetTime(At(10, 9));
            Input(BidirectionalMeterLogicFactory.InputImportReading, 501d);
            var changes = Input(BidirectionalMeterLogicFactory.InputExportReading, 203d);

            Assert.Equal(1, Visu(I[0]));
            Assert.Equal(501, Visu(I[2]));
            Assert.Equal(3, ValueOf(changes, E[0]));
            Assert.Equal(-2, Visu(BidirectionalMeterLogicFactory.OutputPower));
        }
    }

    public class StorageMeterRuleTests : MeterTest<StorageMeterLogicFactory>
    {
        [Fact]
        public void Template_Interfaces()
        {
            AssertKeys("powerIn", "socIn", "chargeReadingIn", "dischargeReadingIn", "power", "chargePower", "dischargePower", "soc", "stored",
                "chargeToday", "chargeMonth", "chargeTotal", "dischargeToday", "dischargeMonth", "dischargeTotal", "capacity", "invert");
        }

        [Fact]
        public async Task ChargeAndDischarge_AreCounted()
        {
            await StartAt(At(10, 8));
            Input(StorageMeterLogicFactory.InputPower, 2d);
            SetTime(At(10, 9));
            var changes = Input(StorageMeterLogicFactory.InputPower, -1d);

            Assert.Equal(2, ValueOf(changes, StorageMeterLogicFactory.OutputChargeCounters[0]));
            Assert.Equal(1, ValueOf(changes, StorageMeterLogicFactory.OutputDischargePower));

            SetTime(At(10, 11));
            Rule.Tick();
            Assert.Equal(2, Visu(StorageMeterLogicFactory.OutputDischargeCounters[0]));
        }

        [Fact]
        public async Task Soc_AndStoredEnergy()
        {
            await StartAt(At(10, 8));
            Param(StorageMeterLogicFactory.ParamCapacity, 10d);

            var changes = Input(StorageMeterLogicFactory.InputSoc, 64d);

            Assert.Equal(64, ValueOf(changes, StorageMeterLogicFactory.OutputSoc));
            Assert.Equal(6.4, ValueOf(changes, StorageMeterLogicFactory.OutputStored));
            Assert.Equal(100, ValueOf(Input(StorageMeterLogicFactory.InputSoc, 140d), StorageMeterLogicFactory.OutputSoc));
        }

        [Fact]
        public async Task WithoutCapacity_NoStoredEnergy()
        {
            await StartAt(At(10, 8));

            Assert.Null(ValueOf(Input(StorageMeterLogicFactory.InputSoc, 50d), StorageMeterLogicFactory.OutputStored));
        }

        [Fact]
        public async Task Restart_KeepsTheChargePowerCounting()
        {
            await StartAt(At(10, 8));
            Input(StorageMeterLogicFactory.InputPower, 2d);
            SetTime(At(10, 9));

            await Logic.Restart(RuleInstance);
            SetTime(At(10, 10));
            Rule.Tick();

            Assert.Equal(4, Visu(StorageMeterLogicFactory.OutputChargeCounters[0]));
        }
    }

    public class PulseMeterRuleTests : MeterTest<PulseMeterLogicFactory>
    {
        [Fact]
        public void Template_Interfaces() => AssertKeys("pulseIn", "power", "today", "month", "total", "pulsesPerKwh");

        [Fact]
        public async Task Pulses_GiveEnergyAndPower()
        {
            await StartAt(At(10, 8));
            Param(PulseMeterLogicFactory.ParamPulsesPerKwh, 1000d);

            Input(PulseMeterLogicFactory.InputPulse, true);
            Input(PulseMeterLogicFactory.InputPulse, false);
            SetTime(At(10, 8).AddSeconds(3.6));
            var changes = Input(PulseMeterLogicFactory.InputPulse, true);

            Assert.Equal(0.002, ValueOf(changes, PulseMeterLogicFactory.OutputCounters[0]));
            Assert.Equal(1, ValueOf(changes, PulseMeterLogicFactory.OutputPower));
        }

        [Fact]
        public async Task PulseCounter_SurvivesARestart()
        {
            await StartAt(At(10, 8));
            Param(PulseMeterLogicFactory.ParamPulsesPerKwh, 100d);
            Input(PulseMeterLogicFactory.InputPulse, 5000d);

            // stop and start: saved and loaded through the store
            await Logic.Stop();
            await Logic.Start();
            var changes = Input(PulseMeterLogicFactory.InputPulse, 5050d);

            Assert.Equal(0.5, ValueOf(changes, PulseMeterLogicFactory.OutputCounters[0]));
        }

        [Fact]
        public async Task NoPulses_PowerFallsToZero()
        {
            await StartAt(At(10, 8));
            Input(PulseMeterLogicFactory.InputPulse, 1d);
            SetTime(At(10, 8, 1));
            Input(PulseMeterLogicFactory.InputPulse, 2d);
            SetTime(At(10, 8, 2));
            Input(PulseMeterLogicFactory.InputPulse, 3d);
            Assert.Equal(0.06, Visu(PulseMeterLogicFactory.OutputPower)); // 1 Wh per minute

            SetTime(At(10, 10));
            Rule.Tick();

            Assert.Equal(0, Visu(PulseMeterLogicFactory.OutputPower));
        }
    }

    public class FixedValueMeterRuleTests : MeterTest<FixedValueMeterLogicFactory>
    {
        [Fact]
        public void Template_Interfaces() => AssertKeys("onIn", "power", "today", "month", "total", "ratedPower");

        [Fact]
        public async Task OnTime_TimesPower()
        {
            await StartAt(At(10, 8));
            Param(FixedValueMeterLogicFactory.ParamPower, 2d);

            Assert.Equal(2, ValueOf(Input(FixedValueMeterLogicFactory.InputOn, true), FixedValueMeterLogicFactory.OutputPower));
            SetTime(At(10, 8, 30));
            var changes = Input(FixedValueMeterLogicFactory.InputOn, false);

            Assert.Equal(0, ValueOf(changes, FixedValueMeterLogicFactory.OutputPower));
            Assert.Equal(1, ValueOf(changes, FixedValueMeterLogicFactory.OutputCounters[0]));

            SetTime(At(10, 12));
            Rule.Tick();
            Assert.Equal(1, Visu(FixedValueMeterLogicFactory.OutputCounters[0]));
        }

        [Fact]
        public async Task PowerChangeWhileOn_CountsFromNowOn()
        {
            await StartAt(At(10, 8));
            Param(FixedValueMeterLogicFactory.ParamPower, 1d);
            Input(FixedValueMeterLogicFactory.InputOn, true);

            SetTime(At(10, 9));
            Param(FixedValueMeterLogicFactory.ParamPower, 3d);
            SetTime(At(10, 10));
            Rule.Tick();

            Assert.Equal(4, Visu(FixedValueMeterLogicFactory.OutputCounters[0]));
        }
    }

    public class UtilityMeterRuleTests : MeterTest<UtilityMeterLogicFactory>
    {
        [Fact]
        public void Template_Interfaces() => AssertKeys("flowIn", "readingIn", "pulseIn", "flow", "today", "month", "total", "unit", "pulsesPerUnit", "readingFactor");

        [Fact]
        public async Task Pulses_PerUnit()
        {
            await StartAt(At(10, 8));
            Param(UtilityMeterLogicFactory.ParamPulsesPerUnit, 100d);

            Input(UtilityMeterLogicFactory.InputPulse, 1000d);
            SetTime(At(10, 9));
            var changes = Input(UtilityMeterLogicFactory.InputPulse, 1025d);

            Assert.Equal(0.25, ValueOf(changes, UtilityMeterLogicFactory.OutputCounters[0]));
        }

        [Fact]
        public async Task Flow_IsIntegrated()
        {
            await StartAt(At(10, 8));
            Assert.Equal(0.6, ValueOf(Input(UtilityMeterLogicFactory.InputFlow, 0.6), UtilityMeterLogicFactory.OutputFlow));

            SetTime(At(10, 10));
            Rule.Tick();

            Assert.Equal(1.2, Visu(UtilityMeterLogicFactory.OutputCounters[0]));
        }

        [Fact]
        public async Task Reading_GivesTotalAndFlow()
        {
            await StartAt(At(10, 8));
            Input(UtilityMeterLogicFactory.InputReading, 812.4);
            SetTime(At(10, 8, 30));
            var changes = Input(UtilityMeterLogicFactory.InputReading, 812.9);

            Assert.Equal(812.9, ValueOf(changes, UtilityMeterLogicFactory.OutputCounters[2]));
            Assert.Equal(1, ValueOf(changes, UtilityMeterLogicFactory.OutputFlow));
        }
    }
}
