using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Automatica.Core.Base.Templates;

namespace P3.Driver.Knx.DriverFactory.Dpt
{
    /// <summary>
    /// A value a codec exposes to Automatica. Single-value datapoint types expose one value with the key
    /// <see cref="KnxDptCodec.SingleValueKey"/>, multi-field types expose one value per child node.
    /// </summary>
    internal sealed class KnxDptValue
    {
        public KnxDptValue(string key, NodeDataType dataType)
        {
            Key = key;
            DataType = dataType;
        }

        public string Key { get; }
        public NodeDataType DataType { get; }
    }

    /// <summary>
    /// Converts between Automatica values and the raw KNX payload of a datapoint type (KNX standard 3.7.2).
    /// </summary>
    internal interface IKnxDptCodec
    {
        /// <summary>
        /// Payload size in bit, 0 for variable length types (DPT 24, DPT 28).
        /// </summary>
        int SizeInBit { get; }

        IReadOnlyList<KnxDptValue> Values { get; }

        /// <summary>
        /// Encodes the current values. <paramref name="changedKey"/> is the value that was written last, codecs
        /// that derive several fields from one value (hex colour, raw bit sets, scene learn) use it.
        /// </summary>
        byte[] Encode(IReadOnlyDictionary<string, object> values, string changedKey);

        /// <summary>
        /// Decodes a payload. Values that are flagged invalid in the telegram are left out.
        /// </summary>
        /// <exception cref="FormatException">payload does not fit the datapoint type</exception>
        IReadOnlyDictionary<string, object> Decode(byte[] data);
    }

    internal enum KnxFieldKind
    {
        Unsigned,
        Signed,
        Boolean,
        /// <summary>2-byte KNX float (DPT 9 format)</summary>
        Float16,
        /// <summary>4-byte IEEE 754 float (DPT 14 format)</summary>
        Float32,
        /// <summary>8-bit 0..255 scaled to 0..100 %</summary>
        Percent8,
        Reserved
    }

    /// <summary>
    /// One field of a bit layout. Fields are listed most significant bit first, like in the KNX specification.
    /// </summary>
    internal sealed class KnxDptField
    {
        public KnxDptField(string key, int width, KnxFieldKind kind, double scale = 1)
        {
            Key = key;
            Width = width;
            Kind = kind;
            Scale = scale;
        }

        public string Key { get; }
        public int Width { get; }
        public KnxFieldKind Kind { get; }

        /// <summary>
        /// Automatica value = raw * scale (only for <see cref="KnxFieldKind.Unsigned"/> and <see cref="KnxFieldKind.Signed"/>).
        /// </summary>
        public double Scale { get; }

        public static KnxDptField U(string key, int width, double scale = 1) => new(key, width, KnxFieldKind.Unsigned, scale);
        public static KnxDptField S(string key, int width) => new(key, width, KnxFieldKind.Signed);
        public static KnxDptField B(string key) => new(key, 1, KnxFieldKind.Boolean);
        public static KnxDptField F16(string key) => new(key, 16, KnxFieldKind.Float16);
        public static KnxDptField F32(string key) => new(key, 32, KnxFieldKind.Float32);
        public static KnxDptField Pct(string key) => new(key, 8, KnxFieldKind.Percent8);
        public static KnxDptField R(int width) => new(null, width, KnxFieldKind.Reserved);

        internal NodeDataType DefaultDataType
        {
            get
            {
                switch (Kind)
                {
                    case KnxFieldKind.Boolean:
                        return NodeDataType.Boolean;
                    case KnxFieldKind.Float16:
                    case KnxFieldKind.Float32:
                    case KnxFieldKind.Percent8:
                        return NodeDataType.Double;
                    case KnxFieldKind.Unsigned:
                        // values above int.MaxValue do not fit an Integer node
                        return Width >= 32 || Math.Abs(Scale - Math.Round(Scale)) > double.Epsilon ? NodeDataType.Double : NodeDataType.Integer;
                    case KnxFieldKind.Signed:
                        return Width > 32 ? NodeDataType.Double : NodeDataType.Integer;
                    default:
                        return NodeDataType.NoAttribute;
                }
            }
        }
    }

