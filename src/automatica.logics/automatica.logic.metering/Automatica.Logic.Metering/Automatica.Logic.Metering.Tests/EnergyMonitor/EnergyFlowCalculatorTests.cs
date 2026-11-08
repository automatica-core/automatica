using Automatica.Logic.Metering.EnergyMonitor;
using Xunit;

namespace Automatica.Logic.Metering.Tests.EnergyMonitor
{
    public class EnergyFlowCalculatorTests
    {
        private const double Precision = 9;

        [Fact]
        public void NoInputs_AllZero_AndNoSoc()
        {
            var s = EnergyFlowCalculator.Calculate(new EnergyFlowInputs());

            Assert.Equal(0, s.GridImport);
            Assert.Equal(0, s.GridExport);
            Assert.Equal(0, s.Pv);
            Assert.Equal(0, s.Battery);
            Assert.Equal(0, s.House);
            Assert.Equal(0, s.Consumption);
            Assert.Null(s.Soc);
            Assert.Equal(0, s.SelfConsumption);
            Assert.Equal(0, s.Autarky);
        }

        [Fact]
        public void PositiveGrid_IsImport()
        {
            var s = EnergyFlowCalculator.Calculate(new EnergyFlowInputs { Grid = 2.5 });

            Assert.Equal(2.5, s.GridImport);
            Assert.Equal(0, s.GridExport);
            Assert.Equal(2.5, s.Grid);
            Assert.Equal(2.5, s.House);
            Assert.Equal(0, s.Autarky);
        }

        [Fact]
        public void NegativeGrid_IsExport()
        {
            var s = EnergyFlowCalculator.Calculate(new EnergyFlowInputs { Grid = -3, Pv = 5 });

            Assert.Equal(0, s.GridImport);
            Assert.Equal(3, s.GridExport);
            Assert.Equal(-3, s.Grid);
            Assert.Equal(2, s.House);
            Assert.Equal(40, s.SelfConsumption, Precision);
            Assert.Equal(100, s.Autarky, Precision);
        }

        [Fact]
        public void InvertGrid_FlipsTheSign()
        {
            var s = EnergyFlowCalculator.Calculate(new EnergyFlowInputs { Grid = -1.2, InvertGrid = true });

            Assert.Equal(1.2, s.GridImport, Precision);
            Assert.Equal(0, s.GridExport);
        }

        [Fact]
        public void SeparateImportAndExport_AreUsedWithoutSignedGrid()
        {
            var s = EnergyFlowCalculator.Calculate(new EnergyFlowInputs { GridImport = 1, GridExport = 0.25, Pv = 2 });

            Assert.Equal(1, s.GridImport);
            Assert.Equal(0.25, s.GridExport);
            Assert.Equal(2.75, s.Consumption, Precision);
        }

        [Fact]
        public void SignedGrid_WinsOverSeparateImportAndExport()
        {
            var s = EnergyFlowCalculator.Calculate(new EnergyFlowInputs { Grid = 0.5, GridImport = 9, GridExport = 9 });

            Assert.Equal(0.5, s.GridImport);
            Assert.Equal(0, s.GridExport);
        }

        [Fact]
        public void NegativeSeparateValues_CountAsZero()
        {
            var s = EnergyFlowCalculator.Calculate(new EnergyFlowInputs { GridImport = -1, GridExport = -2 });

            Assert.Equal(0, s.GridImport);
            Assert.Equal(0, s.GridExport);
        }

        [Fact]
        public void NegativePv_StandbyDraw_CountsAsZero()
        {
            var s = EnergyFlowCalculator.Calculate(new EnergyFlowInputs { Pv = -0.004, Grid = 0.3 });

            Assert.Equal(0, s.Pv);
            Assert.Equal(0.3, s.House, Precision);
        }

        [Fact]
        public void BatteryCharging_IsTakenOutOfTheConsumption()
        {
            // 6 kW PV: 2 kW into the battery, 1 kW exported, 3 kW used in the house
            var s = EnergyFlowCalculator.Calculate(new EnergyFlowInputs { Pv = 6, Battery = 2, Grid = -1 });

            Assert.Equal(2, s.BatteryCharge);
            Assert.Equal(0, s.BatteryDischarge);
            Assert.Equal(2, s.Battery);
            Assert.Equal(3, s.House, Precision);
            // charging the battery counts as self-consumption
            Assert.Equal(100d * 5 / 6, s.SelfConsumption, Precision);
        }

