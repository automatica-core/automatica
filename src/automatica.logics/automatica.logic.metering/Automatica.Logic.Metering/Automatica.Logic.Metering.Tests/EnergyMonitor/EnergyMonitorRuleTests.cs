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
            Assert.Equal(17 + 18, RuleInstance.RuleInterfaceInstance.Count(a => a.This2RuleInterfaceTemplateNavigation.This2RuleInterfaceDirection == (long)LogicInterfaceDirection.Input));
            Assert.Equal(22 + 7, RuleInstance.RuleInterfaceInstance.Count(a => a.This2RuleInterfaceTemplateNavigation.This2RuleInterfaceDirection == (long)LogicInterfaceDirection.Output));
            Assert.Equal(9 + 36, RuleInstance.RuleInterfaceInstance.Count(a => a.This2RuleInterfaceTemplateNavigation.This2RuleInterfaceDirection == (long)LogicInterfaceDirection.Param));

            // the visu tile finds its values by these keys
            var keys = RuleInstance.RuleInterfaceInstance.Select(a => a.This2RuleInterfaceTemplateNavigation.Key).ToList();
            Assert.Contains("house", keys);
            Assert.Contains("grid", keys);
            Assert.Contains("autarkyToday", keys);
            Assert.Contains("consumer6Name", keys);
            Assert.Contains("consumer6", keys);
            Assert.Contains("consumer6In", keys);
            Assert.Contains("circuit18In", keys);
            Assert.Contains("circuit18Name", keys);
            Assert.Contains("circuit18Group", keys);
            Assert.Contains("houseRest", keys);
            Assert.Contains("consumer6Rest", keys);
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
        public void AllSixConsumers_AreTakenOutOfTheHouse()
        {
            Input(F.InputGrid, 10d);
            for (var i = 0; i < F.InputConsumers.Length; i++)
            {
                Input(F.InputConsumers[i], (i + 1) * 0.5);
            }

            // 0.5 + 1 + 1.5 + 2 + 2.5 + 3 = 10.5 > 10: the house stays at 0
            Assert.Equal(0, Rule.Snapshot.House);
            Assert.Equal(3, Rule.Snapshot.Consumers[5]);

            var changes = Input(F.InputConsumers[5], 2d);
            Assert.Equal(2, ValueOf(changes, F.OutputConsumers[5]));
            Assert.Equal(0.5, ValueOf(changes, F.OutputHouse));
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
            Assert.Equal(false, data["hasConsumer6"]);
            // parameters by key, e.g. the consumer names and the peak power for the tile
            Assert.Equal(10d, Convert.ToDouble(data["peakPower"]));
            Assert.True(data.ContainsKey("consumer1Name"));
            Assert.True(data.ContainsKey("consumer6Name"));
        }

        [Fact]
        public void GetDataForVisu_Consumer6IsPresent()
        {
            Input(F.InputConsumers[5], 1.2d);

            var data = Assert.IsType<Dictionary<string, object>>(Logic.GetDataForVisu());

            Assert.Equal(true, data["hasConsumer6"]);
            Assert.Equal(1.2d, data[GetLogicInterfaceByTemplate(F.OutputConsumers[5]).ObjId.ToString()]);
        }

        [Fact]
        public void GetDataForVisu_BatteryIsPresentWithSocOnly()
        {
            Input(F.InputSoc, 80d);

            var data = Assert.IsType<Dictionary<string, object>>(Logic.GetDataForVisu());

            Assert.Equal(true, data["hasBattery"]);
        }

        private void Param(Guid template, object value) => Logic.ValueChanged(GetLogicInterfaceByTemplate(template), Dispatchable, value);

        [Fact]
        public void HouseCircuits_LeaveTheRest()
        {
            Input(F.InputGrid, 3d);
            Input(F.InputCircuits[0], 1d);
            var changes = Input(F.InputCircuits[1], 0.5d);

            Assert.Equal(1.5, ValueOf(changes, F.OutputRests[0]));
            // the house itself stays as it was, the circuits are only its details
            Assert.Equal(3, Rule.Snapshot.House);
            Assert.Null(ValueOf(changes, F.OutputRests[1]));
        }

        [Fact]
        public void WithoutCircuits_NoRestIsSent()
        {
            var changes = Input(F.InputGrid, 3d);

            Assert.All(F.OutputRests, rest => Assert.Null(ValueOf(changes, rest)));
            Assert.Empty(Rule.Breakdown.Groups);
        }

        [Fact]
        public void ConsumerCircuits_SplitTheConsumer_NotTheHouse()
        {
            Input(F.InputGrid, 5d);
            Input(F.InputConsumers[1], 3d);
            Param(F.ParamCircuitGroups[2], 2);
            var changes = Input(F.InputCircuits[2], 2.6d);

            Assert.Equal(0.4, ValueOf(changes, F.OutputRests[2]));
            Assert.Null(ValueOf(changes, F.OutputRests[0]));
            Assert.Equal(2, Rule.Snapshot.House);
        }

        [Fact]
        public void MovingACircuit_ChangesBothRests()
        {
            Input(F.InputGrid, 5d);
            Input(F.InputConsumers[0], 2d);
            Input(F.InputCircuits[0], 1d);
            Assert.Equal(2, Rule.Breakdown.Rest(0)); // house 3 - 1

            Param(F.ParamCircuitGroups[0], "1");

            Assert.Null(Rule.Breakdown.Rest(0));
            Assert.Equal(1, Rule.Breakdown.Rest(1));
        }

        [Fact]
        public void CircuitsAboveTheTotal_LeaveNoNegativeRest()
        {
            Input(F.InputGrid, 1d);
            Input(F.InputCircuits[0], 0.8d);
            var changes = Input(F.InputCircuits[1], 0.5d);

            Assert.Equal(0, ValueOf(changes, F.OutputRests[0]));
            Assert.Equal(1.3, Rule.Breakdown.Groups[0].Measured, 6);
        }

        [Theory]
        [InlineData(7)]
        [InlineData(-1)]
        [InlineData("x")]
        [InlineData(null)]
        [InlineData(100)]
        [InlineData(119)]
        [InlineData("105")] // the circuit itself
        public void UnknownGroup_CountsAsHouse(object group)
        {
            Param(F.ParamCircuitGroups[4], group);
            Input(F.InputCircuits[4], 0.2d);

            Assert.Equal(new[] { 0 }, Rule.Breakdown.Groups.Keys);
            Assert.Equal(new[] { 4 }, Rule.Breakdown.Groups[0].Circuits);
        }

        [Fact]
        public void CircuitBelowACircuit_SplitsThatCircuit_NotTheHouse()
        {
            Input(F.InputGrid, 3d);
            Input(F.InputCircuits[0], 1d);              // kitchen
            Param(F.ParamCircuitGroups[1], "101");      // oven below the kitchen
            var changes = Input(F.InputCircuits[1], 0.6d);

            // the house only counts the kitchen, the oven is part of it: its rest stays 3 - 1 and is not sent again
            Assert.Null(ValueOf(changes, F.OutputRests[0]));
            Assert.Equal(2, Rule.Breakdown.Rest(0)!.Value, 6);
            Assert.Equal(new[] { 0 }, Rule.Breakdown.Groups[0].Circuits);
            var kitchen = Rule.Breakdown.Circuits[0];
            Assert.Equal(new[] { 1 }, kitchen.Children);
            Assert.Equal(0.6, kitchen.Measured, 6);
            Assert.Equal(0.4, Rule.Breakdown.CircuitRest(0)!.Value, 6);
            Assert.Null(Rule.Breakdown.CircuitRest(1));
            Assert.Equal(0, Rule.Breakdown.Circuits[1].Group);
            Assert.Equal(0, Rule.Breakdown.Circuits[1].ParentCircuit);
        }

        [Fact]
        public void MovingACircuitBelowAnother_ChangesTheRests()
        {
            Input(F.InputGrid, 3d);
            Input(F.InputCircuits[0], 1d);
            Input(F.InputCircuits[1], 0.5d);
            Assert.Equal(1.5, Rule.Breakdown.Rest(0)!.Value, 6);
            Assert.Null(Rule.Breakdown.CircuitRest(0));

            Param(F.ParamCircuitGroups[1], 101);

            Assert.Equal(2, Rule.Breakdown.Rest(0)!.Value, 6);
            Assert.Equal(0.5, Rule.Breakdown.CircuitRest(0)!.Value, 6);
        }

        [Fact]
        public void ConsumerCircuits_CanHaveCircuitsToo()
        {
            Input(F.InputGrid, 5d);
            Input(F.InputConsumers[0], 3d);
            Param(F.ParamCircuitGroups[0], 1);
            Input(F.InputCircuits[0], 2.5d);
            Param(F.ParamCircuitGroups[1], 101);
            Input(F.InputCircuits[1], 2d);

            Assert.Equal(0.5, Rule.Breakdown.Rest(1)!.Value, 6);
            Assert.Equal(0.5, Rule.Breakdown.CircuitRest(0)!.Value, 6);
            Assert.Equal(1, Rule.Breakdown.Circuits[1].Group);
            Assert.False(Rule.Breakdown.Groups.ContainsKey(0));
        }

        [Fact]
        public void ThreeLevels_EachLevelHasItsRest()
        {
            Input(F.InputGrid, 2d);
            Input(F.InputCircuits[0], 1.5d);
            Param(F.ParamCircuitGroups[1], 101);
            Input(F.InputCircuits[1], 1d);
            Param(F.ParamCircuitGroups[2], 102);
            Input(F.InputCircuits[2], 0.3d);

            Assert.Equal(0.5, Rule.Breakdown.Rest(0)!.Value, 6);
            Assert.Equal(0.5, Rule.Breakdown.CircuitRest(0)!.Value, 6);
            Assert.Equal(0.7, Rule.Breakdown.CircuitRest(1)!.Value, 6);
            Assert.Null(Rule.Breakdown.CircuitRest(2));
            Assert.Equal(0, Rule.Breakdown.Circuits[2].Group);
        }

        [Fact]
        public void UnmeasuredCircuit_CountsWhatItsCircuitsMeasure()
        {
            Input(F.InputGrid, 3d);
            Param(F.ParamCircuitNames[0], "Kitchen"); // no meter of its own
            Param(F.ParamCircuitGroups[1], 101);
            Input(F.InputCircuits[1], 0.6d);

            Assert.Equal(2.4, Rule.Breakdown.Rest(0)!.Value, 6);
            Assert.Equal(0.6, Rule.Breakdown.Circuits[0].Known!.Value, 6);
            // without its own value nothing is known about its rest
            Assert.Null(Rule.Breakdown.CircuitRest(0));
        }

        [Fact]
        public void UnusedParentCircuit_IsListedForItsCircuits()
        {
            Input(F.InputGrid, 1d);
            Param(F.ParamCircuitGroups[4], 104);
            Input(F.InputCircuits[4], 0.2d);

            Assert.Equal(new[] { 3 }, Rule.Breakdown.Groups[0].Circuits);
            Assert.Equal(new[] { 4 }, Rule.Breakdown.Circuits[3].Children);
        }

        [Fact]
        public void CircuitsBelowEachOther_GoBelowTheHouse()
        {
            Input(F.InputGrid, 3d);
            Param(F.ParamCircuitGroups[0], 102);
            Param(F.ParamCircuitGroups[1], 101);
            Param(F.ParamCircuitGroups[2], 101);
            Input(F.InputCircuits[0], 1d);
            Input(F.InputCircuits[1], 0.5d);
            Input(F.InputCircuits[2], 0.2d);

            Assert.Equal(new[] { 0, 1 }, Rule.Breakdown.Groups[0].Circuits);
            Assert.Equal(new[] { 2 }, Rule.Breakdown.Circuits[0].Children);
            Assert.Equal(1.5, Rule.Breakdown.Rest(0)!.Value, 6);
        }

        [Fact]
        public async Task CircuitTree_IsInTheVisuData_WithTodaysRest()
        {
            await StartAt(EnergyDayCounterTests.At(5, 10));
            Param(F.ParamCircuitNames[0], "Kitchen");
            Param(F.ParamCircuitNames[1], "Oven");
            Param(F.ParamCircuitGroups[1], "101");
            Input(F.InputGrid, 3d);
            Input(F.InputCircuits[0], 1d);
            Input(F.InputCircuits[1], 0.6d);

            SetTime(EnergyDayCounterTests.At(5, 12));
            var data = Assert.IsType<Dictionary<string, object>>(Logic.GetDataForVisu());

            var house = Assert.Single(Assert.IsType<List<Dictionary<string, object>>>(data["circuitGroups"]));
            Assert.Equal(1d, house["measured"]);
            Assert.Equal(2d, house["rest"]);
            var kitchen = Assert.Single((List<Dictionary<string, object>>)house["circuits"]);
            Assert.Equal("Kitchen", kitchen["name"]);
            Assert.Equal(0.6d, kitchen["measured"]);
            Assert.Equal(0.4d, kitchen["rest"]);
            Assert.Equal(0.8d, kitchen["restToday"]);
            var oven = Assert.Single((List<Dictionary<string, object>>)kitchen["circuits"]);
            Assert.Equal("Oven", oven["name"]);
            Assert.Equal(2, oven["circuit"]);
            Assert.Equal(1.2d, oven["today"]);
            Assert.Null(oven["rest"]);
            Assert.Null(oven["restToday"]);
            Assert.Empty((List<Dictionary<string, object>>)oven["circuits"]);
        }

        [Fact]
        public void NamedCircuit_WithoutValue_IsListed()
        {
            Param(F.ParamCircuitNames[5], " Kitchen ");

            var group = Assert.Single(Rule.CircuitGroupsForVisu());
            var circuit = Assert.Single((List<Dictionary<string, object>>)group["circuits"]);
            Assert.Equal(6, circuit["circuit"]);
            Assert.Equal("Kitchen", circuit["name"]);
            Assert.Equal(false, circuit["hasValue"]);
        }

        [Fact]
        public async Task Circuits_CountTodaysEnergy_AndShowInTheVisuData()
        {
            await StartAt(EnergyDayCounterTests.At(5, 10));
            Param(F.ParamCircuitNames[0], "Heat pump");
            Param(F.ParamCircuitGroups[0], 1);
            Input(F.InputGrid, 4d);
            Input(F.InputConsumers[0], 3d);
            Input(F.InputCircuits[0], 2.5d);
            Input(F.InputCircuits[1], 0.5d); // house, no name

            SetTime(EnergyDayCounterTests.At(5, 12));
            var data = Assert.IsType<Dictionary<string, object>>(Logic.GetDataForVisu());

            var groups = Assert.IsType<List<Dictionary<string, object>>>(data["circuitGroups"]);
            Assert.Equal(new object[] { 0, 1 }, groups.Select(g => g["group"]));

            var house = groups[0];
            Assert.Equal(1d, house["total"]);
            Assert.Equal(0.5d, house["rest"]);
            Assert.Equal(1d, house["restToday"]);
            var houseCircuit = Assert.Single((List<Dictionary<string, object>>)house["circuits"]);
            Assert.Null(houseCircuit["name"]);
            Assert.Equal(1d, houseCircuit["today"]);

            var heatPump = groups[1];
            Assert.Equal(3d, heatPump["total"]);
            Assert.Equal(2.5d, heatPump["measured"]);
            Assert.Equal(0.5d, heatPump["rest"]);
            Assert.Equal(1d, heatPump["restToday"]);
            var compressor = Assert.Single((List<Dictionary<string, object>>)heatPump["circuits"]);
            Assert.Equal("Heat pump", compressor["name"]);
            Assert.Equal(2.5d, compressor["power"]);
            Assert.Equal(5d, compressor["today"]);
        }

        [Fact]
        public async Task CircuitEnergy_StartsAgainAtMidnight()
        {
            await StartAt(EnergyDayCounterTests.At(5, 23));
            Input(F.InputGrid, 2d);
            Input(F.InputCircuits[0], 2d);

            SetTime(EnergyDayCounterTests.At(5, 23, 30));
            Rule.Tick();
            Assert.Equal(1, Rule.Counter.TodayExtra(0), 6);

            SetTime(EnergyDayCounterTests.At(6, 0, 30));
            Rule.Tick();
            Assert.Equal(1, Rule.Counter.TodayExtra(0), 6);
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
