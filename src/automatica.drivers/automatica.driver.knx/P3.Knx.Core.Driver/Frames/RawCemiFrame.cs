using System;

namespace P3.Knx.Core.Driver.Frames
{
    internal class RawCemiFrame : KnxFrame
    {
        private readonly byte[] mCemiBytes__;
        private RawCemiFrame(KnxConnection knx, byte[] cemi)
            : base(knx, KnxHelper.ServiceType.TunnellingRequest)
        {
            mCemiBytes__ = cemi;
        }

        internal static RawCemiFrame CreateFrame(KnxConnection knx, byte[] cemi)
        {
            return new RawCemiFrame(knx, cemi);
        }

        internal override byte[] ToFrame()
        {
            var datagram = new byte[10 + mCemiBytes__.Length];
            datagram[00] = 0x06;
            datagram[01] = 0x10;
            datagram[02] = 0x04;
            datagram[03] = 0x20;

            var totalLength = BitConverter.GetBytes(mCemiBytes__.Length + 10);
            datagram[04] = totalLength[1];
            datagram[05] = totalLength[0];

            datagram[06] = 0x04;
            datagram[07] = KnxConnection.ChannelId;
            datagram[08] = KnxConnection.GenerateSequenceNumber();
            datagram[09] = 0x00;

            Array.Copy(mCemiBytes__, 0, datagram, 10, mCemiBytes__.Length);

            return datagram;
        }
    }
}
