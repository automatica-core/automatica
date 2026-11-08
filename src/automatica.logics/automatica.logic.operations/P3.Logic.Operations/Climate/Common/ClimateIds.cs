using System;
using System.Security.Cryptography;
using System.Text;

namespace P3.Logic.Operations.Climate.Common
{
    /// <summary>
    /// Stable ids for the interfaces of the climate blocks: a name based UUID (RFC 4122 version 5) of the logic guid and the interface key.
    /// The ids are stored in the database - never rename a key or change a logic guid once it has shipped.
    /// </summary>
    public static class ClimateIds
    {
        public static Guid Of(Guid logic, string key)
        {
            var ns = logic.ToByteArray();
            SwapByteOrder(ns);

            var name = Encoding.UTF8.GetBytes(key);
            var data = new byte[ns.Length + name.Length];
            Buffer.BlockCopy(ns, 0, data, 0, ns.Length);
            Buffer.BlockCopy(name, 0, data, ns.Length, name.Length);

            var hash = SHA1.HashData(data);
            var bytes = new byte[16];
            Array.Copy(hash, bytes, 16);
            bytes[6] = (byte)((bytes[6] & 0x0F) | 0x50);
            bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80);
            SwapByteOrder(bytes);
            return new Guid(bytes);
        }

        // Guid.ToByteArray is little endian for the first three groups, RFC 4122 wants network order
        private static void SwapByteOrder(byte[] guid)
        {
            (guid[0], guid[3]) = (guid[3], guid[0]);
            (guid[1], guid[2]) = (guid[2], guid[1]);
            (guid[4], guid[5]) = (guid[5], guid[4]);
            (guid[6], guid[7]) = (guid[7], guid[6]);
        }
    }
}
