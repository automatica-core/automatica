using System;
using Microsoft.Extensions.Logging;
using P3.Driver.MBus.Frames.VariableData;

namespace P3.Driver.MBus.Frames
{
    public class FixedDataFrame : MBusFrame
    {
        public int Identification { get; set; }
        public byte AccessNumber { get; set; }
        public MBusStatus Status { get; set; }
        public Medium Medium { get; set; }
        public int Counter1 { get; set; }
        public int Counter2 { get; set; }

        protected override void FromByteArray(Span<byte> frame, ILogger logger)
        {
            base.FromByteArray(frame, logger);

            // EN 13757-3 fixed data structure (CI 0x73/0x77), 16 bytes:
            // ident (4, BCD) | access no (1) | status (1) | medium/unit (2) | counter 1 (4) | counter 2 (4)
            var ud = UserData.Span;
            if (ud.Length < 16)
            {
                logger.LogError($"Fixed data frame too short ({ud.Length}/16 bytes)");
                return;
            }

            var ident = VariableDataBlock.DecodeBcd(ud.Slice(0, 4), false);
            Identification = ident.HasValue ? (int)ident.Value : -1;
            AccessNumber = ud[4];
            Status = (MBusStatus)ud[5];
            // medium: 2 MSB of the first unit byte = medium bits 0-1, 2 MSB of the second unit byte = medium bits 2-3
            Medium = (Medium)(((ud[6] & 0xC0) >> 6) | ((ud[7] & 0xC0) >> 4));

            var binary = (ud[5] & (byte)MBusStatus.Counter1And2CodedSignedBinary) != 0;
            Counter1 = DecodeCounter(ud.Slice(8, 4), binary);
            Counter2 = DecodeCounter(ud.Slice(12, 4), binary);
        }

        private static int DecodeCounter(ReadOnlySpan<byte> data, bool binary)
        {
            if (binary)
            {
                return BitConverter.ToInt32(data);
            }

            var bcd = VariableDataBlock.DecodeBcd(data, true);
            return bcd.HasValue ? (int)bcd.Value : -1;
        }
    }
}
