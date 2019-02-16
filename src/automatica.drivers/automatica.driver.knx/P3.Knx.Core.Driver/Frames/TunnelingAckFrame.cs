namespace P3.Knx.Core.Driver.Frames
{
    internal class TunnelingAckFrame : KnxFrame
    {
        private readonly byte mSequenceNumber__;
        internal TunnelingAckFrame(KnxConnection knx, byte sequenceNumber)
            : base(knx, KnxHelper.ServiceType.TunnellingAck)
        {
            mSequenceNumber__ = sequenceNumber;
        }

        public static TunnelingAckFrame Parse(KnxConnection knx, byte[] data)
        {
            TunnelingAckFrame frame = new TunnelingAckFrame(knx, data[8]);
            return frame;
        }

        internal override byte[] ToFrame()
        {
            var datagram = new byte[10];
            datagram[00] = 0x06;
            datagram[01] = 0x10;
            datagram[02] = 0x04;
            datagram[03] = 0x21;
            datagram[04] = 0x00;
            datagram[05] = 0x0A;

            datagram[06] = 0x04;
            datagram[07] = KnxConnection.ChannelId;
            datagram[08] = mSequenceNumber__;
            datagram[09] = 0x00;

            return datagram;
        }
    }
}
