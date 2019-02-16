namespace P3.Driver.MBus.Frames
{
    internal static class FrameFactory
    {
        internal static MBusFrame CreateMBusFrame(in MBusFrameType type, in int ciField)
        {
            if (type == MBusFrameType.LongFrame && (ciField == 0x72 || ciField == 0x76))
                return new VariableDataFrame();
            if (type == MBusFrameType.LongFrame && (ciField == 0x73 || ciField == 0x77))
                return new FixedDataFrame();
            if (type == MBusFrameType.SingleChar)
                return new AckFrame();
            if (type == MBusFrameType.ShortFrame)
            {
                return new ShortFrame();
            }

            return new MBusFrame(); 
        }
    }
}
