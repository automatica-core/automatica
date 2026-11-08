using System;

namespace P3.Driver.EBus.Wolf
{
    /// <summary>
    /// Wolf's proprietary register access (seen on Wolf heat pumps, BM-2 and the CHA/FHA families).
    /// Read: PB SB = 50 22, data = CRC8(poly 0x5C over the register LE16) + register LE16, the answer is the value LE16 (some devices put 0x02 in front).
    /// 0x8000 means "not available". Writes (50 23) go to the EEPROM of the device and are deliberately not supported.
    /// </summary>
    public static class WolfRegister
    {
        public const byte Primary = 0x50;
        public const byte ReadSecondary = 0x22;
        public const ushort NotAvailable = 0x8000;

        public static byte[] ReadRequest(ushort register)
        {
            var lo = (byte)(register & 0xFF);
            var hi = (byte)(register >> 8);
            return new[] { Crc.Crc8(new[] { lo, hi }, Crc.WolfPolynomial), lo, hi };
        }

        /// <returns>false if the answer can not be read, true with null if the device does not have the register</returns>
        public static bool TryParseReadResponse(ReadOnlySpan<byte> response, out ushort? value)
        {
            value = null;
            if (response.Length == 3 && response[0] == 0x02)
            {
                response = response[1..];
            }

            if (response.Length != 2)
            {
                return false;
            }

            var raw = (ushort)(response[0] | response[1] << 8);
            value = raw == NotAvailable ? null : raw;
            return true;
        }
    }
}
