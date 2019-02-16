using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using P3.Driver.MBus.Frames.VariableData;

namespace P3.Driver.MBus.Frames
{
    public class VariableDataFrame : MBusFrame
    {
        public const int UserDataOffset = 12;
        public int Identifier { get; private set; }
        public short ManufacturerId { get; private set; }
        public string Manufacturer { get; private set; }
        public byte Version { get; private set; }
        public Medium Medium { get; private set; }
        public byte AccessNumber { get; private set; }
        public byte Status { get; private set; }
        public short Signature { get; private set; }

        private readonly List<VariableDataBlock> _vdbs = new List<VariableDataBlock>();
        public List<VariableDataBlock> DataBlocks => _vdbs;


        public VariableDataFrame()
        {
           
        }

        public void ParseVariableDataFrame(ReadOnlySpan<byte> frame, int offset, ILogger logger)
        {
            var usedStorageMap = new Dictionary<Unit, int>();
            int dataLength = frame.Length - offset;
            int internalOffset = offset;
            do
            {
                int usedLength;
                var vdb = VariableDataBlock.Parse(frame,  internalOffset, usedStorageMap, out usedLength, logger);

                _vdbs.Add(vdb);

                internalOffset += usedLength;
                dataLength -= usedLength;
            } while (dataLength > 0);
        }

        protected override void FromByteArray(Span<byte> frame, ILogger logger)
        {
            base.FromByteArray(frame, logger);
            try
            {
                Identifier = BitConverter.ToInt32(UserData.Span);
                ManufacturerId = BitConverter.ToInt16(UserData.Span.Slice(4, 2));
                Manufacturer = DecodeManufacturer(UserData.Span[4], UserData.Span[5], ManufacturerId);
                Version = UserData.Span[6];
                Medium = (Medium)UserData.Span[7];
                AccessNumber = UserData.Span[8];
                Status = UserData.Span[9];
                Signature = BitConverter.ToInt16(UserData.Span.Slice(10, 2));

                int offset = UserDataOffset;
                ParseVariableDataFrame(UserData.Span, offset, logger);

                logger.LogDebug("finished parsing variable data frame");
            }
            catch (Exception e)
            {
                logger.LogError("Error parsing variable data frame {0}", e);
            }
        }

        private string DecodeManufacturer(byte byte1, byte byte2, short value)
        {
            char[] str = new char[4];
            
            str[0] = (char)(((value >> 10) & 0x001F) + 64);
            str[1] = (char)(((value >> 5) & 0x001F) + 64);
            str[2] = (char)(((value) & 0x001F) + 64);
            str[3] = (char)0;

            return new string(str);
        }
    }
}
