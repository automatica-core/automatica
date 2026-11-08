using System;

namespace Automatica.Logic.Metering.EnergyMonitor
{
    /// <summary>
    /// The power values the energy monitor received (kW). Null means the input never got a value.
    /// </summary>
    public sealed class EnergyFlowInputs
    {
        public const int ConsumerCount = 3;

        /// <summary>signed grid power: + import (Bezug), - export (Einspeisung)</summary>
        public double? Grid { get; set; }
        /// <summary>grid import as its own value (only used while <see cref="Grid"/> has no value)</summary>
        public double? GridImport { get; set; }
        /// <summary>grid export as its own value (only used while <see cref="Grid"/> has no value)</summary>
        public double? GridExport { get; set; }
        public double? Pv { get; set; }
        /// <summary>signed battery power: + charging, - discharging</summary>
        public double? Battery { get; set; }
        /// <summary>battery state of charge in %</summary>
        public double? Soc { get; set; }
        public double?[] Consumers { get; } = new double?[ConsumerCount];

        /// <summary>the meter sends - for import and + for export</summary>
        public bool InvertGrid { get; set; }
        /// <summary>the battery sends - for charging and + for discharging</summary>
        public bool InvertBattery { get; set; }
    }

    /// <summary>
    /// The power flows of one moment, all values in kW and never negative (except the signed <see cref="Grid"/> and <see cref="Battery"/>).
    /// </summary>
    public sealed class EnergyFlowSnapshot
    {
        public double GridImport { get; init; }
        public double GridExport { get; init; }
        /// <summary>+ import, - export</summary>
        public double Grid => GridImport - GridExport;
        public double Pv { get; init; }
        public double BatteryCharge { get; init; }
        public double BatteryDischarge { get; init; }
        /// <summary>+ charging, - discharging</summary>
        public double Battery => BatteryCharge - BatteryDischarge;
        public double? Soc { get; init; }
        public double[] Consumers { get; init; } = new double[EnergyFlowInputs.ConsumerCount];
        public double ConsumersTotal { get; init; }
        /// <summary>everything used on site: house + extra consumers</summary>
        public double Consumption { get; init; }
        /// <summary>consumption without the extra consumers</summary>
        public double House { get; init; }
        /// <summary>% of the PV power used on site (incl. charging the battery)</summary>
        public double SelfConsumption { get; init; }
        /// <summary>% of the consumption not taken from the grid</summary>
        public double Autarky { get; init; }
    }

    /// <summary>
    /// Pure balance of the energy monitor:
    /// consumption = PV + grid import - grid export + battery discharge - battery charge,
    /// house = consumption - extra consumers.
    /// </summary>
    public static class EnergyFlowCalculator
    {
        public static EnergyFlowSnapshot Calculate(EnergyFlowInputs inputs)
        {
            double gridImport, gridExport;
            if (inputs.Grid.HasValue)
            {
                var grid = Finite(inputs.Grid.Value) * (inputs.InvertGrid ? -1 : 1);
                gridImport = Math.Max(grid, 0);
                gridExport = Math.Max(-grid, 0);
            }
            else
            {
                gridImport = Positive(inputs.GridImport);
                gridExport = Positive(inputs.GridExport);
            }

            // inverters report a few watts of standby draw at night as negative production
            var pv = Positive(inputs.Pv);

            var battery = Finite(inputs.Battery ?? 0) * (inputs.InvertBattery ? -1 : 1);
            var charge = Math.Max(battery, 0);
            var discharge = Math.Max(-battery, 0);

            var consumers = new double[EnergyFlowInputs.ConsumerCount];
            var consumersTotal = 0d;
            for (var i = 0; i < consumers.Length; i++)
            {
                consumers[i] = Positive(inputs.Consumers[i]);
                consumersTotal += consumers[i];
            }

            // meters are never read at exactly the same moment, the balance can dip below 0 for a moment
            var consumption = Math.Max(pv + gridImport - gridExport + discharge - charge, 0);
            var house = Math.Max(consumption - consumersTotal, 0);

            double? soc = inputs.Soc.HasValue && double.IsFinite(inputs.Soc.Value)
                ? Math.Clamp(inputs.Soc.Value, 0, 100)
                : null;

            return new EnergyFlowSnapshot
            {
                GridImport = gridImport,
                GridExport = gridExport,
                Pv = pv,
                BatteryCharge = charge,
                BatteryDischarge = discharge,
                Soc = soc,
                Consumers = consumers,
                ConsumersTotal = consumersTotal,
                Consumption = consumption,
                House = house,
                SelfConsumption = SelfConsumptionRate(pv, gridExport),
                Autarky = AutarkyRate(consumption, gridImport)
            };
        }

        /// <summary>
        /// % of the production used on site: (production - export) / production.
        /// Without production there is nothing to use, so the rate is 0.
        /// </summary>
        public static double SelfConsumptionRate(double production, double export)
        {
            if (!(production > 0))
            {
                return 0;
            }
            return Math.Clamp((production - export) / production * 100, 0, 100);
        }

        /// <summary>
        /// % of the consumption not covered by the grid: (consumption - import) / consumption.
        /// Without consumption the rate is 0.
        /// </summary>
        public static double AutarkyRate(double consumption, double import)
        {
            if (!(consumption > 0))
            {
                return 0;
            }
            return Math.Clamp((consumption - import) / consumption * 100, 0, 100);
        }

        private static double Finite(double value) => double.IsFinite(value) ? value : 0;

        private static double Positive(double? value) => value.HasValue ? Math.Max(Finite(value.Value), 0) : 0;
    }
}