        [Fact]
        public void BatteryDischarging_AddsToTheConsumption()
        {
            // night: 1.5 kW from the battery, 0.5 kW from the grid
            var s = EnergyFlowCalculator.Calculate(new EnergyFlowInputs { Battery = -1.5, Grid = 0.5 });

            Assert.Equal(0, s.BatteryCharge);
            Assert.Equal(1.5, s.BatteryDischarge);
            Assert.Equal(2, s.House, Precision);
            Assert.Equal(75, s.Autarky, Precision);
            // no production: self-consumption is 0, not NaN
            Assert.Equal(0, s.SelfConsumption);
        }

        [Fact]
        public void InvertBattery_FlipsTheSign()
        {
            var s = EnergyFlowCalculator.Calculate(new EnergyFlowInputs { Battery = 1, InvertBattery = true, Pv = 0, Grid = 0 });

            Assert.Equal(1, s.BatteryDischarge);
            Assert.Equal(0, s.BatteryCharge);
            Assert.Equal(1, s.House);
        }

        [Fact]
        public void ExtraConsumers_AreTakenOutOfTheHouse()
        {
            var inputs = new EnergyFlowInputs { Pv = 4, Grid = 3 };
            inputs.Consumers[0] = 5; // wallbox
            inputs.Consumers[2] = 1; // heat pump

            var s = EnergyFlowCalculator.Calculate(inputs);

            Assert.Equal(7, s.Consumption, Precision);
            Assert.Equal(6, s.ConsumersTotal, Precision);
            Assert.Equal(1, s.House, Precision);
            Assert.Equal(new[] { 5d, 0d, 1d }, s.Consumers);
        }

        [Fact]
        public void HouseNeverGoesNegative_WhenConsumersExceedTheBalance()
        {
            var inputs = new EnergyFlowInputs { Grid = 1 };
            inputs.Consumers[0] = 1.3;

            var s = EnergyFlowCalculator.Calculate(inputs);

            Assert.Equal(0, s.House);
            Assert.Equal(1, s.Consumption);
        }

        [Fact]
        public void ConsumptionNeverGoesNegative()
        {
            // readings of different moments: more export than production
            var s = EnergyFlowCalculator.Calculate(new EnergyFlowInputs { Pv = 1, Grid = -1.5 });

            Assert.Equal(0, s.Consumption);
            Assert.Equal(0, s.House);
            Assert.Equal(0, s.SelfConsumption);
            Assert.Equal(0, s.Autarky);
        }

        [Theory]
        [InlineData(double.NaN)]
        [InlineData(double.PositiveInfinity)]
        [InlineData(double.NegativeInfinity)]
        public void NonFiniteValues_CountAsZero(double value)
        {
            var inputs = new EnergyFlowInputs { Grid = value, Pv = value, Battery = value, Soc = value };
            inputs.Consumers[1] = value;

            var s = EnergyFlowCalculator.Calculate(inputs);

            Assert.Equal(0, s.Grid);
            Assert.Equal(0, s.Pv);
            Assert.Equal(0, s.Battery);
            Assert.Equal(0, s.Consumers[1]);
            Assert.Equal(0, s.House);
            Assert.Null(s.Soc);
        }

        [Theory]
        [InlineData(-5, 0)]
        [InlineData(55.5, 55.5)]
        [InlineData(120, 100)]
        public void Soc_IsClampedTo0To100(double soc, double expected)
        {
            Assert.Equal(expected, EnergyFlowCalculator.Calculate(new EnergyFlowInputs { Soc = soc }).Soc);
        }

        [Theory]
        [InlineData(0, 0, 0)]
        [InlineData(0, 2, 0)]
        [InlineData(-1, 0, 0)]
        [InlineData(4, 1, 75)]
        [InlineData(4, 0, 100)]
        [InlineData(4, 6, 0)]
        public void SelfConsumptionRate(double production, double export, double expected)
        {
            Assert.Equal(expected, EnergyFlowCalculator.SelfConsumptionRate(production, export), Precision);
        }

        [Theory]
        [InlineData(0, 0, 0)]
        [InlineData(0, 3, 0)]
        [InlineData(4, 1, 75)]
        [InlineData(4, 0, 100)]
        [InlineData(4, 4, 0)]
        [InlineData(4, 9, 0)]
        public void AutarkyRate(double consumption, double import, double expected)
        {
            Assert.Equal(expected, EnergyFlowCalculator.AutarkyRate(consumption, import), Precision);
        }

        [Fact]
        public void Rates_WithNaN_AreZero()
        {
            Assert.Equal(0, EnergyFlowCalculator.SelfConsumptionRate(double.NaN, 0));
            Assert.Equal(0, EnergyFlowCalculator.AutarkyRate(double.NaN, 0));
        }
    }
}
