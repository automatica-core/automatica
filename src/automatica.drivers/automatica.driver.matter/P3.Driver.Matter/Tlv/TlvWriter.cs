using System.Buffers.Binary;
using System.Text;

namespace P3.Driver.Matter.Tlv
{
    /// <summary>
    /// Encodes Matter TLV. A <c>tag</c> of <c>null</c> writes an anonymous element, any other value a
    /// context-specific tag. Integers are written in the smallest encoding that fits.
    /// </summary>
    public sealed class TlvWriter
    {
        private readonly MemoryStream _stream = new();
        private int _depth;

        public int Length => (int)_stream.Length;

        public byte[] ToArray()
        {
            if (_depth != 0)
            {
                throw new InvalidOperationException("TLV writer has unclosed containers");
            }

            return _stream.ToArray();
        }

        private void WriteControl(byte type, byte? tag)
        {
            if (tag.HasValue)
            {
                _stream.WriteByte((byte)(0x20 | type));
                _stream.WriteByte(tag.Value);
            }
            else
            {
                _stream.WriteByte(type);
            }
        }

        private void WriteLe(ulong value, int size)
        {
            Span<byte> buf = stackalloc byte[8];
            BinaryPrimitives.WriteUInt64LittleEndian(buf, value);
            _stream.Write(buf[..size]);
        }

        public TlvWriter PutUInt(byte? tag, ulong value)
        {
            if (value <= byte.MaxValue)
            {
                WriteControl(0x04, tag);
                WriteLe(value, 1);
            }
            else if (value <= ushort.MaxValue)
            {
                WriteControl(0x05, tag);
                WriteLe(value, 2);
            }
            else if (value <= uint.MaxValue)
            {
                WriteControl(0x06, tag);
                WriteLe(value, 4);
            }
            else
            {
                WriteControl(0x07, tag);
                WriteLe(value, 8);
            }

            return this;
        }

        public TlvWriter PutInt(byte? tag, long value)
        {
            if (value is >= sbyte.MinValue and <= sbyte.MaxValue)
            {
                WriteControl(0x00, tag);
                WriteLe((ulong)value, 1);
            }
            else if (value is >= short.MinValue and <= short.MaxValue)
            {
                WriteControl(0x01, tag);
                WriteLe((ulong)value, 2);
            }
            else if (value is >= int.MinValue and <= int.MaxValue)
            {
                WriteControl(0x02, tag);
                WriteLe((ulong)value, 4);
            }
            else
            {
                WriteControl(0x03, tag);
                WriteLe((ulong)value, 8);
            }

            return this;
        }

        public TlvWriter PutBool(byte? tag, bool value)
        {
            WriteControl(value ? (byte)0x09 : (byte)0x08, tag);
            return this;
        }

        public TlvWriter PutFloat(byte? tag, float value)
        {
            WriteControl(0x0A, tag);
            Span<byte> buf = stackalloc byte[4];
            BinaryPrimitives.WriteSingleLittleEndian(buf, value);
            _stream.Write(buf);
            return this;
        }

        public TlvWriter PutDouble(byte? tag, double value)
        {
            WriteControl(0x0B, tag);
            Span<byte> buf = stackalloc byte[8];
            BinaryPrimitives.WriteDoubleLittleEndian(buf, value);
            _stream.Write(buf);
            return this;
        }

        public TlvWriter PutString(byte? tag, string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value ?? string.Empty);
            PutStringOrBytes(0x0C, tag, bytes);
            return this;
        }

        public TlvWriter PutBytes(byte? tag, ReadOnlySpan<byte> value)
        {
            PutStringOrBytes(0x10, tag, value);
            return this;
        }

        private void PutStringOrBytes(byte baseType, byte? tag, ReadOnlySpan<byte> bytes)
        {
            if (bytes.Length <= byte.MaxValue)
            {
                WriteControl(baseType, tag);
                WriteLe((ulong)bytes.Length, 1);
            }
            else if (bytes.Length <= ushort.MaxValue)
            {
                WriteControl((byte)(baseType + 1), tag);
                WriteLe((ulong)bytes.Length, 2);
            }
            else
            {
                WriteControl((byte)(baseType + 2), tag);
                WriteLe((ulong)bytes.Length, 4);
            }

            _stream.Write(bytes);
        }

        public TlvWriter PutNull(byte? tag)
        {
            WriteControl(0x14, tag);
            return this;
        }

        public TlvWriter StartStructure(byte? tag = null)
        {
            WriteControl(0x15, tag);
            _depth++;
            return this;
        }

        public TlvWriter StartArray(byte? tag = null)
        {
            WriteControl(0x16, tag);
            _depth++;
            return this;
        }

        public TlvWriter StartList(byte? tag = null)
        {
            WriteControl(0x17, tag);
            _depth++;
            return this;
        }

        public TlvWriter EndContainer()
        {
            if (_depth == 0)
            {
                throw new InvalidOperationException("No open TLV container");
            }

            _stream.WriteByte(0x18);
            _depth--;
            return this;
        }

        /// <summary>Copies already-encoded TLV bytes (a complete element) into the stream.</summary>
        public TlvWriter PutRaw(ReadOnlySpan<byte> encodedElement)
        {
            _stream.Write(encodedElement);
            return this;
        }

        /// <summary>Re-encodes a decoded element under a new tag (context tags only are preserved for children).</summary>
        public TlvWriter PutElement(byte? tag, TlvElement element)
        {
            switch (element.Kind)
            {
                case TlvKind.SignedInt:
                    PutInt(tag, (long)element.Value);
                    break;
                case TlvKind.UnsignedInt:
                    PutUInt(tag, (ulong)element.Value);
                    break;
                case TlvKind.Bool:
                    PutBool(tag, (bool)element.Value);
                    break;
                case TlvKind.Float:
                    PutFloat(tag, (float)element.Value);
                    break;
                case TlvKind.Double:
                    PutDouble(tag, (double)element.Value);
                    break;
                case TlvKind.Utf8String:
                    PutString(tag, (string)element.Value);
                    break;
                case TlvKind.ByteString:
                    PutBytes(tag, (byte[])element.Value);
                    break;
                case TlvKind.Null:
                    PutNull(tag);
                    break;
                case TlvKind.Structure:
                case TlvKind.Array:
                case TlvKind.List:
                    if (element.Kind == TlvKind.Structure)
                    {
                        StartStructure(tag);
                    }
                    else if (element.Kind == TlvKind.Array)
                    {
                        StartArray(tag);
                    }
                    else
                    {
                        StartList(tag);
                    }

                    foreach (var child in element.Children)
                    {
                        PutElement(child.IsContextTag ? (byte)child.Tag : null, child);
                    }

                    EndContainer();
                    break;
            }

            return this;
        }

        public static byte[] Encode(Action<TlvWriter> write)
        {
            var w = new TlvWriter();
            write(w);
            return w.ToArray();
        }
    }
}
