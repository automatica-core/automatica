using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using NodeDataType = Automatica.Core.Base.Templates.NodeDataType;

namespace P3.Driver.Tesla.Driver
{
    /// <summary>
    /// A data point of a vehicle: how it is read from vehicle_data and which command writes it
    /// </summary>
    internal sealed record TeslaDataPoint(
        Guid Template,
        string Key,
        string I18N,
        NodeDataType Type,
        Func<JsonElement, object> Read,
        Func<object, (string Command, object Parameters)> Write = null)
    {
        public bool IsWriteable => Write != null;
    }

    internal static class TeslaDataPoints
    {
        public const double KmPerMile = 1.609344;

        public const string StateKey = "state";
        public const string WakeKey = "wake";

        public static readonly IReadOnlyList<TeslaDataPoint> All = new List<TeslaDataPoint>
        {
            new(new Guid("ca2d1494-4742-4f40-bc19-0c2c83e942de"), StateKey, "STATE", NodeDataType.String, null),
            new(new Guid("2fff8800-e31c-4000-a727-80e89c07c3fe"), "battery_level", "BATTERY_LEVEL", NodeDataType.Integer, r => Int(r, "charge_state", "battery_level")),
            new(new Guid("02d77735-5d74-4b18-b30e-25556368c45d"), "battery_range", "BATTERY_RANGE", NodeDataType.Double, r => Km(Double(r, "charge_state", "battery_range"))),
            new(new Guid("0babbbb6-ebfe-4095-8b47-c001bb591d4a"), "charging_state", "CHARGING_STATE", NodeDataType.String, r => String(r, "charge_state", "charging_state")),
            new(new Guid("c2334a7f-24ac-4dbf-8d65-36f8f3dd12ab"), "charging", "CHARGING", NodeDataType.Boolean,
                r => String(r, "charge_state", "charging_state") is { } s ? s == "Charging" || s == "Starting" : null,
                v => (ToBool(v) ? "charge_start" : "charge_stop", null)),
            new(new Guid("45b63840-39df-4ea8-b4b0-25f80ffa9ab6"), "charge_limit", "CHARGE_LIMIT", NodeDataType.Integer, r => Int(r, "charge_state", "charge_limit_soc"),
                v => ("set_charge_limit", new { percent = Math.Clamp((int)Math.Round(ToDouble(v)), 50, 100) })),
            new(new Guid("2cad8f61-5d3a-4ed9-96a1-2714d120a1ce"), "charger_power", "CHARGER_POWER", NodeDataType.Double, r => Double(r, "charge_state", "charger_power")),
            new(new Guid("eeabdf6b-2c47-4d25-a08d-a24b9ab9b3a3"), "time_to_full", "TIME_TO_FULL", NodeDataType.Double, r => Double(r, "charge_state", "time_to_full_charge")),
            new(new Guid("baa4de5e-0f8a-407e-9b9f-13ae62f94482"), "charge_port_open", "CHARGE_PORT_OPEN", NodeDataType.Boolean, r => Bool(r, "charge_state", "charge_port_door_open"),
                v => (ToBool(v) ? "charge_port_door_open" : "charge_port_door_close", null)),
            new(new Guid("e1f6ee4e-f54f-4ea9-93a0-332b89419729"), "inside_temp", "INSIDE_TEMP", NodeDataType.Double, r => Double(r, "climate_state", "inside_temp")),
            new(new Guid("4eb2b0c7-f1d5-4033-afe6-276290c12373"), "outside_temp", "OUTSIDE_TEMP", NodeDataType.Double, r => Double(r, "climate_state", "outside_temp")),
            new(new Guid("ad8b26a3-70ca-4393-9750-3b3eef11a2bf"), "climate", "CLIMATE", NodeDataType.Boolean, r => Bool(r, "climate_state", "is_climate_on"),
                v => (ToBool(v) ? "auto_conditioning_start" : "auto_conditioning_stop", null)),
            new(new Guid("be4ab27d-1e47-4779-b82d-c817502aabe4"), "target_temp", "TARGET_TEMP", NodeDataType.Double, r => Double(r, "climate_state", "driver_temp_setting"),
                v => ("set_temps", new { driver_temp = Math.Clamp(ToDouble(v), 15d, 28d), passenger_temp = Math.Clamp(ToDouble(v), 15d, 28d) })),
            new(new Guid("8197f796-c8d4-4f0d-9da3-667d426773b7"), "locked", "LOCKED", NodeDataType.Boolean, r => Bool(r, "vehicle_state", "locked"),
                v => (ToBool(v) ? "door_lock" : "door_unlock", null)),
            new(new Guid("aed4654d-f286-47ed-b758-d136313f49e4"), "sentry_mode", "SENTRY_MODE", NodeDataType.Boolean, r => Bool(r, "vehicle_state", "sentry_mode"),
                v => ("set_sentry_mode", new { on = ToBool(v) })),
            new(new Guid("25ff8756-f7ec-45c4-9548-0250d9b52972"), "odometer", "ODOMETER", NodeDataType.Double, r => Km(Double(r, "vehicle_state", "odometer"))),
            new(new Guid("71ba4040-0351-42eb-9662-86fb04362deb"), "latitude", "LATITUDE", NodeDataType.Double, r => Double(r, "drive_state", "latitude")),
            new(new Guid("66b678fe-d549-4636-87fa-cd2257990b1b"), "longitude", "LONGITUDE", NodeDataType.Double, r => Double(r, "drive_state", "longitude")),
            new(new Guid("e0b929f1-3d1f-4a2f-b071-7435d2a358d1"), "shift_state", "SHIFT_STATE", NodeDataType.String, r => String(r, "drive_state", "shift_state") ?? (Section(r, "drive_state") != null ? "P" : null)),
            new(new Guid("3f33024c-1071-44a0-895c-5013d84ad28f"), WakeKey, "WAKE", NodeDataType.Boolean, null, v => (null, null)),
            new(new Guid("b8406026-0b05-4d94-93e9-97a453a2241a"), "flash_lights", "FLASH_LIGHTS", NodeDataType.Boolean, null, v => ToBool(v) ? ("flash_lights", null) : (null, null)),
            new(new Guid("40063f4a-c8ee-4ebb-9069-365a18fc0c25"), "honk", "HONK", NodeDataType.Boolean, null, v => ToBool(v) ? ("honk_horn", null) : (null, null)),
        };

