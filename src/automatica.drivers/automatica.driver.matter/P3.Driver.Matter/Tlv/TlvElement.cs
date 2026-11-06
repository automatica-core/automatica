using System.Text;

namespace P3.Driver.Matter.Tlv
{
    public enum TlvKind
    {
        SignedInt,
        UnsignedInt,
        Bool,
        Float,
        Double,
        Utf8String,
        ByteString,
        Null,
        Structure,
        Array,
        List
    }

    /// <summary>
    /// A decoded Matter TLV element (spec Appendix A). Containers keep their children in order.
    /// Only anonymous and context-specific tags are interpreted; profile tags are kept as raw numbers.
    /// </summary>
    public sealed class TlvElement
    {
        public const int AnonymousTag = -1;

        public TlvKind Kind { get; init; }

        /// <summary>Context tag number (0..255), <see cref="AnonymousTag"/> for anonymous elements, or the
        /// profile-specific tag number for profile tags (see <see cref="IsProfileTag"/>).</summary>
        public long Tag { get; init; } = AnonymousTag;

        public bool IsProfileTag { get; init; }

        public object Value { get; init; }

        public List<TlvElement> Children { get; } = new();

        public bool IsContainer => Kind is TlvKind.Structure or TlvKind.Array or TlvKind.List;

        public bool IsContextTag => !IsProfileTag && Tag >= 0;

        public TlvElement Get(int contextTag)
        {
            foreach (var child in Children)
            {
                if (child.IsContextTag && child.Tag == contextTag)
                {
                    return child;
                }
            }

            return null;
        }

        public bool Has(int contextTag) => Get(contextTag) != null;

        public ulong AsUInt()
        {
            return Kind switch
            {
                TlvKind.UnsignedInt => (ulong)Value,
                TlvKind.SignedInt => (ulong)(long)Value,
                TlvKind.Bool => (bool)Value ? 1UL : 0UL,
                _ => throw new MatterProtocolException($"TLV element {Tag} is {Kind}, expected an unsigned integer")
            };
        }

        public long AsInt()
        {
            return Kind switch
            {
                TlvKind.SignedInt => (long)Value,
                TlvKind.UnsignedInt => (long)(ulong)Value,
                _ => throw new MatterProtocolException($"TLV element {Tag} is {Kind}, expected an integer")
            };
        }

        public bool AsBool()
        {
            return Kind switch
            {
                TlvKind.Bool => (bool)Value,
                TlvKind.UnsignedInt => (ulong)Value != 0,
                _ => throw new MatterProtocolException($"TLV element {Tag} is {Kind}, expected a boolean")
            };
        }

        public byte[] AsBytes()
        {
            if (Kind != TlvKind.ByteString)
            {
                throw new MatterProtocolException($"TLV element {Tag} is {Kind}, expected an octet string");
            }

            return (byte[])Value;
        }

        public string AsString()
        {
            if (Kind != TlvKind.Utf8String)
            {
                throw new MatterProtocolException($"TLV element {Tag} is {Kind}, expected a string");
            }

            return (string)Value;
        }

        public double AsDouble()
        {
            return Kind switch
            {
                TlvKind.Double => (double)Value,
                TlvKind.Float => (float)Value,
                TlvKind.SignedInt => (long)Value,
                TlvKind.UnsignedInt => (ulong)Value,
                _ => throw new MatterProtocolException($"TLV element {Tag} is {Kind}, expected a number")
            };
        }

        public bool IsNull => Kind == TlvKind.Null;

        public ulong GetUInt(int tag) => Require(tag).AsUInt();
        public ulong? GetUIntOrNull(int tag)
        {
            var e = Get(tag);
            return e == null || e.IsNull ? null : e.AsUInt();
        }

        public long GetInt(int tag) => Require(tag).AsInt();
        public bool GetBool(int tag, bool defaultValue = false) => Get(tag)?.AsBool() ?? defaultValue;
        public byte[] GetBytes(int tag) => Require(tag).AsBytes();
        public byte[] GetBytesOrNull(int tag)
        {
            var e = Get(tag);
            return e == null || e.IsNull ? null : e.AsBytes();
        }

        public string GetString(int tag) => Require(tag).AsString();
        public string GetStringOrNull(int tag)
        {
            var e = Get(tag);
            return e == null || e.IsNull ? null : e.AsString();
        }

        public TlvElement Require(int tag)
        {
            return Get(tag) ?? throw new MatterProtocolException($"Missing TLV field with context tag {tag}");
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            Dump(sb, 0);
            return sb.ToString();
        }

        private void Dump(StringBuilder sb, int indent)
        {
            sb.Append(' ', indent * 2);
            if (Tag != AnonymousTag)
            {
                sb.Append(IsProfileTag ? "P" : "").Append(Tag).Append(" = ");
            }

            switch (Kind)
            {
                case TlvKind.Structure:
                case TlvKind.Array:
                case TlvKind.List:
                    sb.Append(Kind == TlvKind.Structure ? "{" : Kind == TlvKind.Array ? "[" : "[[").AppendLine();
                    foreach (var c in Children)
                    {
                        c.Dump(sb, indent + 1);
                    }

                    sb.Append(' ', indent * 2).Append(Kind == TlvKind.Structure ? "}" : Kind == TlvKind.Array ? "]" : "]]").AppendLine();
                    break;
                case TlvKind.ByteString:
                    sb.Append("0x").Append(Convert.ToHexString((byte[])Value)).AppendLine();
                    break;
                case TlvKind.Utf8String:
                    sb.Append('"').Append(Value).Append('"').AppendLine();
                    break;
                case TlvKind.Null:
                    sb.AppendLine("null");
                    break;
                default:
                    sb.Append(Value).AppendLine();
                    break;
            }
        }
    }
}
