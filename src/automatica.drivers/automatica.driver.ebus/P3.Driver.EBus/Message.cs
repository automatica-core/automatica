using System;

namespace P3.Driver.EBus
{
    internal class Message
    {

        public const byte EscapeByte = 0xA9;
        public const byte SyncByte = 0xAA;
        public const byte AckByte = 0x00;
        public const byte NackByte = 0xFF;
        public const byte BrodcastByte = 0xFE;

        public byte Source { get; private set; }
        public byte Target { get; private set; }
        public byte PrimaryCommand { get; private set; }
        public byte SecondaryCommand { get; private set; }

        public Memory<byte> Data { get; private set; }

        public byte Crc16 { get; private set; }

        private Message()
        {
            
        }

        internal static Message Parse(ReadOnlySpan<byte> buffer)
        {
            if (buffer.Length <= 5)
            {
                throw new ArgumentException($"{nameof(buffer)} must be at least bigger than 5");
            }

            var msg = new Message
            {
                Source = buffer[0], Target = buffer[1], PrimaryCommand = buffer[2], SecondaryCommand = buffer[3]
            };

            var dataLen = buffer[4];

            if (dataLen + 4 > buffer.Length)
            {
                throw new ArgumentException("Package is invalid");
            }

            msg.Data = new Memory<byte>(buffer.Slice(5, dataLen-1).ToArray());

            msg.Crc16 = buffer[4 + dataLen];

            var cc = CalculateChecksum(buffer);
            if (CalculateChecksum(buffer.Slice(0, dataLen+4)) != msg.Crc16)
            {
                throw new ArgumentException("Invalid CRC");
            }


            return msg;
        }

        public static byte CalculateChecksum(ReadOnlySpan<byte> data)
        {
            byte crc = 0;
            foreach(var value in data)
            {
                switch (value)
                {
                    case EscapeByte:
                        crc = Crc.Crc8(crc, EscapeByte);
                        crc = Crc.Crc8(crc, 0x00);
                        break;
                    case SyncByte:
                        crc = Crc.Crc8(crc, EscapeByte);
                        crc = Crc.Crc8(crc, 0x01);
                        break;
                    default:
                        crc = Crc.Crc8(crc, value);
                        break;
                }
            }

            return crc;
        }
    }
}