        private static JsonElement? Section(JsonElement root, string section)
        {
            return root.ValueKind == JsonValueKind.Object && root.TryGetProperty(section, out var s) && s.ValueKind == JsonValueKind.Object ? s : null;
        }

        private static JsonElement? Value(JsonElement root, string section, string name)
        {
            var s = Section(root, section);
            return s.HasValue && s.Value.TryGetProperty(name, out var v) && v.ValueKind != JsonValueKind.Null ? v : null;
        }

        internal static object Int(JsonElement root, string section, string name)
        {
            var v = Value(root, section, name);
            return v?.ValueKind == JsonValueKind.Number ? (int)Math.Round(v.Value.GetDouble()) : null;
        }

        internal static object Double(JsonElement root, string section, string name)
        {
            var v = Value(root, section, name);
            return v?.ValueKind == JsonValueKind.Number ? v.Value.GetDouble() : null;
        }

        internal static object Bool(JsonElement root, string section, string name)
        {
            var v = Value(root, section, name);
            return v?.ValueKind switch
            {
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                _ => null
            };
        }

        internal static string String(JsonElement root, string section, string name)
        {
            var v = Value(root, section, name);
            return v?.ValueKind == JsonValueKind.String ? v.Value.GetString() : null;
        }

        private static object Km(object miles) => miles is double d ? Math.Round(d * KmPerMile, 1) : null;

        internal static bool ToBool(object value)
        {
            return value switch
            {
                bool b => b,
                null => false,
                string s => s.Equals("true", StringComparison.OrdinalIgnoreCase) || s == "1" || s.Equals("on", StringComparison.OrdinalIgnoreCase),
                IConvertible c => Convert.ToDouble(c, CultureInfo.InvariantCulture) != 0,
                _ => false
            };
        }

        internal static double ToDouble(object value)
        {
            return value switch
            {
                null => 0,
                bool b => b ? 1 : 0,
                string s => double.Parse(s, NumberStyles.Float, CultureInfo.InvariantCulture),
                IConvertible c => Convert.ToDouble(c, CultureInfo.InvariantCulture),
                _ => 0
            };
        }
    }
}