    /// <summary>
    /// Bit layout based codec: the payload is the concatenation of <see cref="Layout"/> fields (MSB first).
    /// Derived codecs map between the exposed <see cref="Values"/> and the layout fields.
    /// </summary>
    internal class KnxDptCodec : IKnxDptCodec
    {
        public const string SingleValueKey = "value";

        public KnxDptCodec(IReadOnlyList<KnxDptField> layout, IReadOnlyList<KnxDptValue> values = null)
        {
            Layout = layout;
            SizeInBit = layout.Sum(a => a.Width);
            Values = values ?? layout.Where(a => a.Kind != KnxFieldKind.Reserved)
                .Select(a => new KnxDptValue(a.Key, a.DefaultDataType)).ToList();
        }

        public IReadOnlyList<KnxDptField> Layout { get; }
        public int SizeInBit { get; }
        public IReadOnlyList<KnxDptValue> Values { get; }

        private int ByteLength => Math.Max(1, (SizeInBit + 7) / 8);

        /// <summary>
        /// Single value codec with a one-field layout.
        /// </summary>
        public static KnxDptCodec Single(KnxDptField field, NodeDataType? dataType = null)
        {
            return new KnxDptCodec(new[] { field }, new[] { new KnxDptValue(field.Key, dataType ?? field.DefaultDataType) });
        }

        /// <summary>
        /// Single value codec with reserved bits in front of the value (e.g. DPT 17: 2 reserved bits + 6 bit scene number).
        /// </summary>
        public static KnxDptCodec Single(int reservedBits, KnxDptField field)
        {
            return new KnxDptCodec(new[] { KnxDptField.R(reservedBits), field }, new[] { new KnxDptValue(field.Key, field.DefaultDataType) });
        }

        protected virtual IDictionary<string, object> ToLayout(IReadOnlyDictionary<string, object> values, string changedKey)
        {
            return values.ToDictionary(a => a.Key, a => a.Value);
        }

        protected virtual IDictionary<string, object> FromLayout(IDictionary<string, object> layoutValues)
        {
            return layoutValues;
        }

        public virtual byte[] Encode(IReadOnlyDictionary<string, object> values, string changedKey)
        {
            var layoutValues = ToLayout(values, changedKey);
            var data = new byte[ByteLength];
            // short values (< 8 bit) are right aligned in the single data byte
            var bitPos = SizeInBit < 8 ? 8 - SizeInBit : 0;

            foreach (var field in Layout)
            {
                ulong raw = 0;
                if (field.Kind != KnxFieldKind.Reserved && field.Key != null && layoutValues.TryGetValue(field.Key, out var value) && value != null)
                {
                    raw = ToRaw(field, value);
                }

                KnxBits.Write(data, ref bitPos, field.Width, raw);
            }

            return data;
        }

        public virtual IReadOnlyDictionary<string, object> Decode(byte[] data)
        {
            if (data == null || data.Length != ByteLength)
            {
                throw new FormatException($"Expected {ByteLength} byte(s) but got {data?.Length ?? 0}");
            }

            var bitPos = SizeInBit < 8 ? 8 - SizeInBit : 0;
            var layoutValues = new Dictionary<string, object>();

            foreach (var field in Layout)
            {
                var raw = KnxBits.Read(data, ref bitPos, field.Width);
                if (field.Kind == KnxFieldKind.Reserved || field.Key == null)
                {
                    continue;
                }

                var value = FromRaw(field, raw);
                if (value != null)
                {
                    layoutValues[field.Key] = value;
                }
            }

            var result = FromLayout(layoutValues);
            return result.ToDictionary(a => a.Key, a => ConvertToDataType(a.Key, a.Value));
        }

