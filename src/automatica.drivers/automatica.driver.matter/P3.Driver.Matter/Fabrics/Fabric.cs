using System.Buffers.Binary;
using System.Security.Cryptography;
using P3.Driver.Matter.Certificates;
using P3.Driver.Matter.Crypto;
using P3.Driver.Matter.Tlv;

namespace P3.Driver.Matter.Fabrics
{
    public enum AclPrivilege : byte
    {
        View = 1,
        ProxyView = 2,
        Operate = 3,
        Manage = 4,
        Administer = 5
    }

    public enum AclAuthMode : byte
    {
        Pase = 1,
        Case = 2,
        Group = 3
    }

    public sealed class AclTarget
    {
        public uint? Cluster { get; set; }
        public ushort? Endpoint { get; set; }
        public uint? DeviceType { get; set; }
    }

    public sealed class AclEntry
    {
        public AclPrivilege Privilege { get; set; }
        public AclAuthMode AuthMode { get; set; }
        public List<ulong> Subjects { get; set; }
        public List<AclTarget> Targets { get; set; }
        public byte FabricIndex { get; set; }

        public void Write(TlvWriter w, byte? tag)
        {
            w.StartStructure(tag);
            w.PutUInt(1, (byte)Privilege);
            w.PutUInt(2, (byte)AuthMode);
            if (Subjects == null)
            {
                w.PutNull(3);
            }
            else
            {
                w.StartArray(3);
                foreach (var s in Subjects)
                {
                    w.PutUInt(null, s);
                }

                w.EndContainer();
            }

            if (Targets == null)
            {
                w.PutNull(4);
            }
            else
            {
                w.StartArray(4);
                foreach (var t in Targets)
                {
                    w.StartStructure();
                    if (t.Cluster.HasValue) w.PutUInt(0, t.Cluster.Value); else w.PutNull(0);
                    if (t.Endpoint.HasValue) w.PutUInt(1, t.Endpoint.Value); else w.PutNull(1);
                    if (t.DeviceType.HasValue) w.PutUInt(2, t.DeviceType.Value); else w.PutNull(2);
                    w.EndContainer();
                }

                w.EndContainer();
            }

            w.PutUInt(0xFE, FabricIndex);
            w.EndContainer();
        }

        public static AclEntry Read(TlvElement e, byte fabricIndex)
        {
            var entry = new AclEntry
            {
                Privilege = (AclPrivilege)e.GetUInt(1),
                AuthMode = (AclAuthMode)e.GetUInt(2),
                FabricIndex = fabricIndex
            };
            var subjects = e.Get(3);
            if (subjects != null && !subjects.IsNull)
            {
                entry.Subjects = subjects.Children.Select(c => c.AsUInt()).ToList();
            }

            var targets = e.Get(4);
            if (targets != null && !targets.IsNull)
            {
                entry.Targets = targets.Children.Select(c => new AclTarget
                {
                    Cluster = c.GetUIntOrNull(0) is { } cl ? (uint)cl : null,
                    Endpoint = c.GetUIntOrNull(1) is { } ep ? (ushort)ep : null,
                    DeviceType = c.GetUIntOrNull(2) is { } dt ? (uint)dt : null
                }).ToList();
            }

            if (!Enum.IsDefined(entry.Privilege) || !Enum.IsDefined(entry.AuthMode) || entry.AuthMode == AclAuthMode.Pase)
            {
                throw new MatterStatusException(InteractionModel.StatusCode.ConstraintError, "Invalid ACL entry");
            }

            if (entry.AuthMode == AclAuthMode.Group && entry.Privilege == AclPrivilege.Administer)
            {
                throw new MatterStatusException(InteractionModel.StatusCode.ConstraintError, "Group ACL entries cannot administer");
            }

            return entry;
        }

        public byte[] Encode() => TlvWriter.Encode(w => Write(w, null));
    }

    /// <summary>One commissioned fabric (spec 2.5.1 / 11.18): its root of trust, our NOC and operational key.</summary>
    public sealed class Fabric : IDisposable
    {
        public byte FabricIndex { get; init; }
        public byte[] RootCert { get; init; }
        public byte[] Noc { get; set; }
        public byte[] Icac { get; set; }
        public byte[] IpkEpochKey { get; set; }
        public ushort VendorId { get; set; }
        public ulong CaseAdminSubject { get; set; }
        public string Label { get; set; } = "";
        public ECDsa OperationalKey { get; set; }
        public List<AclEntry> Acl { get; set; } = new();

        public ulong FabricId { get; private set; }
        public ulong NodeId { get; private set; }
        public byte[] RootPublicKey { get; private set; }
        public byte[] CompressedFabricId { get; private set; }
        public byte[] OperationalIpk { get; private set; }

        /// <summary>Derives the identifiers from the certificates; call after the certs or IPK change.</summary>
        public void Initialize()
        {
            var root = new MatterCertificate(RootCert);
            var noc = new MatterCertificate(Noc);
            RootPublicKey = root.PublicKey;
            FabricId = noc.SubjectFabricId ?? throw new MatterProtocolException("NOC has no fabric id");
            NodeId = noc.SubjectNodeId ?? throw new MatterProtocolException("NOC has no node id");
            CompressedFabricId = ComputeCompressedFabricId(RootPublicKey, FabricId);
            OperationalIpk = MatterCrypto.Kdf(IpkEpochKey, CompressedFabricId, "GroupKey v1.0"u8.ToArray(), 16);
        }

        public static byte[] ComputeCompressedFabricId(byte[] rootPublicKey, ulong fabricId)
        {
            var salt = new byte[8];
            BinaryPrimitives.WriteUInt64BigEndian(salt, fabricId);
            return MatterCrypto.Kdf(rootPublicKey[1..], salt, "CompressedFabric"u8.ToArray(), 8);
        }

        public string CompressedFabricIdHex => Convert.ToHexString(CompressedFabricId);

        public string OperationalInstanceName => $"{CompressedFabricIdHex}-{NodeId:X16}";

        /// <summary>The CASE destination identifier the initiator computes for this fabric (spec 4.14.2.4.1).</summary>
        public byte[] ComputeDestinationId(byte[] initiatorRandom)
        {
            var msg = new byte[initiatorRandom.Length + RootPublicKey.Length + 16];
            initiatorRandom.CopyTo(msg, 0);
            RootPublicKey.CopyTo(msg, initiatorRandom.Length);
            BinaryPrimitives.WriteUInt64LittleEndian(msg.AsSpan(initiatorRandom.Length + RootPublicKey.Length), FabricId);
            BinaryPrimitives.WriteUInt64LittleEndian(msg.AsSpan(initiatorRandom.Length + RootPublicKey.Length + 8), NodeId);
            return MatterCrypto.HmacSha256(OperationalIpk, msg);
        }

        public void Dispose()
        {
            OperationalKey?.Dispose();
        }
    }
}
