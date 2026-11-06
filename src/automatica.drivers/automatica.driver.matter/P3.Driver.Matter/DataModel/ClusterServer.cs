using System.Security.Cryptography;
using P3.Driver.Matter.Exchanges;
using P3.Driver.Matter.Fabrics;
using P3.Driver.Matter.InteractionModel;
using P3.Driver.Matter.Sessions;
using P3.Driver.Matter.Tlv;

namespace P3.Driver.Matter.DataModel
{
    public sealed class AccessContext
    {
        public SecureSession Session { get; init; }
        public bool IsFabricFiltered { get; init; } = true;
        public byte FabricIndex => Session?.FabricIndex ?? 0;
        public Fabric Fabric => Session?.Fabric;
    }

    public sealed class InvokeContext
    {
        public AccessContext Access { get; init; }
        public MessageExchange Exchange { get; init; }
        public bool IsTimed { get; init; }
        public SecureSession Session => Access.Session;
    }

    public enum ListWriteMode
    {
        /// <summary>Not a list, or the whole list is replaced.</summary>
        Replace,

        /// <summary>One list item is appended (path ListIndex is null).</summary>
        Append
    }

    public delegate void AttributeEncoder(TlvWriter writer, byte? tag, AccessContext context);

    public delegate void AttributeWriter(TlvElement value, AccessContext context, ListWriteMode mode);

    public delegate Task<InvokeResult> CommandHandler(InvokeContext context, TlvElement fields);

    public sealed class AttributeDefinition
    {
        public uint Id { get; init; }
        public AttributeEncoder Encode { get; init; }
        public AttributeWriter Write { get; init; }
        public AclPrivilege ReadPrivilege { get; init; } = AclPrivilege.View;
        public AclPrivilege WritePrivilege { get; init; } = AclPrivilege.Operate;
        public bool RequiresTimedWrite { get; init; }
    }

    public sealed class CommandDefinition
    {
        public uint Id { get; init; }
        public CommandHandler Handler { get; init; }
        public uint? ResponseId { get; init; }
        public AclPrivilege Privilege { get; init; } = AclPrivilege.Operate;
        public bool RequiresTimed { get; init; }
    }

    public sealed class InvokeResult
    {
        public StatusCode Status { get; private init; }
        public byte? ClusterStatus { get; private init; }
        public uint? ResponseCommandId { get; private init; }
        public Action<TlvWriter> WriteFields { get; private init; }

        public static InvokeResult Success() => new() { Status = StatusCode.Success };

        public static InvokeResult Error(StatusCode status, byte? clusterStatus = null) => new() { Status = status, ClusterStatus = clusterStatus };

        /// <summary>A response command; <paramref name="writeFields"/> writes the context-tagged fields into the open struct.</summary>
        public static InvokeResult Response(uint commandId, Action<TlvWriter> writeFields) =>
            new() { Status = StatusCode.Success, ResponseCommandId = commandId, WriteFields = writeFields };

        public static Task<InvokeResult> SuccessTask() => Task.FromResult(Success());
    }

    public static class GlobalAttribute
    {
        public const uint GeneratedCommandList = 0xFFF8;
        public const uint AcceptedCommandList = 0xFFF9;
        public const uint AttributeList = 0xFFFB;
        public const uint FeatureMap = 0xFFFC;
        public const uint ClusterRevision = 0xFFFD;
    }

    /// <summary>Base class of a cluster's server side: attributes, commands, data version and change notification.</summary>
    public abstract class ClusterServer
    {
        private readonly SortedDictionary<uint, AttributeDefinition> _attributes = new();
        private readonly SortedDictionary<uint, CommandDefinition> _commands = new();

        protected ClusterServer(uint clusterId, ushort revision)
        {
            ClusterId = clusterId;
            Revision = revision;
            DataVersion = (uint)RandomNumberGenerator.GetInt32(int.MaxValue);
        }

        public uint ClusterId { get; }
        public ushort Revision { get; }
        public uint FeatureMap { get; protected set; }
        public uint DataVersion { get; private set; }
        public Endpoint Endpoint { get; internal set; }

