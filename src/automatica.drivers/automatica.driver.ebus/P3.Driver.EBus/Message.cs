using System;
using Microsoft.CodeAnalysis.CodeRefactorings;

namespace P3.Driver.EBus
{
    internal class Message
    {

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

            msg.Data = new Memory<byte>(buffer.Slice(5, dataLen).ToArray());

            msg.Crc16 = buffer[5 + dataLen];

            if (CalculateChecksum(buffer.Slice(0, dataLen+5)) != msg.Crc16)
            {
                throw new ArgumentException("Invalid CRC");
            }


            return msg;
        }

        public static byte CalculateChecksum(ReadOnlySpan<byte> data)
        {
            return Crc.Crc8(data);
        }
    }
}
