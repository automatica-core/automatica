using System.Buffers.Binary;

namespace P3.Driver.Matter.Messages
{
    /// <summary>The unencrypted message header (spec 4.4.1).</summary>
    public sealed class MessageHeader
    {
        public const byte FlagSourceNodeIdPresent = 0x04;
        public const byte DsizNodeId = 0x01;
        public const byte DsizGroupId = 0x02;

        public const byte SecurityFlagPrivacy = 0x80;
        public const byte SecurityFlagControl = 0x40;
        public const byte SecurityFlagExtensions = 0x20;
        public const byte SessionTypeGroup = 0x01;

        public ushort SessionId { get; set; }
        public byte SecurityFlags { get; set; }
        public uint MessageCounter { get; set; }
        public ulong? SourceNodeId { get; set; }
        public ulong? DestinationNodeId { get; set; }
        public ushort? DestinationGroupId { get; set; }

        public bool IsGroupSession => (SecurityFlags & 0x03) == SessionTypeGroup;
        public bool IsUnsecured => SessionId == 0 && !IsGroupSession;

        public byte[] Encode()
        {
            var size = 8 + (SourceNodeId.HasValue ? 8 : 0) + (DestinationNodeId.HasValue ? 8 : DestinationGroupId.HasValue ? 2 : 0);
            var buf = new byte[size];
            byte flags = 0;
            if (SourceNodeId.HasValue)
            {
                flags |= FlagSourceNodeIdPresent;
            }

            if (DestinationNodeId.HasValue)
            {
                flags |= DsizNodeId;
            }
            else if (DestinationGroupId.HasValue)
            {
                flags |= DsizGroupId;
            }

            buf[0] = flags;
            BinaryPrimitives.WriteUInt16LittleEndian(buf.AsSpan(1), SessionId);
            buf[3] = (byte)(SecurityFlags & ~SecurityFlagExtensions);
            BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(4), MessageCounter);
            var pos = 8;
            if (SourceNodeId.HasValue)
            {
                BinaryPrimitives.WriteUInt64LittleEndian(buf.AsSpan(pos), SourceNodeId.Value);
                pos += 8;
            }

            if (DestinationNodeId.HasValue)
            {
                BinaryPrimitives.WriteUInt64LittleEndian(buf.AsSpan(pos), DestinationNodeId.Value);
            }
            else if (DestinationGroupId.HasValue)
            {
                BinaryPrimitives.WriteUInt16LittleEndian(buf.AsSpan(pos), DestinationGroupId.Value);
            }

            return buf;
        }

        /// <summary>Decodes the header; <paramref name="headerLength"/> covers message extensions too (they are the AAD).</summary>
        public static MessageHeader Decode(ReadOnlySpan<byte> data, out int headerLength)
        {
            if (data.Length < 8)
            {
                throw new MatterProtocolException("Message too short");
            }

            var flags = data[0];
            if (flags >> 4 != 0)
            {
                throw new MatterProtocolException($"Unsupported message version {flags >> 4}");
            }

            var header = new MessageHeader
            {
                SessionId = BinaryPrimitives.ReadUInt16LittleEndian(data[1..]),
                SecurityFlags = data[3],
                MessageCounter = BinaryPrimitives.ReadUInt32LittleEndian(data[4..])
            };
            var pos = 8;
            if ((flags & FlagSourceNodeIdPresent) != 0)
            {
                Need(data, pos, 8);
                header.SourceNodeId = BinaryPrimitives.ReadUInt64LittleEndian(data[pos..]);
                pos += 8;
            }

            switch (flags & 0x03)
            {
                case DsizNodeId:
                    Need(data, pos, 8);
                    header.DestinationNodeId = BinaryPrimitives.ReadUInt64LittleEndian(data[pos..]);
                    pos += 8;
                    break;
                case DsizGroupId:
                    Need(data, pos, 2);
                    header.DestinationGroupId = BinaryPrimitives.ReadUInt16LittleEndian(data[pos..]);
                    pos += 2;
                    break;
                case 0x03:
                    throw new MatterProtocolException("Invalid DSIZ");
            }

            if ((header.SecurityFlags & SecurityFlagExtensions) != 0)
            {
                Need(data, pos, 2);
                var extLen = BinaryPrimitives.ReadUInt16LittleEndian(data[pos..]);
                pos += 2 + extLen;
                Need(data, pos, 0);
            }

            headerLength = pos;
            return header;
        }

        private static void Need(ReadOnlySpan<byte> data, int pos, int count)
        {
            if (pos + count > data.Length)
            {
                throw new MatterProtocolException("Truncated message header");
            }
        }

        /// <summary>The AES-CCM nonce: security flags || counter (LE) || source node id (LE).</summary>
        public static byte[] BuildNonce(byte securityFlags, uint messageCounter, ulong sourceNodeId)
        {
            var nonce = new byte[13];
            nonce[0] = securityFlags;
            BinaryPrimitives.WriteUInt32LittleEndian(nonce.AsSpan(1), messageCounter);
            BinaryPrimitives.WriteUInt64LittleEndian(nonce.AsSpan(5), sourceNodeId);
            return nonce;
        }
    }
}
