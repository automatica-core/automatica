using System;
using System.Collections.Generic;
using System.IO;

namespace P3.Driver.Mammotion.Driver.Protocol
{
    /// <summary>
    /// Minimal protobuf wire format (varint and length-delimited fields), enough for the few LubaMsg messages the driver uses
    /// </summary>
    internal sealed class ProtoWriter
    {
        private readonly MemoryStream _stream = new();

        public ProtoWriter Varint(int field, long value)
        {
            WriteRawVarint((ulong)((field << 3) | 0));
            WriteRawVarint((ulong)value);
            return this;
        }

        public ProtoWriter Message(int field, ProtoWriter message)
        {
            return Bytes(field, message.ToArray());
        }

        public ProtoWriter Bytes(int field, byte[] bytes)
        {
            WriteRawVarint((ulong)((field << 3) | 2));
            WriteRawVarint((ulong)bytes.Length);
            _stream.Write(bytes, 0, bytes.Length);
            return this;
        }

        public byte[] ToArray() => _stream.ToArray();

        private void WriteRawVarint(ulong value)
        {
            while (value >= 0x80)
            {
                _stream.WriteByte((byte)(value | 0x80));
                value >>= 7;
            }
            _stream.WriteByte((byte)value);
        }
    }

    internal readonly record struct ProtoField(int Number, int WireType, ulong Varint, ReadOnlyMemory<byte> Bytes)
    {
        /// <summary>
        /// int32 / int64 (negative values are sign extended to 64 bit varints)
        /// </summary>
        public long Int64 => (long)Varint;

        public int Int32 => (int)(long)Varint;
    }

    internal static class ProtoReader
    {
        /// <summary>
        /// Reads all fields of a message. Throws <see cref="InvalidDataException"/> on malformed input.
        /// </summary>
        public static List<ProtoField> Read(ReadOnlyMemory<byte> data)
        {
            var fields = new List<ProtoField>();
            var span = data.Span;
            var pos = 0;
            while (pos < span.Length)
            {
                var key = ReadVarint(span, ref pos);
                var number = (int)(key >> 3);
                var wireType = (int)(key & 7);
                switch (wireType)
                {
                    case 0:
                        fields.Add(new ProtoField(number, 0, ReadVarint(span, ref pos), default));
                        break;
                    case 1:
                        Ensure(span, pos, 8);
                        fields.Add(new ProtoField(number, 1, BitConverter.ToUInt64(span.Slice(pos, 8)), data.Slice(pos, 8)));
                        pos += 8;
                        break;
                    case 2:
                        var length = (int)ReadVarint(span, ref pos);
                        Ensure(span, pos, length);
                        fields.Add(new ProtoField(number, 2, 0, data.Slice(pos, length)));
                        pos += length;
                        break;
                    case 5:
                        Ensure(span, pos, 4);
                        fields.Add(new ProtoField(number, 5, BitConverter.ToUInt32(span.Slice(pos, 4)), data.Slice(pos, 4)));
                        pos += 4;
                        break;
                    default:
                        throw new InvalidDataException($"Unsupported wire type {wireType}");
                }
            }
            return fields;
        }

        public static ProtoField? Find(List<ProtoField> fields, int number)
        {
            ProtoField? last = null;
            foreach (var f in fields)
            {
                if (f.Number == number)
                {
                    last = f; // last one wins, like protobuf
                }
            }
            return last;
        }

        public static List<ProtoField> Sub(List<ProtoField> fields, int number)
        {
            var f = Find(fields, number);
            return f is { WireType: 2 } ? Read(f.Value.Bytes) : null;
        }

        private static void Ensure(ReadOnlySpan<byte> span, int pos, int length)
        {
            if (length < 0 || pos + length > span.Length)
            {
                throw new InvalidDataException("Truncated protobuf message");
            }
        }

        private static ulong ReadVarint(ReadOnlySpan<byte> span, ref int pos)
        {
            ulong result = 0;
            for (var shift = 0; shift < 64; shift += 7)
            {
                if (pos >= span.Length)
                {
                    throw new InvalidDataException("Truncated varint");
                }
                var b = span[pos++];
                result |= (ulong)(b & 0x7F) << shift;
                if ((b & 0x80) == 0)
                {
                    return result;
                }
            }
            throw new InvalidDataException("Varint too long");
        }
    }
}
