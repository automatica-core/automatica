using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Automatica.Core.Base.Templates;
using static P3.Driver.Knx.DriverFactory.Dpt.KnxDptField;

namespace P3.Driver.Knx.DriverFactory.Dpt
{
    /// <summary>
    /// Codecs for the datapoint types of KNX standard 3.7.2 that the driver did not support before.
    /// The layouts follow the KNX master data (fields MSB first).
    /// </summary>
    internal static class KnxDptCodecs
    {
        private const string V = KnxDptCodec.SingleValueKey;

        // 4.001 ASCII / 4.002 ISO 8859-1 character
        public static IKnxDptCodec Character(int subType) => new KnxCharacterCodec(subType == 2);

        // 12.xxx 4-byte unsigned
        public static IKnxDptCodec Unsigned32() => KnxDptCodec.Single(U(V, 32));

        // 14.xxx 4-byte float
        public static IKnxDptCodec Float32() => KnxDptCodec.Single(F32(V));

        // 17.001 scene number (2 reserved bits + 6 bit)
        public static IKnxDptCodec SceneNumber() => KnxDptCodec.Single(2, U(V, 6));

        // 20.xxx 1-byte enumeration
        public static IKnxDptCodec Enum8() => KnxDptCodec.Single(U(V, 8));

        // 23.xxx 2-bit enumeration
        public static IKnxDptCodec Enum2() => KnxDptCodec.Single(U(V, 2));

        // 29.xxx 8-byte signed
        public static IKnxDptCodec Signed64() => KnxDptCodec.Single(S(V, 64));

        // 24.001 variable length ISO 8859-1 string, 28.001 variable length UTF-8 string
        public static IKnxDptCodec VariableString8859() => new KnxStringCodec(Encoding.Latin1, 0);
        public static IKnxDptCodec VariableStringUtf8() => new KnxStringCodec(Encoding.UTF8, 0);

        // 234.001 language code / 234.002 region code (2 ASCII characters)
        public static IKnxDptCodec LanguageCode() => new KnxStringCodec(Encoding.ASCII, 2);

        public static IKnxDptCodec DateTime19() => new KnxDateTimeCodec();

        public static IKnxDptCodec AccessData() => new KnxAccessDataCodec();
        public static IKnxDptCodec SceneControl() => new KnxSceneControlCodec();
        public static IKnxDptCodec SceneInfo() => new KnxSceneInfoCodec();
        public static IKnxDptCodec BitSet(int width) => new KnxBitSetCodec(width);
        public static IKnxDptCodec CombinedInfoOnOff() => new KnxCombinedInfoOnOffCodec();
        public static IKnxDptCodec ColourRgb() => new KnxRgbCodec();
        public static IKnxDptCodec ColourRgbw() => new KnxRgbwCodec();

        // 25.1000 busy/nak repetitions
        public static IKnxDptCodec DoubleNibble() => new KnxDptCodec(new[] { U("busy", 4), U("nak", 4) });

        // 206.1xx time delay & mode
        public static IKnxDptCodec TimeDelayAndMode() => new KnxDptCodec(new[] { U("delay", 16), U("mode", 8) });

        // 207.600 status lighting actuator
        public static IKnxDptCodec StatusLightingActuator() => new KnxDptCodec(new[]
        {
            Pct("actual-value"), B("failure"), B("local-override"), B("dimming"), B("staircase"),
            B("night-mode"), B("forced"), B("locked"), B("actual-value-valid")
        });

        // 217.001 datapoint type version
        public static IKnxDptCodec Version() => new KnxDptCodec(new[] { U("magic", 5), U("version", 5), U("revision", 6) });

        // 219.001 alarm info
        public static IKnxDptCodec AlarmInfo() => new KnxDptCodec(new[]
        {
            U("log-number", 8), U("priority", 8), U("application-area", 8), U("error-class", 8),
            R(4), B("errorcode-supported"), B("alarmtext-supported"), B("timestamp-supported"), B("ack-supported"),
            R(5), B("locked"), B("alarm-unack"), B("in-alarm")
        });

