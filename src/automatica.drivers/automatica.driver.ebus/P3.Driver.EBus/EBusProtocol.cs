using System;
using System.Collections.Generic;

namespace P3.Driver.EBus
{
    public static class EBusProtocol
    {
        public const byte Syn = 0xAA;
        public const byte EscapeByte = 0xA9;
        public const byte Ack = 0x00;
        public const byte Nack = 0xFF;
        public const byte Broadcast = 0xFE;

        private static bool IsMasterNibble(int nibble) => nibble is 0x0 or 0x1 or 0x3 or 0x7 or 0xF;

        /// <summary>
        /// The 25 master addresses: both nibbles are 0, 1, 3, 7 or F
        /// </summary>
        public static bool IsMaster(byte address) => IsMasterNibble(address >> 4) && IsMasterNibble(address & 0x0F);

        public static bool IsSlave(byte address) => !IsMaster(address) && address != Syn && address != EscapeByte && address != Broadcast;

        /// <summary>
        /// Every master also answers as slave on its address + 5
        /// </summary>
        public static byte SlaveOf(byte master) => (byte)(master + 5);

        public static byte MasterOf(byte slave) => (byte)(slave - 5);

        /// <summary>
        /// A9 → A9 00 and AA → A9 01, so that AA only appears as SYN on the bus
        /// </summary>
        public static byte[] Escape(ReadOnlySpan<byte> data)
        {
            var result = new List<byte>(data.Length + 2);
            foreach (var b in data)
            {
                if (b == EscapeByte)
                {
                    result.Add(EscapeByte);
                    result.Add(0x00);
                }
                else if (b == Syn)
                {
                    result.Add(EscapeByte);
                    result.Add(0x01);
                }
                else
                {
                    result.Add(b);
                }
            }

            return result.ToArray();
        }

        public static byte[] Unescape(ReadOnlySpan<byte> data)
        {
            var result = new List<byte>(data.Length);
            for (var i = 0; i < data.Length; i++)
            {
                if (data[i] == EscapeByte && i + 1 < data.Length)
                {
                    result.Add(data[i + 1] == 0x01 ? Syn : EscapeByte);
                    i++;
                }
                else
                {
                    result.Add(data[i]);
                }
            }

            return result.ToArray();
        }

        /// <summary>
        /// The CRC is calculated over the escaped bytes
        /// </summary>
        public static byte Checksum(ReadOnlySpan<byte> unescaped) => Crc.Crc8(Escape(unescaped));

        /// <summary>
        /// QQ ZZ PB SB NN data CRC, escaped
        /// </summary>
        public static byte[] BuildMasterPart(byte source, byte target, byte primary, byte secondary, ReadOnlySpan<byte> data)
        {
            if (data.Length > 16)
            {
                throw new ArgumentException("eBUS telegrams carry at most 16 data bytes", nameof(data));
            }

            var telegram = new byte[5 + data.Length + 1];
            telegram[0] = source;
            telegram[1] = target;
            telegram[2] = primary;
            telegram[3] = secondary;
            telegram[4] = (byte)data.Length;
            data.CopyTo(telegram.AsSpan(5));
            telegram[^1] = Checksum(telegram.AsSpan(0, telegram.Length - 1));
            return Escape(telegram);
        }
    }
}
