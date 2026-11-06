using System.Text;

namespace P3.Driver.Matter.Certificates
{
    /// <summary>Minimal DER writer for the X.509 structures Matter certificates map to.</summary>
    internal static class Der
    {
        public static byte[] Tlv(byte tag, params byte[][] content)
        {
            var len = content.Sum(c => c.Length);
            using var ms = new MemoryStream();
            ms.WriteByte(tag);
            WriteLength(ms, len);
            foreach (var c in content)
            {
                ms.Write(c);
            }

            return ms.ToArray();
        }

        private static void WriteLength(Stream s, int len)
        {
            if (len < 0x80)
            {
                s.WriteByte((byte)len);
            }
            else if (len <= 0xFF)
            {
                s.WriteByte(0x81);
                s.WriteByte((byte)len);
            }
            else if (len <= 0xFFFF)
            {
                s.WriteByte(0x82);
                s.WriteByte((byte)(len >> 8));
                s.WriteByte((byte)len);
            }
            else
            {
                s.WriteByte(0x83);
                s.WriteByte((byte)(len >> 16));
                s.WriteByte((byte)(len >> 8));
                s.WriteByte((byte)len);
            }
        }

        public static byte[] Sequence(params byte[][] content) => Tlv(0x30, content);
        public static byte[] Set(params byte[][] content) => Tlv(0x31, content);
        public static byte[] OctetString(byte[] value) => Tlv(0x04, value);
        public static byte[] Utf8String(string value) => Tlv(0x0C, Encoding.UTF8.GetBytes(value));
        public static byte[] PrintableString(string value) => Tlv(0x13, Encoding.ASCII.GetBytes(value));
        public static byte[] Ia5String(string value) => Tlv(0x16, Encoding.ASCII.GetBytes(value));
        public static byte[] Boolean(bool value) => Tlv(0x01, new[] { value ? (byte)0xFF : (byte)0x00 });
        public static byte[] Null() => new byte[] { 0x05, 0x00 };
        public static byte[] ContextExplicit(int n, params byte[][] content) => Tlv((byte)(0xA0 | n), content);
        public static byte[] ContextImplicitPrimitive(int n, byte[] value) => Tlv((byte)(0x80 | n), value);

        public static byte[] BitString(byte unusedBits, byte[] value)
        {
            var content = new byte[value.Length + 1];
            content[0] = unusedBits;
            value.CopyTo(content, 1);
            return Tlv(0x03, content);
        }

        /// <summary>An INTEGER from big-endian content bytes, normalised to a minimal non-negative encoding.</summary>
        public static byte[] Integer(byte[] bigEndian)
        {
            var start = 0;
            while (start < bigEndian.Length - 1 && bigEndian[start] == 0 && (bigEndian[start + 1] & 0x80) == 0)
            {
                start++;
            }

            var trimmed = bigEndian[start..];
            if (trimmed.Length == 0)
            {
                trimmed = new byte[] { 0 };
            }

            if ((trimmed[0] & 0x80) != 0)
            {
                trimmed = new byte[] { 0 }.Concat(trimmed).ToArray();
            }

            return Tlv(0x02, trimmed);
        }

        public static byte[] Integer(long value)
        {
            var bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }

            return Integer(bytes);
        }

        public static byte[] Oid(string dotted)
        {
            var parts = dotted.Split('.').Select(ulong.Parse).ToArray();
            using var ms = new MemoryStream();
            ms.WriteByte((byte)(parts[0] * 40 + parts[1]));
            for (var i = 2; i < parts.Length; i++)
            {
                var v = parts[i];
                var stack = new Stack<byte>();
                stack.Push((byte)(v & 0x7F));
                v >>= 7;
                while (v > 0)
                {
                    stack.Push((byte)(0x80 | (v & 0x7F)));
                    v >>= 7;
                }

                while (stack.Count > 0)
                {
                    ms.WriteByte(stack.Pop());
                }
            }

            return Tlv(0x06, ms.ToArray());
        }
    }
}
