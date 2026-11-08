#nullable enable annotations
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json.Linq;
using NodeDataType = Automatica.Core.Base.Templates.NodeDataType;
using static Automatica.Driver.ShellyFactory.Catalog.ShellyValues;

namespace Automatica.Driver.ShellyFactory.Catalog
{
    /// <summary>
    /// Gen1 devices (https://shelly-api-docs.shelly.cloud/gen1/): a device is the set of sections in its GET /status answer
    /// (relays, rollers, meters, emeters, lights, inputs, adcs, thermostats, ext_* and the sensor values in the root).
    /// </summary>
    internal static class ShellyGen1Catalog
    {
        /// <summary>Namespace of the generated template ids, never change it (the ids are stored in the configuration)</summary>
        public static readonly Guid Namespace = new("b0f5d6c2-3e7a-4d1f-9c58-71a2e4f0b836");

        public static readonly IReadOnlyList<ShellyComponentType> All;
        private static readonly Dictionary<Guid, ShellyComponentType> ByTemplate;

        static ShellyGen1Catalog()
        {
            All = new List<ShellyComponentType>
            {
                Array("relays", "RELAY", new[] { "relay" },
                    new ShellyDataPoint("output", "OUTPUT", NodeDataType.Boolean, "ison").Co("output", "relay0", "relay1")
                        .Gen1((v, i, _) => $"/relay/{i}?turn={(ToBool(v) ? "on" : "off")}"),
                    new ShellyDataPoint("has_timer", "HAS_TIMER", NodeDataType.Boolean, "has_timer"),
                    new ShellyDataPoint("overpower", "OVERPOWER", NodeDataType.Boolean, "overpower").Co("overpower")),
                Array("rollers", "ROLLER", new[] { "roller" },
                    new ShellyDataPoint("state", "COVER_STATE", NodeDataType.String, "state").Co("roller"),
                    new ShellyDataPoint("position", "POSITION", NodeDataType.Integer, "current_pos").Co("rollerPos")
                        .Gen1((v, i, _) => $"/roller/{i}?go=to_pos&roller_pos={Math.Clamp(ToInt(v), 0, 100)}"),
                    new ShellyDataPoint("open", "OPEN", NodeDataType.Boolean, null).Command().Gen1((v, i, _) => ToBool(v) ? $"/roller/{i}?go=open" : null),
                    new ShellyDataPoint("close", "CLOSE", NodeDataType.Boolean, null).Command().Gen1((v, i, _) => ToBool(v) ? $"/roller/{i}?go=close" : null),
                    new ShellyDataPoint("stop", "STOP", NodeDataType.Boolean, null).Command().Gen1((v, i, _) => ToBool(v) ? $"/roller/{i}?go=stop" : null),
                    new ShellyDataPoint("power", "POWER", NodeDataType.Double, "power").Co("rollerPower"),
                    new ShellyDataPoint("stop_reason", "STOP_REASON", NodeDataType.String, "stop_reason").Co("rollerStopReason")),
                Array("meters", "METER", new[] { "relay", "light", "roller", "device" },
                    new ShellyDataPoint("power", "POWER", NodeDataType.Double, "power").Co("power"),
                    // Gen1 meters count watt-minutes, the data point is in Wh like the Gen2+ ones
                    new ShellyDataPoint("energy", "ENERGY", NodeDataType.Double, "total").Read(t => ToDouble(t) / 60d).Co("energy"),
                    new ShellyDataPoint("overpower_value", "OVERPOWER_VALUE", NodeDataType.Double, "overpower").When((c, _) => c["overpower"]?.Type is JTokenType.Integer or JTokenType.Float)),
                Array("emeters", "EMETER", new[] { "emeter" },
                    new ShellyDataPoint("power", "POWER", NodeDataType.Double, "power").Co("power"),
                    new ShellyDataPoint("reactive_power", "REACTIVE_POWER", NodeDataType.Double, "reactive").Co("reactivePower"),
                    new ShellyDataPoint("power_factor", "POWER_FACTOR", NodeDataType.Double, "pf").Co("powerFactor"),
                    new ShellyDataPoint("voltage", "VOLTAGE", NodeDataType.Double, "voltage").Co("voltage"),
                    new ShellyDataPoint("current", "CURRENT", NodeDataType.Double, "current").Co("current"),
                    new ShellyDataPoint("energy", "ENERGY", NodeDataType.Double, "total").Co("energy"),
                    new ShellyDataPoint("returned_energy", "RETURNED_ENERGY", NodeDataType.Double, "total_returned").Co("returnedEnergy")),
                Array("lights", "LIGHT", new[] { "light" },
                    new ShellyDataPoint("output", "OUTPUT", NodeDataType.Boolean, "ison").Co("output")
                        .Gen1((v, i, c) => $"{LightPath(c, i)}?turn={(ToBool(v) ? "on" : "off")}"),
                    new ShellyDataPoint("brightness", "BRIGHTNESS", NodeDataType.Integer, "brightness").Co("brightness")
                        .Gen1((v, i, c) => $"{LightPath(c, i)}?brightness={Math.Clamp(ToInt(v), 0, 100)}"),
                    new ShellyDataPoint("gain", "GAIN", NodeDataType.Integer, "gain").Co("gain")
                        .Gen1((v, i, c) => $"{LightPath(c, i)}?gain={Math.Clamp(ToInt(v), 0, 100)}"),
                    Channel("red", "RED"),
                    Channel("green", "GREEN"),
                    Channel("blue", "BLUE"),
                    new ShellyDataPoint("white", "WHITE", NodeDataType.Integer, "white").Co("white", "whiteLevel")
                        .Gen1((v, i, c) => $"{LightPath(c, i)}?white={Math.Clamp(ToInt(v), 0, IsDuo(c) ? 100 : 255)}"),
                    new ShellyDataPoint("color_temperature", "COLOR_TEMPERATURE", NodeDataType.Integer, "temp").Co("colorTemp")
                        .Gen1((v, i, c) => $"{LightPath(c, i)}?temp={ToInt(v)}"),
                    new ShellyDataPoint("power", "POWER", NodeDataType.Double, "power").Co("power"),
                    new ShellyDataPoint("overpower", "OVERPOWER", NodeDataType.Boolean, "overpower").Co("overpower")),
                Array("inputs", "INPUT", new[] { "input", "relay", "device" },
                    new ShellyDataPoint("state", "STATE", NodeDataType.Boolean, "input").Co("input"),
                    new ShellyDataPoint("event", "EVENT", NodeDataType.String, "event").Co("inputEvent"),
                    new ShellyDataPoint("event_count", "EVENT_COUNT", NodeDataType.Integer, "event_cnt").Co("inputEventCnt")),
                Array("adcs", "ADC", new[] { "adc", "sensor" },
                    new ShellyDataPoint("voltage", "VOLTAGE", NodeDataType.Double, "voltage").Co("adc")),
                Array("thermostats", "THERMOSTAT", new[] { "sensor", "device" },
                    new ShellyDataPoint("temperature", "TEMPERATURE", NodeDataType.Double, "tmp.value").Co("temp"),
                    new ShellyDataPoint("target_temperature", "TARGET_TEMPERATURE", NodeDataType.Double, "target_t.value").Co("targetTemp")
                        .Gen1((v, i, _) => $"/thermostats/{i}?target_t={Num(ToDouble(v))}"),
                    new ShellyDataPoint("position", "VALVE_POSITION", NodeDataType.Integer, "pos").Co("valvePos", "valvePosition")
                        .Gen1((v, i, _) => $"/thermostats/{i}?pos={Math.Clamp(ToInt(v), 0, 100)}"),
                    new ShellyDataPoint("window_open", "WINDOW_OPEN", NodeDataType.Boolean, "window_open").Co("windowOpen")),
                Map("ext_temperature", "EXT_TEMPERATURE",
                    new ShellyDataPoint("temperature", "TEMPERATURE", NodeDataType.Double, "tC").Co("extTemp")),
                Map("ext_humidity", "EXT_HUMIDITY",
                    new ShellyDataPoint("humidity", "HUMIDITY", NodeDataType.Double, "hum").Co("extHumidity")),
                Map("ext_switch", "EXT_SWITCH",
                    new ShellyDataPoint("state", "STATE", NodeDataType.Boolean, "input").Co("extInput")),

                // battery sensors (H&T, Flood, Door/Window, Smoke, Gas, Motion, Sense, Button) report in the status root
                Root("sensors", "SENSORS",
                    new ShellyDataPoint("temperature", "TEMPERATURE", NodeDataType.Double, "tmp.tC")
                        .When((c, _) => c.SelectToken("tmp.tC") != null && c["temperature"] == null && c["thermostats"] == null).Co("temperature", "extTemp"),
                    new ShellyDataPoint("humidity", "HUMIDITY", NodeDataType.Double, "hum.value").Co("humidity"),
                    new ShellyDataPoint("illuminance", "ILLUMINANCE", NodeDataType.Double, "lux.value").Co("luminosity"),
                    new ShellyDataPoint("illumination", "ILLUMINATION", NodeDataType.String, "lux.illumination").Co("illumination"),
                    new ShellyDataPoint("door_open", "DOOR_OPEN", NodeDataType.Boolean, "sensor.state")
                        .When((c, _) => c.SelectToken("sensor.state")?.Type == JTokenType.String).Co("dwIsOpened", "state"),
                    new ShellyDataPoint("motion", "MOTION", NodeDataType.Boolean, "sensor.motion").Co("motion"),
                    new ShellyDataPoint("vibration", "VIBRATION", NodeDataType.Boolean, "sensor.vibration").Co("vibration"),
                    new ShellyDataPoint("tilt", "TILT", NodeDataType.Integer, "accel.tilt").Co("tilt"),
                    new ShellyDataPoint("accel_vibration", "VIBRATION", NodeDataType.Boolean, "accel.vibration")
                        .When((c, _) => c.SelectToken("accel.vibration") != null && c.SelectToken("sensor.vibration") == null).Read(t => ToDouble(t) > 0).Co("vibration"),
                    new ShellyDataPoint("flood", "FLOOD", NodeDataType.Boolean, "flood").Co("flood"),
                    new ShellyDataPoint("smoke", "SMOKE", NodeDataType.Boolean, "smoke").Co("smoke"),
                    new ShellyDataPoint("gas_alarm", "GAS_ALARM", NodeDataType.String, "gas_sensor.alarm_state").Co("gas"),
                    new ShellyDataPoint("gas_concentration", "GAS_CONCENTRATION", NodeDataType.Double, "concentration.ppm").Co("concentration"),
                    new ShellyDataPoint("battery", "BATTERY", NodeDataType.Integer, "bat.value").Co("battery"),
                    new ShellyDataPoint("battery_voltage", "BATTERY_VOLTAGE", NodeDataType.Double, "bat.voltage"),
                    new ShellyDataPoint("charger", "EXTERNAL_POWER", NodeDataType.Boolean, "charger").Co("charger")),
                Root("device", "DEVICE",
                    new ShellyDataPoint("has_update", "HAS_UPDATE", NodeDataType.Boolean, "update.has_update"),
                    new ShellyDataPoint("rssi", "RSSI", NodeDataType.Integer, "wifi_sta.rssi"),
                    new ShellyDataPoint("device_temperature", "DEVICE_TEMPERATURE", NodeDataType.Double, "temperature")
                        .When((c, _) => c["temperature"]?.Type is JTokenType.Integer or JTokenType.Float).Co("deviceTemp"),
                    new ShellyDataPoint("overtemperature", "OVERTEMPERATURE", NodeDataType.Boolean, "overtemperature").Co("overtemp"),
                    new ShellyDataPoint("supply_voltage", "VOLTAGE", NodeDataType.Double, "voltage")
                        .When((c, _) => c["voltage"]?.Type is JTokenType.Integer or JTokenType.Float).Co("voltage"),
                    new ShellyDataPoint("reboot", "REBOOT", NodeDataType.Boolean, null).Command().Gen1((v, _, _) => ToBool(v) ? "/reboot" : null))
            };

            ByTemplate = All.ToDictionary(a => a.Template);
        }

