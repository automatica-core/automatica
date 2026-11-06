using System.Buffers.Binary;
using P3.Driver.Matter.Sessions;
using P3.Driver.Matter.Tlv;

namespace P3.Driver.Matter.SecureChannel
{
    public static class SecureChannelOpcode
    {
        public const byte MsgCounterSyncReq = 0x00;
        public const byte MsgCounterSyncRsp = 0x01;
        public const byte StandaloneAck = 0x10;
        public const byte PbkdfParamRequest = 0x20;
        public const byte PbkdfParamResponse = 0x21;
        public const byte Pake1 = 0x22;
        public const byte Pake2 = 0x23;
        public const byte Pake3 = 0x24;
        public const byte Sigma1 = 0x30;
        public const byte Sigma2 = 0x31;
        public const byte Sigma3 = 0x32;
        public const byte Sigma2Resume = 0x33;
        public const byte StatusReport = 0x40;
    }

    public static class GeneralStatus
    {
        public const ushort Success = 0;
        public const ushort Failure = 1;
        public const ushort BadPrecondition = 2;
        public const ushort OutOfRange = 3;
        public const ushort BadRequest = 4;
        public const ushort Unsupported = 5;
        public const ushort Unexpected = 6;
        public const ushort ResourceExhausted = 7;
        public const ushort Busy = 8;
    }

    public static class SecureChannelStatus
    {
        public const ushort SessionEstablishmentSuccess = 0x0000;
        public const ushort NoSharedTrustRoots = 0x0001;
        public const ushort InvalidParameter = 0x0002;
        public const ushort CloseSession = 0x0003;
        public const ushort Busy = 0x0004;
    }

    /// <summary>The secure channel StatusReport message (spec 4.11.1.2 / Appendix D).</summary>
    public sealed class StatusReport
    {
        public ushort GeneralCode { get; init; }
        public uint ProtocolId { get; init; }
        public ushort ProtocolCode { get; init; }

        public bool IsSuccess => GeneralCode == GeneralStatus.Success;

        public byte[] Encode()
        {
            var buf = new byte[8];
            BinaryPrimitives.WriteUInt16LittleEndian(buf, GeneralCode);
            BinaryPrimitives.WriteUInt32LittleEndian(buf.AsSpan(2), ProtocolId);
            BinaryPrimitives.WriteUInt16LittleEndian(buf.AsSpan(6), ProtocolCode);
            return buf;
        }

        public static StatusReport Decode(ReadOnlySpan<byte> data)
        {
            if (data.Length < 8)
            {
                throw new MatterProtocolException("StatusReport too short");
            }

            return new StatusReport
            {
                GeneralCode = BinaryPrimitives.ReadUInt16LittleEndian(data),
                ProtocolId = BinaryPrimitives.ReadUInt32LittleEndian(data[2..]),
                ProtocolCode = BinaryPrimitives.ReadUInt16LittleEndian(data[6..])
            };
        }

        public static StatusReport Success() => new() { GeneralCode = GeneralStatus.Success, ProtocolCode = SecureChannelStatus.SessionEstablishmentSuccess };

        public static StatusReport Error(ushort protocolCode, ushort generalCode = GeneralStatus.Failure) =>
            new() { GeneralCode = generalCode, ProtocolCode = protocolCode };

        public override string ToString() => $"StatusReport general={GeneralCode} protocol={ProtocolId:X8} code={ProtocolCode}";
    }

    internal static class SessionParametersTlv
    {
        public const ushort DataModelRevision = 17;
        public const ushort InteractionModelRevision = 11;
        public const uint SpecificationVersion = 0x01030000;

        public static void Write(TlvWriter w, byte tag)
        {
            w.StartStructure(tag);
            w.PutUInt(1, 500);
            w.PutUInt(2, 300);
            w.PutUInt(3, 4000);
            w.PutUInt(4, DataModelRevision);
            w.PutUInt(5, InteractionModelRevision);
            w.PutUInt(6, SpecificationVersion);
            w.PutUInt(7, 1);
            w.EndContainer();
        }

        public static SessionParameters Read(TlvElement e)
        {
            var p = new SessionParameters();
            if (e == null)
            {
                return p;
            }

            if (e.GetUIntOrNull(1) is { } idle) p.IdleIntervalMs = (uint)idle;
            if (e.GetUIntOrNull(2) is { } active) p.ActiveIntervalMs = (uint)active;
            if (e.GetUIntOrNull(3) is { } threshold) p.ActiveThresholdMs = (ushort)threshold;
            return p;
        }
    }
}
