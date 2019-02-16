namespace P3.Knx.Core.Driver.Frames
{
    internal class RawDataFrame : KnxFrame
    {
        private readonly byte[] mData__;
        private RawDataFrame(KnxConnection knx, byte[] data)
            : base(knx, KnxHelper.ServiceType.Unknown)
        {
            mData__ = data;
        }

        public static RawDataFrame CreateFrame(KnxConnection knx, byte[] frame)
        {
            return new RawDataFrame(knx, frame);
        }

        internal override byte[] ToFrame()
        {
            return mData__;
        }
    }
}