        // 222.100 / 222.101 3x 2-byte float setpoints
        public static IKnxDptCodec Setpoints3() => new KnxDptCodec(new[] { F16("comfort"), F16("standby"), F16("economy") });

        // 275.100 / 275.101 4x 2-byte float setpoints
        public static IKnxDptCodec Setpoints4() => new KnxDptCodec(new[] { F16("comfort"), F16("standby"), F16("economy"), F16("protection") });

        // 225.001 scaling speed (time period in 100 ms steps) / 225.002 scaling step time (time period in ms)
        public static IKnxDptCodec ScalingSpeed() => new KnxDptCodec(new[] { U("time", 16, 100), Pct("percent") },
            new[] { new KnxDptValue("time", NodeDataType.Integer), new KnxDptValue("percent", NodeDataType.Double) });
        public static IKnxDptCodec ScalingStepTime() => new KnxDptCodec(new[] { U("time", 16), Pct("percent") });

        // 229.001 metering value
        public static IKnxDptCodec MeteringValue() => new KnxDptCodec(new[]
        {
            S("count", 32), U("value-information", 8),
            R(3), B("alarm-unack"), B("in-alarm"), B("overridden"), B("fault"), B("out-of-service")
        });

        // 230.1000 M-Bus address
        public static IKnxDptCodec MBusAddress() => new KnxDptCodec(new[] { U("manufacturer", 16), U("ident-number", 32), U("version", 8), U("medium", 8) });

        // 235.001 active energy with tariff, validity flags: 0 = valid, 1 = invalid
        public static IKnxDptCodec TariffActiveEnergy() => new KnxDptCodec(new[]
        {
            S("energy", 32), U("tariff", 8), R(6), B("energy-invalid"), B("tariff-invalid")
        });

        // 236.001 prioritised mode control
        public static IKnxDptCodec PrioritisedModeControl() => new KnxDptCodec(new[] { B("deactivate"), U("priority", 3), U("mode", 4) });

        // 237.600 DALI control gear diagnostic
        public static IKnxDptCodec DaliControlGearDiagnostic() => new KnxDptCodec(new[]
        {
            R(5), B("converter-error"), B("ballast-failure"), B("lamp-failure"), B("read-response"), B("group-address"), U("address", 6)
        });

        // 238.600 DALI diagnostics
        public static IKnxDptCodec DaliDiagnostics() => new KnxDptCodec(new[] { B("ballast-failure"), B("lamp-failure"), U("address", 6) });

        // 240.800 combined position (height + slats), validity flags: 1 = valid
        public static IKnxDptCodec CombinedPosition() => new KnxCombinedPositionCodec();

        // 242.600 colour xyY, validity flags: 1 = valid
        public static IKnxDptCodec ColourXyY() => new KnxXyYCodec();

        // 246.600 battery information
        public static IKnxDptCodec BatteryInfo() => new KnxDptCodec(new[]
        {
            R(5), B("fully-charged"), B("duration-failure"), B("battery-failure"), U("charge-level", 8)
        });

        // 255.001 geographical location
        public static IKnxDptCodec GeographicalLocation() => new KnxDptCodec(new[] { F32("longitude"), F32("latitude") });
    }

    internal sealed class KnxCharacterCodec : IKnxDptCodec
    {
        private readonly bool _latin1;

        public KnxCharacterCodec(bool latin1)
        {
            _latin1 = latin1;
        }

        public int SizeInBit => 8;
        public IReadOnlyList<KnxDptValue> Values { get; } = new[] { new KnxDptValue(KnxDptCodec.SingleValueKey, NodeDataType.String) };

        public byte[] Encode(IReadOnlyDictionary<string, object> values, string changedKey)
        {
            values.TryGetValue(KnxDptCodec.SingleValueKey, out var value);
            var str = Convert.ToString(value, CultureInfo.InvariantCulture);
            if (string.IsNullOrEmpty(str))
            {
                return new byte[] { 0 };
            }

            var c = str[0];
            var max = _latin1 ? 0xFF : 0x7F;
            return new[] { c > max ? (byte)'?' : (byte)c };
        }

