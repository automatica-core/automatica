using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using P3.Driver.Loxone.Miniserver.Driver;
using P3.Driver.Loxone.Miniserver.Driver.Data.Message;

namespace P3.Driver.Loxone.Miniserver.Tests
{
    /// <summary>
    /// Builds Loxone binary frames (8 byte message header + event tables) the way the miniserver sends them.
    /// </summary>
    internal static class LoxoneFrames
    {
        public const int MaxBinaryMessageLength = 16 * 1024 * 1024;

        // raw uuid bytes and their string form as the driver formats them
        public static readonly byte[] UuidA = { 0x10, 0x41, 0x26, 0x16, 0x01, 0x92, 0x07, 0x42, 0xff, 0xff, 0xc9, 0x26, 0xa2, 0x6a, 0x8d, 0x21 };
        public const string UuidAString = "16264110-9201-4207-ffffc926a26a8d21";

        public static readonly byte[] UuidB = { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f, 0x10 };
        public const string UuidBString = "04030201-0605-0807-090a0b0c0d0e0f10";

        public static readonly byte[] UuidC = { 0xaa, 0xbb, 0xcc, 0xdd, 0xee, 0xff, 0x00, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88, 0x99 };
        public const string UuidCString = "ddccbbaa-ffee-1100-2233445566778899";

        public static byte[] HeaderBytes(HeaderIdentifier identifier, uint length, byte info = 0)
        {
            return new byte[] { 0x03, (byte)identifier, info, 0 }.Concat(BitConverter.GetBytes(length)).ToArray();
        }

        public static byte[] EstimatedHeader(HeaderIdentifier identifier, uint length)
        {
            return HeaderBytes(identifier, length, 0x80);
        }

        public static byte[] ValueState(byte[] uuid, double value)
        {
            return uuid.Concat(BitConverter.GetBytes(value)).ToArray();
        }

        public static byte[] TextState(byte[] uuid, byte[] iconUuid, string text)
        {
            var textBytes = Encoding.UTF8.GetBytes(text);
            var padding = textBytes.Length % 4 == 0 ? 0 : 4 - textBytes.Length % 4;
            return uuid.Concat(iconUuid)
                .Concat(BitConverter.GetBytes((uint)textBytes.Length))
                .Concat(textBytes)
                .Concat(new byte[padding])
                .ToArray();
        }

        public static byte[] DaytimerEntryBytes(int mode, int from, int to, int needActivate, double value)
        {
            return BitConverter.GetBytes(mode)
                .Concat(BitConverter.GetBytes(from))
                .Concat(BitConverter.GetBytes(to))
                .Concat(BitConverter.GetBytes(needActivate))
                .Concat(BitConverter.GetBytes(value))
                .ToArray();
        }

        public static byte[] DaytimerBytes(byte[] uuid, double defaultValue, params byte[][] entries)
        {
            return DaytimerWithCount(uuid, defaultValue, entries.Length, entries);
        }

        public static byte[] DaytimerWithCount(byte[] uuid, double defaultValue, int count, params byte[][] entries)
        {
            return uuid.Concat(BitConverter.GetBytes(defaultValue))
                .Concat(BitConverter.GetBytes(count))
                .Concat(entries.SelectMany(a => a))
                .ToArray();
        }

        public static byte[] WeatherEntryBytes(int timestamp, int weatherType, int windDirection, int solarRadiation, int relativeHumidity,
            double temperature, double perceivedTemperature, double dewPoint, double precipitation, double windSpeed, double pressure)
        {
            return BitConverter.GetBytes(timestamp)
                .Concat(BitConverter.GetBytes(weatherType))
                .Concat(BitConverter.GetBytes(windDirection))
                .Concat(BitConverter.GetBytes(solarRadiation))
                .Concat(BitConverter.GetBytes(relativeHumidity))
                .Concat(BitConverter.GetBytes(temperature))
                .Concat(BitConverter.GetBytes(perceivedTemperature))
                .Concat(BitConverter.GetBytes(dewPoint))
                .Concat(BitConverter.GetBytes(precipitation))
                .Concat(BitConverter.GetBytes(windSpeed))
                .Concat(BitConverter.GetBytes(pressure))
                .ToArray();
        }

        public static byte[] WeatherBytes(byte[] uuid, uint lastUpdate, int count, params byte[][] entries)
        {
            return uuid.Concat(BitConverter.GetBytes(lastUpdate))
                .Concat(BitConverter.GetBytes(count))
                .Concat(entries.SelectMany(a => a))
                .ToArray();
        }

        /// <summary>header + payload as one byte array</summary>
        public static byte[] Message(HeaderIdentifier identifier, byte[] payload)
        {
            return Concat(HeaderBytes(identifier, (uint)payload.Length), payload);
        }

        public static byte[] ValueMessage(params (byte[] Uuid, double Value)[] values)
        {
            return Message(HeaderIdentifier.EventTableOfValueStates, values.SelectMany(a => ValueState(a.Uuid, a.Value)).ToArray());
        }

        public static byte[] Concat(params byte[][] parts)
        {
            var result = new byte[parts.Sum(a => a.Length)];
            var offset = 0;
            foreach (var part in parts)
            {
                Buffer.BlockCopy(part, 0, result, offset, part.Length);
                offset += part.Length;
            }
            return result;
        }

        public static (LoxoneMiniserverConnection Connection, List<BinaryMessage> Messages) CreateConnection()
        {
            var connection = new LoxoneMiniserverConnection("127.0.0.1", 80, "user", "password", NullLogger.Instance);
            var messages = new List<BinaryMessage>();
            connection.OnMessage += (sender, args) => messages.Add(args.Message);
            return (connection, messages);
        }

        public static double SingleValue(BinaryMessage message)
        {
            var valueStates = (EventTableOfValueStates)message;
            return valueStates.Values.Single().Value;
        }
    }
}
