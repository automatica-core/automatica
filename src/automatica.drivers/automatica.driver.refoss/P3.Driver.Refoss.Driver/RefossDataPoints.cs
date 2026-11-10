using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using NodeDataType = Automatica.Core.Base.Templates.NodeDataType;

namespace P3.Driver.Refoss.Driver
{
    /// <param name="Read">Value from the Em.Status entry of the channel, null for driver values</param>
    internal sealed record RefossDataPoint(Guid Template, string Key, string I18N, NodeDataType Type, Func<JsonElement, object> Read);

    /// <summary>
    /// Values of one CT channel as Em.Status.Get reports them: power W, current A, voltage V, energies kWh.
    /// The energies are the device's day / week / month counters (they restart at 0), not lifetime counters.
    /// </summary>
    internal static class RefossDataPoints
    {
        public const string ConnectedKey = "connected";

        /// <summary>
        /// Values of the device itself
        /// </summary>
        public static readonly IReadOnlyList<RefossDataPoint> Device = new List<RefossDataPoint>
        {
            new(new Guid("e0ab9924-6cb1-4c88-ac31-78f1ed80ae66"), ConnectedKey, "CONNECTED", NodeDataType.Boolean, null),
        };

        /// <summary>
        /// Values of every channel
        /// </summary>
        public static readonly IReadOnlyList<RefossDataPoint> Channel = new List<RefossDataPoint>
        {
            new(new Guid("90a06b30-2665-4953-b629-7a9c23a5779a"), "power", "POWER", NodeDataType.Double, e => Round(e, "power", 1)),
            new(new Guid("20684010-b871-4bf3-b506-4e753603140c"), "current", "CURRENT", NodeDataType.Double, e => Round(e, "current", 3)),
            new(new Guid("3c2f3be1-07d5-493d-b609-b08d17e520a7"), "voltage", "VOLTAGE", NodeDataType.Double, e => Round(e, "voltage", 1)),
            new(new Guid("ad342242-60f7-45d2-93ad-06aa7de10cb4"), "pf", "PF", NodeDataType.Double, e => Round(e, "pf", 3)),
            new(new Guid("6fc316c8-841a-4b12-a059-3c6f99bae49b"), "day_energy", "DAY_ENERGY", NodeDataType.Double, e => Round(e, "day_energy", 3)),
            new(new Guid("4a395c6c-0bd5-44b4-bc38-72dd65ce900d"), "week_energy", "WEEK_ENERGY", NodeDataType.Double, e => Round(e, "week_energy", 3)),
            new(new Guid("394d346b-b716-421c-b57a-87a43bfeb74e"), "month_energy", "MONTH_ENERGY", NodeDataType.Double, e => Round(e, "month_energy", 3)),
            new(new Guid("2c1d4853-58ce-4fe0-a8c0-af09e1c20866"), "day_ret_energy", "DAY_RET_ENERGY", NodeDataType.Double, e => Round(e, "day_ret_energy", 3)),
            new(new Guid("8078155e-41e6-4b84-ac92-9ae79f99bf2b"), "week_ret_energy", "WEEK_RET_ENERGY", NodeDataType.Double, e => Round(e, "week_ret_energy", 3)),
            new(new Guid("8b74b649-240a-41bf-9c2f-6a0eb772ac21"), "month_ret_energy", "MONTH_RET_ENERGY", NodeDataType.Double, e => Round(e, "month_ret_energy", 3)),
        };

        /// <summary>
        /// Channel ids of the EM16P: A1–A6 = 1–6, B1–B6 = 7–12, C1–C6 = 13–18
        /// </summary>
        public const int ChannelCount = 18;

        public static string ChannelName(int id) => id is >= 1 and <= ChannelCount ? $"{(char)('A' + (id - 1) / 6)}{(id - 1) % 6 + 1}" : $"Channel {id}";

        /// <summary>
        /// The per-channel entries of a status (or config) response by channel id. Accepts the documented list
        /// ({"status":[{"id":1,...}]} / {"config":[...]}), a bare list, the component map of Refoss.Status.Get
        /// ({"em:1":{...}}) and a single entry.
        /// </summary>
        public static Dictionary<int, JsonElement> Entries(JsonElement root, string listName)
        {
            var result = new Dictionary<int, JsonElement>();
            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty(listName, out var list))
            {
                root = list;
            }

            switch (root.ValueKind)
            {
                case JsonValueKind.Array:
                    foreach (var entry in root.EnumerateArray())
                    {
                        if (Id(entry) is { } id)
                        {
                            result[id] = entry;
                        }
                    }
                    break;
                case JsonValueKind.Object when Id(root) is { } single:
                    result[single] = root;
                    break;
                case JsonValueKind.Object:
                    foreach (var property in root.EnumerateObject())
                    {
                        if (property.Value.ValueKind == JsonValueKind.Object && property.Name.StartsWith("em:", StringComparison.OrdinalIgnoreCase) &&
                            int.TryParse(property.Name.AsSpan(3), NumberStyles.Integer, CultureInfo.InvariantCulture, out var key))
                        {
                            result[Id(property.Value) ?? key] = property.Value;
                        }
                    }
                    break;
            }
            return result;
        }

        private static int? Id(JsonElement e)
        {
            if (e.ValueKind != JsonValueKind.Object || !e.TryGetProperty("id", out var id))
            {
                return null;
            }
            return id.ValueKind == JsonValueKind.Number && id.TryGetInt32(out var i) ? i
                : id.ValueKind == JsonValueKind.String && int.TryParse(id.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var s) ? s
                : null;
        }

        internal static double? Number(JsonElement e, string name)
        {
            if (e.ValueKind != JsonValueKind.Object || !e.TryGetProperty(name, out var v))
            {
                return null;
            }
            return v.ValueKind switch
            {
                JsonValueKind.Number => v.GetDouble(),
                JsonValueKind.String when double.TryParse(v.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var d) => d,
                _ => null
            };
        }

        public static string Text(JsonElement e, string name) =>
            e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

        // + 0d turns -0 into 0
        private static object Round(JsonElement e, string name, int digits) => Number(e, name) is { } v ? Math.Round(v, digits) + 0d : null;
    }
}
