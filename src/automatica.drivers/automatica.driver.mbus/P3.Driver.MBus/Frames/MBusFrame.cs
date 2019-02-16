using System;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace P3.Driver.MBus.Frames
{
    public class MBusFrame
    {
        protected MBusFrameType FrameType;

        public byte ControlField { get; protected set; }
        public byte AddressField { get; protected set; }
        public byte CiField { get; protected set; }
        public ReadOnlyMemory<byte> UserData { get; protected set; }
        protected byte Checksum;
        protected byte CalculatedChecksum;

        public ReadOnlyMemory<byte> RawData { get; protected set; }

        internal MBusFrame()
        {


        }

        public static MBusFrame CreateShortFrame(byte controlField, byte addressField)
        {
            MBusFrame mbusFrame = new MBusFrame
            {
                FrameType = MBusFrameType.ShortFrame,
                AddressField = addressField,
                ControlField = controlField
            };

            return mbusFrame;
        }


        public static MBusFrame CreateChangePrimaryAddressFrame(byte from, byte to)
        {
            MBusFrame mbusFrame = new MBusFrame
            {
                FrameType = MBusFrameType.LongFrame,
                AddressField = from,
                ControlField = 0x53,
                CiField = 0x51
            };

            mbusFrame.UserData = new byte[] { 0x01, 0x7A, to };
            return mbusFrame;
        }

        public static MBusFrame FromSpan(MBusFrameType frameType, ILogger logger, Span<byte> header, Span<byte> data)
        {
            var headerByte = header.ToArray();
            var dataArray = data.ToArray();
            
            var frame = headerByte.Concat(dataArray).ToArray(); //it sucks that i need to copy the data here to get a new span - but it still works
            return FromByteArray(frameType,logger, in frame);
        }

        public static MBusFrame FromByteArray(MBusFrameType frameType, ILogger logger, in byte[] frame)
        {
            var ci = frameType == MBusFrameType.LongFrame ? frame[6] : 0;
            MBusFrame mbusFrame = FrameFactory.CreateMBusFrame(in frameType, in ci);
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

        protected virtual void FromByteArray(Span<byte> frame, ILogger logger)
        {
            Span<byte> spanFrame = frame;
            if (FrameType == MBusFrameType.SingleChar)
            {
                return;
            }
            Checksum = frame[frame.Length - 2];

            if (FrameType == MBusFrameType.ShortFrame)
            {
                ControlField = frame[1];
                AddressField = frame[2];
            }
            else if (FrameType == MBusFrameType.ControlFrame || FrameType == MBusFrameType.LongFrame)
            {
                ControlField = frame[4];
                AddressField = frame[5];
                CiField = frame[6];

                if (FrameType == MBusFrameType.LongFrame)
                {
                    int userDataLength = frame.Length - 9;
                    UserData = spanFrame.Slice(7, userDataLength).ToArray();
                }
            }
        }

        public virtual ReadOnlyMemory<byte> ToByteFrame()
        {
            if (FrameType == MBusFrameType.ShortFrame)
            {
                byte[] data = new byte[5];
                data[0] = MBus.ShortFrameStart;
                data[1] = ControlField;
                data[2] = AddressField;
                data[3] = CalculateChecksum();
                data[4] = MBus.FrameEndByte;

                return data;
            }
            else if (FrameType == MBusFrameType.LongFrame)
            {
                byte[] data = new byte[9 + UserData.Length];

                data[0] = MBus.ControlFrameLongFrameStart;
                data[1] = Convert.ToByte(3 + UserData.Length);
                data[2] = Convert.ToByte(3 + UserData.Length);
                data[3] = MBus.ControlFrameLongFrameStart;
                data[4] = ControlField;
                data[5] = AddressField;
                data[6] = CiField;
                Array.Copy(UserData.Span.ToArray(), 0, data, 7, UserData.Length);

                data[7 + UserData.Length] = CalculateChecksum();
                data[8 + UserData.Length] = MBus.FrameEndByte;
                return data;

            }

            return RawData;
        }

        private byte CalculateChecksum()
        {
            byte chkSum = 0;

            switch (FrameType)
            {
                case MBusFrameType.ShortFrame:
                    chkSum = ControlField;
                    chkSum += AddressField;
                    break;
                case MBusFrameType.ControlFrame:
                    chkSum = ControlField;
                    chkSum += AddressField;
                    chkSum += CiField;
                    break;
                case MBusFrameType.LongFrame:
                    chkSum = ControlField;
                    chkSum += AddressField;
                    chkSum += CiField;

                    foreach (ref readonly byte b in UserData.Span)
                    {
                        chkSum += b;
                    }

                    break;
                case MBusFrameType.SingleChar:
                    return 0;
            }

            return chkSum;
        }
        public bool ChecksumCheck()
        {
            byte chkSum = CalculateChecksum();
            return chkSum == Checksum;
        }

        public override string ToString()
        {
            return Automatica.Core.Driver.Utility.Utils.ByteArrayToString(ToByteFrame());
        }
    }
}