        public IReadOnlyDictionary<string, object> Decode(byte[] data)
        {
            if (data == null || data.Length != 1)
            {
                throw new FormatException("DPT 4 expects 1 byte");
            }

            if (!_latin1 && data[0] > 0x7F)
            {
                throw new FormatException($"0x{data[0]:X2} is not an ASCII character");
            }

            return new Dictionary<string, object> { { KnxDptCodec.SingleValueKey, ((char)data[0]).ToString() } };
        }
    }

    internal sealed class KnxStringCodec : IKnxDptCodec
    {
        private readonly Encoding _encoding;
        private readonly int _fixedLength;

        /// <param name="fixedLength">length in bytes, 0 = variable length terminated by 0x00</param>
        public KnxStringCodec(Encoding encoding, int fixedLength)
        {
            _encoding = encoding;
            _fixedLength = fixedLength;
        }

        public int SizeInBit => _fixedLength * 8;
        public IReadOnlyList<KnxDptValue> Values { get; } = new[] { new KnxDptValue(KnxDptCodec.SingleValueKey, NodeDataType.String) };

        public byte[] Encode(IReadOnlyDictionary<string, object> values, string changedKey)
        {
            values.TryGetValue(KnxDptCodec.SingleValueKey, out var value);
            var str = (Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty).Split('\0')[0];
            var bytes = _encoding.GetBytes(str);

            if (_fixedLength > 0)
            {
                var data = new byte[_fixedLength];
                Array.Copy(bytes, data, Math.Min(bytes.Length, _fixedLength));
                return data;
            }

            var terminated = new byte[bytes.Length + 1];
            Array.Copy(bytes, terminated, bytes.Length);
            return terminated;
        }

        public IReadOnlyDictionary<string, object> Decode(byte[] data)
        {
            if (data == null || (_fixedLength > 0 && data.Length != _fixedLength))
            {
                throw new FormatException($"Expected {_fixedLength} byte(s)");
            }

            var end = Array.IndexOf(data, (byte)0);
            var length = end < 0 ? data.Length : end;
            return new Dictionary<string, object> { { KnxDptCodec.SingleValueKey, _encoding.GetString(data, 0, length) } };
        }
    }

    /// <summary>
    /// 19.001 date time. Written telegrams carry date, time and day of week, "no working day info" and the
    /// summer time flag of the local time zone.
    /// </summary>
    internal sealed class KnxDateTimeCodec : IKnxDptCodec
    {
        public int SizeInBit => 64;
        public IReadOnlyList<KnxDptValue> Values { get; } = new[] { new KnxDptValue(KnxDptCodec.SingleValueKey, NodeDataType.DateTime) };

        public byte[] Encode(IReadOnlyDictionary<string, object> values, string changedKey)
        {
            values.TryGetValue(KnxDptCodec.SingleValueKey, out var value);
            var dt = ToDateTime(value);
            var dayOfWeek = ((int)dt.DayOfWeek + 6) % 7 + 1; // KNX: 1 = Monday ... 7 = Sunday

            byte flags = 0x20; // NWD: no working day information
            if (dt.IsDaylightSavingTime())
            {
                flags |= 0x01; // SUTI
            }

            return new[]
            {
                (byte)Math.Clamp(dt.Year - 1900, 0, 255),
                (byte)dt.Month,
                (byte)dt.Day,
                (byte)((dayOfWeek << 5) | dt.Hour),
                (byte)dt.Minute,
                (byte)dt.Second,
                flags,
                (byte)0
            };
        }

        public IReadOnlyDictionary<string, object> Decode(byte[] data)
        {
            if (data == null || data.Length != 8)
            {
                throw new FormatException("DPT 19 expects 8 bytes");
            }

            var flags = data[6];
            var fault = (flags & 0x80) != 0;
            var noYear = (flags & 0x10) != 0;
            var noDate = (flags & 0x08) != 0;
            var noTime = (flags & 0x02) != 0;

            var result = new Dictionary<string, object>();
            if (fault || noYear || noDate)
            {
                return result; // a date time node needs at least a valid date
            }

            var year = 1900 + data[0];
            var month = data[1] & 0x0F;
            var day = data[2] & 0x1F;
            var hour = noTime ? 0 : data[3] & 0x1F;
            var minute = noTime ? 0 : data[4] & 0x3F;
            var second = noTime ? 0 : data[5] & 0x3F;

            if (month is < 1 or > 12 || day < 1 || day > DateTime.DaysInMonth(year, month) || hour > 24 || minute > 59 || second > 59)
            {
                throw new FormatException("DPT 19 value out of range");
            }

            var dt = new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Local)
                .AddHours(hour).AddMinutes(minute).AddSeconds(second); // hour 24 = end of day

