using System;
using System.Collections.Generic;
using System.Linq;
using NodeDataType = Automatica.Core.Base.Templates.NodeDataType;

namespace P3.Driver.Gardena.Driver
{
    internal sealed record GardenaCommand(string ControlType, string Command, int? Seconds = null);

    /// <summary>
    /// The kinds of service nodes: GARDENA service type, control type and node template
    /// </summary>
    internal sealed record GardenaKind(string Key, string ServiceType, string ControlType, Guid Template, Guid Interface, string I18N, int DefaultMinutes);

    /// <summary>
    /// A data point of a service node. <see cref="Common"/> = the attribute comes from the COMMON service of the device.
    /// <see cref="Write"/> gets the value and the default duration in minutes, null = nothing to send.
    /// </summary>
    internal sealed record GardenaDataPoint(Guid Template, string Kind, string Key, string I18N, NodeDataType Type, string Attribute, bool Common = false,
        Func<object, object> Map = null, Func<object, int, GardenaCommand> Write = null)
    {
        public bool IsWriteable => Write != null;
    }

    internal static class GardenaDataPoints
    {
        public const string Valve = "valve";
        public const string Mower = "mower";
        public const string PowerSocket = "power_socket";
        public const string Sensor = "sensor";

        public static readonly IReadOnlyList<GardenaKind> Kinds = new List<GardenaKind>
        {
            new(Valve, "VALVE", "VALVE_CONTROL", new Guid("878a45a8-be79-47be-95f8-347acf4948f3"), new Guid("c7607a85-156a-4289-afa6-6e32f6d2fce0"), "VALVE", 30),
            new(Mower, "MOWER", "MOWER_CONTROL", new Guid("4546689b-d1cf-44b7-b3e2-353625ed6e0d"), new Guid("b19ba0f8-4bd2-4d00-a21e-4cad16ad2f70"), "MOWER", 60),
            new(PowerSocket, "POWER_SOCKET", "POWER_SOCKET_CONTROL", new Guid("ebfff019-7704-46c0-aaad-26efc15ce875"), new Guid("bcd9a7cd-41ee-4bf4-a482-65250a62eb0e"), "POWER_SOCKET", 60),
            new(Sensor, "SENSOR", null, new Guid("fd85b6f5-3f9f-4354-9be1-8d38d83ebb3a"), new Guid("e4e9f4e1-5eec-4cfa-976d-598c917941c5"), "SENSOR", 0),
        };

        private static readonly HashSet<string> MowingActivities = new() { "OK_CUTTING", "OK_CUTTING_TIMER_OVERRIDDEN", "OK_LEAVING" };

        private static int Seconds(object minutes) => Math.Max(1, (int)Math.Round(Conv.ToDouble(minutes))) * 60;

        private static GardenaCommand Start(string control, int seconds) => new(control, "START_SECONDS_TO_OVERRIDE", seconds);

        private static object Minutes(object seconds) => seconds is int or double ? (int)Math.Round(Conv.ToDouble(seconds) / 60) : null;

