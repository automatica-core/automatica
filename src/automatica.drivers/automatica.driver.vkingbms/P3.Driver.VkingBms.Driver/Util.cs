using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P3.Driver.VkingBms.Driver
{
    internal class Util
    {
        public static byte GetByte(byte[] message, int index)
        {
            if (message.Length < index + 2)
                return 0;
            string upper = Encoding.ASCII.GetString(new byte[2]
            {
                message[index],
                message[index + 1]
            }).ToUpper();
            try
            {
                return Convert.ToByte(upper, 16);
            }
            catch
            {
                return 0;
            }
        }
        public static char GetChar(byte[] message, int index)
        {
            if (message.Length < index + 2)
                return ' ';
            string upper = Encoding.ASCII.GetString(new byte[2]
            {
                message[index],
                message[index + 1]
            }).ToUpper();
            try
            {
                return Convert.ToChar(Convert.ToByte(upper, 16));
            }
            catch
            {
                return ' ';
            }
        }

        public static sbyte GetsByte(byte[] message, int index)
        {
            if (message.Length < index + 2)
                return 0;
            string upper = Encoding.ASCII.GetString(new byte[2]
            {
                message[index],
                message[index + 1]
            }).ToUpper();
            try
            {
                return Convert.ToSByte(upper, 16);
            }
            catch
            {
                return 0;
            }
        }

        public static short GetShort(byte[] message, int index)
        {
            if (message.Length < index + 4)
                return 0;
            string upper = Encoding.ASCII.GetString(new byte[4]
            {
                message[index],
                message[index + 1],
                message[index + 2],
                message[index + 3]
            }).ToUpper();
            try
            {
                return Convert.ToInt16(upper, 16);
            }
            catch
            {
                return 0;
            }
        }

        public static ushort GetUShort(byte[] message, int index)
        {
            if (message.Length < index + 4)
                return 0;
            string upper = Encoding.ASCII.GetString(new byte[4]
            {
                message[index],
                message[index + 1],
                message[index + 2],
                message[index + 3]
            }).ToUpper();
            try
            {
                return Convert.ToUInt16(upper, 16);
            }
            catch
            {
                return 0;
            }
        }

        public static uint GetUint(byte[] message, int index)
        {
            if (message.Length < index + 8)
                return 0;
            string upper = Encoding.ASCII.GetString(new byte[8]
            {
                message[index],
                message[index + 1],
                message[index + 2],
                message[index + 3],
                message[index + 4],
                message[index + 5],
                message[index + 6],
                message[index + 7]
            }).ToUpper();
            try
            {
                return Convert.ToUInt32(upper, 16);
            }
            catch
            {
                return 0;
            }
        }

        public static int GetInt(byte[] message, int index)
        {
            if (message.Length < index + 8)
                return 0;
            string upper = Encoding.ASCII.GetString(new byte[8]
            {
                message[index],
                message[index + 1],
                message[index + 2],
                message[index + 3],
                message[index + 4],
                message[index + 5],
                message[index + 6],
                message[index + 7]
            }).ToUpper();
            try
            {
                return Convert.ToInt32(upper, 16);
            }
            catch
            {
                return 0;
            }
        }

        public static byte GetCmdType(byte[] message) => Util.GetByte(message, 7);

        public static bool CheckResponseSuccess(byte[] message) => Util.GetByte(message, 7) == (byte)0;

        public static ushort CalLCheckSum(ushort len) =>
            (ushort)((ushort)((uint)(byte)((byte)((uint)~(byte)(((len & 3840) >> 8) +
                                                                ((len & 240) >> 4) + (len & 15)) +
                                                  1U) & 15U) << 12) + (uint)len);

        public static ushort CalFrameCheckSum(byte[] datain)
        {
            ushort num = 0;
            for (int index = 0; index < datain.Length; ++index)
                num += (ushort)datain[index];
            return (ushort)((uint)~num + 1U);
        }

        public static ushort CalFrameCheckSum(byte[] datain, int index, int length)
        {
            ushort num = 0;
            if (datain == null || datain.Length < index + length)
                return 0;
            for (int index1 = index; index1 < length + index; ++index1)
                num += (ushort)datain[index1];
            return (ushort)((uint)~num + 1U);
        }
    }
}
