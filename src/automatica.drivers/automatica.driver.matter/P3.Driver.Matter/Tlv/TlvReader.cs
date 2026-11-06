using System.Buffers.Binary;
using System.Text;

namespace P3.Driver.Matter.Tlv
{
    /// <summary>Decodes Matter TLV (spec Appendix A) into a <see cref="TlvElement"/> tree.</summary>
    public static class TlvReader
    {
        private const byte EndOfContainer = 0x18;

        /// <summary>Decodes a single top-level element (usually an anonymous structure).</summary>
        public static TlvElement Decode(ReadOnlySpan<byte> data)
        {
            var pos = 0;
            var element = ReadElement(data, ref pos);
            if (element == null)
            {
                throw new MatterProtocolException("Unexpected end-of-container at top level");
            }

            return element;
        }

        /// <summary>Decodes all consecutive top-level elements.</summary>
        public static List<TlvElement> DecodeAll(ReadOnlySpan<byte> data)
        {
            var list = new List<TlvElement>();
            var pos = 0;
            while (pos < data.Length)
            {
                var e = ReadElement(data, ref pos);
                if (e == null)
                {
                    throw new MatterProtocolException("Unexpected end-of-container at top level");
                }

                list.Add(e);
            }

            return list;
        }

        private static TlvElement ReadElement(ReadOnlySpan<byte> data, ref int pos)
        {
            Need(data, pos, 1);
            var control = data[pos++];
            var type = control & 0x1F;
            var tagControl = control >> 5;

            if (type == EndOfContainer)
            {
                return null;
            }

            long tag = TlvElement.AnonymousTag;
            var isProfile = false;
            switch (tagControl)
            {
                case 0: // anonymous
                    break;
                case 1: // context specific
                    Need(data, pos, 1);
                    tag = data[pos++];
                    break;
                case 2: // common profile, 2 bytes
                case 4: // implicit profile, 2 bytes
                    Need(data, pos, 2);
                    tag = BinaryPrimitives.ReadUInt16LittleEndian(data[pos..]);
                    pos += 2;
                    isProfile = true;
                    break;
                case 3: // common profile, 4 bytes
                case 5: // implicit profile, 4 bytes
                    Need(data, pos, 4);
                    tag = BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
                    pos += 4;
                    isProfile = true;
                    break;
                case 6: // fully qualified, 6 bytes (vendor 2, profile 2, tag 2)
                    Need(data, pos, 6);
                    tag = BinaryPrimitives.ReadUInt16LittleEndian(data[(pos + 4)..]);
                    pos += 6;
                    isProfile = true;
                    break;
                case 7: // fully qualified, 8 bytes (vendor 2, profile 2, tag 4)
                    Need(data, pos, 8);
                    tag = BinaryPrimitives.ReadUInt32LittleEndian(data[(pos + 4)..]);
                    pos += 8;
                    isProfile = true;
                    break;
            }

            switch (type)
            {
                case 0x00:
                case 0x01:
                case 0x02:
                case 0x03:
                {
                    var size = 1 << (type & 0x03);
                    Need(data, pos, size);
                    long v = size switch
                    {
                        1 => (sbyte)data[pos],
                        2 => BinaryPrimitives.ReadInt16LittleEndian(data[pos..]),
                        4 => BinaryPrimitives.ReadInt32LittleEndian(data[pos..]),
                        _ => BinaryPrimitives.ReadInt64LittleEndian(data[pos..])
                    };
                    pos += size;
                    return new TlvElement { Kind = TlvKind.SignedInt, Tag = tag, IsProfileTag = isProfile, Value = v };
                }
                case 0x04:
                case 0x05:
                case 0x06:
                case 0x07:
                {
                    var size = 1 << (type & 0x03);
                    Need(data, pos, size);
                    ulong v = size switch
                    {
                        1 => data[pos],
                        2 => BinaryPrimitives.ReadUInt16LittleEndian(data[pos..]),
                        4 => BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]),
                        _ => BinaryPrimitives.ReadUInt64LittleEndian(data[pos..])
                    };
                    pos += size;
                    return new TlvElement { Kind = TlvKind.UnsignedInt, Tag = tag, IsProfileTag = isProfile, Value = v };
                }
                case 0x08:
                case 0x09:
                    return new TlvElement { Kind = TlvKind.Bool, Tag = tag, IsProfileTag = isProfile, Value = type == 0x09 };
                case 0x0A:
                    Need(data, pos, 4);
                    var f = BinaryPrimitives.ReadSingleLittleEndian(data[pos..]);
                    pos += 4;
                    return new TlvElement { Kind = TlvKind.Float, Tag = tag, IsProfileTag = isProfile, Value = f };
                case 0x0B:
                    Need(data, pos, 8);
                    var d = BinaryPrimitives.ReadDoubleLittleEndian(data[pos..]);
                    pos += 8;
                    return new TlvElement { Kind = TlvKind.Double, Tag = tag, IsProfileTag = isProfile, Value = d };
                case >= 0x0C and <= 0x13:
                {
                    var lenSize = 1 << (type & 0x03);
                    Need(data, pos, lenSize);
                    long len = lenSize switch
                    {
                        1 => data[pos],
                        2 => BinaryPrimitives.ReadUInt16LittleEndian(data[pos..]),
                        4 => BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]),
                        _ => (long)BinaryPrimitives.ReadUInt64LittleEndian(data[pos..])
                    };
                    pos += lenSize;
                    if (len > int.MaxValue)
                    {
                        throw new MatterProtocolException("TLV string too long");
                    }

                    Need(data, pos, (int)len);
                    var bytes = data.Slice(pos, (int)len).ToArray();
                    pos += (int)len;
                    if (type <= 0x0F)
                    {
                        return new TlvElement { Kind = TlvKind.Utf8String, Tag = tag, IsProfileTag = isProfile, Value = Encoding.UTF8.GetString(bytes) };
                    }

                    return new TlvElement { Kind = TlvKind.ByteString, Tag = tag, IsProfileTag = isProfile, Value = bytes };
                }
                case 0x14:
                    return new TlvElement { Kind = TlvKind.Null, Tag = tag, IsProfileTag = isProfile };
                case 0x15:
                case 0x16:
                case 0x17:
                {
                    var kind = type == 0x15 ? TlvKind.Structure : type == 0x16 ? TlvKind.Array : TlvKind.List;
                    var container = new TlvElement { Kind = kind, Tag = tag, IsProfileTag = isProfile };
                    while (true)
                    {
                        var child = ReadElement(data, ref pos);
                        if (child == null)
                        {
                            break;
                        }

                        container.Children.Add(child);
                    }

                    return container;
                }
                default:
                    throw new MatterProtocolException($"Unknown TLV element type 0x{type:X2}");
            }
        }

        private static void Need(ReadOnlySpan<byte> data, int pos, int count)
        {
            if (pos + count > data.Length)
            {
                throw new MatterProtocolException("Truncated TLV data");
            }
        }
    }
}
