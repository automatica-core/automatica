using System;

namespace P3.Knx.Core.Baos.Driver.Frames
{
    internal static class FrameFactory
    {
        internal static BaosFrame CreateFrame(in BaosFrameType frameType)
        {
            switch (frameType)
            {
                case BaosFrameType.SingleChar:
                    return new AckFrame();
                case BaosFrameType.ShortFrame:
                    return new ShortFrame(); 
                case BaosFrameType.LongFrame:
                    return new LongFrame();
            }

            throw new InvalidOperationException();
        }
    }
}
