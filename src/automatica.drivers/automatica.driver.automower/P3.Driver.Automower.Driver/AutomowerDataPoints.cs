using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using NodeDataType = Automatica.Core.Base.Templates.NodeDataType;

namespace P3.Driver.Automower.Driver
{
    /// <summary>
    /// What a write sends: an action (type + duration) or settings attributes. Null = nothing (momentary button released).
    /// </summary>
    internal sealed record AutomowerWrite(string Action, int? Duration = null, object Settings = null);

    internal sealed record AutomowerDataPoint(Guid Template, string Key, string I18N, NodeDataType Type, Func<JsonObject, object> Read,
        Func<object, int, AutomowerWrite> Write = null)
    {
        public bool IsWriteable => Write != null;
    }

    internal static class AutomowerDataPoints
    {
        private static object Get(JsonObject a, params string[] path)
        {
            JsonNode node = a;
            foreach (var p in path)
            {
                node = node is JsonObject o ? o[p] : null;
            }
            return node is JsonValue v ? Conv.Scalar(v.Deserialize<JsonElement>()) : null;
        }

        private static object Str(JsonObject a, params string[] path) => Get(a, path) is { } v ? Conv.ToText(v) : null;
        private static object Int(JsonObject a, params string[] path) => Get(a, path) is int or double ? (int)Math.Round(Conv.ToDouble(Get(a, path))) : null;
        private static object Dbl(JsonObject a, params string[] path) => Get(a, path) is int or double ? Conv.ToDouble(Get(a, path)) : null;
        private static object Hours(JsonObject a, params string[] path) => Get(a, path) is int or double ? Math.Round(Conv.ToDouble(Get(a, path)) / 3600d, 1) : null;

        private static object Position(JsonObject a, string name)
        {
            return a["positions"] is JsonArray { Count: > 0 } positions && positions[0] is JsonObject newest ? Dbl(newest, name) : null;
        }

        /// <summary>
        /// Timestamps in mower local time: REST sends milliseconds, the web socket seconds; 0 = none
        /// </summary>
        internal static object LocalTime(JsonObject a, params string[] path)
        {
            if (Get(a, path) is not (int or double) || Conv.ToDouble(Get(a, path)) is var raw && raw <= 0)
            {
                return null;
            }
            var ms = raw > 1e11 ? raw : raw * 1000;
            return DateTime.SpecifyKind(DateTimeOffset.FromUnixTimeMilliseconds((long)ms).DateTime, DateTimeKind.Unspecified);
        }

        private static int Minutes(object value) => Math.Clamp((int)Math.Round(Conv.ToDouble(value)), 1, 65000);

        public const string MowingKey = "mowing";