        public IEnumerable<uint> AttributeIds => _attributes.Keys.Concat(new[]
        {
            GlobalAttribute.GeneratedCommandList, GlobalAttribute.AcceptedCommandList, GlobalAttribute.AttributeList,
            GlobalAttribute.FeatureMap, GlobalAttribute.ClusterRevision
        });

        public bool HasAttribute(uint id) => _attributes.ContainsKey(id) || id is GlobalAttribute.GeneratedCommandList
            or GlobalAttribute.AcceptedCommandList or GlobalAttribute.AttributeList or GlobalAttribute.FeatureMap or GlobalAttribute.ClusterRevision;

        public AttributeDefinition GetAttribute(uint id)
        {
            if (_attributes.TryGetValue(id, out var def))
            {
                return def;
            }

            return id switch
            {
                GlobalAttribute.ClusterRevision => new AttributeDefinition { Id = id, Encode = (w, t, _) => w.PutUInt(t, Revision) },
                GlobalAttribute.FeatureMap => new AttributeDefinition { Id = id, Encode = (w, t, _) => w.PutUInt(t, FeatureMap) },
                GlobalAttribute.AttributeList => new AttributeDefinition { Id = id, Encode = (w, t, _) => WriteList(w, t, AttributeIds.OrderBy(a => a)) },
                GlobalAttribute.AcceptedCommandList => new AttributeDefinition { Id = id, Encode = (w, t, _) => WriteList(w, t, _commands.Keys) },
                GlobalAttribute.GeneratedCommandList => new AttributeDefinition
                {
                    Id = id,
                    Encode = (w, t, _) => WriteList(w, t, _commands.Values.Where(c => c.ResponseId.HasValue).Select(c => c.ResponseId.Value).Distinct().OrderBy(c => c))
                },
                _ => null
            };
        }

        public CommandDefinition GetCommand(uint id) => _commands.TryGetValue(id, out var c) ? c : null;

        private static void WriteList(TlvWriter w, byte? tag, IEnumerable<uint> values)
        {
            w.StartArray(tag);
            foreach (var v in values)
            {
                w.PutUInt(null, v);
            }

            w.EndContainer();
        }

        protected void Attribute(uint id, AttributeEncoder encode, AttributeWriter write = null,
            AclPrivilege readPrivilege = AclPrivilege.View, AclPrivilege writePrivilege = AclPrivilege.Operate, bool timedWrite = false)
        {
            _attributes[id] = new AttributeDefinition
            {
                Id = id,
                Encode = encode,
                Write = write,
                ReadPrivilege = readPrivilege,
                WritePrivilege = writePrivilege,
                RequiresTimedWrite = timedWrite
            };
        }

        protected void Command(uint id, CommandHandler handler, uint? responseId = null, AclPrivilege privilege = AclPrivilege.Operate, bool timed = false)
        {
            _commands[id] = new CommandDefinition { Id = id, Handler = handler, ResponseId = responseId, Privilege = privilege, RequiresTimed = timed };
        }

        /// <summary>Bumps the data version and notifies subscriptions that <paramref name="attributeId"/> changed.</summary>
        public void MarkChanged(uint attributeId)
        {
            DataVersion = unchecked(DataVersion + 1);
            Endpoint?.Node?.RaiseAttributeChanged(Endpoint.Id, ClusterId, attributeId);
        }

        protected static void PutNullableUInt(TlvWriter w, byte? tag, ulong? value)
        {
            if (value.HasValue)
            {
                w.PutUInt(tag, value.Value);
            }
            else
            {
                w.PutNull(tag);
            }
        }

        protected static T Enum<T>(TlvElement value, params T[] allowed) where T : struct, System.Enum
        {
            var raw = value.AsUInt();
            var e = (T)System.Enum.ToObject(typeof(T), raw);
            if (allowed.Length > 0 ? !allowed.Contains(e) : !System.Enum.IsDefined(e))
            {
                throw new MatterStatusException(StatusCode.ConstraintError);
            }

            return e;
        }
    }
}
