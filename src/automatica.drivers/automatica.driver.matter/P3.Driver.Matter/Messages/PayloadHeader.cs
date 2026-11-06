using System.Buffers.Binary;

namespace P3.Driver.Matter.Messages
{
    public static class ProtocolId
    {
        public const ushort SecureChannel = 0x0000;
        public const ushort InteractionModel = 0x0001;
        public const ushort Bdx = 0x0002;
        public const ushort UserDirectedCommissioning = 0x0003;
    }

    /// <summary>The protocol (exchange) header that starts the possibly encrypted payload (spec 4.4.3).</summary>
    public sealed class PayloadHeader
    {
        public const byte FlagInitiator = 0x01;
        public const byte FlagAck = 0x02;
        public const byte FlagReliability = 0x04;
        public const byte FlagSecuredExtensions = 0x08;
        public const byte FlagVendor = 0x10;

        public byte ExchangeFlags { get; set; }
        public byte Opcode { get; set; }
        public ushort ExchangeId { get; set; }
        public ushort? VendorId { get; set; }
        public ushort ProtocolId { get; set; }
        public uint? AckedMessageCounter { get; set; }

        public bool IsInitiator
        {
            get => (ExchangeFlags & FlagInitiator) != 0;
            set => ExchangeFlags = (byte)(value ? ExchangeFlags | FlagInitiator : ExchangeFlags & ~FlagInitiator);
        }

        public bool NeedsAck
        {
            get => (ExchangeFlags & FlagReliability) != 0;
            set => ExchangeFlags = (byte)(value ? ExchangeFlags | FlagReliability : ExchangeFlags & ~FlagReliability);
        }

        public byte[] Encode()
        {
            var flags = (byte)(ExchangeFlags & ~(FlagAck | FlagVendor | FlagSecuredExtensions));
            if (AckedMessageCounter.HasValue)
            {
                flags |= FlagAck;
            }

            if (VendorId.HasValue)
            {
                flags |= FlagVendor;
            }

            var size = 6 + (VendorId.HasValue ? 2 : 0) + (AckedMessageCounter.HasValue ? 4 : 0);
            var buf = new byte[size];
            buf[0] = flags;
            buf[1] = Opcode;
            BinaryPrimitives.WriteUInt16LittleEndian(buf.AsSpan(2), ExchangeId);
            var pos = 4;
            if (VendorId.HasValue)
            {
                BinaryPrimitives.WriteUInt16LittleEndian(buf.AsSpan(pos), VendorId.Value);
                pos += 2;
            }

            BinaryPrimitives.WriteUInt16LittleEndian(buf.AsSpan(pos), ProtocolId);
            pos += 2;
            if (AckedMessageCounter.HasValue)
            {
                BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(pos), AckedMessageCounter.Value);
            }

            return buf;
        }

        public static PayloadHeader Decode(ReadOnlySpan<byte> data, out int length)
        {
            if (data.Length < 6)
            {
                throw new MatterProtocolException("Payload header too short");
            }

            var h = new PayloadHeader
            {
                ExchangeFlags = data[0],
                Opcode = data[1],
                ExchangeId = BinaryPrimitives.ReadUInt16LittleEndian(data[2..])
            };
            var pos = 4;
            if ((h.ExchangeFlags & FlagVendor) != 0)
            {
                h.VendorId = BinaryPrimitives.ReadUInt16LittleEndian(data[pos..]);
                pos += 2;
            }

            if (pos + 2 > data.Length)
            {
                throw new MatterProtocolException("Payload header too short");
            }

            h.ProtocolId = BinaryPrimitives.ReadUInt16LittleEndian(data[pos..]);
            pos += 2;
            if ((h.ExchangeFlags & FlagAck) != 0)
            {
                if (pos + 4 > data.Length)
                {
                    throw new MatterProtocolException("Payload header too short");
                }

                h.AckedMessageCounter = BinaryPrimitives.ReadUInt32LittleEndian(data[pos..]);
                pos += 4;
            }

            if ((h.ExchangeFlags & FlagSecuredExtensions) != 0)
            {
                if (pos + 2 > data.Length)
                {
                    throw new MatterProtocolException("Payload header too short");
                }

                var extLen = BinaryPrimitives.ReadUInt16LittleEndian(data[pos..]);
                pos += 2 + extLen;
            }

            if (pos > data.Length)
            {
                throw new MatterProtocolException("Payload header too short");
            }

            length = pos;
            return h;
        }
    }

    /// <summary>A received (decrypted) or outgoing message.</summary>
    public sealed class MatterMessage
    {
        public MessageHeader Header { get; init; }
        public PayloadHeader PayloadHeader { get; init; }
        public byte[] Payload { get; init; } = Array.Empty<byte>();

        public ushort ProtocolId => PayloadHeader.ProtocolId;
        public byte Opcode => PayloadHeader.Opcode;

        public override string ToString() =>
            $"ex={PayloadHeader.ExchangeId}{(PayloadHeader.IsInitiator ? "i" : "r")} proto={ProtocolId:X4} op=0x{Opcode:X2} ctr={Header.MessageCounter} " +
            $"{(PayloadHeader.NeedsAck ? "R " : "")}{(PayloadHeader.AckedMessageCounter.HasValue ? "A=" + PayloadHeader.AckedMessageCounter : "")} len={Payload.Length}";
    }
}
