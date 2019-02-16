
using System;

namespace P3.Driver.MBus.Frames
{
    public class AckFrame : MBusFrame
    {
        public override ReadOnlyMemory<byte> ToByteFrame()
        {
            return new[] {MBus.SingleCharFrame};
        }
    }
}
