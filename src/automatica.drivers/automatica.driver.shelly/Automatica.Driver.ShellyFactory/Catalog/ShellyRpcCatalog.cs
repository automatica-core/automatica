#nullable enable annotations
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using NodeDataType = Automatica.Core.Base.Templates.NodeDataType;
using static Automatica.Driver.ShellyFactory.Catalog.ShellyValues;

namespace Automatica.Driver.ShellyFactory.Catalog
{
    /// <summary>
    /// The Gen2 / Gen3 / Gen4 components (https://shelly-api-docs.shelly.cloud/gen2/ComponentsAndServices/...).
    /// A device is the set of components in its Shelly.GetStatus answer, so every model built from these components is supported
    /// without a model list: a new device with known components works as is.
    /// </summary>
    internal static class ShellyRpcCatalog
    {
        /// <summary>Namespace of the generated template ids, never change it (the ids are stored in the configuration)</summary>
        public static readonly Guid Namespace = new("6f3c2a41-9d0e-4c57-8a0b-2f41e5c7d913");

        public static readonly IReadOnlyList<ShellyComponentType> All;
        private static readonly Dictionary<Guid, ShellyComponentType> ByTemplate;
        private static readonly Dictionary<string, ShellyComponentType> ByType;

        static ShellyRpcCatalog()
        {
            All = new List<ShellyComponentType>
            {
                Keyed("switch", "SWITCH",
                    new ShellyDataPoint("output", "OUTPUT", NodeDataType.Boolean, "output").Rpc((v, id, _) => ("Switch.Set", new JObject { ["id"] = id, ["on"] = ToBool(v) })),
                    new ShellyDataPoint("toggle", "TOGGLE", NodeDataType.Boolean, null).Command().Rpc((v, id, _) => ToBool(v) ? ("Switch.Toggle", new JObject { ["id"] = id }) : null),
                    Metering()),
                Keyed("cover", "COVER",
                    new ShellyDataPoint("state", "COVER_STATE", NodeDataType.String, "state"),
                    new ShellyDataPoint("position", "POSITION", NodeDataType.Integer, "current_pos")
                        .Rpc((v, id, _) => ("Cover.GoToPosition", new JObject { ["id"] = id, ["pos"] = Math.Clamp(ToInt(v), 0, 100) })),
                    new ShellyDataPoint("target_position", "TARGET_POSITION", NodeDataType.Integer, "target_pos"),
                    new ShellyDataPoint("slat_position", "SLAT_POSITION", NodeDataType.Integer, "slat_pos")
                        .Rpc((v, id, _) => ("Cover.GoToPosition", new JObject { ["id"] = id, ["slat_pos"] = Math.Clamp(ToInt(v), 0, 100) })),
                    new ShellyDataPoint("open", "OPEN", NodeDataType.Boolean, null).Command().Rpc((v, id, _) => ToBool(v) ? ("Cover.Open", new JObject { ["id"] = id }) : null),
                    new ShellyDataPoint("close", "CLOSE", NodeDataType.Boolean, null).Command().Rpc((v, id, _) => ToBool(v) ? ("Cover.Close", new JObject { ["id"] = id }) : null),
                    new ShellyDataPoint("stop", "STOP", NodeDataType.Boolean, null).Command().Rpc((v, id, _) => ToBool(v) ? ("Cover.Stop", new JObject { ["id"] = id }) : null),
                    new ShellyDataPoint("calibrate", "CALIBRATE", NodeDataType.Boolean, null).Command().Rpc((v, id, _) => ToBool(v) ? ("Cover.Calibrate", new JObject { ["id"] = id }) : null),
                    Metering()),
                Keyed("light", "LIGHT", Dimmable("Light.Set").Concat(Metering()).ToArray()),
                Keyed("rgb", "RGB", Dimmable("RGB.Set").Concat(Rgb("RGB.Set")).Concat(Metering()).ToArray()),
                Keyed("rgbw", "RGBW", Dimmable("RGBW.Set").Concat(Rgb("RGBW.Set"))
                    .Append(new ShellyDataPoint("white", "WHITE", NodeDataType.Integer, "white")
                        .Rpc((v, id, _) => ("RGBW.Set", new JObject { ["id"] = id, ["white"] = Math.Clamp(ToInt(v), 0, 255) })))
                    .Concat(Metering()).ToArray()),
                Keyed("cct", "CCT", Dimmable("CCT.Set")
                    .Append(new ShellyDataPoint("color_temperature", "COLOR_TEMPERATURE", NodeDataType.Integer, "ct")
                        .Rpc((v, id, _) => ("CCT.Set", new JObject { ["id"] = id, ["ct"] = ToInt(v) })))
                    .Concat(Metering()).ToArray()),
                Keyed("rgbcct", "RGBCCT", Dimmable("RGBCCT.Set").Concat(Rgb("RGBCCT.Set"))
                    .Append(new ShellyDataPoint("color_temperature", "COLOR_TEMPERATURE", NodeDataType.Integer, "ct")
                        .Rpc((v, id, _) => ("RGBCCT.Set", new JObject { ["id"] = id, ["ct"] = ToInt(v) })))
                    .Concat(Metering()).ToArray()),

                // Pro 1CB..4CB circuit breakers
                Keyed("cb", "CB",
                    new ShellyDataPoint("output", "OUTPUT", NodeDataType.Boolean, "output").Rpc((v, id, _) => ("CB.Set", new JObject { ["id"] = id, ["output"] = ToBool(v) })),
                    new ShellyDataPoint("total_cycles", "TOTAL_CYCLES", NodeDataType.Integer, "total_cycles"),
                    new ShellyDataPoint("device_temperature", "DEVICE_TEMPERATURE", NodeDataType.Double, "temperature.tC")),

                Keyed("em", "EM", ThreePhase().ToArray()),
                Keyed("emdata", "EMDATA",
                    Phases("total_act_energy", "ENERGY").Concat(Phases("total_act_ret_energy", "RETURNED_ENERGY"))
                        .Append(new ShellyDataPoint("total_act", "TOTAL_ENERGY", NodeDataType.Double, "total_act"))
                        .Append(new ShellyDataPoint("total_act_ret", "TOTAL_RETURNED_ENERGY", NodeDataType.Double, "total_act_ret")).ToArray()),
                Keyed("em1", "EM1",
                    new ShellyDataPoint("active_power", "POWER", NodeDataType.Double, "act_power"),
                    new ShellyDataPoint("apparent_power", "APPARENT_POWER", NodeDataType.Double, "aprt_power"),
                    new ShellyDataPoint("voltage", "VOLTAGE", NodeDataType.Double, "voltage"),
                    new ShellyDataPoint("current", "CURRENT", NodeDataType.Double, "current"),
                    new ShellyDataPoint("power_factor", "POWER_FACTOR", NodeDataType.Double, "pf"),
                    new ShellyDataPoint("frequency", "FREQUENCY", NodeDataType.Double, "freq")),
                Keyed("em1data", "EM1DATA",
                    new ShellyDataPoint("energy", "ENERGY", NodeDataType.Double, "total_act_energy"),
                    new ShellyDataPoint("returned_energy", "RETURNED_ENERGY", NodeDataType.Double, "total_act_ret_energy")),
                Keyed("pm1", "PM1", Metering().ToArray()),

                Keyed("input", "INPUT",
                    new ShellyDataPoint("state", "STATE", NodeDataType.Boolean, "state").When((c, _) => c["state"]?.Type == JTokenType.Boolean),
                    new ShellyDataPoint("percent", "PERCENT", NodeDataType.Double, "percent"),
                    new ShellyDataPoint("counts", "COUNTS", NodeDataType.Double, "counts.total"),
                    new ShellyDataPoint("frequency", "FREQUENCY", NodeDataType.Double, "freq"),
                    new ShellyDataPoint("value", "VALUE", NodeDataType.Double, "xpercent"),
                    // button inputs have state null and report push events
                    new ShellyDataPoint("event", "EVENT", NodeDataType.String, null).Event().When((c, _) => c is JObject o && o.TryGetValue("state", out var s) && s.Type == JTokenType.Null)),
                Keyed("temperature", "TEMPERATURE",
                    new ShellyDataPoint("temperature", "TEMPERATURE", NodeDataType.Double, "tC")),
                Keyed("humidity", "HUMIDITY",
                    new ShellyDataPoint("humidity", "HUMIDITY", NodeDataType.Double, "rh")),
                Keyed("illuminance", "ILLUMINANCE",
                    new ShellyDataPoint("illuminance", "ILLUMINANCE", NodeDataType.Double, "lux"),
                    new ShellyDataPoint("illumination", "ILLUMINATION", NodeDataType.String, "illumination")),
                Keyed("voltmeter", "VOLTMETER",
                    new ShellyDataPoint("voltage", "VOLTAGE", NodeDataType.Double, "voltage"),
                    new ShellyDataPoint("value", "VALUE", NodeDataType.Double, "xvoltage")),
                Keyed("devicepower", "DEVICEPOWER",
                    new ShellyDataPoint("battery", "BATTERY", NodeDataType.Integer, "battery.percent"),
                    new ShellyDataPoint("battery_voltage", "BATTERY_VOLTAGE", NodeDataType.Double, "battery.V"),
                    new ShellyDataPoint("external_power", "EXTERNAL_POWER", NodeDataType.Boolean, "external.present")),
                Keyed("smoke", "SMOKE",
                    new ShellyDataPoint("alarm", "ALARM", NodeDataType.Boolean, "alarm"),
                    new ShellyDataPoint("muted", "MUTED", NodeDataType.Boolean, "mute"),
                    new ShellyDataPoint("mute", "MUTE", NodeDataType.Boolean, null).Command().Rpc((v, id, _) => ToBool(v) ? ("Smoke.Mute", new JObject { ["id"] = id }) : null)),
                Keyed("flood", "FLOOD",
                    new ShellyDataPoint("alarm", "ALARM", NodeDataType.Boolean, "alarm"),
                    // no Flood.Mute in the API: the alarm is muted on the device (alarm_mode)
                    new ShellyDataPoint("muted", "MUTED", NodeDataType.Boolean, "mute")),
                Keyed("presence", "PRESENCE",
                    new ShellyDataPoint("presence", "PRESENCE", NodeDataType.Boolean, "value"),
                    new ShellyDataPoint("objects", "OBJECTS", NodeDataType.Integer, "num_objects")),
                Keyed("presencezone", "PRESENCEZONE",
                    new ShellyDataPoint("presence", "PRESENCE", NodeDataType.Boolean, "value"),
                    new ShellyDataPoint("objects", "OBJECTS", NodeDataType.Integer, "num_objects")),
                Keyed("thermostat", "THERMOSTAT",
                    new ShellyDataPoint("enable", "ENABLE", NodeDataType.Boolean, "enable")
                        .Rpc((v, id, _) => ("Thermostat.SetConfig", new JObject { ["id"] = id, ["config"] = new JObject { ["enable"] = ToBool(v) } })),
                    new ShellyDataPoint("target_temperature", "TARGET_TEMPERATURE", NodeDataType.Double, "target_C")
                        .Rpc((v, id, _) => ("Thermostat.SetConfig", new JObject { ["id"] = id, ["config"] = new JObject { ["target_C"] = ToDouble(v) } })),
                    new ShellyDataPoint("temperature", "TEMPERATURE", NodeDataType.Double, "current_C"),
                    new ShellyDataPoint("output", "OUTPUT", NodeDataType.Boolean, "output")),

                // BLU devices (BTHome over BLE) through a Gen2+ gateway
                Keyed("bthomedevice", "BTHOMEDEVICE",
                    new ShellyDataPoint("battery", "BATTERY", NodeDataType.Integer, "battery"),
                    new ShellyDataPoint("rssi", "RSSI", NodeDataType.Integer, "rssi"),
                    new ShellyDataPoint("event", "EVENT", NodeDataType.String, null).Event()),
                Keyed("bthomesensor", "BTHOMESENSOR",
                    new ShellyDataPoint("value", "VALUE", NodeDataType.Double, "value").When((c, _) => c["value"]?.Type is JTokenType.Integer or JTokenType.Float),
                    new ShellyDataPoint("state", "STATE", NodeDataType.Boolean, "value").When((c, _) => c["value"]?.Type == JTokenType.Boolean)),
                Keyed("blutrv", "BLUTRV",
                    new ShellyDataPoint("temperature", "TEMPERATURE", NodeDataType.Double, "current_C"),
                    new ShellyDataPoint("target_temperature", "TARGET_TEMPERATURE", NodeDataType.Double, "target_C")
                        .Rpc((v, id, _) => ("BluTrv.Call", new JObject
                        {
                            ["id"] = id,
                            ["method"] = "TRV.SetTarget",
                            ["params"] = new JObject { ["id"] = 0, ["target_C"] = ToDouble(v) }
                        })),
                    new ShellyDataPoint("position", "VALVE_POSITION", NodeDataType.Integer, "pos"),
                    new ShellyDataPoint("battery", "BATTERY", NodeDataType.Integer, "battery"),
                    new ShellyDataPoint("rssi", "RSSI", NodeDataType.Integer, "rssi")),

                // virtual components (id 200..299)
                Keyed("number", "NUMBER",
                    new ShellyDataPoint("value", "VALUE", NodeDataType.Double, "value").When((c, _) => c["value"] != null)
                        .Rpc((v, id, _) => ("Number.Set", new JObject { ["id"] = id, ["value"] = ToDouble(v) }))),
                Keyed("boolean", "BOOLEAN",
                    new ShellyDataPoint("value", "STATE", NodeDataType.Boolean, "value").When((c, _) => c["value"] != null)
                        .Rpc((v, id, _) => ("Boolean.Set", new JObject { ["id"] = id, ["value"] = ToBool(v) }))),
                Keyed("text", "TEXT",
                    new ShellyDataPoint("value", "VALUE", NodeDataType.String, "value").When((c, _) => c["value"] != null)
                        .Rpc((v, id, _) => ("Text.Set", new JObject { ["id"] = id, ["value"] = v?.ToString() ?? "" }))),
                Keyed("enum", "ENUM",
                    new ShellyDataPoint("value", "VALUE", NodeDataType.String, "value").When((c, _) => c.Type == JTokenType.Object && ((JObject)c).ContainsKey("value"))
                        .Rpc((v, id, _) => ("Enum.Set", new JObject { ["id"] = id, ["value"] = v?.ToString() }))),
                Keyed("button", "BUTTON",
                    new ShellyDataPoint("trigger", "TRIGGER", NodeDataType.Boolean, null).Command()
                        .Rpc((v, id, _) => ToBool(v) ? ("Button.Trigger", new JObject { ["id"] = id, ["event"] = "single_push" }) : null),
                    new ShellyDataPoint("event", "EVENT", NodeDataType.String, null).Event()),

                Single("sys", "SYS",
                    new ShellyDataPoint("has_update", "HAS_UPDATE", NodeDataType.Boolean, "available_updates")
                        .Read(t => t is JObject o && o["stable"] != null)
                        .When((c, _) => c["available_updates"] != null),
                    new ShellyDataPoint("restart_required", "RESTART_REQUIRED", NodeDataType.Boolean, "restart_required"),
                    new ShellyDataPoint("uptime", "UPTIME", NodeDataType.Integer, "uptime"),
                    new ShellyDataPoint("reboot", "REBOOT", NodeDataType.Boolean, null).Command().Rpc((v, _, _) => ToBool(v) ? ("Shelly.Reboot", new JObject()) : null)),
                Single("wifi", "WIFI",
                    new ShellyDataPoint("rssi", "RSSI", NodeDataType.Integer, "rssi"))
            };

            ByTemplate = All.ToDictionary(a => a.Template);
            ByType = All.ToDictionary(a => a.Type);
        }

