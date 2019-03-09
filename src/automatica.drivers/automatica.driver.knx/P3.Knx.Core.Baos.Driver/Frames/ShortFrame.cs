namespace P3.Knx.Core.Baos.Driver.Frames
{
    public class ShortFrame : BaosFrame
    {
        public ShortFrame()
        {
            FrameType = BaosFrameType.ShortFrame;
        }

        internal static ShortFrame CreateResetFrame()
        {
            var frame = new ShortFrame();
            frame.ControlField = 0x40;
            return frame;
        }
    }
}