        public static readonly IReadOnlyList<GardenaDataPoint> All = new List<GardenaDataPoint>
        {
            // valve
            new(new Guid("07f35ece-49b1-4ae6-b000-ac5ed7448327"), Valve, "watering", "WATERING", NodeDataType.Boolean, "activity", Map: a => a is string s ? s != "CLOSED" : null,
                Write: (v, d) => Conv.ToBool(v) ? Start("VALVE_CONTROL", d * 60) : new GardenaCommand("VALVE_CONTROL", "STOP_UNTIL_NEXT_TASK")),
            new(new Guid("7a8f5ca9-141d-499a-b443-fa65da600983"), Valve, "water_minutes", "WATER_MINUTES", NodeDataType.Integer, "duration", Map: Minutes,
                Write: (v, _) => Conv.ToDouble(v) > 0 ? Start("VALVE_CONTROL", Seconds(v)) : new GardenaCommand("VALVE_CONTROL", "STOP_UNTIL_NEXT_TASK")),
            new(new Guid("4dac3e35-0185-4f5c-84ec-e747fec61e6f"), Valve, "activity", "ACTIVITY", NodeDataType.String, "activity"),
            new(new Guid("18ef411d-3854-4ad6-871c-b124c88e3976"), Valve, "pause", "PAUSE", NodeDataType.Boolean, null,
                Write: (v, _) => new GardenaCommand("VALVE_CONTROL", Conv.ToBool(v) ? "PAUSE" : "UNPAUSE")),
            new(new Guid("d57b22a8-493d-4ffa-be29-a5a108418dba"), Valve, "state", "STATE", NodeDataType.String, "state"),
            new(new Guid("95db7784-74b8-46cc-b057-11b1dad970b3"), Valve, "last_error", "LAST_ERROR", NodeDataType.String, "lastErrorCode"),
            new(new Guid("6dfdb608-eefd-4dfd-b691-72f821503d30"), Valve, "battery_level", "BATTERY_LEVEL", NodeDataType.Integer, "batteryLevel", true),
            new(new Guid("b390d29b-c895-4db6-a0ec-9d95dbff8068"), Valve, "rf_link_state", "RF_LINK_STATE", NodeDataType.String, "rfLinkState", true),

            // mower
            new(new Guid("ef1f78e4-be1c-49aa-bfc3-57c64c7f94af"), Mower, "mowing", "MOWING", NodeDataType.Boolean, "activity", Map: a => a is string s ? MowingActivities.Contains(s) : null,
                Write: (v, d) => Conv.ToBool(v) ? Start("MOWER_CONTROL", d * 60) : new GardenaCommand("MOWER_CONTROL", "PARK_UNTIL_NEXT_TASK")),
            new(new Guid("4538ec84-89a3-4164-8f8b-1afc2647eb80"), Mower, "mow_minutes", "MOW_MINUTES", NodeDataType.Integer, null,
                Write: (v, _) => Conv.ToDouble(v) > 0 ? Start("MOWER_CONTROL", Seconds(v)) : new GardenaCommand("MOWER_CONTROL", "PARK_UNTIL_NEXT_TASK")),
            new(new Guid("8c91265a-37a8-4ebe-955a-088775fffe62"), Mower, "park", "PARK", NodeDataType.Boolean, null,
                Write: (v, _) => Conv.ToBool(v) ? new GardenaCommand("MOWER_CONTROL", "PARK_UNTIL_FURTHER_NOTICE") : null),
            new(new Guid("75d0c602-b5bf-4090-a0ab-8a624f30df4d"), Mower, "resume_schedule", "RESUME_SCHEDULE", NodeDataType.Boolean, null,
                Write: (v, _) => Conv.ToBool(v) ? new GardenaCommand("MOWER_CONTROL", "START_DONT_OVERRIDE") : null),
            new(new Guid("281a3272-7b52-408b-b972-7a4be67e07f5"), Mower, "activity", "ACTIVITY", NodeDataType.String, "activity"),
            new(new Guid("3d857908-8ac8-4363-8f37-4bcec88dd773"), Mower, "state", "STATE", NodeDataType.String, "state"),
            new(new Guid("58ed8d20-06bd-4b9a-b089-b410d75516fd"), Mower, "last_error", "LAST_ERROR", NodeDataType.String, "lastErrorCode"),
            new(new Guid("b2bbbb4d-1221-4531-a073-16e53a46cb19"), Mower, "operating_hours", "OPERATING_HOURS", NodeDataType.Integer, "operatingHours"),
            new(new Guid("57d687b1-d84c-44cd-9841-0f7f7476bbdd"), Mower, "battery_level", "BATTERY_LEVEL", NodeDataType.Integer, "batteryLevel", true),
            new(new Guid("7cb02ed9-90c7-4b2d-af25-50fb76a7313e"), Mower, "battery_state", "BATTERY_STATE", NodeDataType.String, "batteryState", true),
            new(new Guid("f762de77-a373-44c2-babb-f2a6d5e58218"), Mower, "rf_link_state", "RF_LINK_STATE", NodeDataType.String, "rfLinkState", true),

            // power socket
            new(new Guid("6e9cfadd-58c3-4d00-b4ac-536208a0c7af"), PowerSocket, "on", "ON", NodeDataType.Boolean, "activity", Map: a => a is string s ? s != "OFF" : null,
                Write: (v, _) => new GardenaCommand("POWER_SOCKET_CONTROL", Conv.ToBool(v) ? "START_OVERRIDE" : "STOP_UNTIL_NEXT_TASK")),
            new(new Guid("864c99e7-ffbb-4a22-bf9e-279ab09359ae"), PowerSocket, "on_minutes", "ON_MINUTES", NodeDataType.Integer, "duration", Map: Minutes,
                Write: (v, _) => Conv.ToDouble(v) > 0 ? Start("POWER_SOCKET_CONTROL", Seconds(v)) : new GardenaCommand("POWER_SOCKET_CONTROL", "STOP_UNTIL_NEXT_TASK")),
            new(new Guid("b2da0db5-8d28-4923-93ae-7669974099bb"), PowerSocket, "activity", "ACTIVITY", NodeDataType.String, "activity"),
            new(new Guid("4c868bbe-b2ae-4a0c-9020-d5e2650bc9b9"), PowerSocket, "state", "STATE", NodeDataType.String, "state"),
            new(new Guid("21bc383a-1a45-4327-a2f7-78c32a32078b"), PowerSocket, "rf_link_state", "RF_LINK_STATE", NodeDataType.String, "rfLinkState", true),

            // sensor
            new(new Guid("a5b92171-5523-4216-8981-320558aa6f58"), Sensor, "soil_humidity", "SOIL_HUMIDITY", NodeDataType.Integer, "soilHumidity"),
            new(new Guid("358bdaaa-09fe-4e9a-9c97-2e4dab7c68f0"), Sensor, "soil_temperature", "SOIL_TEMPERATURE", NodeDataType.Double, "soilTemperature"),
            new(new Guid("7d54e65b-435f-4748-9edf-4eae575a29d1"), Sensor, "ambient_temperature", "AMBIENT_TEMPERATURE", NodeDataType.Double, "ambientTemperature"),
            new(new Guid("79350f36-09ad-4f1f-bd7c-d4a191cceaaa"), Sensor, "light_intensity", "LIGHT_INTENSITY", NodeDataType.Integer, "lightIntensity"),
            new(new Guid("995cde5f-0b4c-48d0-8ce5-d4e4cf26765f"), Sensor, "battery_level", "BATTERY_LEVEL", NodeDataType.Integer, "batteryLevel", true),
            new(new Guid("1b197fca-06c3-4977-9ef3-ac4b669f23f9"), Sensor, "rf_link_state", "RF_LINK_STATE", NodeDataType.String, "rfLinkState", true),
        };

        public static IEnumerable<GardenaDataPoint> Of(string kind) => All.Where(d => d.Kind == kind);

        public static GardenaKind Kind(string key) => Kinds.FirstOrDefault(k => k.Key == key);
    }
}
