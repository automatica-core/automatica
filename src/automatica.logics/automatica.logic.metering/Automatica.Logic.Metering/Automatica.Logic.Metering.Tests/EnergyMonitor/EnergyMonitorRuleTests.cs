using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using Automatica.Core.UnitTests.Base.Logics;
using Automatica.Logic.Metering.EnergyMonitor;
using Xunit;
using F = Automatica.Logic.Metering.EnergyMonitor.EnergyMonitorLogicFactory;

namespace Automatica.Logic.Metering.Tests.EnergyMonitor
{
    public class EnergyMonitorRuleTests : LogicTest<EnergyMonitorLogicFactory>
    {
        private EnergyMonitorRule Rule => (EnergyMonitorRule)Logic;

        /// <summary>restarts the logic at the given Vienna time, so the day counter starts there</summary>
        private async Task StartAt(DateTimeOffset time)
        {
            FakeTimeProvider.SetTimeZone(EnergyDayCounterTests.Vienna);
            FakeTimeProvider.SetDateTime(time.UtcDateTime);
            await Logic.Restart(RuleInstance);
        }

        private void SetTime(DateTimeOffset time) => FakeTimeProvider.SetDateTime(time.UtcDateTime);

        private IList<ILogicOutputChanged> Input(Guid template, object value)
        {
            return Logic.ValueChanged(GetLogicInterfaceByTemplate(template), Dispatchable, value);
        }

        private static double? ValueOf(IEnumerable<ILogicOutputChanged> changes, Guid template)
        {
            var change = changes.SingleOrDefault(a => a.Instance.RuleInterfaceInstance.This2RuleInterfaceTemplate == template);
            return change == null ? null : Convert.ToDouble(change.Value);
        }

        [Fact]
        public void Template_HasAllInterfaces()
        {
            Assert.Equal(14, RuleInstance.RuleInterfaceInstance.Count(a => a.This2RuleInterfaceTemplateNavigation.This2RuleInterfaceDirection == (long)LogicInterfaceDirection.Input));
            Assert.Equal(19, RuleInstance.RuleInterfaceInstance.Count(a => a.This2RuleInterfaceTemplateNavigation.This2RuleInterfaceDirection == (long)LogicInterfaceDirection.Output));
            Assert.Equal(6, RuleInstance.RuleInterfaceInstance.Count(a => a.This2RuleInterfaceTemplateNavigation.This2RuleInterfaceDirection == (long)LogicInterfaceDirection.Param));

            // the visu tile finds its values by these keys
            var keys = RuleInstance.RuleInterfaceInstance.Select(a => a.This2RuleInterfaceTemplateNavigation.Key).ToList();
            Assert.Contains("house", keys);
            Assert.Contains("grid", keys);
            Assert.Contains("autarkyToday", keys);
            Assert.Contains("consumer3Name", keys);
            Assert.Equal(keys.Count, keys.Distinct().Count());
        }

        [Fact]
        public void HouseOutput_IsTheStatusInterface()
        {
            var house = GetLogicInterfaceByTemplate(F.OutputHouse);

            Assert.Equal(RuleInterfaceType.Status, house.This2RuleInterfaceTemplateNavigation.InterfaceType);
        }

        [Fact]
        public void EnergyFlowVisu_IsUsedWhenTheCoreKnowsIt()
        {
            var known = Enum.TryParse<VisuMobileObjectTemplateTypes>(F.EnergyFlowVisuTemplate, out _);

            Assert.Equal(known, F.UseEnergyFlowVisu(new LogicTemplateFactoryMock(), Instance.LogicGuid));
        }

        [Fact]
        public void PvAndExport_GiveHouseAndRates()
        {
            Input(F.InputPv, 5d);
            var changes = Input(F.InputGrid, -2d);

            Assert.Equal(-2, ValueOf(changes, F.OutputGrid));
            Assert.Equal(3, ValueOf(changes, F.OutputHouse));
            Assert.Equal(3, ValueOf(changes, F.OutputConsumption));
            Assert.Equal(60, ValueOf(changes, F.OutputSelfConsumption));
            // autarky was already 100 % with PV alone, so it is not sent again
            Assert.Null(ValueOf(changes, F.OutputAutarky));
            Assert.Equal(100, Rule.Snapshot.Autarky);
        }