        private object ConvertToDataType(string key, object value)
        {
            var target = Values.FirstOrDefault(a => a.Key == key);
            if (target == null || value == null)
            {
                return value;
            }

            switch (target.DataType)
            {
                case NodeDataType.Integer when value is long or ulong or int:
                    return Convert.ToInt32(value);
                case NodeDataType.Integer when value is double d:
                    return Convert.ToInt32(Math.Round(d));
                case NodeDataType.Double when value is long or ulong or int:
                    return Convert.ToDouble(value);
                default:
                    return value;
            }
        }

        internal static ulong ToRaw(KnxDptField field, object value)
        {
            switch (field.Kind)
            {
                case KnxFieldKind.Boolean:
                    return KnxValueConverter.ToBoolean(value) ? 1UL : 0UL;
                case KnxFieldKind.Unsigned:
                {
                    var max = field.Width >= 64 ? ulong.MaxValue : (1UL << field.Width) - 1;
                    var raw = KnxValueConverter.ToScaledInteger(value, field.Scale);
                    if (raw < 0)
                    {
                        return 0;
                    }

                    return (ulong)raw > max ? max : (ulong)raw;
                }
                case KnxFieldKind.Signed:
                {
                    var raw = KnxValueConverter.ToScaledInteger(value, field.Scale);
                    if (field.Width < 64)
                    {
                        var min = -(1L << (field.Width - 1));
                        var max = (1L << (field.Width - 1)) - 1;
                        raw = Math.Clamp(raw, min, max);
                        return (ulong)raw & ((1UL << field.Width) - 1);
                    }

                    return unchecked((ulong)raw);
                }
                case KnxFieldKind.Percent8:
                {
                    var percent = Math.Clamp(KnxValueConverter.ToDouble(value), 0, 100);
                    return (ulong)Math.Round(percent * 255 / 100, MidpointRounding.AwayFromZero);
                }
                case KnxFieldKind.Float16:
                    return KnxFloat16.Encode(KnxValueConverter.ToDouble(value));
                case KnxFieldKind.Float32:
                    return BitConverter.SingleToUInt32Bits((float)KnxValueConverter.ToDouble(value));
                default:
                    return 0;
            }
        }

        internal static object FromRaw(KnxDptField field, ulong raw)
        {
            switch (field.Kind)
            {
                case KnxFieldKind.Boolean:
                    return raw != 0;
                case KnxFieldKind.Unsigned:
                    if (Math.Abs(field.Scale - 1) < double.Epsilon)
                    {
                        return raw <= long.MaxValue ? (object)(long)raw : raw;
                    }

                    return raw * field.Scale;
                case KnxFieldKind.Signed:
                {
                    long value;
                    if (field.Width < 64)
                    {
                        var signBit = 1UL << (field.Width - 1);
                        value = (raw & signBit) != 0 ? (long)raw - (1L << field.Width) : (long)raw;
                    }
                    else
                    {
                        value = unchecked((long)raw);
                    }

                    if (Math.Abs(field.Scale - 1) < double.Epsilon)
                    {
                        return value;
                    }

                    return value * field.Scale;
                }
                case KnxFieldKind.Percent8:
                    return Math.Round(raw * 100.0 / 255, 1);
                case KnxFieldKind.Float16:
                    return KnxFloat16.Decode((ushort)raw);
                case KnxFieldKind.Float32:
                    return KnxValueConverter.FloatToDouble(BitConverter.UInt32BitsToSingle((uint)raw));
                default:
                    return null;
            }
        }
    }

    internal static class KnxBits
    {
        public static void Write(byte[] data, ref int bitPos, int width, ulong value)
        {
            for (var i = width - 1; i >= 0; i--)
            {
                var bit = (value >> i) & 1;
                if (bit != 0)
                {
                    data[bitPos / 8] |= (byte)(0x80 >> (bitPos % 8));
                }

                bitPos++;
            }
        }

