using System;
using System.Collections.Generic;
using System.Text.Json;
using NodeDataType = Automatica.Core.Base.Templates.NodeDataType;

namespace P3.Driver.Miele.Driver
{
    /// <summary>
    /// How a value is written: PUT /devices/{id}/actions or /devices/{id}/programs with the body
    /// </summary>
    internal sealed record MieleWrite(string Endpoint, object Body);

    /// <summary>
    /// A data point of an appliance: read from the state object, written as action or program
    /// </summary>
    internal sealed record MieleDataPoint(Guid Template, string Key, string I18N, NodeDataType Type, Func<JsonElement, object> Read, Func<object, MieleWrite> Write = null)
    {
        public bool IsWriteable => Write != null;
    }

    internal static class MieleDataPoints
    {
        public const int NoTemperature = -32768;

        public static readonly IReadOnlyDictionary<int, string> StatusNames = new Dictionary<int, string>
        {
            [1] = "Off", [2] = "On", [3] = "Programmed", [4] = "WaitingToStart", [5] = "Running", [6] = "Pause", [7] = "EndProgrammed",
            [8] = "Failure", [9] = "ProgrammeInterrupted", [10] = "Idle", [11] = "RinseHold", [12] = "Service", [13] = "Superfreezing",
            [14] = "Supercooling", [15] = "Superheating", [146] = "SupercoolingSuperfreezing", [255] = "NotConnected"
        };

        private static MieleWrite Action(object body) => new("actions", body);