        [Fact]
        public void OnlyChangedOutputs_AreSent()
        {
            Input(F.InputGrid, 1d);

            var changes = Input(F.InputGrid, 1d);
            Assert.DoesNotContain(changes, a => a.Instance.RuleInterfaceInstance.This2RuleInterfaceTemplate == F.OutputGrid);

            changes = Input(F.InputPv, 1d);
            Assert.Equal(1, ValueOf(changes, F.OutputPv));
            Assert.Equal(2, ValueOf(changes, F.OutputHouse));
            Assert.Null(ValueOf(changes, F.OutputGrid));
        }

        [Fact]
        public void Soc_IsSentOnlyOnceItHasAValue()
        {
            var changes = Input(F.InputGrid, 1d);
            Assert.Null(ValueOf(changes, F.OutputSoc));

            changes = Input(F.InputSoc, 64.44);
            Assert.Equal(64.4, ValueOf(changes, F.OutputSoc));
        }

        [Fact]
        public void Consumers_AreSentAndTakenOutOfTheHouse()
        {
            Input(F.InputGrid, 4d);
            var changes = Input(F.InputConsumers[1], 3d);

            Assert.Equal(3, ValueOf(changes, F.OutputConsumers[1]));
            Assert.Equal(1, ValueOf(changes, F.OutputHouse));
            Assert.Equal(4, Rule.Snapshot.Consumption);
        }

        [Fact]
        public void SeparateImportExport_Inputs()
        {
            Input(F.InputGridImport, 0.7d);
            var changes = Input(F.InputGridExport, 0.2d);

            Assert.Equal(0.5, ValueOf(changes, F.OutputGrid));
        }

        [Theory]
        [InlineData("2.5", 2.5)]
        [InlineData(" -1.25 ", -1.25)]
        [InlineData(3, 3.0)]
        [InlineData(true, 1.0)]
        public void InputValues_AreParsed(object value, double expected)
        {
            var changes = Input(F.InputGrid, value);

            Assert.Equal(expected, ValueOf(changes, F.OutputGrid));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("abc")]
        [InlineData(double.NaN)]
        public void InvalidOrMissingValue_ClearsTheInput(object value)
        {
            Input(F.InputPv, 2d);

            var changes = Input(F.InputPv, value);

            Assert.Equal(0, ValueOf(changes, F.OutputPv));
            Assert.Null(Rule.Snapshot.Soc);
        }

        [Fact]
        public void UnknownInterface_ProducesNothing()
        {
            var changes = Input(F.OutputHouse, 5d);

            Assert.Empty(changes);
        }

        [Fact]
        public void InvertParameters_FlipTheSigns()
        {
            Logic.ValueChanged(GetLogicInterfaceByTemplate(F.ParamInvertGrid), Dispatchable, true);
            Logic.ValueChanged(GetLogicInterfaceByTemplate(F.ParamInvertBattery), Dispatchable, "true");

            Input(F.InputBattery, 1d); // inverted: discharging 1 kW
            var changes = Input(F.InputGrid, 0.5d); // inverted: export 0.5 kW

            Assert.Equal(-0.5, ValueOf(changes, F.OutputGrid));
            Assert.Equal(-1, Rule.Snapshot.Battery);
            Assert.Equal(0.5, ValueOf(changes, F.OutputHouse));
        }

        [Fact]
        public async Task Tick_BooksTheHeldPower_AndDispatchesTodaysEnergy()
        {
            await StartAt(EnergyDayCounterTests.At(5, 10));
            await Context.Dispatcher.ClearValues();
            Input(F.InputPv, 4d);
            Input(F.InputGrid, -1d);

            SetTime(EnergyDayCounterTests.At(5, 11, 30));
            Rule.Tick();

            var values = Context.Dispatcher.GetValues(DispatchableType.RuleInstance);
            Assert.Equal(6d, Convert.ToDouble(values[GetLogicInterfaceByTemplate(F.OutputPvToday).ObjId].Value), 3);
            Assert.Equal(1.5d, Convert.ToDouble(values[GetLogicInterfaceByTemplate(F.OutputExportToday).ObjId].Value), 3);
            Assert.Equal(4.5d, Convert.ToDouble(values[GetLogicInterfaceByTemplate(F.OutputConsumptionToday).ObjId].Value), 3);
            Assert.Equal(75d, Convert.ToDouble(values[GetLogicInterfaceByTemplate(F.OutputSelfConsumptionToday).ObjId].Value), 1);
        }

