using P3.Driver.Matter.DataModel;
using P3.Driver.Matter.Fabrics;
using P3.Driver.Matter.Tlv;

namespace P3.Driver.Matter.InteractionModel
{
    public sealed class AttributePath
    {
        public ushort? Endpoint { get; init; }
        public uint? Cluster { get; init; }
        public uint? Attribute { get; init; }

        /// <summary>True when the path carried a ListIndex (only null, i.e. append, is valid).</summary>
        public bool HasListIndex { get; init; }

        public bool IsConcrete => Endpoint.HasValue && Cluster.HasValue && Attribute.HasValue;

        public static AttributePath Parse(TlvElement e)
        {
            var list = e.Get(5);
            if (list != null && !list.IsNull)
            {
                throw new MatterStatusException(StatusCode.InvalidAction, "Only null list indices are supported");
            }

            return new AttributePath
            {
                Endpoint = e.GetUIntOrNull(2) is { } ep ? (ushort)ep : null,
                Cluster = e.GetUIntOrNull(3) is { } cl ? (uint)cl : null,
                Attribute = e.GetUIntOrNull(4) is { } at ? (uint)at : null,
                HasListIndex = list != null
            };
        }

        public bool Matches(ushort endpoint, uint cluster, uint attribute) =>
            (!Endpoint.HasValue || Endpoint == endpoint) && (!Cluster.HasValue || Cluster == cluster) && (!Attribute.HasValue || Attribute == attribute);

        public static void Write(TlvWriter w, byte? tag, ushort endpoint, uint cluster, uint attribute, bool appendListItem = false)
        {
            w.StartList(tag);
            w.PutUInt(2, endpoint);
            w.PutUInt(3, cluster);
            w.PutUInt(4, attribute);
            if (appendListItem)
            {
                w.PutNull(5);
            }

            w.EndContainer();
        }

        public override string ToString() => $"{Endpoint?.ToString() ?? "*"}/{Cluster?.ToString("X4") ?? "*"}/{Attribute?.ToString("X4") ?? "*"}";
    }

    public sealed class CommandPath
    {
        public ushort? Endpoint { get; init; }
        public uint Cluster { get; init; }
        public uint Command { get; init; }

        public static CommandPath Parse(TlvElement e) => new()
        {
            Endpoint = e.GetUIntOrNull(0) is { } ep ? (ushort)ep : null,
            Cluster = (uint)e.GetUInt(1),
            Command = (uint)e.GetUInt(2)
        };

        public static void Write(TlvWriter w, byte? tag, ushort endpoint, uint cluster, uint command)
        {
            w.StartList(tag);
            w.PutUInt(0, endpoint);
            w.PutUInt(1, cluster);
            w.PutUInt(2, command);
            w.EndContainer();
        }
    }

    /// <summary>Access control (spec 6.6): PASE sessions are administrators, CASE sessions go through the fabric ACL.</summary>
    public static class AccessControl
    {
        public static bool Check(AccessContext ctx, Endpoint endpoint, uint cluster, AclPrivilege required)
        {
            var session = ctx.Session;
            if (session == null)
            {
                return false;
            }

            if (session.IsPase)
            {
                return true;
            }

            var fabric = session.Fabric;
            if (fabric == null)
            {
                return false;
            }

            foreach (var entry in fabric.Acl)
            {
                if (entry.AuthMode != AclAuthMode.Case || !Grants(entry.Privilege, required))
                {
                    continue;
                }

                if (entry.Subjects is { Count: > 0 } && !entry.Subjects.Any(s => SubjectMatches(s, session.PeerNodeId, session.CaseAuthenticatedTags)))
                {
                    continue;
                }

                if (entry.Targets is { Count: > 0 } && !entry.Targets.Any(t =>
                        (!t.Cluster.HasValue || t.Cluster == cluster) &&
                        (!t.Endpoint.HasValue || t.Endpoint == endpoint.Id) &&
                        (!t.DeviceType.HasValue || endpoint.DeviceTypes.Any(d => d.Id == t.DeviceType))))
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        private static bool Grants(AclPrivilege granted, AclPrivilege required)
        {
            if (granted == AclPrivilege.ProxyView)
            {
                return required is AclPrivilege.View or AclPrivilege.ProxyView;
            }

            if (required == AclPrivilege.ProxyView)
            {
                return granted == AclPrivilege.Administer;
            }

            return granted >= required;
        }

        private static bool SubjectMatches(ulong subject, ulong peerNodeId, uint[] cats)
        {
            if (subject == peerNodeId)
            {
                return true;
            }

            if ((subject >> 32) != 0xFFFFFFFD)
            {
                return false;
            }

            var catId = (ushort)(subject >> 16);
            var catVersion = (ushort)subject;
            return cats.Any(c => (ushort)(c >> 16) == catId && (ushort)c >= catVersion);
        }
    }
}