        public static ulong Read(byte[] data, ref int bitPos, int width)
        {
            ulong value = 0;
            for (var i = 0; i < width; i++)
            {
                var bit = (data[bitPos / 8] >> (7 - bitPos % 8)) & 1;
                value = (value << 1) | (uint)bit;
                bitPos++;
            }

            return value;
        }
    }

    /// <summary>
    /// KNX 2-byte float: value = 0.01 * M * 2^E, M 12 bit two's complement (sign in bit 15), E 4 bit.
    /// </summary>
    internal static class KnxFloat16
    {
        public const ushort Invalid = 0x7FFF;

        public static ushort Encode(double value)
        {
            if (double.IsNaN(value))
            {
                return Invalid;
            }

            var scaled = value * 100;
            var exponent = 0;
            var mantissa = Math.Round(scaled, MidpointRounding.AwayFromZero);

            while ((mantissa < -2048 || mantissa > 2047) && exponent < 15)
            {
                exponent++;
                mantissa = Math.Round(scaled / (1 << exponent), MidpointRounding.AwayFromZero);
            }

            mantissa = Math.Clamp(mantissa, -2048, 2047);
            var m = (int)mantissa;
            var raw = (m < 0 ? 0x8000 : 0) | (exponent << 11) | (m & 0x7FF);
            return raw == Invalid ? (ushort)(Invalid - 1) : (ushort)raw; // 0x7FFF means "invalid", clamp to the largest valid value
        }

        public static double? Decode(ushort raw)
        {
            if (raw == Invalid)
            {
                return null;
            }

            var mantissa = raw & 0x7FF;
            if ((raw & 0x8000) != 0)
            {
                mantissa -= 2048;
            }

            var exponent = (raw >> 11) & 0x0F;
            return Math.Round(0.01 * mantissa * (1 << exponent), 2);
        }
    }

    internal static class KnxValueConverter
    {
        public static bool ToBoolean(object value)
        {
            switch (value)
            {
                case bool b:
                    return b;
                case string s when bool.TryParse(s, out var parsed):
                    return parsed;
                case string s:
                    return Math.Abs(ToDouble(s)) > double.Epsilon;
                default:
                    return Convert.ToBoolean(value, CultureInfo.InvariantCulture);
            }
        }

        public static double ToDouble(object value)
        {
            switch (value)
            {
                case bool b:
                    return b ? 1 : 0;
                case string s:
                    return double.Parse(s, NumberStyles.Float, CultureInfo.InvariantCulture);
                default:
                    return Convert.ToDouble(value, CultureInfo.InvariantCulture);
            }
        }

        /// <summary>
        /// raw = value / scale, integral inputs keep their full 64 bit precision.
        /// </summary>
        public static long ToScaledInteger(object value, double scale)
        {
            var isUnscaled = Math.Abs(scale - 1) < double.Epsilon;
            switch (value)
            {
                case long l when isUnscaled:
                    return l;
                case int i when isUnscaled:
                    return i;
                case ulong ul when isUnscaled:
                    return ul > long.MaxValue ? long.MaxValue : (long)ul;
                case uint ui when isUnscaled:
                    return ui;
                case short s when isUnscaled:
                    return s;
                case ushort us when isUnscaled:
                    return us;
                case byte by when isUnscaled:
                    return by;
                case sbyte sb when isUnscaled:
                    return sb;
            }

            var d = ToDouble(value) / scale;
            if (d >= long.MaxValue)
            {
                return long.MaxValue;
            }

            if (d <= long.MinValue)
            {
                return long.MinValue;
            }

            return (long)Math.Round(d, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// Converts a float to the shortest double that round trips (0.1f =&gt; 0.1 instead of 0.100000001).
        /// </summary>
        public static double FloatToDouble(float value)
        {
            return double.Parse(value.ToString("R", CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
        }
    }
}