        public static readonly IReadOnlyList<MieleDataPoint> All = new List<MieleDataPoint>
        {
            new(new Guid("95530d46-85cf-4f8d-8607-18ede136d403"), "connected", "CONNECTED", NodeDataType.Boolean, s => Status(s) is { } c ? c != 255 : null),
            new(new Guid("8579dd26-7f48-43c9-8b43-42e2b0609bce"), "status_code", "STATUS_CODE", NodeDataType.Integer, s => Status(s)),
            new(new Guid("79c0aeba-8c14-481a-b92f-76e5df3f684d"), "status", "STATUS", NodeDataType.String,
                s => Status(s) is { } c ? StatusNames.TryGetValue(c, out var n) ? n : c.ToString() : null),
            new(new Guid("663894f7-5e8c-4822-ad22-79e34fc87b70"), "power", "POWER", NodeDataType.Boolean, s => Status(s) is { } c ? c is not (1 or 255) : null,
                v => Action(Conv.ToBool(v) ? new { powerOn = true } : new { powerOff = true })),
            new(new Guid("0de5c0a2-b7c2-4de1-bb00-ee48d6f75462"), "running", "RUNNING", NodeDataType.Boolean, s => Status(s) is { } c ? c == 5 : null,
                v => Action(new { processAction = Conv.ToBool(v) ? 1 : 2 })),
            new(new Guid("b717b2ec-dcc4-4579-a04b-d4d16ffb97cb"), "paused", "PAUSED", NodeDataType.Boolean, s => Status(s) is { } c ? c == 6 : null,
                v => Action(new { processAction = Conv.ToBool(v) ? 3 : 1 })),
            new(new Guid("aff56a24-d088-41f9-a1ef-2eaffd8cd66f"), "program", "PROGRAM", NodeDataType.String, s => Localized(s, "ProgramID")),
            new(new Guid("54cb782f-3b68-47ed-b6b4-baac91d6834b"), "program_id", "PROGRAM_ID", NodeDataType.Integer, s => Conv.Int(s, "ProgramID", "value_raw"),
                v => new MieleWrite("programs", new { programId = (int)Math.Round(Conv.ToDouble(v)) })),
            new(new Guid("5c135b93-f9fe-4c8a-a066-3cf1f991f7dd"), "program_phase", "PROGRAM_PHASE", NodeDataType.String, s => Localized(s, "programPhase")),
            new(new Guid("50e9f2a7-9a20-42d0-bbcf-3283fb5ce334"), "remaining_time", "REMAINING_TIME", NodeDataType.Integer, s => Minutes(s, "remainingTime")),
            new(new Guid("3a2db473-4c83-4406-98a8-638d84186ced"), "elapsed_time", "ELAPSED_TIME", NodeDataType.Integer, s => Minutes(s, "elapsedTime")),
            new(new Guid("3fa5cb9e-e32a-4bb9-8fad-f22319689831"), "start_time", "START_TIME", NodeDataType.Integer, s => Minutes(s, "startTime"),
                v => Action(new { startTime = HoursMinutes(Conv.ToDouble(v)) })),
            new(new Guid("840bd77f-e0f0-4e1f-b345-cd2ffea02cbe"), "temperature", "TEMPERATURE", NodeDataType.Double, s => Temperature(s, "temperature", 0)),
            new(new Guid("afac051a-bfba-4dad-a722-e5aa70653b91"), "temperature_2", "TEMPERATURE_2", NodeDataType.Double, s => Temperature(s, "temperature", 1)),
            new(new Guid("4f35efab-46a6-49b1-aee4-40d912d8a1c5"), "target_temperature", "TARGET_TEMPERATURE", NodeDataType.Double, s => Temperature(s, "targetTemperature", 0),
                v => Action(new { targetTemperature = new[] { new { zone = 1, value = (int)Math.Round(Conv.ToDouble(v)) } } })),
            new(new Guid("812d4619-759a-467c-95ff-858847d87b2d"), "target_temperature_2", "TARGET_TEMPERATURE_2", NodeDataType.Double, s => Temperature(s, "targetTemperature", 1),
                v => Action(new { targetTemperature = new[] { new { zone = 2, value = (int)Math.Round(Conv.ToDouble(v)) } } })),
            new(new Guid("172e5db3-7728-41b7-be85-7abec0969348"), "core_temperature", "CORE_TEMPERATURE", NodeDataType.Double, s => Temperature(s, "coreTemperature", 0)),
            new(new Guid("5c1c3fdb-2028-4473-adbf-6700c192ad7e"), "door_open", "DOOR_OPEN", NodeDataType.Boolean, s => Conv.Bool(s, "signalDoor")),
            new(new Guid("0063dc53-23df-4311-aa6d-84d7f1c137eb"), "signal_info", "SIGNAL_INFO", NodeDataType.Boolean, s => Conv.Bool(s, "signalInfo")),
            new(new Guid("67c38b6b-3709-47a3-959d-625cab004d70"), "signal_failure", "SIGNAL_FAILURE", NodeDataType.Boolean, s => Conv.Bool(s, "signalFailure")),
            new(new Guid("dcb9e6d2-77ef-4703-a58e-7f2f205159c6"), "remote_control", "REMOTE_CONTROL", NodeDataType.Boolean, s => Conv.Bool(s, "remoteEnable", "fullRemoteControl")),
            new(new Guid("37684f7c-d20e-4c63-864b-c237bbacff34"), "mobile_start", "MOBILE_START", NodeDataType.Boolean, s => Conv.Bool(s, "remoteEnable", "mobileStart")),
            new(new Guid("5300301d-e729-44c5-a822-53898a56151c"), "smart_grid", "SMART_GRID", NodeDataType.Boolean, s => Conv.Bool(s, "remoteEnable", "smartGrid")),
            new(new Guid("60e41cf1-14eb-40bf-992c-3542a81b223f"), "light", "LIGHT", NodeDataType.Boolean, s => Conv.Int(s, "light") switch { 1 => true, 2 => false, _ => null },
                v => Action(new { light = Conv.ToBool(v) ? 1 : 2 })),
            new(new Guid("7a78d648-bb39-4eb0-9d07-7c8b7bee4b86"), "superfreezing", "SUPERFREEZING", NodeDataType.Boolean, s => Status(s) is { } c ? c is 13 or 146 : null,
                v => Action(new { processAction = Conv.ToBool(v) ? 4 : 5 })),
            new(new Guid("26044d0d-5bf1-4ef1-afba-9cb75c7aff44"), "supercooling", "SUPERCOOLING", NodeDataType.Boolean, s => Status(s) is { } c ? c is 14 or 146 : null,
                v => Action(new { processAction = Conv.ToBool(v) ? 6 : 7 })),
            new(new Guid("5c07f2cc-655b-4a81-b032-3c8098d1ea90"), "ventilation_step", "VENTILATION_STEP", NodeDataType.Integer, s => Conv.Int(s, "ventilationStep", "value_raw"),
                v => Action(new { ventilationStep = Math.Clamp((int)Math.Round(Conv.ToDouble(v)), 0, 4) })),
            new(new Guid("130c6b53-9c48-46fb-a103-89f8a945add4"), "spinning_speed", "SPINNING_SPEED", NodeDataType.Integer, s => Conv.Int(s, "spinningSpeed", "value_raw")),
            new(new Guid("58068351-3441-47e9-ad50-a7390ae122d7"), "drying_step", "DRYING_STEP", NodeDataType.String, s => Localized(s, "dryingStep")),
            new(new Guid("43d90710-f6be-4df2-a438-43f03b4488d3"), "energy_consumption", "ENERGY_CONSUMPTION", NodeDataType.Double, s => Conv.Num(s, "ecoFeedback", "currentEnergyConsumption", "value")),
            new(new Guid("2ad70cd7-31be-4fc7-bf02-c35f71f58f79"), "water_consumption", "WATER_CONSUMPTION", NodeDataType.Double, s => Conv.Num(s, "ecoFeedback", "currentWaterConsumption", "value")),
            new(new Guid("9d3a0fce-609a-4a0a-9ae5-dab58a14bb11"), "battery_level", "BATTERY_LEVEL", NodeDataType.Integer, s => Conv.Int(s, "batteryLevel")),
        };

        private static int? Status(JsonElement state) => Conv.Int(state, "status", "value_raw");

        private static string Localized(JsonElement state, string name)
        {
            var raw = Conv.Path(state, name, "value_raw");
            if (raw == null)
            {
                return null;
            }
            return Conv.Str(state, name, "value_localized") ?? "";
        }

        /// <summary>
        /// [h, m] → minutes
        /// </summary>
        internal static object Minutes(JsonElement state, string name)
        {
            var v = Conv.Path(state, name);
            if (v is not { ValueKind: JsonValueKind.Array } array || array.GetArrayLength() < 2)
            {
                return null;
            }
            return array[0].GetInt32() * 60 + array[1].GetInt32();
        }

        internal static int[] HoursMinutes(double minutes)
        {
            var total = Math.Max(0, (int)Math.Round(minutes));
            return new[] { total / 60, total % 60 };
        }

        /// <summary>
        /// value_raw is in 1/100 °C, -32768 = no value
        /// </summary>
        internal static object Temperature(JsonElement state, string name, int index)
        {
            var v = Conv.Path(state, name);
            if (v is not { ValueKind: JsonValueKind.Array } array || array.GetArrayLength() <= index)
            {
                return null;
            }
            var raw = Conv.Int(array[index], "value_raw");
            if (raw is null or NoTemperature)
            {
                return null;
            }
            return Math.Round(raw.Value / 100d, 2);
        }
    }
}