        public static ShellyComponentType? Get(Guid template) => ByTemplate.TryGetValue(template, out var c) ? c : null;

        /// <summary>
        /// Gen1 light URLs: the RGBW2 uses /color/{i} in color mode and /white/{i} in white mode, every other light
        /// (Dimmer, Bulb, Duo, Vintage) /light/{i} (the Bulb takes red/green/blue/white/gain/temp there too).
        /// </summary>
        internal static string LightPath(Gen1WriteContext context, int index)
        {
            var type = context.DeviceType?.ToUpperInvariant() ?? "";
            var mode = context.Mode?.ToLowerInvariant() ?? context.ComponentStatus?.Value<string>("mode")?.ToLowerInvariant();
            var colorDevice = type.StartsWith("SHRGBW2", StringComparison.Ordinal);

            if (colorDevice && mode == "color")
            {
                return $"/color/{index}";
            }

            if (colorDevice && mode == "white")
            {
                return $"/white/{index}";
            }

            return $"/light/{index}";
        }

        private static bool IsDuo(Gen1WriteContext context) => context.DeviceType?.StartsWith("SHBDUO", StringComparison.OrdinalIgnoreCase) == true;

        private static ShellyDataPoint Channel(string key, string i18n) =>
            new ShellyDataPoint(key, i18n, NodeDataType.Integer, key).Co(key)
                .Gen1((v, i, c) => $"{LightPath(c, i)}?{key}={Math.Clamp(ToInt(v), 0, 255).ToString(CultureInfo.InvariantCulture)}");

        private static ShellyComponentType Array(string section, string i18n, string[] blocks, params ShellyDataPoint[] dataPoints) =>
            new(ShellyApi.Gen1, section, i18n, ShellyGuid.Create(Namespace, section), ShellyComponentLayout.Array, dataPoints) { CoIoTBlocks = blocks };

        private static ShellyComponentType Map(string section, string i18n, params ShellyDataPoint[] dataPoints) =>
            new(ShellyApi.Gen1, section, i18n, ShellyGuid.Create(Namespace, section), ShellyComponentLayout.Map, dataPoints) { CoIoTBlocks = new[] { "sensor" } };

        private static ShellyComponentType Root(string name, string i18n, params ShellyDataPoint[] dataPoints) =>
            new(ShellyApi.Gen1, name, i18n, ShellyGuid.Create(Namespace, name.Length == 0 ? "sensors" : name), ShellyComponentLayout.Root, dataPoints);
    }
}
