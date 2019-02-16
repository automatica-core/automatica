using System;
using Automatica.Core.WebApi.Converter.MessagePack.GuidFormatters.MessagePack.Internal;
using MessagePack;
using MessagePack.Formatters;

namespace Automatica.Core.WebApi.Converter.MessagePack.GuidFormatters
{

    public sealed class NullableGuidFormatter : IMessagePackFormatter<Guid?>
    {
        public static readonly IMessagePackFormatter<Guid?> Instance = new NullableGuidFormatter();


        NullableGuidFormatter()
        {

        }

        public int Serialize(ref byte[] bytes, int offset, Guid? value, IFormatterResolver formatterResolver)
        {
            if (value == null)
            {
                return MessagePackBinary.WriteNil(ref bytes, offset);
            }
            MessagePackBinary.EnsureCapacity(ref bytes, offset, 38);

            var guid = value.Value;
            bytes[offset] = MessagePackCode.Str8;
            bytes[offset + 1] = unchecked((byte) 36);
            new GuidBits(ref guid).Write(bytes, offset + 2);
            return 38;
        }

        public Guid? Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            var segment = MessagePackBinary.ReadStringSegment(bytes, offset, out readSize);

            try
            {
                var guid = new GuidBits(segment);
                return guid.Value;
            }
            catch
            {
                return null;
            }
        }
    }
}
