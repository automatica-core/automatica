#nullable enable annotations
using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Automatica.Driver.Shelly.CoIoT
{
    /// <summary>
    /// One sensor value of a CoIoT status ("G": [[channel, sensorId, value], ...]).
    /// </summary>
    public sealed record CoIoTValue(int Channel, int SensorId, JToken Value);

    /// <summary>
    /// A Gen1 CoIoT (CoAP) status packet, multicast to 224.0.1.187:5683 by every Gen1 device and by the sleeping ones when they wake up.
    /// Option 3332 is "TYPE#DEVICEID#VERSION", e.g. "SHHT-1#A4CF12F3D2B1#2".
    /// https://shelly-api-docs.shelly.cloud/gen1/#coiot-protocol
    /// </summary>
    public sealed class CoIoTMessage
    {
        public const int GlobalDeviceIdOption = 3332;
        public const int StatusValidityOption = 3412;
        public const int StatusSerialOption = 3420;

        public int Code { get; private init; }
        public string? DeviceType { get; private init; }
        public string? DeviceId { get; private init; }
        public int ProtocolVersion { get; private init; }
        public int? Serial { get; private init; }
        public string? UriPath { get; private init; }
        public IReadOnlyList<CoIoTValue> Values { get; private init; } = Array.Empty<CoIoTValue>();
        public string Payload { get; private init; } = "";

        public bool IsStatus => UriPath == "cit/s" || Code == 30;

        /// <summary>
        /// Parses a CoAP packet, null if it is no CoIoT packet of a Shelly.
        /// </summary>
        public static CoIoTMessage? Parse(ReadOnlySpan<byte> data)
        {
            if (data.Length < 4 || data[0] >> 6 != 1)
            {
                return null;
            }

            var tokenLength = data[0] & 0x0F;
            var code = data[1];
            var index = 4 + tokenLength;
            if (index > data.Length)
            {
                return null;
            }

            var option = 0;
            string? globalId = null;
            int? serial = null;
            var path = new List<string>();

            while (index < data.Length && data[index] != 0xFF)
            {
                var header = data[index++];
                var delta = header >> 4;
                var length = header & 0x0F;

                if (!ReadExtended(data, ref index, ref delta) || !ReadExtended(data, ref index, ref length))
                {
                    return null;
                }

                if (index + length > data.Length)
                {
                    return null;
                }

                option += delta;
                var value = data.Slice(index, length);
                index += length;

                switch (option)
                {
                    case 11: // Uri-Path
                        path.Add(Encoding.UTF8.GetString(value));
                        break;
                    case GlobalDeviceIdOption:
                        globalId = Encoding.UTF8.GetString(value);
                        break;
                    case StatusSerialOption:
                        var s = 0;
                        foreach (var b in value)
                        {
                            s = (s << 8) | b;
                        }
                        serial = s;
                        break;
                }
            }

            if (globalId == null)
            {
                return null;
            }

            var payload = index < data.Length ? Encoding.UTF8.GetString(data.Slice(index + 1)) : "";
            var parts = globalId.Split('#');

            return new CoIoTMessage
            {
                Code = code,
                DeviceType = parts.Length > 0 ? parts[0] : null,
                DeviceId = parts.Length > 1 ? parts[1] : null,
                ProtocolVersion = parts.Length > 2 && int.TryParse(parts[2], out var v) ? v : 1,
                Serial = serial,
                UriPath = path.Count > 0 ? string.Join("/", path) : null,
                Payload = payload,
                Values = ParseValues(payload)
            };
        }

        private static bool ReadExtended(ReadOnlySpan<byte> data, ref int index, ref int value)
        {
            switch (value)
            {
                case 13:
                    if (index >= data.Length)
                    {
                        return false;
                    }
                    value = data[index++] + 13;
                    return true;
                case 14:
                    if (index + 1 >= data.Length)
                    {
                        return false;
                    }
                    value = ((data[index] << 8) | data[index + 1]) + 269;
                    index += 2;
                    return true;
                case 15:
                    return false;
                default:
                    return true;
            }
        }

        private static IReadOnlyList<CoIoTValue> ParseValues(string payload)
        {
            if (string.IsNullOrWhiteSpace(payload))
            {
                return Array.Empty<CoIoTValue>();
            }

            try
            {
                var json = JObject.Parse(payload);
                if (json["G"] is not JArray g)
                {
                    return Array.Empty<CoIoTValue>();
                }

                var values = new List<CoIoTValue>();
                foreach (var entry in g)
                {
                    if (entry is JArray { Count: >= 3 } a && a[0].Type == JTokenType.Integer && a[1].Type == JTokenType.Integer)
                    {
                        values.Add(new CoIoTValue(a[0].Value<int>(), a[1].Value<int>(), a[2]));
                    }
                }
                return values;
            }
            catch (JsonException)
            {
                return Array.Empty<CoIoTValue>();
            }
        }

        /// <summary>
        /// Builds a status packet (used by the tests and the console tool to play a device).
        /// </summary>
        public static byte[] CreateStatusPacket(string deviceType, string deviceId, int serial, string payload)
        {
            var bytes = new List<byte> { 0x50, 30, 0x00, 0x01 }; // ver 1, NON, no token, code 0.30, message id 1
            var last = 0;

            void AddOption(int number, byte[] value)
            {
                var delta = number - last;
                last = number;
                var deltaNibble = delta < 13 ? delta : delta < 269 ? 13 : 14;
                var lengthNibble = value.Length < 13 ? value.Length : value.Length < 269 ? 13 : 14;
                bytes.Add((byte)((deltaNibble << 4) | lengthNibble));
                AddExtended(delta, deltaNibble);
                AddExtended(value.Length, lengthNibble);
                bytes.AddRange(value);
            }

            void AddExtended(int value, int nibble)
            {
                if (nibble == 13)
                {
                    bytes.Add((byte)(value - 13));
                }
                else if (nibble == 14)
                {
                    var v = value - 269;
                    bytes.Add((byte)(v >> 8));
                    bytes.Add((byte)(v & 0xFF));
                }
            }

            AddOption(11, Encoding.UTF8.GetBytes("cit"));
            AddOption(11, Encoding.UTF8.GetBytes("s"));
            AddOption(GlobalDeviceIdOption, Encoding.UTF8.GetBytes($"{deviceType}#{deviceId}#2"));
            AddOption(StatusValidityOption, new byte[] { 0x0E, 0x10 });
            AddOption(StatusSerialOption, new[] { (byte)(serial >> 8), (byte)(serial & 0xFF) });
            bytes.Add(0xFF);
            bytes.AddRange(Encoding.UTF8.GetBytes(payload));
            return bytes.ToArray();
        }
    }
}