        public static ShellyComponentType? Get(Guid template) => ByTemplate.TryGetValue(template, out var c) ? c : null;

        public static ShellyComponentType? Get(string type) => ByType.TryGetValue(type, out var c) ? c : null;

        /// <summary>
        /// "switch:0" → ("switch", 0), "sys" → ("sys", 0)
        /// </summary>
        public static (string Type, int Id) SplitKey(string key)
        {
            var colon = key.IndexOf(':');
            if (colon < 0)
            {
                return (key, 0);
            }

            return int.TryParse(key.AsSpan(colon + 1), out var id) ? (key.Substring(0, colon), id) : (key, 0);
        }

        private static ShellyComponentType Keyed(string type, string i18n, params object[] dataPoints) =>
            new(ShellyApi.Rpc, type, i18n, ShellyGuid.Create(Namespace, type), ShellyComponentLayout.Keyed, Flatten(dataPoints));

        private static ShellyComponentType Single(string type, string i18n, params object[] dataPoints) =>
            new(ShellyApi.Rpc, type, i18n, ShellyGuid.Create(Namespace, type), ShellyComponentLayout.Single, Flatten(dataPoints));

        private static IEnumerable<ShellyDataPoint> Flatten(IEnumerable<object> items)
        {
            foreach (var item in items)
            {
                switch (item)
                {
                    case ShellyDataPoint dp:
                        yield return dp;
                        break;
                    case IEnumerable<ShellyDataPoint> list:
                        foreach (var dp in list)
                        {
                            yield return dp;
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// Power metering of switch, cover, light, pm1... (only created when the device reports it).
        /// </summary>
        private static IEnumerable<ShellyDataPoint> Metering()
        {
            yield return new ShellyDataPoint("power", "POWER", NodeDataType.Double, "apower");
            yield return new ShellyDataPoint("voltage", "VOLTAGE", NodeDataType.Double, "voltage");
            yield return new ShellyDataPoint("current", "CURRENT", NodeDataType.Double, "current");
            yield return new ShellyDataPoint("power_factor", "POWER_FACTOR", NodeDataType.Double, "pf");
            yield return new ShellyDataPoint("frequency", "FREQUENCY", NodeDataType.Double, "freq");
            yield return new ShellyDataPoint("energy", "ENERGY", NodeDataType.Double, "aenergy.total");
            yield return new ShellyDataPoint("returned_energy", "RETURNED_ENERGY", NodeDataType.Double, "ret_aenergy.total");
            yield return new ShellyDataPoint("device_temperature", "DEVICE_TEMPERATURE", NodeDataType.Double, "temperature.tC");
        }

        private static IEnumerable<ShellyDataPoint> Dimmable(string method)
        {
            yield return new ShellyDataPoint("output", "OUTPUT", NodeDataType.Boolean, "output")
                .Rpc((v, id, _) => (method, new JObject { ["id"] = id, ["on"] = ToBool(v) }));
            yield return new ShellyDataPoint("brightness", "BRIGHTNESS", NodeDataType.Integer, "brightness")
                .Rpc((v, id, _) => (method, new JObject { ["id"] = id, ["brightness"] = Math.Clamp(ToInt(v), 0, 100) }));
        }

        private static IEnumerable<ShellyDataPoint> Rgb(string method)
        {
            var names = new[] { ("red", "RED"), ("green", "GREEN"), ("blue", "BLUE") };
            for (var i = 0; i < names.Length; i++)
            {
                var channel = i;
                yield return new ShellyDataPoint(names[i].Item1, names[i].Item2, NodeDataType.Integer, $"rgb[{i}]")
                    .Rpc((v, id, status) =>
                    {
                        // RGB.Set needs all three channels: the others come from the last known status
                        var rgb = new JArray(0, 0, 0);
                        if (status?["rgb"] is JArray current && current.Count == 3)
                        {
                            rgb = new JArray(current[0], current[1], current[2]);
                        }
                        rgb[channel] = Math.Clamp(ToInt(v), 0, 255);
                        return (method, new JObject { ["id"] = id, ["rgb"] = rgb });
                    });
            }
        }

        private static IEnumerable<ShellyDataPoint> Phases(string field, string i18n)
        {
            foreach (var phase in new[] { "a", "b", "c" })
            {
                yield return new ShellyDataPoint($"{phase}_{field}", $"PHASE_{phase.ToUpperInvariant()}_{i18n}", NodeDataType.Double, $"{phase}_{field}");
            }
        }

        private static IEnumerable<ShellyDataPoint> ThreePhase()
        {
            foreach (var phase in new[] { "a", "b", "c" })
            {
                var p = phase.ToUpperInvariant();
                yield return new ShellyDataPoint($"{phase}_active_power", $"PHASE_{p}_POWER", NodeDataType.Double, $"{phase}_act_power");
                yield return new ShellyDataPoint($"{phase}_apparent_power", $"PHASE_{p}_APPARENT_POWER", NodeDataType.Double, $"{phase}_aprt_power");
                yield return new ShellyDataPoint($"{phase}_voltage", $"PHASE_{p}_VOLTAGE", NodeDataType.Double, $"{phase}_voltage");
                yield return new ShellyDataPoint($"{phase}_current", $"PHASE_{p}_CURRENT", NodeDataType.Double, $"{phase}_current");
                yield return new ShellyDataPoint($"{phase}_power_factor", $"PHASE_{p}_POWER_FACTOR", NodeDataType.Double, $"{phase}_pf");
                yield return new ShellyDataPoint($"{phase}_frequency", $"PHASE_{p}_FREQUENCY", NodeDataType.Double, $"{phase}_freq");
            }

            yield return new ShellyDataPoint("n_current", "NEUTRAL_CURRENT", NodeDataType.Double, "n_current");
            yield return new ShellyDataPoint("total_current", "TOTAL_CURRENT", NodeDataType.Double, "total_current");
            yield return new ShellyDataPoint("total_active_power", "TOTAL_POWER", NodeDataType.Double, "total_act_power");
            yield return new ShellyDataPoint("total_apparent_power", "TOTAL_APPARENT_POWER", NodeDataType.Double, "total_aprt_power");
        }
    }
}
