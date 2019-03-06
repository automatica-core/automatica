using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using P3.Knx.Core.Baos.Driver.Frames;

namespace P3.Knx.Core.Baos.Driver
{
    public enum BaosFrameType
    {
        SingleChar,
        ShortFrame,
        LongFrame
    }

    public abstract class BaosFrame
    {
        public const byte SingleCharFrame = 0xE5;
        public const byte ShortFrameStart = 0x10;
        public const byte ControlFrameLongFrameStart = 0x68;

        public const byte FrameEndByte = 0x16;

        public byte ControlField { get; protected set; }

        public BaosFrameType FrameType { get; protected set; }

        public ReadOnlyMemory<byte> UserData { get; protected set; }
        protected byte Checksum;
        protected byte CalculatedChecksum;

        public ReadOnlyMemory<byte> RawData { get; protected set; }

        protected virtual void FromByteArray(in byte[] frame, ILogger logger)
        {
            Span<byte> spanFrame = frame;
            if (FrameType == BaosFrameType.SingleChar)
            {
                return;
            }
            Checksum = frame[frame.Length - 2];

            if (FrameType == BaosFrameType.ShortFrame)
            {
                ControlField = frame[1];
            }
            else if (FrameType == BaosFrameType.LongFrame)
            {
                ControlField = frame[4];

                int userDataLength = frame.Length - 7;
                UserData = spanFrame.Slice(5, userDataLength).ToArray();
            }
        }

        public virtual ReadOnlyMemory<byte> ToByteFrame()
        {
            if (FrameType == BaosFrameType.ShortFrame)
            {
                byte[] data = new byte[4];
                data[0] = ShortFrameStart;
                data[1] = ControlField;
                data[2] = CalculateChecksum();
                data[3] = FrameEndByte;

                return data;
            }
            else if (FrameType == BaosFrameType.LongFrame)
            {
                byte[] data = new byte[7 + UserData.Length];

                data[0] = ControlFrameLongFrameStart;
                data[1] = Convert.ToByte(3 + UserData.Length);
                data[2] = Convert.ToByte(3 + UserData.Length);
                data[3] = ControlFrameLongFrameStart;
                data[4] = ControlField;
                Array.Copy(UserData.Span.ToArray(), 0, data, 5, UserData.Length);

                data[7 + UserData.Length] = CalculateChecksum();
                data[8 + UserData.Length] = FrameEndByte;
                return data;

            }

            return RawData;
        }

        internal static BaosFrame FromByteArray(BaosFrameType frameType, ILogger logger, in byte[] frame)
        {
            BaosFrame mbusFrame = FrameFactory.CreateFrame(in frameType);
            
            mbusFrame.FrameType = frameType;
            mbusFrame.FromByteArray(frame, logger);
            mbusFrame.RawData = frame;

            if (mbusFrame.ChecksumCheck())
            {
                return mbusFrame;
            }

            logger.LogError("Invalid frame recevied checksum is invalid");
            return null;
        }

        internal static BaosFrame FromSpan(BaosFrameType frameType, ILogger logger, Span<byte> header, Span<byte> data)
        {
            var headerByte = header.ToArray();
            var dataArray = data.ToArray();

            var frame = headerByte.Concat(dataArray).ToArray(); //it sucks that i need to copy the data here to get a new span - but it still works
            return FromByteArray(frameType, logger, in frame);
        }

        public bool ChecksumCheck()
        {
            byte chkSum = CalculateChecksum();
            return chkSum == Checksum;
        }

        private byte CalculateChecksum()
        {
            byte chkSum = 0;

            switch (FrameType)
            {
                case BaosFrameType.ShortFrame:
                    chkSum = ControlField;
                    break;
                case BaosFrameType.LongFrame:
                    chkSum = ControlField;

                    foreach (ref readonly byte b in UserData.Span)
                    {
                        chkSum += b;
                    }

                    break;
                case BaosFrameType.SingleChar:
                    return 0;
            }

            return chkSum;
        }
    }
}