            result[KnxDptCodec.SingleValueKey] = dt;
            return result;
        }

        private static DateTime ToDateTime(object value)
        {
            switch (value)
            {
                case DateTime dt:
                    return dt;
                case DateTimeOffset dto:
                    return dto.LocalDateTime;
                case DateOnly d:
                    return d.ToDateTime(TimeOnly.MinValue);
                case string s:
                    return DateTime.Parse(s, CultureInfo.InvariantCulture);
                case null:
                    return DateTime.Now;
                default:
                    throw new ArgumentException($"Cannot convert {value.GetType()} to a date time");
            }
        }
    }

    /// <summary>
    /// 15.000 entrance access: 6 BCD digits access code, 4 flags and the index of the access identification code.
    /// </summary>
    internal sealed class KnxAccessDataCodec : KnxDptCodec
    {
        public KnxAccessDataCodec() : base(new[]
        {
            U("d6", 4), U("d5", 4), U("d4", 4), U("d3", 4), U("d2", 4), U("d1", 4),
            B("error"), B("permission"), B("direction"), B("encrypted"), U("index", 4)
        }, new[]
        {
            new KnxDptValue("code", NodeDataType.Integer),
            new KnxDptValue("error", NodeDataType.Boolean),
            new KnxDptValue("permission", NodeDataType.Boolean),
            new KnxDptValue("direction", NodeDataType.Boolean),
            new KnxDptValue("encrypted", NodeDataType.Boolean),
            new KnxDptValue("index", NodeDataType.Integer)
        })
        {
        }

        protected override IDictionary<string, object> ToLayout(IReadOnlyDictionary<string, object> values, string changedKey)
        {
            var layout = base.ToLayout(values, changedKey);
            var code = values.TryGetValue("code", out var c) && c != null ? Math.Clamp(KnxValueConverter.ToScaledInteger(c, 1), 0, 999999) : 0;
            for (var i = 1; i <= 6; i++)
            {
                layout[$"d{i}"] = code % 10;
                code /= 10;
            }

            return layout;
        }

        protected override IDictionary<string, object> FromLayout(IDictionary<string, object> layoutValues)
        {
            long code = 0;
            for (var i = 6; i >= 1; i--)
            {
                var digit = Convert.ToInt64(layoutValues[$"d{i}"]);
                if (digit > 9)
                {
                    throw new FormatException("DPT 15 access code is not BCD encoded");
                }

                code = code * 10 + digit;
                layoutValues.Remove($"d{i}");
            }

            layoutValues["code"] = code;
            return layoutValues;
        }
    }

    /// <summary>
    /// 18.001 scene control. Writing "activate" recalls the scene, writing "learn" stores it.
    /// </summary>
    internal sealed class KnxSceneControlCodec : KnxDptCodec
    {
        public KnxSceneControlCodec() : base(new[] { B("learn-bit"), R(1), U("scene", 6) }, new[]
        {
            new KnxDptValue("activate", NodeDataType.Integer),
            new KnxDptValue("learn", NodeDataType.Integer)
        })
        {
        }

        protected override IDictionary<string, object> ToLayout(IReadOnlyDictionary<string, object> values, string changedKey)
        {
            var learn = changedKey == "learn";
            values.TryGetValue(learn ? "learn" : "activate", out var scene);
            return new Dictionary<string, object> { { "learn-bit", learn }, { "scene", scene ?? 0 } };
        }

        protected override IDictionary<string, object> FromLayout(IDictionary<string, object> layoutValues)
        {
            var learn = (bool)layoutValues["learn-bit"];
            return new Dictionary<string, object> { { learn ? "learn" : "activate", layoutValues["scene"] } };
        }
    }

    /// <summary>
    /// 26.001 scene information (bit 6: 0 = active, 1 = inactive).
    /// </summary>
    internal sealed class KnxSceneInfoCodec : KnxDptCodec
    {
        public KnxSceneInfoCodec() : base(new[] { R(1), B("inactive"), U("scene", 6) }, new[]
        {
            new KnxDptValue("scene", NodeDataType.Integer),
            new KnxDptValue("active", NodeDataType.Boolean)
        })
        {
        }

        protected override IDictionary<string, object> ToLayout(IReadOnlyDictionary<string, object> values, string changedKey)
        {
            var active = !values.TryGetValue("active", out var a) || a == null || KnxValueConverter.ToBoolean(a);
            values.TryGetValue("scene", out var scene);
            return new Dictionary<string, object> { { "inactive", !active }, { "scene", scene ?? 0 } };
        }

        protected override IDictionary<string, object> FromLayout(IDictionary<string, object> layoutValues)
        {
            return new Dictionary<string, object>
            {
                { "scene", layoutValues["scene"] },
                { "active", !(bool)layoutValues["inactive"] }
            };
        }
    }

    /// <summary>
    /// 21.xxx (8 bit), 22.xxx (16 bit) bit sets: the raw value plus one node per bit (bit 0 = LSB).
    /// </summary>
    internal sealed class KnxBitSetCodec : IKnxDptCodec
    {
        public KnxBitSetCodec(int width)
        {
            SizeInBit = width;
            var values = new List<KnxDptValue> { new(KnxDptCodec.SingleValueKey, NodeDataType.Integer) };
            values.AddRange(Enumerable.Range(0, width).Select(i => new KnxDptValue($"bit{i}", NodeDataType.Boolean)));
            Values = values;
        }

        public int SizeInBit { get; }
        public IReadOnlyList<KnxDptValue> Values { get; }

        public byte[] Encode(IReadOnlyDictionary<string, object> values, string changedKey)
        {
            ulong raw = 0;
            if (changedKey == KnxDptCodec.SingleValueKey && values.TryGetValue(KnxDptCodec.SingleValueKey, out var rawValue) && rawValue != null)
            {
                raw = (ulong)Math.Clamp(KnxValueConverter.ToScaledInteger(rawValue, 1), 0, (1L << SizeInBit) - 1);
            }
            else
            {
                for (var i = 0; i < SizeInBit; i++)
                {
                    if (values.TryGetValue($"bit{i}", out var bit) && bit != null && KnxValueConverter.ToBoolean(bit))
                    {
                        raw |= 1UL << i;
                    }
                }
            }

            var data = new byte[SizeInBit / 8];
            for (var i = 0; i < data.Length; i++)
            {
                data[data.Length - 1 - i] = (byte)(raw >> (8 * i));
            }

            return data;
        }

        public IReadOnlyDictionary<string, object> Decode(byte[] data)
        {
            if (data == null || data.Length != SizeInBit / 8)
            {
                throw new FormatException($"Expected {SizeInBit / 8} byte(s)");
            }

            ulong raw = 0;
            foreach (var b in data)
            {
                raw = (raw << 8) | b;
            }

            var result = new Dictionary<string, object> { { KnxDptCodec.SingleValueKey, (int)raw } };
            for (var i = 0; i < SizeInBit; i++)
            {
                result[$"bit{i}"] = (raw & (1UL << i)) != 0;
            }

            return result;
        }
    }

    /// <summary>
    /// 27.001 combined info on/off: 16 mask bits followed by 16 state bits (output 16 first).
    /// Only outputs with a value are marked valid in the mask.
    /// </summary>
    internal sealed class KnxCombinedInfoOnOffCodec : IKnxDptCodec
    {
        public int SizeInBit => 32;

        public IReadOnlyList<KnxDptValue> Values { get; } =
            Enumerable.Range(1, 16).Select(i => new KnxDptValue($"output{i}", NodeDataType.Boolean)).ToList();

        public byte[] Encode(IReadOnlyDictionary<string, object> values, string changedKey)
        {
            uint mask = 0, state = 0;
            for (var i = 1; i <= 16; i++)
            {
                if (values.TryGetValue($"output{i}", out var value) && value != null)
                {
                    mask |= 1u << (i - 1);
                    if (KnxValueConverter.ToBoolean(value))
                    {
                        state |= 1u << (i - 1);
                    }
                }
            }

            return new[] { (byte)(mask >> 8), (byte)mask, (byte)(state >> 8), (byte)state };
        }

        public IReadOnlyDictionary<string, object> Decode(byte[] data)
        {
            if (data == null || data.Length != 4)
            {
                throw new FormatException("DPT 27 expects 4 bytes");
            }

            var mask = (data[0] << 8) | data[1];
            var state = (data[2] << 8) | data[3];
            var result = new Dictionary<string, object>();
            for (var i = 1; i <= 16; i++)
            {
                if ((mask & (1 << (i - 1))) != 0)
                {
                    result[$"output{i}"] = (state & (1 << (i - 1))) != 0;
                }
            }

            return result;
        }
    }

    /// <summary>
    /// 232.600 RGB (3x 0..255) plus a "#RRGGBB" hex value.
    /// </summary>
    internal sealed class KnxRgbCodec : KnxDptCodec
    {
        public KnxRgbCodec() : base(new[] { U("red", 8), U("green", 8), U("blue", 8) }, new[]
        {
            new KnxDptValue("red", NodeDataType.Integer),
            new KnxDptValue("green", NodeDataType.Integer),
            new KnxDptValue("blue", NodeDataType.Integer),
            new KnxDptValue("hex", NodeDataType.String)
        })
        {
        }

        protected override IDictionary<string, object> ToLayout(IReadOnlyDictionary<string, object> values, string changedKey)
        {
            var layout = base.ToLayout(values, changedKey);
            if (changedKey == "hex" && values.TryGetValue("hex", out var hex))
            {
                var channels = KnxHexColor.Parse(Convert.ToString(hex, CultureInfo.InvariantCulture), 3);
                layout["red"] = channels[0];
                layout["green"] = channels[1];
                layout["blue"] = channels[2];
            }

            return layout;
        }

        protected override IDictionary<string, object> FromLayout(IDictionary<string, object> layoutValues)
        {
            layoutValues["hex"] = KnxHexColor.Format(layoutValues["red"], layoutValues["green"], layoutValues["blue"]);
            return layoutValues;
        }
    }

    /// <summary>
    /// 251.600 RGBW (4x 0..255, 1 reserved byte, mask byte mR mG mB mW) plus a "#RRGGBBWW" hex value.
    /// </summary>
    internal sealed class KnxRgbwCodec : KnxDptCodec
    {
        private static readonly string[] Channels = { "red", "green", "blue", "white" };

        public KnxRgbwCodec() : base(new[]
        {
            U("red", 8), U("green", 8), U("blue", 8), U("white", 8), R(8), R(4),
            B("red-valid"), B("green-valid"), B("blue-valid"), B("white-valid")
        }, new[]
        {
            new KnxDptValue("red", NodeDataType.Integer),
            new KnxDptValue("green", NodeDataType.Integer),
            new KnxDptValue("blue", NodeDataType.Integer),
            new KnxDptValue("white", NodeDataType.Integer),
            new KnxDptValue("hex", NodeDataType.String)
        })
        {
        }

        protected override IDictionary<string, object> ToLayout(IReadOnlyDictionary<string, object> values, string changedKey)
        {
            var layout = base.ToLayout(values, changedKey);
            if (changedKey == "hex" && values.TryGetValue("hex", out var hex))
            {
                var parsed = KnxHexColor.Parse(Convert.ToString(hex, CultureInfo.InvariantCulture), 4);
                for (var i = 0; i < Channels.Length; i++)
                {
                    layout[Channels[i]] = parsed[i];
                }
            }

            foreach (var channel in Channels)
            {
                layout[$"{channel}-valid"] = layout.TryGetValue(channel, out var v) && v != null;
            }

            return layout;
        }

        protected override IDictionary<string, object> FromLayout(IDictionary<string, object> layoutValues)
        {
            var result = new Dictionary<string, object>();
            foreach (var channel in Channels)
            {
                if ((bool)layoutValues[$"{channel}-valid"])
                {
                    result[channel] = layoutValues[channel];
                }
            }

            if (result.Count == Channels.Length)
            {
                result["hex"] = KnxHexColor.Format(result["red"], result["green"], result["blue"], result["white"]);
            }

            return result;
        }
    }

    /// <summary>
    /// 242.600 colour xyY: x and y as 0..1 (16 bit), brightness 0..255, validity bits (1 = valid).
    /// </summary>
    internal sealed class KnxXyYCodec : KnxDptCodec
    {
        public KnxXyYCodec() : base(new[]
        {
            U("x", 16, 1.0 / 65535), U("y", 16, 1.0 / 65535), U("brightness", 8), R(6), B("xy-valid"), B("brightness-valid")
        }, new[]
        {
            new KnxDptValue("x", NodeDataType.Double),
            new KnxDptValue("y", NodeDataType.Double),
            new KnxDptValue("brightness", NodeDataType.Integer)
        })
        {
        }

        protected override IDictionary<string, object> ToLayout(IReadOnlyDictionary<string, object> values, string changedKey)
        {
            var layout = base.ToLayout(values, changedKey);
            layout["xy-valid"] = layout.TryGetValue("x", out var x) && x != null && layout.TryGetValue("y", out var y) && y != null;
            layout["brightness-valid"] = layout.TryGetValue("brightness", out var b) && b != null;
            return layout;
        }

        protected override IDictionary<string, object> FromLayout(IDictionary<string, object> layoutValues)
        {
            var result = new Dictionary<string, object>();
            if ((bool)layoutValues["xy-valid"])
            {
                result["x"] = Math.Round(Convert.ToDouble(layoutValues["x"]), 4);
                result["y"] = Math.Round(Convert.ToDouble(layoutValues["y"]), 4);
            }

            if ((bool)layoutValues["brightness-valid"])
            {
                result["brightness"] = layoutValues["brightness"];
            }

            return result;
        }
    }

    /// <summary>
    /// 240.800 combined position: height and slats in %, validity bits (1 = valid) are set for the values that are known.
    /// </summary>
    internal sealed class KnxCombinedPositionCodec : KnxDptCodec
    {
        public KnxCombinedPositionCodec() : base(new[] { Pct("height"), Pct("slats"), R(6), B("height-valid"), B("slats-valid") }, new[]
        {
            new KnxDptValue("height", NodeDataType.Double),
            new KnxDptValue("slats", NodeDataType.Double)
        })
        {
        }

        protected override IDictionary<string, object> ToLayout(IReadOnlyDictionary<string, object> values, string changedKey)
        {
            var layout = base.ToLayout(values, changedKey);
            layout["height-valid"] = layout.TryGetValue("height", out var h) && h != null;
            layout["slats-valid"] = layout.TryGetValue("slats", out var s) && s != null;
            return layout;
        }

        protected override IDictionary<string, object> FromLayout(IDictionary<string, object> layoutValues)
        {
            var result = new Dictionary<string, object>();
            if ((bool)layoutValues["height-valid"])
            {
                result["height"] = layoutValues["height"];
            }

            if ((bool)layoutValues["slats-valid"])
            {
                result["slats"] = layoutValues["slats"];
            }

            return result;
        }
    }

    internal static class KnxHexColor
    {
        public static long[] Parse(string hex, int channels)
        {
            var str = (hex ?? string.Empty).Trim().TrimStart('#');
            if (str.Length != channels * 2 || !long.TryParse(str, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value))
            {
                throw new ArgumentException($"'{hex}' is not a #{new string('X', channels * 2)} colour");
            }

            var result = new long[channels];
            for (var i = 0; i < channels; i++)
            {
                result[i] = (value >> (8 * (channels - 1 - i))) & 0xFF;
            }

            return result;
        }

        public static string Format(params object[] channels)
        {
            return "#" + string.Concat(channels.Select(a => Convert.ToByte(a).ToString("X2", CultureInfo.InvariantCulture)));
        }
    }
}