        public static readonly IReadOnlyList<AutomowerDataPoint> All = new List<AutomowerDataPoint>
        {
            new(new Guid("d2ca04a1-4007-4db8-9da5-b843d1c082ac"), "connected", "CONNECTED", NodeDataType.Boolean, a => Get(a, "metadata", "connected") as bool?),
            new(new Guid("05b374d8-d1e8-43ef-a606-ded6b93db803"), "battery", "BATTERY", NodeDataType.Integer, a => Int(a, "battery", "batteryPercent")),
            new(new Guid("d8178727-1b6c-43d5-9e76-5f02fca3717d"), "mode", "MODE", NodeDataType.String, a => Str(a, "mower", "mode")),
            new(new Guid("7f09f32d-cc07-4d5a-9341-4cccbe9541e0"), "activity", "ACTIVITY", NodeDataType.String, a => Str(a, "mower", "activity")),
            new(new Guid("92f9602a-d8f7-419f-b341-d1cf32893dd6"), "state", "STATE", NodeDataType.String, a => Str(a, "mower", "state")),
            new(new Guid("de193d9a-e41a-4f8e-b063-17ae34d388e3"), "inactive_reason", "INACTIVE_REASON", NodeDataType.String, a => Str(a, "mower", "inactiveReason")),
            new(new Guid("95f648d9-57e3-4b78-b17e-680702b85a4b"), "error_code", "ERROR_CODE", NodeDataType.Integer, a => Int(a, "mower", "errorCode")),
            new(new Guid("103947fd-e244-4516-bb8c-ef885713578b"), "error_time", "ERROR_TIME", NodeDataType.DateTime, a => LocalTime(a, "mower", "errorCodeTimestamp")),
            new(new Guid("73259d46-1d7b-42a1-a6df-534491b6f59f"), "next_start", "NEXT_START", NodeDataType.DateTime, a => LocalTime(a, "planner", "nextStartTimestamp")),
            new(new Guid("94c579a4-6c71-44de-a86f-643ed044880c"), "override_action", "OVERRIDE_ACTION", NodeDataType.String, a => Str(a, "planner", "override", "action")),
            new(new Guid("cfb971ff-5ab3-4c0f-8717-7e1337708d4e"), "restricted_reason", "RESTRICTED_REASON", NodeDataType.String, a => Str(a, "planner", "restrictedReason")),
            new(new Guid("6d9e2a0e-9877-4e55-bb64-8c72a65a115b"), MowingKey, "MOWING", NodeDataType.Boolean, a => Str(a, "mower", "activity") is string s ? s is "MOWING" or "LEAVING" : null,
                (v, d) => Conv.ToBool(v) ? new AutomowerWrite("Start", d) : new AutomowerWrite("ParkUntilNextSchedule")),
            new(new Guid("95138ca1-5d09-49a2-bad4-fa07d02b2947"), "mow_minutes", "MOW_MINUTES", NodeDataType.Integer, null,
                (v, _) => Conv.ToDouble(v) > 0 ? new AutomowerWrite("Start", Minutes(v)) : new AutomowerWrite("ParkUntilNextSchedule")),
            new(new Guid("493a4434-541f-4bf1-adac-5167cbfeb411"), "park_minutes", "PARK_MINUTES", NodeDataType.Integer, null,
                (v, _) => Conv.ToDouble(v) > 0 ? new AutomowerWrite("Park", Minutes(v)) : null),
            new(new Guid("b6169ddc-bb8b-4e17-aa8f-73c3ccf12f48"), "park", "PARK", NodeDataType.Boolean, null, (v, _) => Conv.ToBool(v) ? new AutomowerWrite("ParkUntilFurtherNotice") : null),
            new(new Guid("fe6b3d79-f0f4-4e98-ac52-e69c173259c1"), "pause", "PAUSE", NodeDataType.Boolean, null, (v, _) => Conv.ToBool(v) ? new AutomowerWrite("Pause") : null),
            new(new Guid("b8929258-a4ee-4c6d-96f3-e50afee9cf3d"), "resume_schedule", "RESUME_SCHEDULE", NodeDataType.Boolean, null, (v, _) => Conv.ToBool(v) ? new AutomowerWrite("ResumeSchedule") : null),
            new(new Guid("8d47b3c5-c9ef-4738-994b-d9989e96b094"), "cutting_height", "CUTTING_HEIGHT", NodeDataType.Integer, a => Int(a, "settings", "cuttingHeight"),
                (v, _) => new AutomowerWrite(null, Settings: new { cuttingHeight = Math.Clamp((int)Math.Round(Conv.ToDouble(v)), 1, 9) })),
            new(new Guid("2689d581-abcd-433a-9ae7-e8f4cf61a57a"), "headlight", "HEADLIGHT", NodeDataType.String, a => Str(a, "settings", "headlight", "mode"),
                (v, _) => new AutomowerWrite(null, Settings: new { headlight = new { mode = Conv.ToText(v).Trim().ToUpperInvariant() } })),
            new(new Guid("cc553291-0571-4a19-a753-3288dd5a73f5"), "latitude", "LATITUDE", NodeDataType.Double, a => Position(a, "latitude")),
            new(new Guid("8ad0c92c-53bf-4776-9724-e85bd8ee5c50"), "longitude", "LONGITUDE", NodeDataType.Double, a => Position(a, "longitude")),
            new(new Guid("73c22c13-00dd-4565-95c9-b3ba974f0ee3"), "cutting_time", "CUTTING_TIME", NodeDataType.Double, a => Hours(a, "statistics", "totalCuttingTime")),
            new(new Guid("463a2e50-05fb-4f89-8547-2762de85620a"), "running_time", "RUNNING_TIME", NodeDataType.Double, a => Hours(a, "statistics", "totalRunningTime")),
            new(new Guid("50ab2328-7af9-427e-b4b7-0bb5e00fca42"), "blade_time", "BLADE_TIME", NodeDataType.Double, a => Hours(a, "statistics", "cuttingBladeUsageTime")),
            new(new Guid("326c6861-1dfa-41ab-9441-f36a72f7bc73"), "charging_cycles", "CHARGING_CYCLES", NodeDataType.Integer, a => Int(a, "statistics", "numberOfChargingCycles")),
            new(new Guid("f40eac51-fb77-457f-a012-f2d0be246e3a"), "collisions", "COLLISIONS", NodeDataType.Integer, a => Int(a, "statistics", "numberOfCollisions")),
        };

        /// <summary>
        /// Merges a v2 web socket event into the REST shaped attributes of the mower. False = unknown event.
        /// </summary>
        internal static bool Merge(JsonObject attributes, string type, JsonObject eventAttributes)
        {
            void MergeBlock(string target, JsonNode source)
            {
                if (source is not JsonObject block)
                {
                    return;
                }
                if (attributes[target] is not JsonObject existing)
                {
                    attributes[target] = existing = new JsonObject();
                }
                foreach (var (name, value) in block)
                {
                    existing[name] = value?.DeepClone();
                }
            }

            switch (type)
            {
                case "battery-event-v2":
                    MergeBlock("battery", eventAttributes["battery"]);
                    return true;
                case "mower-event-v2":
                    MergeBlock("mower", eventAttributes["mower"]);
                    return true;
                case "planner-event-v2":
                    MergeBlock("planner", eventAttributes["planner"]);
                    return true;
                case "cuttingHeight-event-v2":
                    if (eventAttributes["cuttingHeight"]?["height"] is { } height)
                    {
                        MergeBlock("settings", new JsonObject { ["cuttingHeight"] = height.DeepClone() });
                    }
                    return true;
                case "headlights-event-v2":
                    MergeBlock("settings", new JsonObject { ["headlight"] = eventAttributes["headlight"]?.DeepClone() });
                    return true;
                case "position-event-v2":
                    if (eventAttributes["position"] is JsonObject position)
                    {
                        attributes["positions"] = new JsonArray(position.DeepClone());
                    }
                    return true;
                default:
                    return false;
            }
        }
    }
}
