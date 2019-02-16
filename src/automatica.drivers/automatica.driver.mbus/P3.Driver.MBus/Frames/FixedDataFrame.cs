using System;
using Microsoft.Extensions.Logging;

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

            Identification = BitConverter.ToInt32(UserData.Span);
            AccessNumber = UserData.Span[5];
            Status = (MBusStatus)UserData.Span[6];
            Medium = (Medium) BitConverter.ToInt16(UserData.Span.Slice(7, 2));
            Counter1 = BitConverter.ToInt32(UserData.Span.Slice(9, 4));
            Counter2 = BitConverter.ToInt32(UserData.Span.Slice(13, 4));

        }
    }
}
