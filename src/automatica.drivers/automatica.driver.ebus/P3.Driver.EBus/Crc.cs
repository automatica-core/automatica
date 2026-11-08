using System;

namespace P3.Driver.EBus
{
    /// <summary>
    /// The eBUS CRC-8: polynomial 0x9B, the data is shifted in at the low bit (not XORed into the register).
    /// Wolf uses the same algorithm with polynomial 0x5C for the register id of its read requests.
    /// </summary>
    public static class Crc
    {
        public const byte EBusPolynomial = 0x9B;
        public const byte WolfPolynomial = 0x5C;

        public static byte Crc8(ReadOnlySpan<byte> data, byte polynomial = EBusPolynomial)
        {
            byte crc = 0;
            foreach (var b in data)
            {
                crc = Crc8(crc, b, polynomial);
            }

            return crc;
        }

        public static byte Crc8(byte crc, byte data, byte polynomial = EBusPolynomial)
        {
            for (var i = 0; i < 8; i++)
            {
                crc = (crc & 0x80) != 0 ? (byte)((crc << 1) ^ polynomial) : (byte)(crc << 1);
                if ((data & 0x80) != 0)
                {
                    crc ^= 0x01;
                }
                data <<= 1;
            }

            return crc;
        }
    }
}
