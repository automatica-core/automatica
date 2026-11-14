using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using NodeDataType = Automatica.Core.Base.Templates.NodeDataType;

namespace P3.Driver.Tuya.Driver
{
    /// <param name="Dp">DP id of the device, null for driver values</param>
    /// <param name="Scale">The device sends integers: value = raw × scale</param>
    internal sealed record TuyaDataPoint(Guid Template, string Key, string I18N, NodeDataType Type, string Dp, double Scale = 1, bool Writable = false)
    {
        /// <summary>
        /// Value for Automatica from the raw DP value
        /// </summary>
        public object Read(JsonElement raw) => raw.ValueKind switch
        {
            JsonValueKind.True or JsonValueKind.False when Type == NodeDataType.Boolean => raw.GetBoolean(),
            JsonValueKind.Number when Type == NodeDataType.Double => Math.Round(raw.GetDouble() * Scale, 4),
            JsonValueKind.Number when Type == NodeDataType.Integer => raw.GetInt64(),
            JsonValueKind.String when Type == NodeDataType.String => raw.GetString(),
            _ => null
        };

        /// <summary>
        /// Raw DP value to send from a value written in Automatica
        /// </summary>
        public object ToRaw(object value) => Type switch
        {
            NodeDataType.Boolean => value is string s ? s is "1" || s.Equals("true", StringComparison.OrdinalIgnoreCase) || s.Equals("on", StringComparison.OrdinalIgnoreCase) : Convert.ToBoolean(value, CultureInfo.InvariantCulture),
            NodeDataType.Integer => Convert.ToInt64(value, CultureInfo.InvariantCulture),
            NodeDataType.Double => (long)Math.Round(Convert.ToDouble(value, CultureInfo.InvariantCulture) / Scale),
            _ => Convert.ToString(value, CultureInfo.InvariantCulture)
        };
    }

    /// <summary>
    /// Data points of a Tuya smart plug with power meter (category "cz", e.g. LSC Power Plug w/ Power Meter EU):
    /// 1 switch, 9 countdown s, 17 energy added (Wh), 18 current mA, 19 power 0.1 W, 20 voltage 0.1 V, 38 state after a power loss
    /// </summary>
    internal static class TuyaDataPoints
    {
        public const string ConnectedKey = "connected";

        public static readonly IReadOnlyList<TuyaDataPoint> Plug = new List<TuyaDataPoint>
        {
            new(new Guid("11799c32-7484-4a7f-bf78-39f9f7523a6e"), ConnectedKey, "CONNECTED", NodeDataType.Boolean, null),
            new(new Guid("e05d8c2e-a0d9-4092-9570-ec0c725e66f3"), "switch", "SWITCH", NodeDataType.Boolean, "1", Writable: true),
            new(new Guid("38e59a3f-ca6b-4d48-9e45-67e783c035a9"), "power", "POWER", NodeDataType.Double, "19", 0.1),
            new(new Guid("c5395a6a-3a9f-45f6-9a89-4f4d736b9398"), "voltage", "VOLTAGE", NodeDataType.Double, "20", 0.1),
            new(new Guid("b393b402-160d-463d-828a-c667f0bbede7"), "current", "CURRENT", NodeDataType.Double, "18", 0.001),
            new(new Guid("dec6c8e1-e1d1-41f2-ad20-c557bededef2"), "energy_added", "ENERGY_ADDED", NodeDataType.Double, "17", 0.001),
            new(new Guid("48005620-498b-4a3b-8333-a032cae9df6b"), "countdown", "COUNTDOWN", NodeDataType.Integer, "9", Writable: true),
            new(new Guid("a5e545df-c28f-4c43-a731-e019027620ee"), "power_on_state", "POWER_ON_STATE", NodeDataType.String, "38"),
        };
    }
}
