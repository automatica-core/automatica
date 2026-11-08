using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using NodeDataType = Automatica.Core.Base.Templates.NodeDataType;

namespace P3.Driver.Navimow.Driver
{
    /// <summary>
    /// A command to send (Google smart-home intent name + params). Null = nothing (momentary button released).
    /// </summary>
    internal sealed record NavimowCommand(string Command, object Params = null);

    internal sealed record NavimowDataPoint(Guid Template, string Key, string I18N, NodeDataType Type, Func<JsonObject, NavimowDevice, object> Read,
        Func<object, NavimowCommand> Write = null)
    {
        public bool IsWriteable => Write != null;
    }

    internal static class NavimowDataPoints
    {
        internal static string State(JsonObject status) => status?["vehicleState"] is JsonValue v ? v.ToString() : null;

        private static object Is(JsonObject status, params string[] states) =>
            State(status) is { } s ? states.Any(x => string.Equals(x, s, StringComparison.OrdinalIgnoreCase)) : null;

        private static object Online(JsonObject status, NavimowDevice device)
        {
            if (State(status) is { } s && string.Equals(s, "Offline", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            if (status?["online"] is JsonValue v && v.TryGetValue<bool>(out var online))
            {
                return online;
            }
            return device?.Online ?? (State(status) != null ? true : null);
        }

        internal static object Battery(JsonObject status)
        {
            if (status?["capacityRemaining"] is not JsonArray capacities)
            {
                return null;
            }
            foreach (var capacity in capacities.OfType<JsonObject>())
            {
                var unit = capacity["unit"]?.ToString();
                if ((unit == null || string.Equals(unit, "PERCENTAGE", StringComparison.OrdinalIgnoreCase)) && capacity["rawValue"] is JsonValue raw &&
                    double.TryParse(raw.ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var percent))
                {
                    return (int)Math.Round(percent);
                }
            }
            return null;
        }

        private static NavimowCommand OnOff(object value, NavimowCommand on, NavimowCommand off) => Conv.ToBool(value) ? on : off;

        public static readonly IReadOnlyList<NavimowDataPoint> All = new List<NavimowDataPoint>
        {
            new(new Guid("b6a6aeae-3e9d-47a6-9564-78a6f78f0247"), "online", "ONLINE", NodeDataType.Boolean, Online),
            new(new Guid("eae76c4a-4c24-40b4-809e-4336eee788ac"), "state", "STATE", NodeDataType.String, (s, _) => State(s)),
            new(new Guid("51aa6802-7429-483d-abe0-fb1645863601"), "mowing", "MOWING", NodeDataType.Boolean, (s, _) => Is(s, "isRunning"),
                v => OnOff(v, new NavimowCommand(NavimowApi.StartStop, new { on = true }), new NavimowCommand(NavimowApi.Dock))),
            new(new Guid("7092e2e8-f80e-476f-884c-de9be0a78c69"), "paused", "PAUSED", NodeDataType.Boolean, (s, _) => Is(s, "isPaused"),
                v => OnOff(v, new NavimowCommand(NavimowApi.PauseUnpause, new { on = false }), new NavimowCommand(NavimowApi.PauseUnpause, new { on = true }))),
            new(new Guid("a8157d57-5dd1-4ac1-87d2-d033024cfc2a"), "dock", "DOCK", NodeDataType.Boolean, null, v => OnOff(v, new NavimowCommand(NavimowApi.Dock), null)),
            new(new Guid("982119d9-702d-414a-bf6d-cdbc86a7e481"), "stop", "STOP", NodeDataType.Boolean, null, v => OnOff(v, new NavimowCommand(NavimowApi.StartStop, new { on = false }), null)),
            new(new Guid("b65442f4-7706-4a98-b5d5-cd8519fe63ef"), "docked", "DOCKED", NodeDataType.Boolean, (s, _) => Is(s, "isDocked")),
            new(new Guid("d6569f4f-217c-4b85-a7c1-bf3d9beb232f"), "returning", "RETURNING", NodeDataType.Boolean, (s, _) => Is(s, "isDocking")),
            new(new Guid("89759257-3d87-408f-91b4-59a5f52a2812"), "error", "ERROR", NodeDataType.Boolean, (s, _) => Is(s, "Error")),
            new(new Guid("ee47bc2d-bb0a-466d-9f94-84081c647ee5"), "lifted", "LIFTED", NodeDataType.Boolean, (s, _) => Is(s, "isLifted")),
            new(new Guid("7a7c17e1-686b-4653-82db-714c2542ea51"), "battery", "BATTERY", NodeDataType.Integer, (s, _) => Battery(s)),
            new(new Guid("f2ff25d0-68d7-41ff-9b4a-add3206a1d31"), "firmware", "FIRMWARE", NodeDataType.String, (_, d) => d?.Firmware),
        };
    }
}
