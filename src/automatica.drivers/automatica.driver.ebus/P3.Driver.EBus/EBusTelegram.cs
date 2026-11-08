using System;
using System.Linq;

namespace P3.Driver.EBus
{
    /// <summary>
    /// A telegram seen on the bus. <see cref="Response"/> is the slave answer of a master-slave telegram, null for broadcasts,
    /// master-master telegrams and requests that were not answered.
    /// </summary>
    public sealed record EBusTelegram(byte Source, byte Target, byte Primary, byte Secondary, byte[] Data, byte[] Response)
    {
        public ushort Command => (ushort)(Primary << 8 | Secondary);

        public bool IsBroadcast => Target == EBusProtocol.Broadcast;

        /// <summary>
        /// Parses the bytes between two SYN (still escaped). Returns null for anything that is not a complete telegram with a valid master CRC.
        /// </summary>
        public static EBusTelegram TryParse(ReadOnlySpan<byte> escaped)
        {
            var b = EBusProtocol.Unescape(escaped);
            if (b.Length < 6)
            {
                return null;
            }

            var length = b[4];
            if (length > 16 || b.Length < 6 + length)
            {
                return null;
            }

            if (EBusProtocol.Checksum(b.AsSpan(0, 5 + length)) != b[5 + length])
            {
                return null;
            }

            var data = b.AsSpan(5, length).ToArray();
            byte[] response = null;
            var rest = b.AsSpan(6 + length);
            if (EBusProtocol.IsSlave(b[1]) && rest.Length >= 2 && rest[0] == EBusProtocol.Ack)
            {
                var responseLength = rest[1];
                if (responseLength <= 16 && rest.Length >= 3 + responseLength &&
                    EBusProtocol.Checksum(rest.Slice(1, 1 + responseLength)) == rest[2 + responseLength])
                {
                    response = rest.Slice(2, responseLength).ToArray();
                }
            }

            return new EBusTelegram(b[0], b[1], b[2], b[3], data, response);
        }

        public override string ToString()
        {
            var text = $"{Source:x2}->{Target:x2} {Command:x4} {string.Join(" ", Data.Select(a => a.ToString("x2")))}";
            return Response == null ? text : $"{text} | {string.Join(" ", Response.Select(a => a.ToString("x2")))}";
        }
    }
}
