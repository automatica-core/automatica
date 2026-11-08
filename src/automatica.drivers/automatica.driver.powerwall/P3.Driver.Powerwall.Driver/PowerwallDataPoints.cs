using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using NodeDataType = Automatica.Core.Base.Templates.NodeDataType;

namespace P3.Driver.Powerwall.Driver
{
    /// <summary>
    /// The responses of one poll
    /// </summary>
    internal sealed class PowerwallSnapshot
    {
        public JsonElement? Aggregates { get; init; }
        public JsonElement? Soe { get; init; }
        public JsonElement? GridStatus { get; init; }
        public JsonElement? Operation { get; init; }
    }

    internal sealed record PowerwallDataPoint(Guid Template, string Key, string I18N, NodeDataType Type, Func<PowerwallSnapshot, object> Read, bool IsWriteable = false);

    /// <summary>
    /// Powers in kW with the signs of the energy monitor and the energy manager: grid + import / - export, battery + charging / - discharging.
    /// Energies in kWh (lifetime counters of the gateway). SoC and reserve on the scale of the Tesla app.
    /// </summary>
    internal static class PowerwallDataPoints
    {
        public const string ConnectedKey = "connected";
        public const string ModeKey = "operation_mode";
        public const string ReserveKey = "backup_reserve";

        public static readonly IReadOnlyList<string> Modes = new[] { "self_consumption", "autonomous", "backup" };

        public static readonly IReadOnlyList<PowerwallDataPoint> All = new List<PowerwallDataPoint>
        {
            new(new Guid("468fa68c-19a6-4dc8-94a6-87836778bc2f"), ConnectedKey, "CONNECTED", NodeDataType.Boolean, null),
            new(new Guid("70a033f2-e829-4bbc-a396-9c5a764faa05"), "grid_power", "GRID_POWER", NodeDataType.Double, s => Kw(Meter(s, "site", "instant_power"))),
            new(new Guid("481d80fe-f256-4b00-987e-82eea6f711ab"), "solar_power", "SOLAR_POWER", NodeDataType.Double, s => Kw(Meter(s, "solar", "instant_power"))),
            // the gateway reports + discharging
            new(new Guid("57510ab5-d248-44e0-b6f5-f1051c393ea7"), "battery_power", "BATTERY_POWER", NodeDataType.Double, s => Kw(Negate(Meter(s, "battery", "instant_power")))),
            new(new Guid("1c2650f1-895d-4c12-a763-d6f359e146f8"), "load_power", "LOAD_POWER", NodeDataType.Double, s => Kw(Meter(s, "load", "instant_power"))),
            new(new Guid("6f811054-ae00-48bb-a630-3ff9710f6616"), "soc", "SOC", NodeDataType.Double, s => Round(AppPercent(Number(s.Soe, "percentage")), 1)),
            new(new Guid("d4bf8a9d-df3a-40ba-aff7-46b590513bc0"), "grid_status", "GRID_STATUS", NodeDataType.String, s => Text(s.GridStatus, "grid_status")),
            new(new Guid("64b241a4-89f1-430e-976c-59ef04104b4f"), "grid_connected", "GRID_CONNECTED", NodeDataType.Boolean,
                s => Text(s.GridStatus, "grid_status") is { } g ? g == "SystemGridConnected" : null),
            new(new Guid("6e8f82b4-5f38-4067-ac8b-212d8a9477ad"), ModeKey, "OPERATION_MODE", NodeDataType.String, s => Text(s.Operation, "real_mode"), true),
            new(new Guid("b1a6612c-6d84-467d-808b-f31f0719631f"), ReserveKey, "BACKUP_RESERVE", NodeDataType.Double, s => Round(AppPercent(Number(s.Operation, "backup_reserve_percent")), 1), true),
            new(new Guid("b587260b-7c29-48f1-b91f-2e092e3cccc1"), "grid_import_energy", "GRID_IMPORT_ENERGY", NodeDataType.Double, s => Kwh(Meter(s, "site", "energy_imported"))),
            new(new Guid("d1c1da32-d149-4be7-9b06-e6366b507dc2"), "grid_export_energy", "GRID_EXPORT_ENERGY", NodeDataType.Double, s => Kwh(Meter(s, "site", "energy_exported"))),
            new(new Guid("23c38520-52ca-4455-8b2b-6a9701831a05"), "solar_energy", "SOLAR_ENERGY", NodeDataType.Double, s => Kwh(Meter(s, "solar", "energy_exported"))),
            new(new Guid("19f84bf4-d35a-463d-99dc-ed1d68e4f4b8"), "battery_charge_energy", "BATTERY_CHARGE_ENERGY", NodeDataType.Double, s => Kwh(Meter(s, "battery", "energy_imported"))),
            new(new Guid("441a028d-f634-4b89-86ce-733887c9f2bb"), "battery_discharge_energy", "BATTERY_DISCHARGE_ENERGY", NodeDataType.Double, s => Kwh(Meter(s, "battery", "energy_exported"))),
            new(new Guid("97d036b8-f71e-4c42-9984-ba4c46353f9c"), "load_energy", "LOAD_ENERGY", NodeDataType.Double, s => Kwh(Meter(s, "load", "energy_imported"))),
        };

        private static double? Meter(PowerwallSnapshot s, string meter, string field)
        {
            if (s.Aggregates is not { ValueKind: JsonValueKind.Object } root || !root.TryGetProperty(meter, out var m) || m.ValueKind != JsonValueKind.Object)
            {
                return null;
            }
            return Number(m, field);
        }

        internal static double? Number(JsonElement? element, string name)
        {
            return element is { ValueKind: JsonValueKind.Object } e && e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetDouble() : null;
        }

        private static string Text(JsonElement? element, string name)
        {
            return element is { ValueKind: JsonValueKind.Object } e && e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
        }

        private static double? Negate(double? value) => value.HasValue ? -value.Value : null;

        private static object Kw(double? watt) => watt.HasValue ? Math.Round(watt.Value / 1000, 3) + 0d : null;

        private static object Kwh(double? wattHours) => wattHours.HasValue ? Math.Round(wattHours.Value / 1000, 3) : null;

        private static object Round(double? value, int digits) => value.HasValue ? Math.Round(value.Value, digits) : null;

        /// <summary>
        /// The gateway keeps 5 % that the app does not show: app = (raw - 5) / 0.95
        /// </summary>
        internal static double? AppPercent(double? raw) => raw.HasValue ? Math.Clamp((raw.Value - 5) / 0.95, 0, 100) : null;

        internal static double RawPercent(double app) => Math.Round(Math.Clamp(app, 0, 100) * 0.95 + 5, 2);

        internal static double ToDouble(object value)
        {
            return value switch
            {
                null => double.NaN,
                bool b => b ? 1 : 0,
                string s => double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var d) ? d : double.NaN,
                IConvertible c => Convert.ToDouble(c, CultureInfo.InvariantCulture),
                _ => double.NaN
            };
        }
    }
}