        [Fact]
        public async Task Tick_AtMidnight_ResetsTodaysEnergy()
        {
            await StartAt(EnergyDayCounterTests.At(5, 23));
            Input(F.InputGrid, 2d);

            SetTime(EnergyDayCounterTests.At(5, 23, 45));
            Rule.Tick();
            Assert.Equal(1.5, Rule.Counter.Today(EnergyFlowKind.GridImport), 6);

            await Context.Dispatcher.ClearValues();
            SetTime(EnergyDayCounterTests.At(6, 0, 0));
            Rule.Tick();

            var values = Context.Dispatcher.GetValues(DispatchableType.RuleInstance);
            Assert.Equal(0d, Convert.ToDouble(values[GetLogicInterfaceByTemplate(F.OutputImportToday).ObjId].Value));
            Assert.Equal(new DateTime(2026, 1, 6), Rule.Counter.Day);

            SetTime(EnergyDayCounterTests.At(6, 0, 30));
            Rule.Tick();
            Assert.Equal(1, Rule.Counter.Today(EnergyFlowKind.GridImport), 6);
        }

        [Fact]
        public async Task MeterInputs_DriveTodaysEnergy()
        {
            await StartAt(EnergyDayCounterTests.At(5, 6));
            Input(F.InputImportMeter, 1000d);

            SetTime(EnergyDayCounterTests.At(5, 9));
            var changes = Input(F.InputImportMeter, "1002.25");

            Assert.Equal(2.25, ValueOf(changes, F.OutputImportToday));
        }

        [Fact]
        public async Task Tick_WithoutChanges_DispatchesNothing()
        {
            await StartAt(EnergyDayCounterTests.At(5, 12));
            Input(F.InputGrid, 0d);
            Rule.Tick();
            await Context.Dispatcher.ClearValues();

            SetTime(EnergyDayCounterTests.At(5, 13));
            Rule.Tick();

            Assert.Empty(Context.Dispatcher.GetValues(DispatchableType.RuleInstance));
        }

        [Fact]
        public void GetDataForVisu_HasAllOutputsAndPresence()
        {
            Input(F.InputGrid, 1d);
            Input(F.InputConsumers[2], 0.4d);

            var data = Assert.IsType<Dictionary<string, object>>(Logic.GetDataForVisu());

            Assert.Equal(1d, data[GetLogicInterfaceByTemplate(F.OutputGrid).ObjId.ToString()]);
            Assert.Equal(0.6d, data[GetLogicInterfaceByTemplate(F.OutputHouse).ObjId.ToString()]);
            Assert.True(data.ContainsKey(GetLogicInterfaceByTemplate(F.OutputAutarkyToday).ObjId.ToString()));
            Assert.False(data.ContainsKey(GetLogicInterfaceByTemplate(F.OutputSoc).ObjId.ToString()));
            Assert.Equal(false, data["hasBattery"]);
            Assert.Equal(false, data["hasConsumer1"]);
            Assert.Equal(true, data["hasConsumer3"]);
            // parameters by key, e.g. the consumer names and the peak power for the tile
            Assert.Equal(10d, Convert.ToDouble(data["peakPower"]));
            Assert.True(data.ContainsKey("consumer1Name"));
        }

        [Fact]
        public void GetDataForVisu_BatteryIsPresentWithSocOnly()
        {
            Input(F.InputSoc, 80d);

            var data = Assert.IsType<Dictionary<string, object>>(Logic.GetDataForVisu());

            Assert.Equal(true, data["hasBattery"]);
        }

        [Fact]
        public async Task StopAndStart_Work()
        {
            Assert.True(await Logic.Stop());
            Rule.Tick(); // a late timer event after stop must not throw
            Assert.True(await Logic.Start());
        }

        [Fact]
        public void CreateLogicInstance_ReturnsTheRule()
        {
            Assert.IsType<EnergyMonitorRule>(Instance.CreateLogicInstance(Context));
        }
    }
}
